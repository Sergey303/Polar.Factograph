# Document binary writing

Polar.Factograph stores document binaries in the existing cassette layout and keeps RDF metadata in Fog/XML.

## Routes

```text
POST /api/documents/files?fileName={name.ext}&cassetteId={optional-id}
PUT  /api/documents/files?uri={iiss-uri}&fileName={name.ext}
```

The HTTP request body is the raw binary stream. Multipart form data is not required, so large files can be copied without buffering the complete request in memory.

Add requires the cassette `addDocuments` right. When `cassetteId` is omitted, the effective default write cassette is used. Replace derives the cassette from `uri` and requires the independent `replaceDocuments` right.

## Compatible layout

A new binary receives the next numeric four-character folder/document pair:

```text
originals/0001/0001.pdf
originals/0001/0002.jpg
```

The returned identifier uses the established form:

```text
iiss://Cassette@iis.nsk.su/0001/0001
```

Allocation happens under both the project operation gate and a cassette-local lock file. Concurrent uploads therefore cannot receive the same pair.

Preview reads recognize the established directories plus one optional compact variant:

```text
documents/icon/{folder}/{document}.{ext}
documents/small/{folder}/{document}.{ext}
documents/medium/{folder}/{document}.{ext}
documents/normal/{folder}/{document}.{ext}
```

`icon` is a derived technical preview and does not change RDF/Fog metadata. The table view requests `icon`; when that physical file is absent, the API transparently serves the current `small` preview. Existing cassettes therefore remain compatible. A deployment may start generating physical icon files later without changing resource identifiers or client routes.

## Atomic file transaction

1. Validate the leaf file name and extension.
2. Stream into a temporary file in the target directory.
3. Enforce the configured byte limit while copying.
4. Calculate SHA-256 while copying.
5. Flush the temporary file to disk.
6. Rename it to the final original path.
7. Remove the temporary file after any failure.

Empty uploads are rejected. Extensions contain only ASCII letters or digits and are normalized to lower case for new files.

Replacement preserves the existing `iiss://` URI and original path. The replacement extension must match the existing extension, which avoids an interval with two files resolving to the same document number.

## Response

Both routes return cassette identity, `iiss://` URI, folder/document numbers, stored file name, byte length, SHA-256, and whether the operation replaced an existing binary. Local filesystem paths are never returned.

The response also reports preview orchestration:

- `previewState` is `queued` when a durable request was written;
- `previewState` is `queue-failed` when the original was committed but the request could not be persisted;
- `previewRequestId` and `previewQueuedAtUtc` are present only for a queued request.

A queue failure does not turn the binary write into an HTTP failure. This prevents a client from retrying an add whose original file was already committed and accidentally creating another document.

Add returns `201 Created`; replace returns `200 OK`. A missing original returns `404 document_not_found`.

## Upload limit

The default maximum is 1 GiB. It can be changed without code changes:

```json
{
  "Documents": {
    "MaxUploadBytes": 1073741824
  }
}
```

The declared `Content-Length` is rejected early when it exceeds the limit. Requests without a declared length are still limited during streaming.

## Metadata workflow

Binary storage does not change the RDF cloud and therefore does not mark Polar.DB as `DIRTY` or trigger a rebuild.

For a new document:

1. Upload the binary and receive its `iiss://` URI.
2. Call `POST /api/resources` with the appropriate ontology document class and a `uri` literal.
3. Optionally add the resulting resource to a collection.

Updating document metadata uses the same append-only resource route. Replacing only the binary leaves the existing RDF identifier and `iiss://` URI unchanged.

### Web intake workflow

The React workspace exposes these two contracts as one guided workflow without pretending they are one atomic server transaction.

- The action is available only when at least one cassette grants both `addDocuments` and `writeMetadata`.
- Before upload, the user selects the document class and a free-form literal property that will contain the `iiss://` URI.
- The original and its RDF revision are written to the same selected cassette.
- After a successful upload, the returned URI, selected class, and cassette are locked while the remaining metadata is completed.
- If RDF writing fails, the upload result stays in memory and only `POST /api/resources` is retried.
- Cancelling after upload warns that the committed binary remains available under the returned URI.

The workflow never automatically retries the binary add after a successful response. This avoids duplicate originals when preview queueing or metadata creation fails after the file commit.

## Preview generation queue

Every successful add or replacement attempts to write one atomic JSON request under:

```text
documents/preview-queue/{folder}-{document}-{requestId}.json
```

The request contains logical cassette/document identifiers, original file metadata, length, SHA-256, replacement flag, request time, completed attempt count, optional retry time, and the last renderer error. It never contains a server filesystem path.

A queue processor atomically moves one request to `documents/preview-processing` before invoking a renderer. On success it removes the request. A retryable failure increments the attempt, records the error, schedules `notBeforeUtc`, and returns the request to the queue. A permanent failure or exhausted retry limit moves the request to `documents/preview-failed`. Invalid JSON is isolated there with an error note instead of blocking later requests.

Processing claims older than the configured lease timeout are returned to the queue. This recovers requests left in `preview-processing` after a worker crash. A heartbeat renews active claims during long rendering. Renderers must still be idempotent because a crash can happen after preview files are committed but before the queue request is removed.

Administrators with `rebuildIndex` may inspect aggregate queue state without filesystem paths:

```text
GET /api/admin/previews/status
```

The response contains queued, processing, and failed counts per enabled cassette plus the oldest queued timestamp.

## Hosted preview worker

The API contains a background worker, disabled by default. It reloads the project configuration, visits enabled cassettes in round-robin order, and processes a bounded number of requests per cycle.

```json
{
  "Previews": {
    "Enabled": true,
    "Executable": "/opt/polar-factograph/bin/render-preview",
    "PrefixArguments": [],
    "OutputExtension": "jpg",
    "SmallWidth": 240,
    "MediumWidth": 800,
    "NormalWidth": 1600,
    "PollIntervalSeconds": 5,
    "RenderTimeoutSeconds": 300,
    "MaxItemsPerCycle": 8,
    "MaxAttempts": 3,
    "RetryDelaySeconds": 300,
    "LeaseTimeoutSeconds": 1800
  }
}
```

The executable is started directly without a command shell. `PrefixArguments` supports wrappers such as `dotnet renderer.dll`; every value remains a separate process argument.

After the prefix arguments, Polar.Factograph passes this fixed positional contract:

```text
originalPath smallTemporaryPath mediumTemporaryPath normalTemporaryPath smallWidth mediumWidth normalWidth
```

The renderer must create three non-empty files and exit with:

- `0` for success;
- `64` for a permanently unsupported document;
- any other non-zero code for a retryable failure.

Before and after the process runs, the worker verifies the original length and SHA-256 from the queue request. A superseded request is completed without publishing stale previews. Output files are first written under temporary names and then individually replaced in the established `small`, `medium`, and `normal` directories. An existing preview extension is preserved; otherwise `OutputExtension` is used.

The current external renderer contract deliberately remains three-output. It does not need to change merely because the read API understands `icon`: until a separate compatible tool or later renderer revision creates `documents/icon`, icon requests fall back to `small`.

The repository supplies the safe hosted lifecycle and process adapter, but does not bundle a PDF/image conversion executable. Deployments must provide one that implements the contract above. Existing preview reads continue to use generated files in the established cassette directories.
