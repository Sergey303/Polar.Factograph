using System.Text;

namespace Polar.Factograph.Api.Previews;

internal static class BoundedProcessOutput
{
    private const int MaximumCharacters = 16_384;

    public static async Task<string> ReadAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        char[] buffer = GC.AllocateUninitializedArray<char>(1024);
        StringBuilder captured = new(MaximumCharacters);
        int read;
        while ((read = await reader.ReadAsync(buffer, cancellationToken)) > 0)
        {
            int remaining = MaximumCharacters - captured.Length;
            if (remaining > 0)
            {
                captured.Append(buffer, 0, Math.Min(read, remaining));
            }
        }

        return captured.ToString();
    }
}
