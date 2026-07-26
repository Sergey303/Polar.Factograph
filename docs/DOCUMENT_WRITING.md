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

## Preview generation queue

Every successful add or replacement attempts to write one atomic JSON request under:

```text
documents/preview-queue/{folder}-{document}-{requestId}.json
```

The request contains logical cassette/document identifiers, original file metadata, length, SHA-256, replacement flag, and request time. It never contains a server filesystem path. A preview worker can claim these files and generate the compatible `small`, `medium`, and `normal` variants.

The queue is now produced by Polar.Factograph, but image/PDF rendering and worker lifecycle remain separate follow-up work. Existing preview reads continue to use generated files in the established cassette directories.
