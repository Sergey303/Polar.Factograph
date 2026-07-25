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
- four Polar.DB-compatible physical sets: resource heads, triples, name search, and word search;
- collision-free synthetic compound lookup keys;
- legacy-compatible materialization of name prefixes and searchable words;
- transactional project-index rebuild contracts covering all four physical sets;
- atomic generation directories and the `CURRENT` pointer;
- raw resource portraits with literal, direct, and inverse relations;
- an XML ontology catalog with inheritance, labels, inverse labels, priorities, domain/range, and enumeration values;
- ontology-aware portrait presentation with raw-value fallbacks;
- safe resolution of `iiss://` URIs to originals and three preview sizes;
- search storage contracts and Application ranking for names and words;
- cassette visibility, language-aware display names, type enrichment, and bounded search results;
- Minimal API diagnostics for sources and materialization statistics;
- integration tests against the unchanged `SypCassete_current.fog` fixture plus focused unit tests for compatibility and application rules.

The existing `Polar.DB.Typed.DbSet<T>` implementation is now packaged by the `Sergey303/Polar.DB` repository without copying or changing its storage code. Polar.Factograph still needs a released or otherwise restorable `Polar.DB.Typed` version before the concrete generation writer, RDF store, and search store can be connected in CI.

Public portrait/search/document endpoints, authentication, compatible editing, and the React client remain focused follow-up increments.

## Physical index layout

```text
resource-heads
triples
name-search
word-search
```

All four sets belong to one atomic generation. Readers switch to a rebuilt generation only after every set has been written successfully.

## Start

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