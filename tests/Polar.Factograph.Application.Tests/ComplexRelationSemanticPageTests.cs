using System.Runtime.CompilerServices;
using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ComplexRelationSemanticPageTests
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private const string O = "http://fogid.net/o/";
    private static readonly DateTimeOffset ModifiedAt =
        new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAsync_ExpandsAspirantToCounterpartEntityOnBothSides()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-complex-relation-tests",
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
            RelationStore store = new(CreateHeads(), CreateTriples());
            ProjectResourcePortraitService portraits = new(store);
            ProjectResourceSearchService search = new(store, store, ontology);
            AuthorizedProjectReadService reads = new(portraits, search);
            SemanticResourcePageService service = new(
                reads,
                new OntologyResourcePortraitPresenter(ontology),
                ontology);
            ProjectAccessSnapshot access = CreateAccess();

            PresentedSemanticResourcePage? personPage = await service.GetAsync(
                "person-1",
                access);
            PresentedSemanticResourcePage? organizationPage = await service.GetAsync(
                "org-1",
                access);

            Assert.NotNull(personPage);
            SemanticResourceLink organization = Assert.Single(personPage.RelatedResources);
            Assert.Equal("org-1", organization.ResourceId);
            Assert.Equal("Институт", organization.DisplayName);
            Assert.Equal("образование в · Аспирант", organization.RelationLabel);
            Assert.Equal("aspirant-1", organization.RelationResourceId);
            Assert.Equal("1987–1990", organization.DisplayDate);
            Assert.Equal("1987-01-01", organization.SortDate);
            Assert.Equal(O + "aspirant", organization.GroupKey);
            Assert.Equal("Аспирант", organization.GroupLabel);
            Assert.DoesNotContain(personPage.RelatedResources, link =>
                link.ResourceId == "aspirant-1");

            SemanticRelationEntry personEntry = Assert.Single(personPage.Entries);
            Assert.Equal("aspirant-1", personEntry.Key);
            Assert.Equal("aspirant-1", personEntry.RelationResourceId);
            Assert.Equal("Аспирант", personEntry.Title);
            Assert.Equal(O + "aspirant", personEntry.GroupKey);
            Assert.Equal("Аспирант", personEntry.GroupLabel);
            Assert.Equal("1987–1990", personEntry.DisplayDate);
            Assert.Equal("1987-01-01", personEntry.SortDate);
            Assert.Equal(2, personEntry.Members.Count);
            SemanticRelationMember personMember = Assert.Single(
                personEntry.Members,
                member => member.ResourceId == "person-1");
            Assert.Equal("ученик", personMember.RoleLabel);
            SemanticRelationMember organizationMember = Assert.Single(
                personEntry.Members,
                member => member.ResourceId == "org-1");
            Assert.Equal("в учебном заведении", organizationMember.RoleLabel);

            Assert.NotNull(organizationPage);
            SemanticResourceLink learner = Assert.Single(organizationPage.RelatedResources);
            Assert.Equal("person-1", learner.ResourceId);
            Assert.Equal("Иван Иванов", learner.DisplayName);
            Assert.Equal("учащийся · Аспирант", learner.RelationLabel);
            Assert.Equal("aspirant-1", learner.RelationResourceId);
            Assert.Equal("1987–1990", learner.DisplayDate);
            Assert.Equal("1987-01-01", learner.SortDate);
            Assert.Equal(O + "aspirant", learner.GroupKey);
            Assert.Equal("Аспирант", learner.GroupLabel);
            Assert.DoesNotContain(organizationPage.RelatedResources, link =>
                link.ResourceId == "aspirant-1");

            SemanticRelationEntry organizationEntry = Assert.Single(organizationPage.Entries);
            Assert.Equal("aspirant-1", organizationEntry.RelationResourceId);
            Assert.Equal("Аспирант", organizationEntry.Title);
            Assert.Equal("1987–1990", organizationEntry.DisplayDate);
            Assert.Equal(2, organizationEntry.Members.Count);
            Assert.Equal(
                "в учебном заведении",
                Assert.Single(
                    organizationEntry.Members,
                    member => member.ResourceId == "org-1").RoleLabel);
            Assert.Equal(
                "ученик",
                Assert.Single(
                    organizationEntry.Members,
                    member => member.ResourceId == "person-1").RoleLabel);
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
        HashSet<string> cassetteRights = new(StringComparer.Ordinal)
        {
            CassetteRights.Read
        };
        Dictionary<string, CassetteAccessSnapshot> cassettes = new(StringComparer.Ordinal)
        {
            ["cass"] = new CassetteAccessSnapshot(
                "cass",
                Enabled: true,
                AllowWrite: false,
                cassetteRights)
        };
        return new ProjectAccessSnapshot(
            "user-1",
            IsMember: true,
            projectRights,
            cassettes,
            DefaultWriteCassetteId: null);
    }

    private static ResourceHead[] CreateHeads() =>
    [
        Head("person-1"),
        Head("org-1"),
        Head("aspirant-1")
    ];

    private static TripleRow[] CreateTriples() =>
    [
        Iri("person-1", RdfType, O + "person"),
        Literal("person-1", O + "name", "Иван Иванов", "ru"),

        Iri("org-1", RdfType, O + "org-sys"),
        Literal("org-1", O + "name", "Институт", "ru"),

        Iri("aspirant-1", RdfType, O + "aspirant"),
        Literal("aspirant-1", O + "from-date", "1987", language: null),
        Literal("aspirant-1", O + "to-date", "1990", language: null),
        Iri("aspirant-1", O + "learner", "person-1"),
        Iri("aspirant-1", O + "learning-org", "org-1")
    ];

    private static ResourceHead Head(string id) => new(
        id,
        Guid.NewGuid(),
        "cass",
        "source.fog",
        ModifiedAt,
        IsDeleted: false);

    private static TripleRow Iri(string subject, string predicate, string value) =>
        Triple(subject, predicate, TripleObjectKind.Iri, value, language: null);

    private static TripleRow Literal(
        string subject,
        string predicate,
        string value,
        string? language) =>
        Triple(subject, predicate, TripleObjectKind.Literal, value, language);

    private static TripleRow Triple(
        string subject,
        string predicate,
        TripleObjectKind kind,
        string value,
        string? language) => new(
            Guid.NewGuid(),
            subject,
            predicate,
            kind,
            value,
            language,
            DataType: null,
            Guid.NewGuid(),
            "cass",
            "source.fog",
            ModifiedAt);

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="http://fogid.net/o/entity" abstract="yes" />
          <Class rdf:about="http://fogid.net/o/sys-obj" abstract="yes">
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <Class rdf:about="http://fogid.net/o/person">
            <label xml:lang="ru">Персона</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <Class rdf:about="http://fogid.net/o/org-sys">
            <label xml:lang="ru">Организация</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <Class rdf:about="http://fogid.net/o/learn" abstract="yes">
            <label xml:lang="ru">Учёба</label>
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <Class rdf:about="http://fogid.net/o/aspirant">
            <label xml:lang="ru">Аспирант</label>
            <SubClassOf rdf:resource="http://fogid.net/o/learn" />
          </Class>
          <DatatypeProperty rdf:about="http://fogid.net/o/name">
            <label xml:lang="ru">имя</label>
            <domain rdf:resource="http://fogid.net/o/sys-obj" />
            <range rdf:resource="http://fogid.net/o/text" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/from-date">
            <label xml:lang="ru">нач.дата</label>
            <domain rdf:resource="http://fogid.net/o/entity" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="http://fogid.net/o/to-date">
            <label xml:lang="ru">кон.дата</label>
            <domain rdf:resource="http://fogid.net/o/entity" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="http://fogid.net/o/learner">
            <label xml:lang="ru">ученик</label>
            <inverse-label xml:lang="ru">образование в</inverse-label>
            <domain rdf:resource="http://fogid.net/o/learn" />
            <range rdf:resource="http://fogid.net/o/person" />
          </ObjectProperty>
          <ObjectProperty rdf:about="http://fogid.net/o/learning-org">
            <label xml:lang="ru">в учебном заведении</label>
            <inverse-label xml:lang="ru">учащийся</inverse-label>
            <domain rdf:resource="http://fogid.net/o/learn" />
            <range rdf:resource="http://fogid.net/o/org-sys" />
          </ObjectProperty>
        </Ontology>
        """;

    private sealed class RelationStore(
        IEnumerable<ResourceHead> heads,
        IEnumerable<TripleRow> triples) : IProjectRdfStore, IProjectSearchStore
    {
        private readonly Dictionary<string, ResourceHead> _heads =
            heads.ToDictionary(head => head.ResourceId, StringComparer.Ordinal);
        private readonly TripleRow[] _triples = triples.ToArray();

        public ValueTask<ResourceHead?> GetResourceHeadAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                _heads.TryGetValue(resourceId, out ResourceHead? head) ? head : null);
        }

        public async IAsyncEnumerable<TripleRow> FindAsync(
            TriplePattern pattern,
            IReadOnlySet<string> allowedCassetteIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            foreach (TripleRow triple in _triples)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!allowedCassetteIds.Contains(triple.SourceCassetteId) ||
                    pattern.Subject is not null && !string.Equals(
                        pattern.Subject,
                        triple.Subject,
                        StringComparison.Ordinal) ||
                    pattern.Predicate is not null && !string.Equals(
                        pattern.Predicate,
                        triple.Predicate,
                        StringComparison.Ordinal) ||
                    pattern.ObjectKind is not null && pattern.ObjectKind != triple.ObjectKind ||
                    pattern.ObjectValue is not null && !string.Equals(
                        pattern.ObjectValue,
                        triple.ObjectValue,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                yield return triple;
            }
        }

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

        public Task RebuildAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
