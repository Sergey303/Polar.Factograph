# Architecture

## Product boundary

Polar.Factograph is a project-oriented web application. A project declares an ontology, users and roles, a set of compatible cassettes, and a project-level Polar.DB index.

The initial version does not replace or migrate cassette data. Existing Fog/XML files and cassette directories remain the source of truth.

## Data flow

```text
project.json
  -> enabled cassettes
  -> cassette meta Fog and additional Fog documents
  -> streaming XML canonicalization
  -> global delete/substitute resolution
  -> latest definition selection by mT
  -> current RDF records with provenance
  -> logical resource heads and RDF triples
  -> resource/triple/search physical rows
  -> Polar.DB.Typed generation writer
  -> atomic project index generation
  -> Polar.DB.Typed RDF/search store
  -> storage ports for RDF, portraits, and search
  -> ontology-aware application models
  -> Minimal API
  -> web client
```

## Main invariants

1. The indexed unit is the complete RDF cloud of a project, not one cassette.
2. Every materialized record, triple, and search row preserves its source cassette; records and triples also preserve their Fog source.
3. The Polar.DB index is derived data and can be deleted and rebuilt from Fog/XML.
4. Reads are performed over the unified current cloud and then filtered by project cassette permissions.
5. Writes are routed to a cassette and writable Fog allowed for the authenticated user.
6. Existing identifiers, namespaces, `owner`, `prefix`, `counter`, `mT`, `xml:lang`, `rdf:resource`, `delete`, `substitute`, and `iiss://` behavior are preserved.
7. A failed index update must never invalidate a successfully written Fog file; the index is repaired from the source.
8. The compatibility pipeline reopens and streams Fog files for each analysis pass instead of holding the complete project cloud in memory.
9. A rebuilt index becomes visible only after every resource, triple, name-search row, and word-search row has been written and indexed successfully.
10. Application services depend on logical storage ports and do not know the concrete `DbSet<T>` layout.
11. Ontology presentation never replaces raw RDF identifiers or values; it adds display labels and ordering while preserving fallbacks.
12. A read-store instance is bound to one immutable completed generation and never observes a partially rebuilt generation.

## Compatibility pipeline

The legacy resolver requires three logical passes:

1. collect deletes, substitutions, and duplicate identifier candidates;
2. select the first definition having the maximum `mT` for every duplicated identifier;
3. emit current resources, omit deleted/substituted subjects, and rewrite object references through closed substitution chains.

A direct reference to a deleted identifier remains a dangling reference, matching the existing `UpiAdapter` behavior. A substitution cycle is rejected explicitly rather than causing unbounded recursion.

The synthetic resource `cassetterootcollection` is emitted only when no current source definition exists. This gives old collection memberships a root while preventing duplicate current identifiers.

## Read-side application flow

### Resource portrait

`ProjectResourcePortraitService` reads a current resource head, validates cassette access before querying triples, and returns:

- literal fields;
- direct RDF links;
- inverse RDF links;
- resource type;
- source record, cassette, Fog path, and modification time.

`OntologyResourcePortraitPresenter` then adds:

- localized type and property labels;
- inverse labels for incoming relations;
- translated enumeration values;
- ontology-priority property order;
- stable raw-value fallbacks for unknown ontology terms.

### Search

The legacy `UpiAdapter` behavior is represented by two materialized indexes:

- name search: `name` and `alias` literals;
- word search: `name`, `alias`, `description`, and `doc-content` literals.

`LegacySearchIndexProjector` creates exact normalized keys during index rebuild:

- all prefixes of the complete normalized name phrase;
- all prefixes of each normalized name word;
- one exact row for each distinct normalized searchable word.

`IProjectSearchStore` exposes exact-key queries suitable for `DbSet<T>` external indexes. `ProjectResourceSearchService` performs visibility checks, ranking, language-aware display-name selection, type enrichment, and bounded result enrichment.

## Layers

- `Polar.Factograph.Domain` — project configuration and stable contracts.
- `Polar.Factograph.Application` — configuration loading, validation, authorization boundaries, portraits, ontology presentation, search ranking, and future write use cases.
- `Polar.Factograph.Fog` — compatible cassette discovery, streaming Fog reading, canonicalization, revision resolution, future writing, and document path resolution.
- `Polar.Factograph.Storage` — logical RDF/search contracts, physical rows, atomic generation lifecycle, concrete `Polar.DB.Typed.DbSet<T>` writer, and concrete RDF/search store.
- `Polar.Factograph.Api` — Minimal API host.
- `web` — future React/TypeScript client.

## Polar.DB source dependency

Polar.Factograph uses the existing `Polar.DB.Typed` project directly and does not copy or fork `DbSet<T>`.

The solution-level external reference is:

