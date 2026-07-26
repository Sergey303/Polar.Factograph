namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectWriteCommittedException : Exception
{
    public ProjectWriteCommittedException(
        string resourceId,
        Exception innerException)
        : base(
            $"Resource '{resourceId}' was written to Fog, but the project index could not be refreshed. Rebuild the index before reading the change.",
            innerException)
    {
        ResourceId = resourceId;
    }

    public string ResourceId { get; }
}
