using CMK.UK.BankModulus.Data;
using CMK.UK.BankModulus.Validation;

namespace GbBankValidator.Tests;

public class ValidationTests
{
    private static Tables GetEmbeddedTables() => Tables.FromEmbedded();

    [Test]
    public async Task NormalizeSortCode()
    {
        await Assert.That(BankValidation.NormalizeSortCode("12-34-56")).IsEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
        await Assert.That(BankValidation.NormalizeSortCode("123456")).IsEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6 });
        await Assert.That(BankValidation.NormalizeSortCode("12345")).IsNull();
        await Assert.That(BankValidation.NormalizeSortCode("12a456")).IsNull();
    }

    [Test]
    public async Task NormalizeAccount()
    {
        await Assert.That(BankValidation.NormalizeAccount("12345678")).IsNotNull();
        await Assert.That(BankValidation.NormalizeAccount("1")).IsNotNull();
        await Assert.That((int)BankValidation.NormalizeAccount("1")![0]).IsEqualTo(0);
        await Assert.That((int)BankValidation.NormalizeAccount("1")![7]).IsEqualTo(1);
        await Assert.That(BankValidation.NormalizeAccount("")).IsNull();
        await Assert.That(BankValidation.NormalizeAccount("12345678901")).IsNotNull();
        await Assert.That(BankValidation.NormalizeAccount("123456789012")).IsNull();
    }

    /// <summary>Non-Santander 9-digit accounts (e.g. Coventry): we drop the leading digit and validate the last 8.</summary>
    [Test]
    public async Task NineDigit_NonSantander_DropLeadingDigit()
    {
        var t = GetEmbeddedTables();
        await Assert.That(BankValidation.Validate("089999", "66374958", t)).IsTrue();
        await Assert.That(BankValidation.Validate("089999", "166374958", t)).IsTrue();
        await Assert.That(BankValidation.Validate("089999", "66374959", t)).IsFalse();
        await Assert.That(BankValidation.Validate("089999", "166374959", t)).IsFalse();
    }

    /// <summary>34 official test cases from "Test data and expected results"</summary>
    [Test]
    [Arguments("089999", "66374958", true)] // 1. Pass modulus 10 check.
    [Arguments("107999", "88837491", true)] // 2. Pass modulus 11 check.
    [Arguments("202959", "63748472", true)] // 3. Pass modulus 11 and double alternate checks.
    [Arguments("871427", "46238510", true)] // 4. Exception 10 & 11 where first check passes and second check fails.
    [Arguments("872427", "46238510", true)] // 5. Exception 10 & 11 where first check fails and second check passes.
    [Arguments("871427", "09123496", true)] // 6. Exception 10 where in the account number ab=09 and the g=9. The first check passes and second check fails.
    [Arguments("871427", "99123496", true)] // 7. Exception 10 where in the account number ab=99 and the g=9. The first check passes and second check fails.
    [Arguments("820000", "73688637", true)] // 8. Exception 3, and the sorting code is the start of a range. As c=6 the second check should be ignored.
    [Arguments("827999", "73988638", true)] // 9. Exception 3, and the sorting code is the end of a range. As c=9 the second check should be ignored.
    [Arguments("827101", "28748352", true)] // 10. Exception 3. As c<>6 or 9 perform both checks pass.
    [Arguments("134020", "63849203", true)] // 11. Exception 4 where the remainder is equal to the checkdigit.
    [Arguments("118765", "64371389", true)] // 12. Exception 1 – ensures that 27 has been added to the accumulated total and passes double alternate modulus check.
    [Arguments("200915", "41011166", true)] // 13. Exception 6 where the account fails standard check but is a foreign currency account.
    [Arguments("938611", "07806039", true)] // 14. Exception 5 where the check passes.
    [Arguments("938600", "42368003", true)] // 15. Exception 5 where the check passes with substitution.
    [Arguments("938063", "55065200", true)] // 16. Exception 5 where both checks produce a remainder of 0 and pass.
    [Arguments("772798", "99345694", true)] // 17. Exception 7 where passes but would fail the standard check.
    [Arguments("086090", "06774744", true)] // 18. Exception 8 where the check passes.
    [Arguments("309070", "02355688", true)] // 19. Exception 2 & 9 where the first check passes.
    [Arguments("309070", "12345668", true)] // 20. Exception 2 & 9 where the first check fails and second check passes with substitution.
    [Arguments("309070", "12345677", true)] // 21. Exception 2 & 9 where a≠0 and g≠9 and passes.
    [Arguments("309070", "99345694", true)] // 22. Exception 2 & 9 where a≠0 and g=9 and passes.
    [Arguments("938063", "15764273", false)] // 23. Exception 5 where the first checkdigit is correct and the second incorrect.
    [Arguments("938063", "15764264", false)] // 24. Exception 5 where the first checkdigit is incorrect and the second correct.
    [Arguments("938063", "15763217", false)] // 25. Exception 5 where the first checkdigit is incorrect with a remainder of 1.
    [Arguments("118765", "64371388", false)] // 26. Exception 1 where it fails double alternate check.
    [Arguments("203099", "66831036", false)] // 27. Pass modulus 11 check and fail double alternate check.
    [Arguments("203099", "58716970", false)] // 28. Fail modulus 11 check and pass double alternate check.
    [Arguments("089999", "66374959", false)] // 29. Fail modulus 10 check.
    [Arguments("107999", "88837493", false)] // 30. Fail modulus 11 check.
    [Arguments("074456", "12345112", true)] // 31. Exception 12/13 where passes modulus 11 check (in this example, modulus 10 check fails, however, there is no need for it to be performed as the first check passed).
    [Arguments("070116", "34012583", true)] // 32. Exception 12/13 where passes the modulus 11check (in this example, modulus 10 check passes as well, however, there is no need for it to be performed as the first check passed).
    [Arguments("074456", "11104102", true)] // 33. Exception 12/13 where fails the modulus 11 check, but passes the modulus 10 check.
    [Arguments("180002", "00000190", true)] // 34. Exception 14 where the first check fails and the second check passes.
    public async Task CanPassCurrentVocalinkTestCases(string sortCode, string account, bool expectedResult)
    {
        var t = GetEmbeddedTables();
        var result = BankValidation.Validate(sortCode, account, t);
        await Assert.That(result == expectedResult).IsTrue().Because($"sort_code={sortCode} account={account} expected={expectedResult} got={result}");
    }
}
