using GbBankValidator.Data;

namespace GbBankValidator.Modulus;

/// <summary>Modulus check implementations: MOD10, MOD11, Double Alternate (DBLAL).</summary>
public static class ModulusChecks
{
    /// <summary>Fourteen digits: [u,v,w,x,y,z,a,b,c,d,e,f,g,h]. Weighted sum by method.</summary>
    public static int WeightedSum(byte[] digits, short[] weights, ModulusMethod method)
    {
        if (digits == null || digits.Length != 14 || weights == null || weights.Length != 14)
            throw new ArgumentException("Need 14 digits and 14 weights");
        return WeightedSum(digits.AsSpan(), weights.AsSpan(), method);
    }

    /// <summary>Fourteen digits: [u,v,w,x,y,z,a,b,c,d,e,f,g,h]. Weighted sum by method.</summary>
    public static int WeightedSum(ReadOnlySpan<byte> digits, ReadOnlySpan<short> weights, ModulusMethod method)
    {
        if (digits.Length != 14 || weights.Length != 14)
            throw new ArgumentException("Need 14 digits and 14 weights");

        var sum = 0;
        switch (method)
        {
            case ModulusMethod.DblAl:
                for (var i = 0; i < 14; i++)
                {
                    var product = (int)digits[i] * weights[i];
                    sum += SumDigits(product);
                }
                break;
            case ModulusMethod.Mod10:
            case ModulusMethod.Mod11:
                for (var i = 0; i < 14; i++)
                    sum += (int)digits[i] * weights[i];
                break;
        }
        return sum;
    }

    /// <summary>Sum of decimal digits of n (e.g. 18 -> 1+8 = 9). Handles negative by absolute value.</summary>
    public static int SumDigits(int n)
    {
        if (n < 0) n = -n;
        var s = 0;
        while (n > 0)
        {
            s += n % 10;
            n /= 10;
        }
        return s;
    }

    /// <summary>Modulus value for the method (10 or 11).</summary>
    public static int ModulusFor(ModulusMethod method) =>
        method switch
        {
            ModulusMethod.Mod10 => 10,
            ModulusMethod.Mod11 => 11,
            ModulusMethod.DblAl => 10,
            _ => 10
        };

    /// <summary>Run the modulus check: weighted sum then remainder. Returns (remainder, modulus).</summary>
    public static (int remainder, int modulus) RunCheck(byte[] digits, WeightRow row)
    {
        return RunCheck(digits.AsSpan(), row);
    }

    /// <summary>Run the modulus check: weighted sum then remainder. Returns (remainder, modulus).</summary>
    public static (int remainder, int modulus) RunCheck(ReadOnlySpan<byte> digits, WeightRow row)
    {
        var sum = WeightedSum(digits, row.Weights.AsSpan(), row.Method);
        var m = ModulusFor(row.Method);
        var remainder = ((sum % m) + m) % m;
        return (remainder, m);
    }
}
