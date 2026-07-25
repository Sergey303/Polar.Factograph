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

The top bar shows the current project, global search, authenticated user, and current write cassette. When only one write cassette is available it is selected automatically.

## Resource portrait

A portrait keeps the familiar ontology-driven presentation:

- type and display name;
- literal fields with language variants;
- direct relations;
- inverse relations;
- document previews and original links;
- source/provenance details for diagnostics;
- edit action when permitted.

## Editing

The browser sends intent-level commands. The server reads the current resource, applies the change, creates a complete compatible Fog record, assigns a new `mT`, and writes it to the selected writable Fog. This prevents older or unknown properties from disappearing merely because the browser did not display them.

## Administration

Cassette mounting, index rebuild, source diagnostics, validation reports, users, and permissions are separate administrative views and are hidden from ordinary users.
