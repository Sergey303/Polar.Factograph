using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

public sealed record PreviewSubsystemStatus(
    ProjectPreviewQueueStatus Queue,
    PreviewWorkerRuntimeSnapshot Worker,
    PreviewWorkerHealth Health);
