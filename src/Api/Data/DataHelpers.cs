namespace CMK.UK.BankModulus.Data;

/// <summary>Helpers for building sort code from digits. No validation.</summary>
public static class DataHelpers
{
    /// <summary>Convert 6 digit bytes (each 0..=9) to sort code u32.</summary>
    public static uint SortCodeFromDigits(byte[] d)
    {
        if (d == null || d.Length != 6) throw new ArgumentException("Need 6 digits", nameof(d));
        return SortCodeFromDigits(d.AsSpan());
    }

    /// <summary>Convert 6 digit bytes (each 0..=9) to sort code u32.</summary>
    public static uint SortCodeFromDigits(ReadOnlySpan<byte> d)
    {
        if (d.Length != 6) throw new ArgumentException("Need 6 digits", nameof(d));
        return (uint)(d[0] * 100_000 + d[1] * 10_000 + d[2] * 1_000 + d[3] * 100 + d[4] * 10 + d[5]);
    }
}
