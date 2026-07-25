# UX target

The goal is a modern web implementation of the established Factograph and cassette-manager workflows, not a generic database administration panel.

## Primary workflows

```text
search -> resource portrait -> follow a relation -> edit
```

```text
open collection -> browse contained resources/documents -> open original or preview -> add/replace document
```

## Main workspace

The desktop layout uses three coordinated areas:

1. Collection and navigation tree.
2. Search results or collection contents.
3. Selected resource portrait and editor.

The top bar shows the current project, authenticated user, global search, readable cassette scope, and current write cassette. When one usable write cassette is resolved from the user's role order it is selected automatically. When no write target is available, the interface remains read-only and explains whether the cause is missing rights, `allowWrite: false`, a disabled cassette, or the absence of a writable Fog with `prefix` and `counter`.

## Search

The search control exposes two explicit modes rather than hiding different semantics behind one field:

- **Names** — prefix search over `name` and `alias` values;
- **Words** — exact normalized-word search over `name`, `alias`, `description`, and `doc-content`.

Results show:

- display name with language preference;
- ontology type label when available;
- the source cassette when diagnostics are permitted;
- matched field/value evidence;
- deterministic relevance order.

Repeated query words do not increase relevance. Search never returns rows from cassettes outside the effective access snapshot.

## Resource portrait

A portrait keeps the familiar ontology-driven presentation:

- type and display name;
- literal fields with language variants;
- translated enumeration values while retaining the raw stored value;
- direct relations;
- inverse relations using inverse ontology labels;
- ontology-priority field order;
- document previews and original links;
- source/provenance details for diagnostics;
- edit action only when the selected write target permits the operation.

Unknown ontology types and properties remain visible under their stable identifiers instead of disappearing.

## Access and error states

The client does not send cassette ids as authority. The server derives readable cassettes from the authenticated member's access snapshot.

Expected API distinctions:

- `401` — no authenticated identity;
- `403` — authenticated user lacks the required project right;
- `404` — resource is absent, deleted, or outside readable cassette scope;
- `409` — a write command conflicts with the current resource revision or routing state;
- `422` — project/Fog data is structurally invalid for the requested operation.

A resource outside cassette scope is intentionally indistinguishable from a missing resource to ordinary users.

## Editing

The browser sends intent-level commands. The server reads the current resource, checks the access snapshot and selected write route, applies the change, creates a complete compatible Fog record, assigns a new `mT`, and writes it to the selected writable Fog. This prevents older or unknown properties from disappearing merely because the browser did not display them.

## Administration

Cassette mounting, index rebuild, source diagnostics, validation reports, users, roles, member overrides, and permissions are separate administrative views and are hidden from ordinary users.