using GbBankValidator.Data;
using GbBankValidator.Validation;

namespace GbBankValidator;

/// <summary>Public API for GB bank account validation against sort codes.</summary>
public static class BankValidator
{
    private static Tables? Tables;
    private static readonly Lock TableLock = new();

    /// <summary>Validates a UK bank account number for the given sort code.</summary>
    /// <param name="sortCode">Sort code (6 digits, optional hyphens e.g. "12-34-56" or "123456").</param>
    /// <param name="accountNumber">Account number (up to 11 digits; last 8 used for checks).</param>
    /// <returns>True if the account passes modulus and exception rules for the sort code.</returns>
    public static bool Validate(string sortCode, string accountNumber)
    {
        return BankValidation.Validate(sortCode, accountNumber, GetTables());
    }

    /// <summary>Validates using custom tables (e.g. for testing or custom data).</summary>
    public static bool Validate(string sortCode, string accountNumber, Tables tables) =>
        BankValidation.Validate(sortCode, accountNumber, tables);

    private static Tables GetTables()
    {
        if (Tables != null) return Tables;
        lock (TableLock)
        {
            Tables ??= Tables.FromEmbedded();
            return Tables;
        }
    }
}
