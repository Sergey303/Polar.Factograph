# Architecture

## Product boundary

Polar.Factograph is a project-oriented web application. A project declares an ontology, users and roles, a set of compatible cassettes, and a project-level Polar.DB index.

The initial version does not replace or migrate cassette data. Existing Fog/XML files and cassette directories remain the source of truth.

## Data flow

```text
project.json
  -> enabled cassettes
  -> cassette meta Fog and additional Fog documents
  -> XML canonicalization
  -> delete/substitute resolution
  -> latest definition selection by mT
  -> current RDF triples with provenance
  -> project-level Polar.DB indexes
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

## Layers

- `Polar.Factograph.Domain` — project configuration and stable contracts.
- `Polar.Factograph.Application` — configuration loading, validation, authorization, and use cases.
- `Polar.Factograph.Fog` — compatible cassette discovery, Fog reading/writing, canonicalization, revision resolution, and file path resolution.
- `Polar.Factograph.Storage` — project RDF store contracts and the future Polar.DB implementation.
- `Polar.Factograph.Api` — Minimal API host.
- `web` — future React/TypeScript client.

## Storage model

The future Polar.DB implementation should expose at least these logical sets:

- source files and their fingerprints;
- all source records, including delete and substitute operations;
- resolved substitutions;
- current resource heads;
- current triples;
- search terms;
- document locations.

The current triple representation must carry provenance so that diagnostics, rights filtering, and write routing remain possible.

## Write transaction

1. Resolve the user's project and cassette permissions.
2. Select the target cassette and writable Fog.
3. Build a complete compatible XML definition.
4. Write a temporary Fog file, flush it, parse it, and atomically replace the original.
5. Update only affected resources in the project index.
6. If the index update fails, mark the index dirty and rebuild it from Fog/XML.

## Delivery order

1. Configuration and contracts.
2. Read-only Fog scanner and compatibility fixtures.
3. Full project materialization into Polar.DB.
4. Read-only API and legacy-equivalent search/portrait UX.
5. Compatible metadata editing.
6. Documents, previews, collection management, delete, and substitute.
7. Administration, diagnostics, authentication, and incremental rebuilds.
8. Only after proven compatibility: discussion of a cassette v2 format.
