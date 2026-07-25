# Project configuration

`project.json` is application configuration, not cassette data. It declares which existing cassettes form one RDF cloud and how users may read or write them.

## Separation of concerns

- User credentials and password hashes belong to the authentication store.
- Project membership, roles, cassette permissions, and write routing belong to `project.json`.
- Fog/XML files remain authoritative for factographic records.
- Polar.DB files remain rebuildable derived indexes.

## Access levels

Project rights apply to the complete cloud, for example `read`, `search`, `export`, `manageUsers`, `manageCassettes`, and `rebuildIndex`.

Cassette rights apply to a physical source or write target, for example `read`, `writeMetadata`, `addDocuments`, `replaceDocuments`, `delete`, `substitute`, and `manage`.

The first compatible release may use a simpler rule: project members read the complete cloud, while cassette permissions primarily control writes. The data model still preserves source cassette identifiers so stricter read isolation can be added safely.

## Write routing

A user may have one default write cassette through a role and optional per-user overrides. The server must still verify that the cassette permits writes and that a compatible writable Fog with `prefix` and `counter` exists.

See `examples/project.sample.json`.
