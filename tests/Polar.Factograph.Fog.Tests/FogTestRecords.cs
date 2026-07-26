using Polar.Factograph.Fog;

namespace Polar.Factograph.Fog.Tests;

internal static class FogTestRecords
{
    public static async Task<List<FogSourceRecord>> ReadAllAsync(
        FogSourceDescriptor source)
    {
        List<FogSourceRecord> result = new();
        await foreach (FogSourceRecord record in new FileSystemFogRecordReader()
                           .ReadAsync(source))
        {
            result.Add(record);
        }

        return result;
    }
}
