using System.Reflection;

namespace CMK.UK.BankModulus.Data;

/// <summary>Combined tables: weight rows (EISCD) and substitution table.</summary>
public class Tables
{
    /// <summary>All weight rows. Lookup: find all rows where sort_code >= start && sort_code <= end.</summary>
    public IReadOnlyList<WeightRow> WeightRows { get; }

    public SubstitutionTable Substitution { get; }

    public Tables(List<WeightRow> weightRows, SubstitutionTable substitution)
    {
        WeightRows = weightRows ?? throw new ArgumentNullException(nameof(weightRows));
        Substitution = substitution ?? throw new ArgumentNullException(nameof(substitution));
    }

    /// <summary>Build tables from embedded data.</summary>
    public static Tables FromEmbedded()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var valacdosStream = asm.GetManifestResourceStream("valacdos.txt")
            ?? throw new InvalidOperationException("Embedded resource valacdos.txt not found.");
        using var scsubtabStream = asm.GetManifestResourceStream("scsubtab.txt")
            ?? throw new InvalidOperationException("Embedded resource scsubtab.txt not found.");
        using var valacdosReader = new StreamReader(valacdosStream);
        using var scsubtabReader = new StreamReader(scsubtabStream);
        return Parse(valacdosReader.ReadToEnd(), scsubtabReader.ReadToEnd());
    }

    /// <summary>Parse valacdos and scsubtab content into Tables.</summary>
    public static Tables Parse(string valacdos, string sortcodeSubsTable)
    {
        var weightRows = ParseValacdos(valacdos);
        var substitution = ParseSortCodeSubstitutions(sortcodeSubsTable);
        return new Tables(weightRows, substitution);
    }

    /// <summary>Find all weight rows that apply to this sort code (numeric, 6-digit).</summary>
    public IReadOnlyList<WeightRow> RowsForSortCode(uint sortCode) =>
        WeightRows.Where(r => r.Start <= sortCode && r.End >= sortCode).ToList();

    private static ModulusMethod ParseMethod(string s)
    {
        return s switch
        {
            "MOD10" => ModulusMethod.Mod10,
            "MOD11" => ModulusMethod.Mod11,
            "DBLAL" => ModulusMethod.DblAl,
            _ => throw new FormatException($"Unknown method: {s}")
        };
    }

    private static List<WeightRow> ParseValacdos(string content)
    {
        var rows = new List<WeightRow>();
        var lineNum = 0;
        foreach (var line in content.Split('\n'))
        {
            lineNum++;
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 17)
                throw new FormatException($"valacdos line {lineNum}: expected at least 17 fields (start end method 14 weights)");

            var start = uint.Parse(parts[0]);
            var end = uint.Parse(parts[1]);
            var method = ParseMethod(parts[2]);
            var weights = new short[14];
            for (var j = 0; j < 14; j++)
                weights[j] = short.Parse(parts[3 + j]);

            byte? exception = null;
            if (parts.Length > 17 && byte.TryParse(parts[17], out var ex) && ex >= 1 && ex <= 14)
                exception = ex;

            rows.Add(new WeightRow(start, end, method, weights, exception));
        }
        return rows;
    }

    private static SubstitutionTable ParseSortCodeSubstitutions(string content)
    {
        var table = new SubstitutionTable();
        var lineNum = 0;
        foreach (var line in content.Split('\n'))
        {
            lineNum++;
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            var parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new FormatException($"scsubtab line {lineNum}: expected original and substitute");
            var orig = parts[0];
            var sub = parts[1];
            if (orig.Length != 6 || sub.Length != 6)
                throw new FormatException($"scsubtab line {lineNum}: original and substitute must be 6 characters");

            table.Add(uint.Parse(orig), uint.Parse(sub));
        }
        return table;
    }
}
