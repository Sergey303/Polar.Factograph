using Polar.Factograph.Fog;

namespace Polar.Factograph.Fog.Tests;

internal static class FogTestRecords
{
    public static Task<List<FogSourceRecord>> ReadAllAsync(
        FogSourceDescriptor source) =>
        ReadAllAsync(new FileSystemFogRecordReader().ReadAsync(source));

    public static async Task<List<T>> ReadAllAsync<T>(IAsyncEnumerable<T> source)
    {
        List<T> result = new();
        await foreach (T item in source)
        {
            result.Add(item);
        }

        return result;
    }
}
