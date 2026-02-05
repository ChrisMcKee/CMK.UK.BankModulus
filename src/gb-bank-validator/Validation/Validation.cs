using GbBankValidator.Data;
using GbBankValidator.Modulus;

namespace GbBankValidator.Validation;

/// <summary>Validation pipeline: normalize input, substitution, EISCD lookup, modulus checks, exceptions.</summary>
public static class BankValidation
{
    // Exception 2 & 9: alternative weights for the second check when a≠0 (Vocalink).
    // ReSharper disable once InconsistentNaming
    private static readonly short[] Ex2And9_ANotZero_GNotNine_Weights = { 0, 0, 1, 2, 5, 3, 6, 4, 8, 7, 10, 9, 3, 1 };
    // ReSharper disable once InconsistentNaming
    private static readonly short[] Ex2And9_ANotZero_GIsNine_Weights = { 0, 0, 0, 0, 0, 0, 0, 0, 8, 7, 10, 9, 3, 1 };

    /// <summary>Normalise sort code: strip '-' and ensure 6 digits. Returns null if invalid.</summary>
    public static byte[]? NormalizeSortCode(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        var digits = new List<byte>();
        foreach (var c in s)
        {
            if (c == '-') continue;
            if (digits.Count >= 6) return null;
            if (!char.IsDigit(c)) return null;
            digits.Add((byte)(c - '0'));
        }
        return digits.Count == 6 ? digits.ToArray() : null;
    }

