using GbBankValidator.Data;
using GbBankValidator.Modulus;
using Xunit;

namespace GbBankValidator.Tests;

public class ModulusTests
{
    private static WeightRow MakeRow(short[] weights, ModulusMethod method) =>
        new(0, 999_999, method, weights, null);

    [Fact]
    public void SumDigits()
    {
        Assert.Equal(9, ModulusChecks.SumDigits(18));
        Assert.Equal(1, ModulusChecks.SumDigits(10));
        Assert.Equal(0, ModulusChecks.SumDigits(0));
    }

    [Fact]
    public void DblAl_Example()
    {
        // Sort code 499273, account 12345678 -> uvwxyzabcdefgh = 4,9,9,2,7,3,1,2,3,4,5,6,7,8
        // Weights 2 1 2 1 2 1 2 1 2 1 2 1 2 1 -> digit-sum of products = 70, remainder 0
        var digits = new byte[] { 4, 9, 9, 2, 7, 3, 1, 2, 3, 4, 5, 6, 7, 8 };
        var weights = new short[] { 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1 };
        var row = MakeRow(weights, ModulusMethod.DblAl);
        var (rem, m) = ModulusChecks.RunCheck(digits, row);
        Assert.Equal(10, m);
        Assert.Equal(0, rem);
    }
}
