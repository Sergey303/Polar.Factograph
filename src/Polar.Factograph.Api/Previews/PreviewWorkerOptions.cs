namespace Polar.Factograph.Api.Previews;

public sealed record PreviewWorkerOptions
{
    public const string SectionName = "Previews";

    public bool Enabled { get; init; }
    public string? Executable { get; init; }
    public string[] PrefixArguments { get; init; } = Array.Empty<string>();
    public string OutputExtension { get; init; } = "jpg";
    public int SmallWidth { get; init; } = 240;
    public int MediumWidth { get; init; } = 800;
    public int NormalWidth { get; init; } = 1600;
    public int PollIntervalSeconds { get; init; } = 5;
    public int RenderTimeoutSeconds { get; init; } = 300;
    public int MaxItemsPerCycle { get; init; } = 8;
    public int MaxAttempts { get; init; } = 3;
    public int RetryDelaySeconds { get; init; } = 300;
    public int LeaseTimeoutSeconds { get; init; } = 1800;

    public bool IsValid()
    {
        if (!Enabled)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(Executable) &&
               IsSafeExtension(OutputExtension) &&
               SmallWidth > 0 && MediumWidth >= SmallWidth && NormalWidth >= MediumWidth &&
               PollIntervalSeconds > 0 && RenderTimeoutSeconds > 0 &&
               MaxItemsPerCycle > 0 && MaxAttempts > 0 &&
               RetryDelaySeconds >= 0 && LeaseTimeoutSeconds > 0;
    }

    private static bool IsSafeExtension(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 10 &&
        value.All(character => char.IsAsciiLetterOrDigit(character));
}
