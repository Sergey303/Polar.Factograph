# Project configuration

`project.json` is application configuration, not cassette data. It declares which existing cassettes form one RDF cloud and how users may read or write them.

## Cassette paths

The `cassettes` section has exactly two fields:

```json
"cassettes": {
  "items": [
    "D:/FactographProjects/archive-history",
    "D:/FactographProjects/archive-current"
  ],
  "write": "D:/FactographProjects/archive-current"
}
```

- `items` is a non-empty list of full cassette directory paths;
- `write` is one full path and must exactly match an entry in `items`;
- all listed cassettes are enabled and readable by default;
- the folder name becomes both the cassette id and display name;
- folder names must therefore be unique, including differences only in letter case;
- exactly one cassette is writable: the path in `write`.

There is no separate `enabled`, `defaultAccess`, `allowWrite`, or per-role write-routing configuration. To remove a cassette from the project, remove its path from `items`. To change the write target, change `write` to another path already present in `items`.

## Separation of concerns

- User credentials and password hashes belong to the authentication store.
- Project membership, roles, cassette permissions, and cassette paths belong to `project.json`.
- Fog/XML files remain authoritative for factographic records.
- Polar.DB files remain rebuildable derived indexes.

## Effective access calculation

`ProjectAccessService` calculates one immutable access snapshot for a project member.

1. Project rights from all member roles are combined.
2. Cassette rights from the wildcard `"*"` and the exact derived cassette id are combined across all roles.
3. Every configured cassette grants `read` only to a member who also has the project right `read`.
4. A member wildcard override replaces the calculated rights for every cassette.
5. A member exact cassette override replaces the wildcard override and all role/default rights for that cassette.
6. Write rights are retained only for the one cassette selected by `cassettes.write`.
7. Unknown users receive no project or cassette access.

The effective default write cassette is the single configured write cassette when the current member has at least one write right for it. A member override may still remove those rights, in which case that member has no default write target.

## Project rights

Supported project rights are:

- `read`;
- `search`;
- `export`;
- `manageUsers`;
- `manageCassettes`;
- `rebuildIndex`.

## Cassette rights

Supported cassette rights are:

- `read`;
- `writeMetadata`;
- `addDocuments`;
- `replaceDocuments`;
- `delete`;
- `substitute`;
- `manage`.

The wildcard key `"*"` is permitted in role rights and member overrides. All other keys must identify a cassette by the final folder name from its configured path.

An empty exact member override deliberately removes all rights for that cassette:

```json
{
  "userId": "restricted-user",
  "roles": ["editor"],
  "cassetteOverrides": {
    "archive-current": []
  }
}
```

Before modifying Fog/XML, the server additionally verifies that the selected source has a compatible writable Fog with both `prefix` and `counter`.

## Validation

Configuration loading rejects:

- unsupported schema versions;
- missing project, ontology, index, role, or member identifiers;
- a missing or empty cassette list;
- relative cassette paths;
- duplicate cassette paths or duplicate folder names;
- a `write` path absent from `items`;
- fields in `cassettes` other than `items` and `write`;
- unknown or duplicate rights;
- unknown role and cassette references;
- malformed JSON, reported as an `InvalidDataException` suitable for API error handling.

See `examples/project.sample.json` and `examples/syp.project.json`.
