using CMK.UK.BankModulus.Data;

namespace GbBankValidator.Tests;

public class DataTests
{
    [Test]
    public async Task ParseValacdos_Line()
    {
        var s = "089000 089999 MOD10    0    0    0    0    0    0    7    1    3    7    1    3    7    1";
        var tables = Tables.Parse(s, "");
        var rows = tables.WeightRows;
        await Assert.That(rows).HasSingleItem();
        await Assert.That(rows[0].Start).IsEqualTo(89_000u);
        await Assert.That(rows[0].End).IsEqualTo(89_999u);
        await Assert.That(rows[0].Method).IsEqualTo(ModulusMethod.Mod10);
        await Assert.That((int)rows[0].Weights[6]).IsEqualTo(7);
        await Assert.That(rows[0].Exception).IsNull();
    }

    [Test]
    public async Task ParseValacdos_WithException()
    {
        var s = "070116 070116 MOD11    0    0    7    6    5    8    9    4    5    6    7    8    9   -1  12";
        var tables = Tables.Parse(s, "");
        var rows = tables.WeightRows;
        await Assert.That(rows).HasSingleItem();
        await Assert.That(rows[0].Exception).IsEqualTo((byte?)12);
        await Assert.That((int)rows[0].Weights[13]).IsEqualTo(-1);
    }

    [Test]
    public async Task ParseScsubtab()
    {
        var s = "938600 938611\n938173 938017";
        var tables = Tables.Parse("", s);
        await Assert.That(tables.Substitution.Substitute(938600)).IsEqualTo(938611u);
        await Assert.That(tables.Substitution.Substitute(938173)).IsEqualTo(938017u);
        await Assert.That(tables.Substitution.Substitute(999999)).IsEqualTo(999999u);
    }

    [Test]
    public async Task SortCodeFromDigits()
    {
        await Assert.That(DataHelpers.SortCodeFromDigits(new byte[] { 0, 8, 9, 9, 9, 9 })).IsEqualTo(89_999u);
        await Assert.That(DataHelpers.SortCodeFromDigits(new byte[] { 1, 2, 3, 4, 5, 6 })).IsEqualTo(123_456u);
    }
}
