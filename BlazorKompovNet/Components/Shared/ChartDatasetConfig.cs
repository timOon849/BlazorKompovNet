namespace BlazorKompovNet.Components.Shared;

public sealed class ChartDatasetConfig : IEquatable<ChartDatasetConfig>
{
    public string? Label { get; set; }

    public double[] Data { get; set; } = [];

    public object? BackgroundColor { get; set; }

    public object? BorderColor { get; set; }

    public int BorderWidth { get; set; } = 1;

    public bool Fill { get; set; }

    public double Tension { get; set; }

    public bool Equals(ChartDatasetConfig? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Label == other.Label
            && BorderWidth == other.BorderWidth
            && Fill == other.Fill
            && Tension.Equals(other.Tension)
            && Data.SequenceEqual(other.Data);
    }

    public override bool Equals(object? obj) => Equals(obj as ChartDatasetConfig);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Label);
        hash.Add(BorderWidth);
        hash.Add(Fill);
        hash.Add(Tension);

        foreach (var value in Data)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}
