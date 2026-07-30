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
15. The primary public relation unit is one authorized relation entry. A complex relation node appears once and contains all authorized entity members with ontology role labels. Specialized arrays and the flattened `Links` stream remain compatibility views only.
16. Relation groups and labels come from ontology terms. Media is detected by an actual `iiss://` attachment, not by a hardcoded domain class such as `photo-doc`.
17. Timeline virtualization uses the page scroll. Offscreen chunks retain measured placeholders, so no nested scroll container is introduced.
18. `icon` is an optional derived cassette preview. Its absence falls back to `small` and does not require an RDF, Fog, or identifier migration.

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

The HTTP presentation applies a separate provenance policy. A viewer receives no source details, an editor of the source cassette receives only its logical id, and an administrator may receive full provenance.

### Semantic relations and time

`SemanticResourcePageService` publishes `Entries` as the primary ontology-driven relation contract. `SemanticRelationEntryCollector` creates:

- one entry for each complex relation node;
- all authorized direct and inverse entity members of that node;
- the ontology label of each member role;
- relation type and group metadata;
- one effective date and optional media attachment.

A relation node is deduplicated by its stable resource id. Relation ids observed while constructing specialized legacy views are retained by `SemanticResourceGraph` and fed back into the entry collector, so indirect collection/media views are not lost.

Ordinary direct or inverse properties become one-member entries. The specialized `Photos`, `Participants`, `Organizations`, `Collections`, `RelatedResources`, and flattened `Links` fields remain available for older clients. They are not the preferred source for new public rendering.

For timeline ordering:

1. `from-date` is used as the beginning of an interval and `to-date` is retained for display;
2. otherwise the earliest value among ontology properties whose range is `date` is used;
3. if the relation has no date and one of its members carries media, the earliest media-content date, including a shooting-date property, is used;
4. undated entries follow all dated entries.

The React page shows the timeline by default. Unchecking `Хронология` renders one block per selected relation group. Each block owns its list, table, or media-grid view. Grouped blocks use previous/next portions; the timeline uses page-scroll chunk virtualization.

The browser prefers `Entries`. For compatibility with a server that exposes older flattened links, missing compatible links are converted to one-member entries without duplicating relation ids already represented by a whole entry.

### Search

The legacy `UpiAdapter` behavior is represented by two materialized indexes:

- name search: `name` and `alias` literals;
- word search: `name`, `alias`, `description`, and `doc-content` literals.

`LegacySearchIndexProjector` creates exact normalized keys during index rebuild:

- all prefixes of the complete normalized name phrase;
- all prefixes of each normalized name word;
- one exact row for each distinct normalized searchable word.

`IProjectSearchStore` exposes exact-key queries suitable for `DbSet<T>` external indexes. `ProjectResourceSearchService` performs visibility checks, ranking, language-aware display-name selection, type enrichment, and bounded result enrichment.

The browser groups the returned search results by ontology type, shows counts, and stores the selected bounded-result facet in the `type` query parameter. These counts describe only the current API result set, not the entire archive.

Ontology class search is separate from ordinary entity ranking. An exact entity named `Организация` remains the first ordinary result when its name rank wins; a distinct category action may open all instances of the ontology class `Организация`. Class suggestions match localized labels and identifiers. Category retrieval expands ontology descendants, then queries exact `(rdf:type, IRI, classId)` values through the existing `PredicateObjectKey` index. It does not scan unrelated RDF triples. The first implementation resolves and sorts all matching visible summaries before applying offset/limit; a dedicated materialized type/name index may replace this when representative archives show that repeated large-category paging needs it.

Addressable category routes use:

```text
/search?q=Организация&class={encodedClassId}&offset=50
```

The ordinary bounded-result facet continues to use `type`; `class` denotes a server category query and takes precedence when both appear.

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
/search?q=...&class=...&offset=...
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

For an exact public resource route, `DynamicBaseUrlMiddleware` buffers the SPA HTML, reads the authorized semantic page, resolves the canonical resource identifier, and injects the title, description, canonical link, Open Graph fields, and Twitter fields before the response leaves the server. When the entity has an authorized media attachment with an image original or preview, the metadata uses the stable `/api/documents/image?uri=...` endpoint. That endpoint selects `normal`, `medium`, `small`, `icon`, then an image original. A non-image original without an image preview is not advertised as `og:image`.

The dynamic response removes static-file ETag and Last-Modified validators and uses `private, no-store`, so metadata cannot become stale or leak between access contexts. A metadata read failure is logged and falls back to the generic SPA document instead of breaking the page.

After React loads, `ResourceDocumentMetadata` mirrors title, description, canonical URL, and server-verified image metadata. It removes an image left by the preceding SPA route and never invents an unverified Open Graph image in the browser.

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
