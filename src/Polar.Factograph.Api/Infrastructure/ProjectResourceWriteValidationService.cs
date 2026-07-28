using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectResourceWriteValidationService(
    ProjectStoreProvider storeProvider,
    OntologyCatalogProvider ontologyProvider,
    OntologyResourceWriteValidator validator)
{
    public async Task ValidateAsync(
        ProjectAccessContext context,
        FogResourceWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        OntologyCatalog ontology = await ontologyProvider.GetAsync(
            context.Project.Ontology.Path,
            cancellationToken);
        IReadOnlyDictionary<string, IReadOnlySet<FogPropertyKind>> legacyProperties =
            await ReadLegacyPropertyShapesAsync(context, request, cancellationToken);
        validator.Validate(ontology, request, legacyProperties);
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlySet<FogPropertyKind>>>
        ReadLegacyPropertyShapesAsync(
            ProjectAccessContext context,
            FogResourceWriteRequest request,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResourceId))
        {
            return new Dictionary<string, IReadOnlySet<FogPropertyKind>>(
                StringComparer.Ordinal);
        }

        string resourceId = FogIdentifier.Clean(request.ResourceId);
        PolarDbTypedProjectStore store = storeProvider.GetCurrent(
            context.Project.Index.Path);
        Dictionary<string, HashSet<FogPropertyKind>> shapes =
            new(StringComparer.Ordinal);

        await foreach (TripleRow triple in store.FindAsync(
                           new TriplePattern(Subject: resourceId),
                           context.Access.ReadableCassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            FogPropertyKind? kind = triple.ObjectKind switch
            {
                TripleObjectKind.Iri => FogPropertyKind.Resource,
                TripleObjectKind.Literal => FogPropertyKind.Literal,
                _ => null
            };
            if (kind is null)
            {
                continue;
            }

            if (!shapes.TryGetValue(triple.Predicate, out HashSet<FogPropertyKind>? kinds))
            {
                kinds = [];
                shapes.Add(triple.Predicate, kinds);
            }
            kinds.Add(kind.Value);
        }

        return shapes.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlySet<FogPropertyKind>)pair.Value,
            StringComparer.Ordinal);
    }
}
