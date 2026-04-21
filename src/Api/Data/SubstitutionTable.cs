namespace CMK.UK.BankModulus.Data;

/// <summary>Sort-code substitution: original -> substitute (both 6-digit).</summary>
public class SubstitutionTable
{
    private readonly Dictionary<uint, uint> _map = new();

    /// <summary>Substitute sort code if present. Input <paramref name="sortCode"/> is 6-digit number. Returns substitute or same.</summary>
    public uint Substitute(uint sortCode) => _map.GetValueOrDefault(sortCode, sortCode);

    internal void Add(uint original, uint substitute) => _map[original] = substitute;
}
