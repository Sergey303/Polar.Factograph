# Polar.Factograph

Polar.Factograph is a modern web factographic system built around a project-wide RDF cloud assembled from compatible Fog/XML cassettes and indexed with Polar.DB.

The first product version preserves the existing cassette directory structure, Fog/XML data, `iiss://` identifiers, revision rules, document locations, ontology presentation, search behavior, and editing semantics used by the earlier Factograph and cassette-management applications.

## Core principles

- A **project** combines an ontology, users, access rules, and multiple cassettes.
- All enabled cassette Fog files are materialized into one current RDF cloud.
- Polar.DB is a rebuildable project-level index; Fog/XML remains the source of truth.
- Reads operate over the unified current cloud and are filtered by effective cassette access.
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
- local application-cookie authentication with reloadable JSON users and devices;
- one numbered writable cassette Fog assigned to each registered user;
- atomic append-only resource, delete, substitute, and collection membership mutations;
- ontology-aware write validation for class, domain, property kind, enumeration, target existence, and object range;
- atomic streamed document original upload and replacement with independent cassette rights;
- durable preview-generation requests created after document add and replacement;
- atomic preview queue claiming, retry scheduling, stale-lease recovery, dead-letter isolation, and administrative diagnostics;
- a configurable hosted preview worker with safe external-process invocation, source-version checks, fair cassette processing, runtime health, and failure counters;
- a React/TypeScript workspace for search, portraits, relation and collection navigation, document replacement, and ontology-driven resource revisions;
- an authorized write-schema route exposing only localized classes, allowed properties, value kinds, ranges, and enumeration choices;
- a two-stage document intake workflow that preserves a committed original while retrying only RDF metadata;
- an authorized React administration dashboard for safe index status, preview health, on-demand Fog statistics, and confirmed full rebuilds;
- index runtime diagnostics for `DIRTY`, `CURRENT`, completed generations, and interrupted builds;
- shared mutation orchestration with serialized rebuild, `DIRTY` recovery, and stale-read protection;
- integration tests against unchanged `SypCassete_current.fog` and real Polar.DB.Typed persistence.

Incremental index refresh, a deployment-supplied PDF/image renderer executable, remaining production hardening, and final browser/E2E coverage remain focused follow-up increments.

## Polar.DB source dependency

The solution uses the existing `Polar.DB.Typed` project from `Sergey303/Polar.DB`; no `DbSet<T>` implementation is copied into this repository.

```text
../../Polar.DB/src/Polar.DB.Typed/Polar.DB.Typed.csproj
```

CI checks out the exact Polar.DB commit recorded in `eng/PolarDb.version`. This keeps CI reproducible while retaining a normal sibling-repository workflow for local development.

## Physical index layout

```text
resource-heads
triples
name-search
word-search
```

All four sets belong to one atomic generation. Readers switch only after all rows and external indexes are complete.

## Windows launch shortcuts

Before starting, place these files in the repository root:

```text
factograph.project.json
ontology.xml
```

The configured cassette directories and the sibling Polar.DB repository must also exist.

### Development

Run or double-click:

```text
1-run-dev.cmd
```

The shortcut:

1. verifies the project configuration and ontology;
2. installs React dependencies when `node_modules` is absent;
3. builds React into the API `wwwroot`;
4. copies a clean runtime `appsettings.json` under ignored `project-data`;
5. starts `dotnet run -c Debug` at `http://localhost:5000`.

It deliberately does not load `src/Polar.Factograph.Api/appsettings.json`, so local experimental edits or duplicate JSON keys in that source file do not break this launch path.

### Release publish and launch

Run with an explicit destination:

```text
2-publish-run-release.cmd "D:\Publish\Polar.Factograph"
```

Without an argument, the default destination is:

```text
publish\Polar.Factograph
```

The shortcut performs `dotnet publish -c Release`, writes the clean runtime settings into the published directory, and starts the published API at:

```text
https://localhost:5001
```

Production-mode cookies require HTTPS. When the local certificate is missing, create and trust it once:

```powershell
dotnet dev-certs https --trust
```

Both shortcuts keep `identity.json`, Data Protection keys, indexes, and runtime data under the repository-level ignored `project-data` directory. They select the `editor` role and `syp-cassette-small` as the default writable cassette through environment overrides.

## Direct command-line development

For other environments, restore and run the API project explicitly:

```bash
dotnet restore Polar.Factograph.slnx
dotnet run --project src/Polar.Factograph.Api
```

Useful routes:

```text
GET  /api/system/health
GET  /api/auth/session
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
GET  /api/project
GET  /api/ontology/write-schema
GET  /api/resources/portrait?id={rdf-id}
POST /api/resources
POST /api/resources/delete
POST /api/resources/substitute
GET  /api/collections/items?id={collection-id}
POST /api/collections/items
POST /api/collections/items/remove
GET  /api/search/names?q={text}
GET  /api/search/words?q={text}
GET  /api/documents/location?uri={iiss-uri}
GET  /api/documents/content?uri={iiss-uri}&variant={variant}
POST /api/documents/files?fileName={name.ext}&cassetteId={optional-id}
PUT  /api/documents/files?uri={iiss-uri}&fileName={name.ext}
GET  /api/admin/project/sources
GET  /api/admin/project/materialization-summary
GET  /api/admin/index/status
POST /api/admin/index/rebuild
GET  /api/admin/previews/status
```

See [API](docs/API.md), [authentication](docs/AUTHENTICATION.md), [architecture](docs/ARCHITECTURE.md), [project configuration](docs/PROJECT_CONFIGURATION.md), [Fog writing](docs/WRITING.md), [document writing](docs/DOCUMENT_WRITING.md), [collections](docs/COLLECTIONS.md), [code structure](docs/CODE_STRUCTURE.md), [UX](docs/UX.md), [web workspace](src/Polar.Factograph.Web/README.md), and [legacy sources](docs/LEGACY_SOURCES.md).
