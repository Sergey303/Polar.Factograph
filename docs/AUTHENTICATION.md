# Authentication

Polar.Factograph authenticates browser users locally. ASP.NET Core issues an encrypted application cookie; passwords, devices, roles, and the user-to-Fog assignment are stored in a small reloadable JSON file.

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
      "DefaultRole": "editor",
      "DefaultCassetteId": "main-cassette",
      "SessionDays": 30,
      "MaxFogBytes": 1048576
    }
  }
}
```

`DefaultRole` must exist in the loaded project. `DefaultCassetteId` must identify an enabled writable cassette. It may be omitted only when the project contains exactly one enabled writable cassette.

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
- assigned cassette id, Fog document URI, and cassette-relative Fog path.

Each device record contains its id, user id, display name, creation and expiry times, last-seen time, and optional revocation time.

The application reads the complete file into memory. Application changes are serialized to a temporary file and published by atomic replacement. On Windows, a short-lived reader lock is retried before the operation is reported as unavailable. External valid edits are picked up through the standard JSON configuration provider with `reloadOnChange`. An invalid external snapshot is rejected and the previous valid in-memory snapshot remains active.

## Login rules

A login contains 3-63 Unicode letters or digits and may also contain dots, underscores, and hyphens. It must start with a letter or digit and cannot end with a dot. Cyrillic logins are supported. Login comparison is case-insensitive after Unicode normalization.

The restrictions keep the login safe for use in the physical Fog filename on Windows and Linux. The stable user id remains independent of the login, so a future login-renaming operation does not need to change RDF ownership.

## Registration and user Fog

Registration performs one serialized operation:

1. normalize and reserve the login;
2. load the current project and verify the configured role and cassette;
3. allocate a normal numbered cassette document;
4. create an empty writable `.fog` under `originals/NNNN/`;
5. store the user, password hash, device, roles, and Fog mapping in `identity.json`;
6. issue the application cookie.

The physical filename contains both the normal cassette document number and the login, for example:

```text
originals/0001/0042-Сергей.fog
```

The number preserves unique `iiss://` allocation and compatibility with later document additions. The login keeps the file recognizable to an administrator. The Fog root stores the stable user id as `dbid` and `owner`; its technical RDF prefix is also derived from that stable id rather than from the Unicode filename.

Registered users are overlaid onto the project membership at request time. Existing explicit entries in `project.json` keep priority. Every RDF mutation made by a registered user is routed only to the Fog assigned to that user. Static project users that are absent from `identity.json` retain the legacy writable-Fog selection behavior.

## Browser session API

```text
GET  api/auth/session
POST api/auth/register
POST api/auth/login
POST api/auth/logout
POST api/auth/logout-all
POST api/auth/devices/{deviceId}/revoke
```

`GET api/auth/session` returns the current user and devices when authenticated and always returns a request-verification token. The React client sends that token in `X-CSRF-TOKEN` for every mutating request.

Logging out revokes the current device. Logging out everywhere increments the user's security version and revokes all device records. Cookie validation checks the user, device, expiry, revocation, and security version on subsequent requests.

## Browser interface

Until a valid local session is present, the React application displays only the login or registration screen. Project cassettes, search, collections, resource portraits, documents, and administration controls are not mounted and do not issue API requests.

## Project and source configuration

Changes to cassette and Fog source configuration may be saved through the administration interface, but the running process continues with the loaded project configuration until the server is restarted. User registration and user-Fog creation do not require a restart.

## Development fallback

`Api:DevelopmentUserId` remains available only when the host environment is `Development` and there is no authenticated cookie. It is ignored in production.
