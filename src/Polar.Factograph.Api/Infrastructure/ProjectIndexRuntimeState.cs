namespace Polar.Factograph.Api.Infrastructure;

internal static class ProjectIndexRuntimeState
{
    public static string Resolve(
        bool dirty,
        ProjectIndexPointerSnapshot pointer)
    {
        ArgumentNullException.ThrowIfNull(pointer);
        if (pointer.State == "invalid" ||
            pointer.State == "valid" && !pointer.GenerationAvailable)
        {
            return "invalid";
        }

        if (dirty)
        {
            return "dirty";
        }

        return pointer.State == "missing" ? "missing" : "ready";
    }
}
