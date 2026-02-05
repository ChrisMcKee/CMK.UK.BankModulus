using Xunit;

namespace GbBankValidator.Tests;

public class BankValidatorTests
{
    [Fact]
    public void Validate_UsingPublicApi_WithEmbeddedTables()
    {
        Assert.True(BankValidator.Validate("089999", "66374958"));
        Assert.False(BankValidator.Validate("089999", "66374959"));
    }

    [Fact]
    public void Validate_AcceptsHyphenatedSortCode()
    {
        Assert.True(BankValidator.Validate("08-99-99", "66374958"));
    }
}