    /// <summary>Normalise an account number: digits only, max 11. Returns null if invalid. Uses last 8 digits (left-padded if fewer).</summary>
    public static byte[]? NormalizeAccount(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length > 11) return null;
        if (s.Any(c => !char.IsDigit(c))) return null;
        var arr = new byte[8];
        if (s.Length >= 8)
        {
            var start = s.Length - 8;
            for (var i = 0; i < 8; i++)
                arr[i] = (byte)(s[start + i] - '0');
        }
        else
        {
            var pad = 8 - s.Length;
            for (var i = 0; i < s.Length; i++)
                arr[pad + i] = (byte)(s[i] - '0');
        }
        return arr;
    }

    /// <summary>Fourteen digits from sort code (6-digit) + account (8).</summary>
    private static byte[] Digits14FromU32(uint sortCode, byte[] account8)
    {
        var d = new byte[14];
        for (var i = 0; i < 6; i++)
        {
            d[i] = (byte)(sortCode / (uint)Math.Pow(10, 5 - i) % 10);
        }

        for (var i = 0; i < 8; i++)
        {
            d[6 + i] = account8[i];
        }
        return d;
    }

    /// <summary>Main validation: returns true if the account number is valid for the sort code.</summary>
    public static bool Validate(string sortCode, string accountNumber, Tables tables)
    {
        var sortCodeNorm = NormalizeSortCode(sortCode);
        if (sortCodeNorm == null) return false;

        var account8 = NormalizeAccount(accountNumber);
        if (account8 == null) return false;

        var sortCodeFromDigits = DataHelpers.SortCodeFromDigits(sortCodeNorm);
        // Apply substitution (Exception 5 and flowchart: if in table, use substitute for checks).
        var checkSortCode = tables.Substitution.Substitute(sortCodeFromDigits);

        // EISCD: find weight rows for (substituted) sort code. If none, "cannot be checked" -> valid.
        var rows = tables.RowsForSortCode(checkSortCode).ToList();
        if (rows.Count == 0) return true;

        // Build exception set  (indices 1..=14).
        var hasEx = new bool[15];
        foreach (var r in rows)
        {
            if (r.Exception is >= 1 and <= 14)
                hasEx[r.Exception.Value] = true;
        }

        var digits = Digits14FromU32(checkSortCode, account8);
        var (a, b, c, d, e, f, g, h) = (account8[0], account8[1], account8[2], account8[3], account8[4], account8[5], account8[6], account8[7]);

        // Exception 6: Foreign currency accounts
        if (hasEx[6])
        {
            if ((e, f, g, h) == (6, 5, 6, 7) || (e, f, g, h) == (6, 5, 6, 8)) return true;
            if (g == h) return true;
        }

        // Order of checks: typically first row is "first check", second row is "second check".
        // Exceptions 2, 5, 9, 10, 11, 12, 13, 14 may require two checks. Others may require one or two by row count.
        var firstRow = rows.Count > 0 ? rows[0] : null;
        var secondRow = rows.Count > 1 ? rows[1] : null;

        // Exception 8: use sort code 090126 for check
        var digitsEx8 = Digits14FromU32(90_126, account8);

        bool? firstPasses = firstRow != null
            ? RunOneCheck(firstRow.Exception == 8 ? digitsEx8 : digits, firstRow, account8)
            : null;
        bool? secondPasses = secondRow != null
            ? RunOneCheck(secondRow.Exception == 8 ? digitsEx8 : digits, secondRow, account8)
            : null;

        // Exception 3: if c is 6 or 9, the second check is ignored (only first counts).
        var ex3SkipSecond = hasEx[3] && (c == 6 || c == 9);

        // Exception 12/13: first check ex 12, second ex 13; either pass -> valid.
        var ex12_13 = hasEx[12] && hasEx[13];
        if (ex12_13)
        {
            if (firstPasses == true) return true;
            if (!ex3SkipSecond && secondPasses == true) return true;
            return false;
        }

        // Exception 2 & 9 (Lloyds Euro): two MOD11 rows; if the first passes, valid; else try the second (same sort code); else try the second with substituted sort code 309634.
        if (hasEx[2] || hasEx[9])
        {
            if (firstPasses == true) return true;
            if (secondPasses == true) return true;
            // When a≠0, second check uses alternative weights (Vocalink / RedBearSys).
            if (a != 0)
            {
                var altWeights = g == 9 ? Ex2And9_ANotZero_GIsNine_Weights : Ex2And9_ANotZero_GNotNine_Weights;
                var sum = ModulusChecks.WeightedSum(digits, altWeights, ModulusMethod.Mod11);
                var remainder = ((sum % 11) + 11) % 11;
                if (remainder == 0) return true;
            }
            // Second check with sort code 309634: use the second weight row only with substituted digits.
            const uint subSc = 309634;
            var subDigits = Digits14FromU32(subSc, account8);
            if (secondRow != null && RunOneCheck(subDigits, secondRow, account8))
                return true;

            return false;
        }

        // Exception 14
        if (hasEx[14])
        {
            if (firstPasses == true) return true;
            if (h != 0 && h != 1 && h != 9) return false;
            var modAccount = new byte[] { 0, a, b, c, d, e, f, g };
            var modDigits = Digits14FromU32(checkSortCode, modAccount);
            short[] ex14Weights = [0, 0, 0, 0, 0, 0, 8, 7, 6, 5, 4, 3, 2, 1];
            var sum = modDigits.Zip(ex14Weights, (d, w) => (int)d * w).Sum();
            var remainder = ((sum % 11) + 11) % 11;
            return remainder == 0;
        }

        // Exception 10 & 11
        if (hasEx[10] || hasEx[11])
        {
            if (firstPasses == true) return true;
            if (secondPasses == true) return true;
            return false;
        }

        // Exception 5
        if (hasEx[5])
        {
            return ApplyException5(rows, digits, account8);
        }

        // Default: one or two checks; both must pass when two required
        if (firstPasses == true)
        {
            if (secondRow == null || ex3SkipSecond) return true;
            if (secondPasses == true) return true;
        }

        return false;
    }

    private static bool RunOneCheck(byte[] digits, WeightRow row, byte[] accountNo)
    {
        var (a, b, g, h) = (accountNo[0], accountNo[1], accountNo[6], accountNo[7]);
        var exception = row.Exception;
        var weights = (short[])row.Weights.Clone();

        // Exception 7: if g=9, zeroize weighting positions u-b (first 8 weights).
        if (exception == 7 && g == 9)
        {
            for (var i = 0; i < 8; i++) weights[i] = 0;
        }

        // Exception 2, 9, 10 & 11: if ab=09 or 99 and g=9, zeroize u-b (Lloyds Euro / Coutts style).
        if ((exception == 2 || exception == 9 || exception== 10 || exception== 11)
            && (a == 0 && b == 9 || a == 9 && b == 9) && g == 9)
        {
            for (var i = 0; i < 8; i++) weights[i] = 0;
        }

        var sum = ModulusChecks.WeightedSum(digits, weights, row.Method);

        // Exception 1: add 27 to the accumulated total before dividing by 10.
        if (exception == 1) sum += 27;

        var m = ModulusChecks.ModulusFor(row.Method);
        var remainder = ((sum % m) + m) % m;

        // Exception 4: the remainder must equal the two-digit check digit gh (as 0-10? or just g?).
        if (exception == 4)
        {
            var checkDigit = (10 * g + h) % 11;
            return remainder == checkDigit;
        }

        // Exception 5: standard/modulus check with a remainder; both check digits must pass (handled in apply_exception_5).
        return remainder == 0;
    }

    private static bool ApplyException5(IReadOnlyList<WeightRow> rows, byte[] digits, byte[] accountNumber)
    {
        var g = accountNumber[6];
        var h = accountNumber[7];
        int? firstRemainder = null;
        int? secondRemainder = null;
        foreach (var row in rows)
        {
            var (rem, _) = ModulusChecks.RunCheck(digits, row);
            if (row.Exception != 5) continue;
            if (row.Method == ModulusMethod.Mod11) firstRemainder = rem;
            if (row.Method == ModulusMethod.DblAl || row.Method == ModulusMethod.Mod10) secondRemainder = rem;
        }
        // Ex 5: MOD11 remainder vs digit g; DBLAL remainder vs digit h. Expected check digit = (modulus - remainder) % modulus.
        var firstOk = firstRemainder.HasValue && g == (11 - firstRemainder.Value + 11) % 11;
        var secondOk = secondRemainder.HasValue && h == (10 - secondRemainder.Value + 10) % 10;
        return firstOk && secondOk;
    }
}
