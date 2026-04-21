namespace CMK.UK.BankModulus.Data;

/// <summary>One weight-table row: sort-code range, method, 14 weights (u,v,w,x,y,z for sort code; a..h for account), optional exception.</summary>
public class WeightRow
{
    public uint Start { get; }
    public uint End { get; }
    public ModulusMethod Method { get; }
    public short[] Weights { get; }
    public byte? Exception { get; }

    public WeightRow(uint start, uint end, ModulusMethod method, short[] weights, byte? exception = null)
    {
        Start = start;
        End = end;
        Method = method;
        Weights = weights ?? throw new ArgumentNullException(nameof(weights));
        if (Weights.Length != 14) throw new ArgumentException("Must have 14 weights", nameof(weights));
        Exception = exception is >= 1 and <= 14 ? exception : null;
    }
}
