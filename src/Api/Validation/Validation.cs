using CMK.UK.BankModulus.Data;
using CMK.UK.BankModulus.Modulus;

namespace CMK.UK.BankModulus.Validation;

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
        Span<byte> digits = stackalloc byte[6];
        if (!TryNormalizeSortCode(s.AsSpan(), digits)) return null;
        return digits.ToArray();
    }

    /// <summary>Try to normalise sort code into exactly 6 digits. Returns false if invalid.</summary>
    private static bool TryNormalizeSortCode(ReadOnlySpan<char> s, Span<byte> digits)
    {
        if (digits.Length < 6) return false;
        var count = 0;
        foreach (var c in s)
        {
            if (c == '-') continue;
            if (count >= 6) return false;
            if (!char.IsDigit(c)) return false;
            digits[count++] = (byte)(c - '0');
        }
        return count == 6;
    }

    /// <summary>Normalise an account number: digits only, max 11. Returns null if invalid. Uses last 8 digits (left-padded if fewer).</summary>
    public static byte[]? NormalizeAccount(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length > 11) return null;
        Span<byte> digits = stackalloc byte[8];
        if (!TryNormalizeAccount(s.AsSpan(), digits)) return null;
        return digits.ToArray();
    }

    /// <summary>Try to normalise an account number into exactly 8 digits (last 8, left-padded). Returns false if invalid.</summary>
    private static bool TryNormalizeAccount(ReadOnlySpan<char> s, Span<byte> digits)
    {
        if (s.IsEmpty || s.Length > 11 || digits.Length < 8) return false;
        for (var i = 0; i < s.Length; i++)
            if (!char.IsDigit(s[i])) return false;
        if (s.Length >= 8)
        {
            var start = s.Length - 8;
            for (var i = 0; i < 8; i++)
                digits[i] = (byte)(s[start + i] - '0');
        }
        else
        {
            var pad = 8 - s.Length;
            for (var i = 0; i < pad; i++)
                digits[i] = 0;
            for (var i = 0; i < s.Length; i++)
                digits[pad + i] = (byte)(s[i] - '0');
        }
        return true;
    }

    /// <summary>Write 14 digits from sort code + account (8 digits) into the given span.</summary>
    private static void Digits14FromU32(uint sortCode, ReadOnlySpan<byte> account8, Span<byte> digits14)
    {
        if (digits14.Length < 14 || account8.Length < 8) return;
        var divisor = 100_000u;
        for (var i = 0; i < 6; i++)
        {
            digits14[i] = (byte)((sortCode / divisor) % 10);
            divisor /= 10;
        }
        for (var i = 0; i < 8; i++)
            digits14[6 + i] = account8[i];
    }

    /// <summary>Main validation: returns true if the account number is valid for the sort code.</summary>
    public static bool Validate(string sortCode, string accountNumber, Tables tables)
    {
        Span<byte> sortCodeDigits = stackalloc byte[6];
        Span<byte> accountDigits = stackalloc byte[8];
        if (!TryNormalizeSortCode(sortCode.AsSpan(), sortCodeDigits)) return false;
        if (!TryNormalizeAccount(accountNumber.AsSpan(), accountDigits)) return false;
        return ValidateCore(sortCodeDigits, accountDigits, tables);
    }

    private static bool ValidateCore(ReadOnlySpan<byte> sortCode6, ReadOnlySpan<byte> account8, Tables tables)
    {
        var sortCodeFromDigits = DataHelpers.SortCodeFromDigits(sortCode6);
        var checkSortCode = tables.Substitution.Substitute(sortCodeFromDigits);

        // EISCD: find weight rows for (substituted) sort code. If none, "cannot be checked" -> valid.
        var rows = tables.RowsForSortCode(checkSortCode).ToList();
        if (rows.Count == 0) return true;

        Span<bool> hasEx = stackalloc bool[15];
        foreach (var r in rows)
        {
            if (r.Exception is >= 1 and <= 14)
                hasEx[r.Exception.Value] = true;
        }

        Span<byte> digits = stackalloc byte[14];
        Digits14FromU32(checkSortCode, account8, digits);

        // Exception 8: use sort code 090126 for check
        Span<byte> digitsEx8Buf = stackalloc byte[14];
        Digits14FromU32(90_126, account8, digitsEx8Buf);

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

        bool? firstPasses = firstRow != null
            ? RunOneCheck(firstRow.Exception == 8 ? digitsEx8Buf : digits, firstRow, account8)
            : null;
        bool? secondPasses = secondRow != null
            ? RunOneCheck(secondRow.Exception == 8 ? digitsEx8Buf : digits, secondRow, account8)
            : null;

        // Exception 3: if c is 6 or 9, the second check is ignored (only first counts).
        var ex3SkipSecond = hasEx[3] && (c == 6 || c == 9);

        // Exception 12/13: first check ex 12, second ex 13; either pass -> valid.
        // ReSharper disable once InconsistentNaming
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
            // When a≠0, the second check uses alternative weights (Vocalink).
            if (a != 0)
            {
                var altWeights = g == 9 ? Ex2And9_ANotZero_GIsNine_Weights : Ex2And9_ANotZero_GNotNine_Weights;
                var sum = ModulusChecks.WeightedSum(digits, altWeights.AsSpan(), ModulusMethod.Mod11);
                var remainder = ((sum % 11) + 11) % 11;
                if (remainder == 0) return true;
            }
            // Second check with sort code 309634: use the second weight row only with substituted digits.
            const uint subSc = 309634;
            Span<byte> subDigitsBuf = stackalloc byte[14];
            Digits14FromU32(subSc, account8, subDigitsBuf);
            if (secondRow != null && RunOneCheck(subDigitsBuf, secondRow, account8))
                return true;
            return false;
        }

        // Exception 14
        if (hasEx[14])
        {
            if (firstPasses == true) return true;
            if (h != 0 && h != 1 && h != 9) return false;
            Span<byte> modAccount = stackalloc byte[8];
            modAccount[0] = 0;
            modAccount[1] = a;
            modAccount[2] = b;
            modAccount[3] = c;
            modAccount[4] = d;
            modAccount[5] = e;
            modAccount[6] = f;
            modAccount[7] = g;
            Span<byte> modDigits = stackalloc byte[14];
            Digits14FromU32(checkSortCode, modAccount, modDigits);
            Span<short> ex14Weights = stackalloc short[14];
            ex14Weights[0] = 0;
            ex14Weights[1] = 0;
            ex14Weights[2] = 0;
            ex14Weights[3] = 0;
            ex14Weights[4] = 0;
            ex14Weights[5] = 0;
            ex14Weights[6] = 8;
            ex14Weights[7] = 7;
            ex14Weights[8] = 6;
            ex14Weights[9] = 5;
            ex14Weights[10] = 4;
            ex14Weights[11] = 3;
            ex14Weights[12] = 2;
            ex14Weights[13] = 1;
            var sum = 0;
            for (var i = 0; i < 14; i++)
                sum += modDigits[i] * ex14Weights[i];
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

    private static bool RunOneCheck(ReadOnlySpan<byte> digits, WeightRow row, ReadOnlySpan<byte> accountNo)
    {
        var (a, b, g, h) = (accountNo[0], accountNo[1], accountNo[6], accountNo[7]);
        var exception = row.Exception;
        Span<short> weights = stackalloc short[14];
        row.Weights.AsSpan().CopyTo(weights);

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

    private static bool ApplyException5(IReadOnlyList<WeightRow> rows, ReadOnlySpan<byte> digits, ReadOnlySpan<byte> accountNumber)
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
