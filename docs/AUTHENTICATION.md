# Authentication

Polar.Factograph uses local accounts and an encrypted ASP.NET Core cookie. Password hashes, devices, assigned access level, and optional editor Fog references are stored in `identity.json`. Project structure remains in `factograph.project.json`.

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
        "editor-one"
      ],
      "AdminLogins": [
        "admin"
      ],
      "SessionDays": 30,
      "MaxFogBytes": 1048576
    }
  }
}
```

`EditorLogins` and `AdminLogins` are compared after case-insensitive Unicode normalization.

- a registered login absent from both lists is a viewer;
- a login in `EditorLogins` is an editor;
- a login in `AdminLogins` is an administrator and automatically also an editor;
- an administrator therefore needs to be listed only in `AdminLogins`;
- an absent list is equivalent to an empty list.

Access rules are built into the application. They are not configured through `roles` or `members` in the project file.

## Built-in access

- viewers read and search every configured cassette;
- editors additionally write metadata and add or replace documents in the cassette selected by `cassettes.write`;
- administrators additionally export, rebuild the index, manage users and cassettes, and use all write operations in the single write cassette.

When `PublicReadEnabled` is true, an unauthenticated visitor uses the synthetic `PublicUserId` and receives viewer access. This identity is not stored in `identity.json` and can never receive a writable Fog.

## Registration and role reconciliation

At registration the server normalizes the login and determines its built-in access level from the two login lists. Viewers receive no Fog. Editors and administrators receive a numbered writable `.fog` in the single project write cassette:

```text
originals/0001/0042-Сергей.fog
```

The Fog root uses the stable user id as `dbid` and `owner`; the filename remains recognizable to an administrator.

At startup registered users are reconciled with the current login lists:

- newly assigned editors and administrators receive a Fog when one is missing;
- users removed from both privileged lists become viewers;
- an old Fog is retained for inspection or recovery rather than deleted automatically.

## identity.json

The file is created on first registration:

```json
{
  "schemaVersion": 1,
  "users": [],
  "devices": []
}
```

A user stores a stable id, login, normalized login, display name, password hash, enabled state, security version, effective role names, timestamps, and an optional Fog reference. A device stores its id, owner, display name, creation and expiry times, last-seen time, and optional revocation time.

Application writes use a temporary file followed by atomic publication. Short Windows file locks are retried. Invalid external edits do not replace the last valid in-memory snapshot.

## Login rules

A login contains 3–63 Unicode letters or digits and may also contain dots, underscores, and hyphens. It must begin with a letter or digit and cannot end with a dot. Cyrillic logins are supported.

Passwords contain at least 10 characters.

## Session security

Production cookies are `Secure`, `HttpOnly`, and `SameSite=Lax`. Data Protection keys must be stored outside disposable publish output. Mutating browser requests require the antiforgery token returned by the session endpoint.

Logging out revokes the current device. Logging out everywhere increments the user security version and revokes every device.

## Browser session API

```text
GET  /api/auth/session
POST /api/auth/register
POST /api/auth/login
POST /api/auth/logout
POST /api/auth/logout-all
POST /api/auth/devices/{deviceId}/revoke
```

`GET /api/auth/session` returns authentication state, public-reading state, the request-verification token, user information, and active devices.

Changes to login lists, public-reading settings, project cassette paths, or the write cassette require an application restart because these settings are loaded during startup.
