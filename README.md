# Polar.Factograph

Polar.Factograph is a modern web factographic system built around a project-wide RDF cloud assembled from compatible Fog/XML cassettes and indexed with Polar.DB.

The first product version preserves the existing cassette directory structure, Fog/XML data, `iiss://` identifiers, revision rules, and editing semantics used by the earlier Factograph and cassette-management applications.

## Core principles

- A **project** combines an ontology, users, access rules, and multiple cassettes.
- All enabled cassette Fog files are materialized into one current RDF cloud.
- Polar.DB is a rebuildable project-level index; Fog/XML remains the source of truth.
- Reads operate over the unified cloud; writes are routed to a cassette and writable Fog allowed for the current user.
- The web UX preserves the established workflows: search, resource portrait, direct and inverse links, collection tree, documents, previews, and editing.

## Repository state

The current compatibility increment contains:

- project JSON configuration and project-relative path resolution;
- compatible cassette/Fog source discovery;
- streaming Fog record parsing and canonicalization;
- project-wide `delete`, `substitute`, and latest-`mT` resolution;
- substitution rewriting for RDF object references;
- source provenance for every current resource;
- synthetic `cassetterootcollection` when the sources do not define one;
- Minimal API diagnostics for sources and materialization statistics;
- integration tests against the unchanged `SypCassete_current.fog` fixture.

The Polar.DB-backed index, search/portrait API, editor, authentication, and React client remain focused follow-up increments.

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

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/UX.md](docs/UX.md), and [docs/LEGACY_SOURCES.md](docs/LEGACY_SOURCES.md).
