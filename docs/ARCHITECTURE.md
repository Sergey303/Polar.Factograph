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
  -> Polar.DB-compatible physical rows
  -> atomic project index generation
  -> Minimal API
  -> web client
```

## Main invariants

1. The indexed unit is the complete RDF cloud of a project, not one cassette.
2. Every materialized record and triple preserves its source cassette and Fog file.
3. The Polar.DB index is derived data and can be deleted and rebuilt from Fog/XML.
4. Reads are performed over the unified current cloud.
5. Writes are routed to a cassette and writable Fog allowed for the authenticated user.
6. Existing identifiers, namespaces, `owner`, `prefix`, `counter`, `mT`, `xml:lang`, `rdf:resource`, `delete`, `substitute`, and `iiss://` behavior are preserved.
7. A failed index update must never invalidate a successfully written Fog file; the index is repaired from the source.
8. The compatibility pipeline reopens and streams Fog files for each analysis pass instead of holding the complete project cloud in memory.
9. A rebuilt index becomes visible only after every resource and triple has been written successfully.

## Compatibility pipeline

The legacy resolver requires three logical passes:

1. collect deletes, substitutions, and duplicate identifier candidates;
2. select the first definition having the maximum `mT` for every duplicated identifier;
3. emit current resources, omit deleted/substituted subjects, and rewrite object references through closed substitution chains.

A direct reference to a deleted identifier remains a dangling reference, matching the existing `UpiAdapter` behavior. A substitution cycle is rejected explicitly rather than causing unbounded recursion.

The synthetic resource `cassetterootcollection` is emitted only when no current source definition exists. This gives old collection memberships a root while preventing duplicate current identifiers.

## Layers

- `Polar.Factograph.Domain` — project configuration and stable contracts.
- `Polar.Factograph.Application` — configuration loading, validation, authorization, and use cases.
- `Polar.Factograph.Fog` — compatible cassette discovery, streaming Fog reading, canonicalization, revision resolution, future writing, and file path resolution.
- `Polar.Factograph.Storage` — logical RDF rows, Polar.DB-compatible physical rows, index-generation lifecycle, and the future concrete DbSet writer.
- `Polar.Factograph.Api` — Minimal API host.
- `web` — future React/TypeScript client.

## Storage model

The project index should expose at least these logical sets:

- source files and their fingerprints;
- all source records, including delete and substitute operations;
- resolved substitutions;
- current resource heads;
- current triples;
- search terms;
- document locations.

The current triple representation carries provenance so that diagnostics, rights filtering, and write routing remain possible.

### Logical and physical rows

The Fog pipeline produces logical `ResourceHead` and `TripleRow` values. A separate mapping converts them to `PolarDbResourceHeadRow` and `PolarDbTripleRow`.

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

The intended physical sets are:

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
```

### Atomic generations

A rebuild starts in:

```text
{indexRoot}/generation-{guid}.building/
```

After all physical rows are written and their DbSet indexes are built, the directory is renamed to:

```text
{indexRoot}/generation-{guid}/
```

Only then is the `CURRENT` pointer atomically replaced. Readers continue using the preceding generation until that final switch. An aborted or disposed incomplete generation deletes only its `.building` directory. Previously completed generations remain available for rollback and later cleanup.

`ProjectIndexRebuilder` enforces this sequence through `IProjectIndexGenerationWriter`: write resources and triples, commit after the full stream succeeds, and abort on any exception.

## Write transaction

1. Resolve the user's project and cassette permissions.
2. Select the target cassette and writable Fog.
3. Build a complete compatible XML definition.
4. Write a temporary Fog file, flush it, parse it, and atomically replace the original.
5. Update only affected resources in the project index.
6. If the index update fails, mark the index dirty and rebuild it from Fog/XML.

## Delivery order

1. Configuration and contracts — complete.
2. Read-only Fog scanner and compatibility fixtures — complete.
3. Streaming record canonicalization and legacy revision resolution — complete.
4. Logical RDF projection, Polar.DB-compatible physical rows, and atomic generation lifecycle — complete.
5. Concrete `Polar.DB.Typed.DbSet` generation writer and query adapter.
6. Read-only search/portrait API and legacy-equivalent UX.
7. Compatible metadata editing.
8. Documents, previews, collection management, delete, and substitute.
9. Administration, diagnostics, authentication, and incremental rebuilds.
10. Only after proven compatibility: discussion of a cassette v2 format.
