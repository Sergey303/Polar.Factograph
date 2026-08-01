using System.Runtime.CompilerServices;
using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class SemanticResourceCompactPageTests
{
    [Fact]
    public async Task GetCompactAsync_DoesNotReadOrEmbedInverseRelations()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-compact-page-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string ontologyPath = Path.Combine(directory, "ontology.xml");
            await File.WriteAllTextAsync(
                ontologyPath,
                OntologyXml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            OntologyCatalog ontology = await new XmlOntologyCatalogLoader()
                .LoadAsync(ontologyPath);
            CompactStore store = new();
            ProjectResourcePortraitService rawPortraits = new(store);
            ProjectResourceSearchService search = new(store, store, ontology);
            AuthorizedProjectReadService reads = new(rawPortraits, search);
            SemanticResourcePageService service = new(
                reads,
                new OntologyResourcePortraitPresenter(ontology),
                ontology);

            PresentedSemanticResourcePage? page = await service.GetCompactAsync(
                "city-1",
                CreateAccess());

            Assert.NotNull(page);
            Assert.Equal("city-1", page.Portrait.ResourceId);
            Assert.Equal("Новосибирск", Assert.Single(page.Portrait.Literals).DisplayValue);
            Assert.Empty(page.Portrait.DirectLinks);
            Assert.Empty(page.Portrait.InverseLinks);
            Assert.Empty(page.Entries);
            Assert.Empty(page.Links);
            Assert.Empty(page.Photos);
            Assert.False(store.InverseQueryAttempted);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ProjectAccessSnapshot CreateAccess()
    {
        HashSet<string> projectRights = new(StringComparer.Ordinal)
        {
            ProjectRights.Read,
            ProjectRights.Search
        };
        Dictionary<string, CassetteAccessSnapshot> cassettes = new(StringComparer.Ordinal)
        {
            ["cass"] = new CassetteAccessSnapshot(
                "cass",
                Enabled: true,
                AllowWrite: false,
                new HashSet<string>([CassetteRights.Read], StringComparer.Ordinal))
        };
        return new ProjectAccessSnapshot(
            "viewer",
            IsMember: true,
            projectRights,
            cassettes,
            DefaultWriteCassetteId: null);
    }

    private sealed class CompactStore : IProjectRdfStore, IProjectSearchStore
    {
        private static readonly DateTimeOffset ModifiedAt =
            new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

        public bool InverseQueryAttempted { get; private set; }

        public ValueTask<ResourceHead?> GetResourceHeadAsync(
            string resourceId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<ResourceHead?>(new ResourceHead(
                resourceId,
                Guid.NewGuid(),
                "cass",
                "source.fog",
                ModifiedAt,
                IsDeleted: false));

        public async IAsyncEnumerable<TripleRow> FindAsync(
            TriplePattern pattern,
            IReadOnlySet<string> allowedCassetteIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (pattern.ObjectValue is not null)
            {
                InverseQueryAttempted = true;
                throw new InvalidOperationException(
                    "The compact resource page must not query inverse relations.");
            }

            if (!string.Equals(pattern.Subject, "city-1", StringComparison.Ordinal))
            {
                yield break;
            }

            yield return Triple(
                "http://www.w3.org/1999/02/22-rdf-syntax-ns#type",
                TripleObjectKind.Iri,
                "http://fogid.net/o/city",
                language: null);
            yield return Triple(
                "http://fogid.net/o/name",
                TripleObjectKind.Literal,
                "Новосибирск",
                "ru");
        }

        public Task RebuildAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
            string normalizedSearchKey,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(Array.Empty<NameSearchHit>());

        public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
            string resourceId,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NameSearchHit>>(Array.Empty<NameSearchHit>());

        public Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
            string normalizedWord,
            IReadOnlySet<string> allowedCassetteIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WordSearchHit>>(Array.Empty<WordSearchHit>());

        private static TripleRow Triple(
            string predicate,
            TripleObjectKind objectKind,
            string objectValue,
            string? language) => new(
                Guid.NewGuid(),
                "city-1",
                predicate,
                objectKind,
                objectValue,
                language,
                DataType: null,
                Guid.NewGuid(),
                "cass",
                "source.fog",
                ModifiedAt);
    }

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="http://fogid.net/o/entity" abstract="yes" />
          <Class rdf:about="http://fogid.net/o/sys-obj" abstract="yes">
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <Class rdf:about="http://fogid.net/o/city">
            <label xml:lang="ru">Город</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <DatatypeProperty rdf:about="http://fogid.net/o/name">
            <label xml:lang="ru">название</label>
            <domain rdf:resource="http://fogid.net/o/sys-obj" />
            <range rdf:resource="http://fogid.net/o/text" />
          </DatatypeProperty>
        </Ontology>
        """;
}
