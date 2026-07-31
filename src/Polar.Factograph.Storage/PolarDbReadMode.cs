namespace Polar.Factograph.Storage;

/// <summary>
/// Selects how read queries access secondary Polar.DB.Typed structures.
/// FullScan is the safe compatibility mode for generations created before the
/// external-index offset issue is fixed. ExternalIndexes is the intended fast path.
/// </summary>
public enum PolarDbReadMode
{
    FullScan = 0,
    ExternalIndexes = 1
}
