using CMK.UK.BankModulus;

namespace GbBankValidator.Tests;

public class BankValidatorTests
{
    [Test]
    public async Task Validate_UsingPublicApi_WithEmbeddedTables()
    {
        await Assert.That(BankValidator.Validate("089999", "66374958")).IsTrue();
        await Assert.That(BankValidator.Validate("089999", "66374959")).IsFalse();
    }

    [Test]
    public async Task Validate_AcceptsHyphenatedSortCode()
    {
        await Assert.That(BankValidator.Validate("08-99-99", "66374958")).IsTrue();
    }
}
