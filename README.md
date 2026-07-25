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

This initial scaffold contains the project configuration model, compatibility boundaries, storage contracts, Minimal API host, and architecture/UX documentation. The Fog reader, Polar.DB-backed index, editor, and React client are intentionally left for focused follow-up increments.

## Start

```bash
dotnet restore Polar.Factograph.slnx
dotnet run --project src/Polar.Factograph.Api
```

The sample project configuration is stored in `examples/project.sample.json`.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the architecture and [docs/UX.md](docs/UX.md) for the target user experience.
