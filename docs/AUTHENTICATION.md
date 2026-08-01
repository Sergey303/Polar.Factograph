# Authentication

Polar.Factograph authenticates browser editors locally. ASP.NET Core issues an encrypted application cookie; passwords, devices, roles, and optional editor-to-Fog assignments are stored in a small reloadable JSON file. Public visitors can be given read-only access without an account.

No bearer token or external identity provider is required for the first version.

## Configuration

```json
{
  "Authentication": {
    "Local": {
      "IdentityPath": "project-data/identity.json",
      "DataProtectionKeysPath": "project-data/data-protection-keys",
      "CookieName": "Polar.Factograph.Session",
      "RegistrationEnabled": true,
      "PublicReadEnabled": true,
      "PublicUserId": "$public",
      "EditorLogins": [
        "editor-one",
        "редактор-два"
      ],
      "SessionDays": 30,
      "MaxFogBytes": 1048576
    }
  }
}
```

`EditorLogins` is the only local editor access list. Login comparison uses the same case-insensitive Unicode normalization as registration.

- a registered login in `EditorLogins` receives the project role `editor` and a writable Fog;
- every other registered login receives the project role `viewer` and no Fog;
- an absent or empty `EditorLogins` list means that there are no local editors;
- `viewer` and `editor` must exist in the loaded project once registered users need reconciliation;
- editor Fogs are created in the single cassette selected by `cassettes.write` in `project.json`.

`PublicReadEnabled` opens project reads and searches to visitors without a cookie. `PublicUserId` is a stable synthetic project member id; it is not a login and is not stored in `identity.json`. On every anonymous request this member is overlaid with exactly the project role `viewer`, even if an explicit project member with the same id was accidentally assigned stronger roles. The `viewer` role therefore defines the public cassette boundary and must contain only the intended `read` and `search` rights.

At application startup the identity store is reconciled with the configured editor list. Missing editor Fogs are created. Users removed from the list are changed to `viewer`; their old Fog files are not deleted automatically and remain available for administrator inspection or recovery.

Data Protection keys must be kept in durable storage outside a disposable publish directory. Production cookies are `Secure`, `HttpOnly`, and `SameSite=Lax`.

## identity.json

The file is created when the first account is registered. Its current schema is:

```json
{
  "schemaVersion": 1,
  "users": [],
  "devices": []
}
```

Each user record contains:

- stable user id;
- login and normalized login;
- display name;
- ASP.NET Core `PasswordHasher` result;
- enabled state and security version;
- project roles;
- an optional editor Fog reference with cassette id, document URI, and cassette-relative path.

Each device record contains its id, user id, display name, creation and expiry times, last-seen time, and optional revocation time.

The application reads the complete file into memory. Application changes are serialized to a temporary file and published by atomic replacement. On Windows, a short-lived reader lock is retried before the operation is reported as unavailable. External valid edits are picked up through the standard JSON configuration provider with `reloadOnChange`. An invalid external snapshot is rejected and the previous valid in-memory snapshot remains active.

## Login rules

A login contains 3-63 Unicode letters or digits and may also contain dots, underscores, and hyphens. It must start with a letter or digit and cannot end with a dot. Cyrillic logins are supported. Login comparison is case-insensitive after Unicode normalization.

The restrictions keep editor logins safe for use in the physical Fog filename on Windows and Linux. The stable user id remains independent of the login, so a future login-renaming operation does not need to change RDF ownership.

## Registration and editor Fog

Registration performs one serialized operation:

1. normalize and reserve the login;
2. load the current project;
3. choose `editor` or `viewer` from `EditorLogins`;
4. only for an editor, allocate a normal numbered cassette document in the project write cassette and create an empty writable `.fog` under `originals/NNNN/`;
5. store the user, password hash, device, role, and optional Fog mapping in `identity.json`;
6. issue the application cookie.

The physical editor Fog filename contains both the normal cassette document number and the login, for example:

```text
originals/0001/0042-Сергей.fog
```

The number preserves unique `iiss://` allocation and compatibility with later document additions. The login keeps the file recognizable to an administrator. The Fog root stores the stable user id as `dbid` and `owner`; its technical RDF prefix is also derived from that stable id rather than from the Unicode filename.

Registered users are overlaid onto the project membership at request time. Existing explicit entries in `project.json` keep priority for authenticated users. Every RDF mutation made by an editor is routed only to the Fog assigned to that editor. A registered viewer has no writable Fog. The synthetic public viewer is rejected by the Fog resolver before the legacy writable-source fallback, so it cannot obtain write access even if a higher-level authorization check is accidentally bypassed. Static project users that are absent from `identity.json` retain the legacy writable-Fog selection behavior.

User-correctable registration validation remains visible to the browser. Operational or project-configuration failures return the stable `registration_unavailable` code with HTTP 409 and a neutral message. Role names, cassette identifiers, paths, and the original exception remain in server logs only.

## Browser session API

```text
GET  api/auth/session
POST api/auth/register
POST api/auth/login
POST api/auth/logout
POST api/auth/logout-all
POST api/auth/devices/{deviceId}/revoke
```

`GET api/auth/session` returns the current user and devices when authenticated, the `publicReadEnabled` flag, and a request-verification token. Fog fields are `null` for viewers. The React client sends the request-verification token in `X-CSRF-TOKEN` for every mutating request.

Logging out revokes the current device. Logging out everywhere increments the user's security version and revokes all device records. Cookie validation checks the user, device, expiry, revocation, and security version on subsequent requests.

## Browser interface

When public reading is enabled, an unauthenticated visitor enters the normal search and resource workspace immediately. The top bar still contains the login and registration form. Project access data hides editing, relation, document-upload, and administration controls; manually entered edit URLs are returned to the resource portrait, and the create route displays a read-only explanation instead of mounting an editor.

When public reading is disabled, the React application displays only the login or registration screen until a valid local session is present.

After login, the project access response determines which editing controls are shown. Registered viewer accounts can open and search project data but do not receive write rights or a Fog.

## Project and source configuration

Changes to cassette and Fog source configuration may be saved through the administration interface, but the running process continues with the loaded project configuration until the server is restarted. Adding or removing editor logins, enabling public reading, or changing the public user id also requires a restart because these settings are read during service configuration.

## Development fallback

`Api:DevelopmentUserId` remains available only when the host environment is `Development` and there is no authenticated cookie. It takes priority over the synthetic public viewer and is ignored in production.
