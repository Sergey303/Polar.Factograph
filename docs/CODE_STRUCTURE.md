# Code structure

Polar.Factograph favors small cohesive types over files that accumulate unrelated runtime, storage, HTTP, and test concerns.

## Rules

1. Do not use `partial` classes to distribute ordinary application logic.
2. Keep `Program.cs` as bootstrap code only.
3. Put endpoint groups, authorization, runtime coordination, storage lifecycle, and query algorithms in separate files.
4. Prefer one substantial top-level type per code file. Small response records may stay beside the endpoint that owns them.
5. Review a code file when it approaches roughly 200 lines. Split it by responsibility, not by arbitrary line ranges.
6. Do not copy `DbSet<T>` or other Polar.DB implementation code into Factograph. Compose the referenced library through focused adapters.
7. Keep authorization outside endpoint-specific ad hoc conditions. Endpoint code calls the shared access boundary.
8. Keep tests focused on one component or contract; do not require a `partial Program` solely to make tests possible.

## Current examples

The Polar.DB adapter is separated into:

- generation writer;
- external-index builder;
- set factory;
- set lifecycle owner;
- RDF reader;
- search reader;
- small project-store facade.

The API is separated into:

- service registration;
- endpoint mapping;
- project path and identity resolution;
- request-context construction;
- active store selection;
- rebuild coordination;
- exception mapping;
- one file per endpoint group.
