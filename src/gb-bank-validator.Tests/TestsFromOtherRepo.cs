using Xunit;

namespace GbBankValidator.Tests;

public class TestsFromOtherRepo
{
    // From https://github.com/pauldambra/ModulusChecker/issues/5

    private const string Sortcode = "775024";
    private const string AccNumber = "26862368";

    [Fact]
    public void ItCanRevalidateDetailsOnImmediateRepeat()
    {
        Assert.True(BankValidator.Validate(Sortcode, AccNumber));
        Assert.True(BankValidator.Validate(Sortcode, AccNumber));
    }

    [Theory]
    [InlineData("776203", "01193899")]
    [InlineData("089999", "66374958")]
    public void SeparatingCheckPassesInIsolation(string sc, string an)
    {
        Assert.True(BankValidator.Validate(sc, an));
    }

    [Theory]
    [InlineData("776203", "01193899")]
    [InlineData("089999", "66374958")]
    public void ItCanRevalidateDetailsOnSeparatedRepeat(string sc, string an)
    {
        Assert.True(BankValidator.Validate(Sortcode, AccNumber), $"first check should have passed for {Sortcode} and {AccNumber}");
        Assert.True(BankValidator.Validate(sc, an), $"separating check should have passed for {sc} and {an}");
        Assert.True(BankValidator.Validate(Sortcode, AccNumber), $"second check should have passed for {Sortcode} and {AccNumber}");
    }

    [Theory]
    [InlineData("100000", "41610008")]
    [InlineData("902127", "29823529")]
    [InlineData("800551", "813796")]
    [InlineData("200052", "75849855")]
    [InlineData("185008", "12098709")]
    [InlineData("826632", "20400952")]
    [InlineData("400515", "12345674")]
    [InlineData("309493", "1273801")]
    [InlineData("600920", "67037135")]
    [InlineData("070116", "10909132")]
    [InlineData("830402", "10126939")]
    [InlineData("090126", "3367219")]
    [InlineData("609104", "17068859")]
    [InlineData("080054", "70328725")]
    public void IBANExampleTests(string sc, string an)
    {
        // https://iban.co.uk/examples.html
        Assert.True(BankValidator.Validate(sc, an));
    }
}