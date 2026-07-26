namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectIndexRuntimeStatusReader(
    ProjectIndexDirtyMarker dirtyMarker)
{
    public ProjectIndexRuntimeStatus Read(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        bool dirty = dirtyMarker.Exists(indexRoot);
        ProjectIndexPointerSnapshot pointer = ProjectIndexPointerReader.Read(indexRoot);
        ProjectIndexGenerationInventory inventory =
            ProjectIndexGenerationInventoryReader.Read(indexRoot);
        return new ProjectIndexRuntimeStatus(
            ProjectIndexRuntimeState.Resolve(dirty, pointer),
            dirty,
            dirtyMarker.ReadMarkedAtUtc(indexRoot),
            pointer.State,
            pointer.GenerationId,
            pointer.GenerationAvailable,
            inventory.CompletedCount,
            inventory.BuildingCount);
    }
}
