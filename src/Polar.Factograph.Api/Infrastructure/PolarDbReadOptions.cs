using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class PolarDbReadOptions
{
    public const string SectionName = "PolarDb";

    public PolarDbReadMode ReadMode { get; set; } = PolarDbReadMode.FullScan;
}
