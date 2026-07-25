# Polar.Factograph

Polar.Factograph is a modern web factographic system built around a project-wide RDF cloud assembled from compatible Fog/XML cassettes and indexed with Polar.DB.

The first product version preserves the existing cassette directory structure, Fog/XML data, `iiss://` identifiers, revision rules, document locations, ontology presentation, search behavior, and editing semantics used by the earlier Factograph and cassette-management applications.

## Core principles

- A **project** combines an ontology, users, access rules, and multiple cassettes.
- All enabled cassette Fog files are materialized into one current RDF cloud.
- Polar.DB is a rebuildable project-level index; Fog/XML remains the source of truth.
- Reads operate over the unified cloud and are filtered by effective cassette access.
- Writes are routed to a cassette and writable Fog allowed for the current user.
- The web UX preserves the established workflows: search, resource portrait, direct and inverse links, collection tree, documents, previews, and editing.

## Repository state

The current compatibility increment contains:

- project JSON configuration, validation, and project-relative path resolution;
- deterministic role/member/cassette access calculation and default write routing;
- compatible cassette/Fog source discovery;
- streaming Fog record parsing and canonicalization;
- project-wide `delete`, `substitute`, and latest-`mT` resolution;
- substitution rewriting for RDF object references;
- source provenance for every current resource and triple;
- synthetic `cassetterootcollection` when the sources do not define one;
- deterministic projection into logical resource heads and RDF triples;
- four physical `Polar.DB.Typed.DbSet<T>` sets: resource heads, triples, name search, and word search;
- collision-free synthetic compound lookup keys;
- legacy-compatible materialization of name prefixes and searchable words;
- a concrete generation writer that builds all external indexes before commit;
- atomic generation directories and the `CURRENT` pointer;
- a concrete RDF/search store bound to one completed immutable generation;
- exact indexed lookup by resource, subject, predicate, object, compound RDF keys, name prefixes, and words;
- raw resource portraits with literal, direct, and inverse relations;
- an XML ontology catalog with inheritance, labels, inverse labels, priorities, domain/range, and enumeration values;
- ontology-aware portrait presentation with raw-value fallbacks;
- safe resolution of `iiss://` URIs to originals and three preview sizes;
- cassette visibility, language-aware display names, type enrichment, and bounded search results;
- Minimal API diagnostics for sources and materialization statistics;
- integration tests against the unchanged `SypCassete_current.fog` fixture and against the real `Polar.DB.Typed` persistence implementation.

Public portrait/search/document endpoints, authentication identity extraction, compatible editing, and the React client remain focused follow-up increments.

## Polar.DB source dependency

The solution uses the existing `Polar.DB.Typed` project from `Sergey303/Polar.DB`; no `DbSet<T>` implementation is copied into this repository.

The external project path introduced by the repository layout is:

```text
../../Polar.DB/src/Polar.DB.Typed/Polar.DB.Typed.csproj
```

`Polar.Factograph.Storage` references the same project from its own directory. CI checks out the exact Polar.DB commit recorded in:

```text
eng/PolarDb.version
```

This keeps source builds reproducible while allowing local development against the sibling Polar.DB checkout.

## Physical index layout

```text
resource-heads
triples
name-search
word-search
```

All four sets belong to one atomic generation. `PolarDbTypedIndexGenerationWriter` writes the rows, builds the declared external indexes, closes all sets, and only then switches `CURRENT`. `PolarDbTypedProjectStore` opens the completed generation and implements both RDF and search storage ports.

## Start

Place the Polar.DB repository at the external path shown above, then run:

```bash
dotnet restore Polar.Factograph.slnx
dotnet run --project src/Polar.Factograph.Api
```

Useful diagnostic endpoints:

```text
GET /api/system/health
GET /api/project
GET /api/project/sources
GET /api/project/materialization-summary
```

The configured compatibility project is stored in `examples/syp.project.json`.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/PROJECT_CONFIGURATION.md](docs/PROJECT_CONFIGURATION.md), [docs/UX.md](docs/UX.md), and [docs/LEGACY_SOURCES.md](docs/LEGACY_SOURCES.md).
