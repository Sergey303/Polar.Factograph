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
  -> React/TypeScript public and editorial workspace
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
13. Anonymous public access is represented by a synthetic project member that is forcibly assigned the validated read-only `viewer` role and can never resolve a writable Fog.
14. Public entity URLs are real server paths. Hash routes remain migration input only and are replaced in browser history.

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

The browser groups the returned search results by ontology type, shows counts, and stores the selected type in the `type` query parameter. These are result-set facets over the current bounded API response, not yet complete archive-wide aggregate counts. A future server facet contract must calculate totals before the result limit and must not scan all RDF triples per request.

### Duplicate prevention

Before a new entity is written, the editor checks entered short textual properties against existing visible resources:

- exact `(predicate, literal kind, value)` matches use the existing `PredicateObjectKey` index;
- names and aliases additionally use transliteration and keyboard-layout variants through the name-search index;
- candidates are restricted to the selected ontology type or its descendants;
- numeric, date, Boolean, enumeration, empty, and oversized values are excluded;
- the user may reuse an existing entity or explicitly continue creating a new one.

No additional normalized-literal index is created for all RDF properties until its storage and rebuild cost is measured on representative projects.

## Public and editorial delivery

The same API and project index serve two access modes:

- an anonymous public visitor receives a synthetic project member id and the validated `viewer` access snapshot;
- an authenticated viewer receives their stored viewer role;
- an authenticated editor receives the role selected by `EditorLogins` and a dedicated writable Fog;
- administration remains available only through explicit project rights.

Startup fails when public reading is enabled but the effective public snapshot lacks `read/search`, contains any other project right, contains any cassette right other than `read`, or resolves a default writable cassette. The Fog resolver independently rejects the public user before its compatibility fallback for legacy static users.

The React application uses real addressable routes:

```text
/search?q=...&type=...
/entity/new
/resource/{encodedResourceId}
/resource/{encodedResourceId}/edit
/resource/{encodedResourceId}/relations
/resource/{encodedResourceId}/documents/new
```

The API host serves the SPA fallback for these paths. Client navigation uses `history.pushState`; browser back/forward uses `popstate`. Old hash URLs are migrated with `replaceState`.

Legacy SORAN1957-style links are preserved by a server redirect:

```text
/default.aspx?id={legacyResourceId}
  -> 301 /resource/{encodedResourceId}
```

A request without `id` is redirected temporarily to `/search`. The redirect preserves `PathBase`, so the application may be hosted below the domain root.

For an exact public resource route, `DynamicBaseUrlMiddleware` buffers the SPA HTML, reads the authorized semantic page, resolves the canonical resource identifier, and injects the title, description, canonical link, Open Graph fields, and Twitter summary fields before the response leaves the server. This supports crawlers and social preview bots that do not execute React. The dynamic response removes static-file ETag and Last-Modified validators and uses `private, no-store`, so metadata cannot become stale or leak between access contexts. A metadata read failure is logged and falls back to the normal generic SPA document instead of breaking the page.

After React loads, `ResourceDocumentMetadata` applies the same title, description, canonical URL, and Open Graph values in the browser. The client layer therefore mirrors rather than replaces the server metadata contract.

## Layers

- `Polar.Factograph.Domain` — project configuration and stable contracts.
- `Polar.Factograph.Application` — configuration loading, validation, authorization boundaries, portraits, ontology presentation, search ranking, and write use cases.
- `Polar.Factograph.Fog` — compatible cassette discovery, streaming Fog reading, canonicalization, revision resolution, writing, and document path resolution.
- `Polar.Factograph.Storage` — logical RDF/search contracts, physical rows, atomic generation lifecycle, concrete `Polar.DB.Typed.DbSet<T>` writer, and concrete RDF/search store.
- `Polar.Factograph.Api` — Minimal API host, authentication, public access boundary, compatibility redirects, server HTML metadata, and runtime coordination.
- `Polar.Factograph.Web` — React/TypeScript public catalogue and permission-driven editorial workspace.

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

Completed foundations:

1. project configuration, validation, roles, and cassette access;
2. compatible Fog scanning, canonicalization, delete/substitute resolution, and revision selection;
3. logical RDF projection, physical Polar.DB.Typed rows, indexed search, and atomic generations;
4. ontology-aware portraits, semantic linked sections, timeline presentation, and document resolution;
5. metadata, relation, collection, and document write coordination with per-editor Fog routing;
6. local authentication, device sessions, editor allow-list reconciliation, and anonymous viewer boundary;
7. addressable React routes, duplicate warnings, public resource metadata in server and browser HTML, legacy URL redirects, and bounded type facets.

Next delivery priorities:

1. complete server-side search facets and pagination before the result limit;
2. per-fact temporal/provenance/uncertainty editorial models;
3. duplicate merge, substitution preview, redirects, and reversible editorial operations;
4. publication states, moderation, audit history, and rollback;
5. photo viewer, identification workflow, rights, embargo, and curated exhibitions;
6. only after proven compatibility: discussion of a cassette v2 format.
