using GbBankValidator.Data;
using Xunit;

namespace GbBankValidator.Tests;

public class DataTests
{
    [Fact]
    public void ParseValacdos_Line()
    {
        var s = "089000 089999 MOD10    0    0    0    0    0    0    7    1    3    7    1    3    7    1";
        var tables = Tables.Parse(s, "");
        var rows = tables.WeightRows;
        Assert.Single(rows);
        Assert.Equal(89_000u, rows[0].Start);
        Assert.Equal(89_999u, rows[0].End);
        Assert.Equal(ModulusMethod.Mod10, rows[0].Method);
        Assert.Equal(7, rows[0].Weights[6]);
        Assert.Null(rows[0].Exception);
    }

    [Fact]
    public void ParseValacdos_WithException()
    {
        var s = "070116 070116 MOD11    0    0    7    6    5    8    9    4    5    6    7    8    9   -1  12";
        var tables = Tables.Parse(s, "");
        var rows = tables.WeightRows;
        Assert.Single(rows);
        Assert.Equal((byte?)12, rows[0].Exception);
        Assert.Equal(-1, rows[0].Weights[13]);
    }

    [Fact]
    public void ParseScsubtab()
    {
        var s = "938600 938611\n938173 938017";
        var tables = Tables.Parse("", s);
        Assert.Equal(938611u, tables.Substitution.Substitute(938600));
        Assert.Equal(938017u, tables.Substitution.Substitute(938173));
        Assert.Equal(999999u, tables.Substitution.Substitute(999999));
    }

    [Fact]
    public void SortCodeFromDigits()
    {
        Assert.Equal(89_999u, DataHelpers.SortCodeFromDigits(new byte[] { 0, 8, 9, 9, 9, 9 }));
        Assert.Equal(123_456u, DataHelpers.SortCodeFromDigits(new byte[] { 1, 2, 3, 4, 5, 6 }));
    }
}
