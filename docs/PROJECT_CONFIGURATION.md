# Project configuration

`factograph.project.json` contains only the project structure. User accounts and login lists are configured separately in `appsettings.json`.

## Minimal format

```json
{
  "schemaVersion": 1,
  "projectId": "archive",
  "name": "Factographic archive",
  "homeResourceIds": [
    "featured-collection"
  ],
  "ontology": {
    "path": "./ontology.xml"
  },
  "index": {
    "path": "./project-data/index",
    "rebuildMode": "whenSourcesChanged"
  },
  "cassettes": {
    "items": [
      "D:/FactographProjects/archive-history",
      "D:/FactographProjects/archive-current"
    ],
    "write": "D:/FactographProjects/archive-current"
  }
}
```

## Cassette paths

The `cassettes` section has two fields:

- `items` is a non-empty list of full cassette directory paths;
- `write` is the full path of the single write cassette and must match one item exactly.

All listed cassettes are enabled and readable. The final folder name becomes both the cassette identifier and its display name, so folder names must be unique without regard to letter case.

There are no `id`, `name`, `enabled`, `defaultAccess`, `allowWrite`, or per-role routing fields. Remove a path from `items` to exclude a cassette. Change `write` to another item to select a different write cassette.

## Built-in access

Access rules are fixed by the application and are not repeated in the project file:

| User level | Project access | Cassette access |
|---|---|---|
| Viewer | Read and search | Read every configured cassette |
| Editor | Viewer access | Write metadata and add or replace documents in `cassettes.write` |
| Administrator | Read, search, export and administration | All rights for every configured cassette |

A registered login absent from both configured login lists is a viewer. `Authentication:Local:EditorLogins` identifies editors and `Authentication:Local:AdminLogins` identifies administrators. Anonymous visitors are viewers when `PublicReadEnabled` is enabled.

The historical `roles`, `members`, and `writeRouting` sections are no longer used. During migration they may still be present in an older file, but the loader replaces them with the built-in rules. They should be removed.

Before modifying Fog/XML, the server additionally verifies that the user has a writable Fog in the selected write cassette.

## Validation

Configuration loading rejects:

- unsupported schema versions;
- missing project, ontology, or index values;
- a missing or empty cassette list;
- relative cassette paths;
- duplicate cassette paths or duplicate final folder names;
- a `write` path absent from `items`;
- fields in `cassettes` other than `items` and `write`;
- malformed JSON.

Fog/XML remains the authoritative source of factographic data. Polar.DB remains a rebuildable derived index.

See `examples/project.sample.json` and `examples/syp.project.json`.
