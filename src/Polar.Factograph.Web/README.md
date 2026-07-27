# Polar.Factograph Web

React/TypeScript client for the project-wide Factograph workspace.

## Run locally

Start the API first, then run:

```bash
cd src/Polar.Factograph.Web
npm install
npm run dev
```

Vite proxies `/api` to `http://localhost:5000` by default. Override the target without changing source code:

```bash
FACTOGRAPH_API_URL=https://localhost:7001 npm run dev
```

## Authentication

Development API configuration may resolve its configured development member without a token. For production-like testing, open **Доступ** in the top bar and paste a JWT. The token is kept only in `sessionStorage`; it is not written to local storage or a cookie.

## Implemented workflows

- load project identity, current member and readable cassettes;
- show the effective default write cassette or read-only state;
- explicit name-prefix and normalized-word search modes;
- deterministic result list with type and match evidence;
- ontology-labelled resource portrait;
- direct and inverse relation navigation;
- create a resource from the authorized ontology write schema;
- edit an existing resource as a complete new Fog revision;
- keep properties outside the current write schema visible and block an invalid save with a clear message;
- choose among cassettes where `writeMetadata` is actually granted;
- upload a new document only to a cassette granting both `addDocuments` and `writeMetadata`;
- preserve the successful binary upload while retrying only RDF metadata after a second-stage failure;
- lock the generated `iiss://` URI, selected document type, and cassette during metadata completion;
- authorized preview and original document loading;
- open collections by resource id and navigate back through collection history;
- select collection items and open nested resources as collections;
- add the selected resource through the default writable cassette;
- remove a membership only when `delete` is granted on its actual source cassette;
- replace a document original only when `replaceDocuments` is granted for that document cassette;
- open an administration dashboard only when `rebuildIndex` is present;
- inspect safe index and preview-worker status, calculate Fog materialization statistics on demand, and confirm a full index rebuild.

Changing a type while creating a resource clears the draft properties so incompatible values are not silently submitted. Editing keeps the existing type fixed. The editor sends all current literal and direct-link values. When an older property is no longer allowed by the current ontology, it remains visible but saving is blocked until the ontology or data is reconciled; the client does not silently discard it.

Document intake intentionally remains a two-stage operation because binary storage and Fog metadata have separate server contracts. After the original is committed, cancelling the metadata stage warns that the binary will remain addressable by its returned `iiss://` URI. A metadata retry never uploads the original again.

The administration dashboard does not call the physical source-list route and does not display server filesystem paths. Lightweight index and preview status are loaded immediately. Fog materialization statistics are calculated only on request because that operation rereads the full source stream. A rebuild remains server-authorized and requires an explicit browser confirmation.

The client never treats a cassette id supplied by the browser as authority. Read scope and write routing continue to be resolved by the API. Explicit cassette ids are sent only for an operation already enabled by the effective access snapshot returned by the server; the API re-authorizes every request.
