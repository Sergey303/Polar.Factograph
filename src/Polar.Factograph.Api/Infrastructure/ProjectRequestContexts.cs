using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectAccessContext(
    ProjectDefinition Project,
    ProjectAccessSnapshot Access);

public sealed record ProjectReadContext(
    ProjectDefinition Project,
    ProjectAccessSnapshot Access,
    PolarDbTypedProjectStore Store,
    AuthorizedProjectReadService Reads);
