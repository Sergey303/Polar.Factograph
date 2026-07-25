# Project configuration

`project.json` is application configuration, not cassette data. It declares which existing cassettes form one RDF cloud and how users may read or write them.

## Separation of concerns

- User credentials and password hashes belong to the authentication store.
- Project membership, roles, cassette permissions, and write routing belong to `project.json`.
- Fog/XML files remain authoritative for factographic records.
- Polar.DB files remain rebuildable derived indexes.

## Effective access calculation

`ProjectAccessService` calculates one immutable access snapshot for a project member.

1. Project rights from all member roles are combined.
2. Cassette rights from the wildcard `"*"` and the exact cassette id are combined across all roles.
3. `defaultAccess: "read"` grants cassette `read` only to a member who also has the project right `read`.
4. A member wildcard override replaces the calculated rights for every cassette.
5. A member exact cassette override replaces the wildcard override and all role/default rights for that cassette.
6. A disabled cassette has no effective rights.
7. When `allowWrite` is `false`, all write rights are removed even if a role declared them.
8. Unknown users receive no project or cassette access.

This gives search and portrait services an explicit set of readable cassette identifiers. Source cassette identifiers are retained in all index rows so visibility is enforced before returning data.

## Project rights

Supported project rights are:

- `read`;
- `search`;
- `export`;
- `manageUsers`;
- `manageCassettes`;
- `rebuildIndex`.

Project rights apply to the complete configured project, but they do not by themselves expose a cassette whose effective cassette rights lack `read`.

## Cassette rights

Supported cassette rights are:

- `read`;
- `writeMetadata`;
- `addDocuments`;
- `replaceDocuments`;
- `delete`;
- `substitute`;
- `manage`.

The wildcard key `"*"` is permitted in role rights and member overrides. All other keys must identify a configured cassette.

An empty exact member override deliberately removes all rights for that cassette:

```json
{
  "userId": "restricted-user",
  "roles": ["editor"],
  "cassetteOverrides": {
    "archive-private": []
  }
}
```

## Write routing

`writeRouting.defaultCassetteByRole` selects the first usable default cassette according to the order of the member's roles.

A configured route is valid only when:

- the role exists;
- the cassette exists and is enabled;
- the cassette has `allowWrite: true`;
- the role declares at least one write right for that cassette or through `"*"`.

The effective member snapshot may still have no default write cassette when a member override removes the required write rights.

Before modifying Fog/XML, the server must additionally verify that the selected source has a compatible writable Fog with both `prefix` and `counter`.

## Validation

Configuration loading rejects:

- unsupported schema versions;
- missing project, ontology, index, cassette, role, or member identifiers;
- duplicate cassette ids or member user ids;
- unknown or duplicate rights;
- unknown role and cassette references;
- unsupported `defaultAccess` values;
- routes to disabled or read-only cassettes;
- malformed JSON, reported as an `InvalidDataException` suitable for API error handling.

See `examples/project.sample.json` and `examples/syp.project.json`.