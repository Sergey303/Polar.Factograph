# UX target

The product is a public factographic catalogue with a permission-driven editorial layer. It is not a generic RDF or database administration panel.

## Primary workflows

```text
search -> public resource page -> follow a fact or participant -> continue exploring
```

```text
sign in as editor -> open a resource -> revise fields, add a relation, or attach media
```

A public visitor and an editor read the same ontology-driven page. Editorial actions appear only when the server exposes an effective write capability.

## Public resource page

The page uses one browser scroll and presents content directly. Generic technical headings such as `Сведения` and `Связи` are not used as public section titles.

The header contains:

- ontology type label;
- display name;
- `Скопировать ссылку`, which always copies the canonical view route rather than an edit route.

Literal values, media, and relation content follow without exposing storage internals. Provenance is absent for a viewer, limited to the logical source cassette for an authorized editor, and complete only for administration.

## Relation composition

The compact sticky toolbar contains only page-wide controls:

- `Хронология` — enabled by default;
- `Разделы` — selects which ontology relation groups participate in the page.

The section selector affects both chronological and grouped composition. A large group list stays inside a popover and gains title search. `Выбрать все` and `Снять все` are available inside the popover.

### Timeline

With `Хронология` enabled, one block named `Хронология` mixes the selected relation groups and orders them by:

1. the earliest date or interval start stored in the relation node;
2. a media-content date when the relation itself is undated;
3. undated entries at the end.

A complex relation node is one timeline entry. The entry contains its title, date, all authorized entity participants, and their ontology role labels. A relation is not duplicated into one row per participant.

The timeline uses the page scroll and virtualized chunks. Offscreen chunks keep measured placeholders; a chunk containing keyboard focus remains mounted.

### Grouped composition

With `Хронология` disabled, every selected ontology relation type becomes its own block. The same whole relation entries are grouped by relation type.

Each block owns its display mode and remembers it locally:

- list;
- table;
- small, medium, or large icons when the block contains media.

The current layout icon remains visible. Its text explanation appears on hover or keyboard focus without changing header width. Touch devices keep an explicit visible control. Menus close on outside interaction or `Escape`; Escape returns focus to the trigger.

Large grouped blocks show one portion at a time with `Предыдущие` and `Следующие`. After a page change the browser returns to the start of that block, below the sticky toolbar, and announces the new item range to assistive technology.

## Media layouts

Media uses a regular fixed-cell CSS grid rather than Masonry. Reading order remains left-to-right and top-to-bottom.

Preview mapping:

- table -> `icon`, falling back to `small`;
- small icons -> `small`;
- medium icons -> `medium`;
- large icons -> `normal`.

Images use `object-fit: contain`, so archival photographs and documents are not decoratively cropped. A failed preview request is retried when the user switches to another size.

## Table and list language

The table keeps only lightweight user-facing columns:

- an unlabeled media/icon column;
- `Название`;
- `Дата`.

The relation group is shown inside a chronological item rather than as a technical database column. In grouped mode the block heading already supplies that context.

On narrow screens a table row becomes a compact card and does not require horizontal page scrolling.

## Search

Ordinary entity search and ontology-category search are distinct actions. A real entity named `Организация` may remain the best ordinary result while a separate category action opens all instances of the class.

Results show only:

- display name;
- localized type label;
- a visible matched literal snippet when useful.

Raw predicate identifiers, evidence language metadata, source cassette ids, and ranking diagnostics stay inside the application layer.

Before creating a new entity, the editor checks short textual values for exact, normalized, keyboard-layout, and transliteration candidates. A candidate is never selected or merged automatically.

## Access and error states

The browser never sends a user or cassette id as proof of authority. The server derives capabilities from the effective access snapshot.

Expected API distinctions:

- `401` — authentication is required for a non-public deployment;
- `403` — the identity lacks the required capability;
- `404` — a resource is absent, deleted, or outside readable scope;
- `409` — a write conflicts with current routing or revision state;
- `422` — project, ontology, or Fog data is structurally invalid;
- `503` — derived project data is unavailable or rebuilding.

A hidden resource is intentionally indistinguishable from a missing resource to an ordinary reader.

## Editing

Registered viewers do not receive a writable Fog merely by registering. The configured `EditorLogins` list controls editor provisioning. At startup and after editor registration the application ensures that every configured editor has the dedicated writable Fog required by the existing cassette model.

The browser sends intent-level complete revisions. The server validates ontology shape and targets, writes one append-only Fog record, rebuilds the derived generation, and switches readers only after successful completion.

## Administration

Index rebuilds, source diagnostics, ontology validation, preview health, users, roles, and storage provenance are separate administration concerns. Public pages do not expose filesystem paths, raw access rights, cassette lists, document slot numbers, or internal search evidence.
