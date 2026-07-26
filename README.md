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

- validated project configuration, roles, members, cassette rights, and write routing;
- compatible cassette/Fog discovery and streaming record parsing;
- project-wide `delete`, `substitute`, and latest-`mT` resolution;
- deterministic resource, RDF-triple, name-search, and word-search projection;
- four concrete `Polar.DB.Typed.DbSet<T>` sets in one atomic generation;
- external RDF and search indexes built before `CURRENT` is switched;
- a concrete RDF/search store bound to one completed generation;
- raw resource portraits with direct and inverse relations;
- ontology catalog and ontology-aware presentation contracts;
- safe `iiss://` resolution plus authorized metadata and streamed original/preview content;
- authorized project overview, portrait, search, diagnostics, and index rebuild routes;
- production JWT identity plus a development-only configured identity;
- atomic append-only resource, delete, and substitute Fog mutations;
- shared mutation orchestration with serialized rebuild, `DIRTY` recovery, and stale-read protection;
- integration tests against unchanged `SypCassete_current.fog` and real Polar.DB.Typed persistence.

Document upload operations, collection mutation, ontology-aware edit validation, incremental index refresh, and the React client remain focused follow-up increments.

## Polar.DB source dependency

The solution uses the existing `Polar.DB.Typed` project from `Sergey303/Polar.DB`; no `DbSet<T>` implementation is copied into this repository.

```text
../../Polar.DB/src/Polar.DB.Typed/Polar.DB.Typed.csproj
```

CI checks out the exact Polar.DB commit recorded in `eng/PolarDb.version`. This keeps source builds reproducible while allowing local development against the sibling Polar.DB checkout.

## Physical index layout

```text
resource-heads
triples
name-search
word-search
```

All four sets belong to one atomic generation. Readers switch only after all rows and external indexes are complete.

## Start

Place the Polar.DB repository at the external path shown above, then run:

```bash
dotnet restore Polar.Factograph.slnx
dotnet run --project src/Polar.Factograph.Api
```

Development configuration selects `examples/syp.project.json` and its existing `admin` member. Production requests require an authenticated identity claim.

Useful routes:

```text
GET  /api/system/health
GET  /api/project
GET  /api/resources/portrait?id={rdf-id}
POST /api/resources
POST /api/resources/delete
POST /api/resources/substitute
GET  /api/search/names?q={text}
GET  /api/search/words?q={text}
GET  /api/documents/location?uri={iiss-uri}
GET  /api/documents/content?uri={iiss-uri}&variant={variant}
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
POST /api/admin/index/rebuild
```

See [API](docs/API.md), [architecture](docs/ARCHITECTURE.md), [project configuration](docs/PROJECT_CONFIGURATION.md), [Fog writing](docs/WRITING.md), [code structure](docs/CODE_STRUCTURE.md), [UX](docs/UX.md), and [legacy sources](docs/LEGACY_SOURCES.md).