```text
../../Polar.DB/src/Polar.DB.Typed/Polar.DB.Typed.csproj
```

The Storage project reaches that same checkout through its project-relative path. CI reads the exact Polar.DB commit from `eng/PolarDb.version`, fetches that commit into the external path, and then restores the combined solution. This keeps CI reproducible while retaining a normal sibling-repository workflow for local development.

## Storage model

The project index should expose at least these logical sets:

- source files and their fingerprints;
- all source records, including delete and substitute operations;
- resolved substitutions;
- current resource heads;
- current triples;
- name-search keys;
- normalized word-search keys;
- document locations.

The current triple and resource-head representations carry provenance so that diagnostics, rights filtering, and write routing remain possible. Search rows carry the source cassette required for filtering before results are returned.

### Logical and physical rows

The Fog pipeline produces logical `ResourceHead` and `TripleRow` values. Separate mappings convert them to `PolarDbResourceHeadRow` and `PolarDbTripleRow`. Search projection produces `PolarDbNameSearchRow` and `PolarDbWordSearchRow` directly from current literal triples.

Physical rows intentionally use only CLR types supported automatically by the current `Polar.DB.Typed.DbSet` schema builder:

- `int`;
- `long`;
- `Guid`;
- `string`;
- `bool`.

`DateTimeOffset` is stored as UTC ticks. Nullable language and datatype values use an empty physical string and are restored to `null` in the logical model. The RDF object enum is stored as an integer and validated while reading.

Because a `DbSet` external index addresses one field, exact compound lookups use collision-free length-prefixed synthetic fields:

- `SubjectPredicateKey` for `(subject, predicate)`;
- `PredicateObjectKey` for `(predicate, object kind, object value)`.

The physical sets are:

```text
resource-heads
  primary key: ResourceId
  external key: SourceCassetteId

triples
  primary key: TripleId
  external keys:
    Subject
    Predicate
    ObjectValue
    SourceCassetteId
    SubjectPredicateKey
    PredicateObjectKey

name-search
  primary key: SearchRowId
  external keys:
    SearchKey
    ResourceId
    SourceCassetteId

word-search
  primary key: SearchRowId
  external keys:
    Word
    ResourceId
    SourceCassetteId
```

### Atomic generations

A rebuild starts in:

```text
{indexRoot}/generation-{guid}.building/
```

`PolarDbTypedIndexGenerationWriter` opens four existing `DbSet<T>` instances in that staging directory. It appends the projected rows and, during commit, forces every declared external index to build before closing the sets.

After all four sets and their indexes are complete, the directory is renamed to:

```text
{indexRoot}/generation-{guid}/
```

Only then is the `CURRENT` pointer atomically replaced. Readers continue using the preceding generation until that final switch. An aborted or disposed incomplete generation deletes only its `.building` directory. Previously completed generations remain available for rollback and later cleanup.

`ProjectIndexRebuilder` enforces this sequence through `IProjectIndexGenerationWriter`: write resource heads, triples, name-search rows, and word-search rows; commit after the full stream succeeds; abort on any exception.

### Completed-generation reads

`PolarDbTypedProjectStore.OpenCurrent` resolves `CURRENT`, verifies that the completed generation exists, and opens the same four physical sets read-only by convention. It implements:

- primary-key resource-head lookup;
- indexed triple lookup by subject, predicate, object value, subject+predicate, and predicate+object;
- exact name-prefix lookup;
- name lookup by resource;
- exact word lookup;
- cassette filtering before logical rows leave Storage.

The store remains bound to the generation path captured during opening. A later rebuild produces a new store instance rather than mutating readers that may already be serving requests.

## Write transaction

1. Resolve the user's project and cassette permissions.
2. Select the target cassette and writable Fog.
3. Build a complete compatible XML definition.
4. Write a temporary Fog file, flush it, parse it, and atomically replace the original.
5. Update only affected resources and search rows in the project index.
6. If the index update fails, mark the index dirty and rebuild it from Fog/XML.

## Delivery order

1. Configuration and contracts — complete.
2. Read-only Fog scanner and compatibility fixtures — complete.
3. Streaming record canonicalization and legacy revision resolution — complete.
4. Logical RDF projection, physical rows, and atomic generation lifecycle — complete.
5. Raw portraits, ontology catalog/presentation, compatible document path resolution, and indexed-search contracts — complete.
6. Concrete `Polar.DB.Typed.DbSet<T>` generation writer, RDF store, and search store — complete.
7. Read-only portrait/search/document API endpoints.
8. Legacy-equivalent React UX.
9. Compatible metadata editing and write routing.
10. Documents, uploads, collection management, delete, and substitute operations.
11. Administration, diagnostics, authentication, and incremental rebuilds.
12. Only after proven compatibility: discussion of a cassette v2 format.
