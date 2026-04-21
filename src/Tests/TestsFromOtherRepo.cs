using CMK.UK.BankModulus;

namespace GbBankValidator.Tests;

public class TestsFromOtherRepo
{
    // From https://github.com/pauldambra/ModulusChecker/issues/5
    private const string Sortcode = "775024";
    private const string AccNumber = "26862368";

    [Test]
    public async Task ItCanRevalidateDetailsOnImmediateRepeat()
    {
        await Assert.That(BankValidator.Validate(Sortcode, AccNumber)).IsTrue();
        await Assert.That(BankValidator.Validate(Sortcode, AccNumber)).IsTrue();
    }

    [Test]
    [Arguments("776203", "01193899")]
    [Arguments("089999", "66374958")]
    public async Task SeparatingCheckPassesInIsolation(string sc, string an)
    {
        await Assert.That(BankValidator.Validate(sc, an)).IsTrue();
    }

    [Test]
    [Arguments("776203", "01193899")]
    [Arguments("089999", "66374958")]
    public async Task ItCanRevalidateDetailsOnSeparatedRepeat(string sc, string an)
    {
        await Assert.That(BankValidator.Validate(Sortcode, AccNumber)).IsTrue().Because($"first check should have passed for {Sortcode} and {AccNumber}");
        await Assert.That(BankValidator.Validate(sc, an)).IsTrue().Because($"separating check should have passed for {sc} and {an}");
        await Assert.That(BankValidator.Validate(Sortcode, AccNumber)).IsTrue().Because($"second check should have passed for {Sortcode} and {AccNumber}");
    }

    [Test]
    [Arguments("100000", "41610008")]
    [Arguments("902127", "29823529")]
    [Arguments("800551", "813796")]
    [Arguments("200052", "75849855")]
    [Arguments("185008", "12098709")]
    [Arguments("826632", "20400952")]
    [Arguments("400515", "12345674")]
    [Arguments("309493", "1273801")]
    [Arguments("600920", "67037135")]
    [Arguments("070116", "10909132")]
    [Arguments("830402", "10126939")]
    [Arguments("090126", "3367219")]
    [Arguments("609104", "17068859")]
    [Arguments("080054", "70328725")]
    public async Task IBANExampleTests(string sc, string an)
    {
        // https://iban.co.uk/examples.html
        await Assert.That(BankValidator.Validate(sc, an)).IsTrue();
    }
}
