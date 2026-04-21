using CMK.UK.BankModulus.Data;
using CMK.UK.BankModulus.Modulus;

namespace GbBankValidator.Tests;

public class ModulusTests
{
    private static WeightRow MakeRow(short[] weights, ModulusMethod method) =>
        new(0, 999_999, method, weights, null);

    [Test]
    public async Task SumDigits()
    {
        await Assert.That(ModulusChecks.SumDigits(18)).IsEqualTo(9);
        await Assert.That(ModulusChecks.SumDigits(10)).IsEqualTo(1);
        await Assert.That(ModulusChecks.SumDigits(0)).IsEqualTo(0);
    }

    [Test]
    public async Task DblAl_Example()
    {
        // Sort code 499273, account 12345678 -> uvwxyzabcdefgh = 4,9,9,2,7,3,1,2,3,4,5,6,7,8
        // Weights 2 1 2 1 2 1 2 1 2 1 2 1 2 1 -> digit-sum of products = 70, remainder 0
        var digits = new byte[] { 4, 9, 9, 2, 7, 3, 1, 2, 3, 4, 5, 6, 7, 8 };
        var weights = new short[] { 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1, 2, 1 };
        var row = MakeRow(weights, ModulusMethod.DblAl);
        var (rem, m) = ModulusChecks.RunCheck(digits, row);
        await Assert.That(m).IsEqualTo(10);
        await Assert.That(rem).IsEqualTo(0);
    }
}
