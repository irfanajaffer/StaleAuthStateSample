# Validation - Analyzer diagnostics for stale authentication state

Scenario: [dotnet/aspnetcore #68488](https://github.com/dotnet/aspnetcore/issues/68488)

Manual: [dotnet/aspnetcore #68479](https://github.com/dotnet/aspnetcore/issues/68479)

## Build tested

```text
.NET SDK 11.0.100-preview.7.26381.103
```

The scenario requires .NET 11 Preview 7 or later.

## Sample application

This repository contains a Blazor Web App using Interactive Server rendering for the validation page.

Start at `/stale-auth-demo` to access the complete sample set.

### Expected BL0013 warnings

| Consumer | Route | Purpose |
|---|---|---|
| `StaleUserBadge` | `/stale-auth-demo` | Reads authentication state once without subscribing to changes |
| `CachedUserService` | `/stale-auth-demo` | Caches authentication state without subscribing to changes |

### Expected no BL0013 warning

| Consumer | Route | Purpose |
|---|---|---|
| `LiveUserBadge` | `/stale-auth-demo` | Subscribes to `AuthenticationStateChanged` |
| `LiveCachedUserService` | `/stale-auth-demo` | Keeps its cached state synchronized with authentication changes |
| `CascadingUserBadge` | `/stale-auth-demo` | Reads authentication state from a cascading parameter |
| `<AuthorizeView>` | `/stale-auth-demo` | Uses the framework authorization-state consumer |
| `<CascadingAuthenticationState>` | `/stale-auth-demo` | Supplies live authentication state to descendants |

## How to run

From the repository root:

```powershell
dotnet run --project StaleAuthStateSample.csproj --launch-profile http
```

Open `http://localhost:5093/stale-auth-demo`.

## How to verify the build diagnostic

```powershell
dotnet clean StaleAuthStateSample.slnx -nologo -v:minimal
dotnet build StaleAuthStateSample.slnx -t:Rebuild -nologo -v:minimal
```

Expected result: `StaleUserBadge` and `CachedUserService` produce `BL0013` warnings. `LiveUserBadge`, `LiveCachedUserService`, `CascadingUserBadge`, `<AuthorizeView>`, and `<CascadingAuthenticationState>` produce no `BL0013` warning.

## Configuration tested

- Blazor Web App - Interactive Server
- Visual Studio 2022 and command-line builds
- Published Release output (`dotnet publish -c Release`)

The diagnostic is produced at compile time and does not vary by render mode.

## Evidence

- Screenshots: [`evidence/screenshots/`](evidence/screenshots/)
- Interaction recordings: [`evidence/videos/`](evidence/videos/)
- Test report: [`test-report/VALIDATION_REPORT.md`](test-report/VALIDATION_REPORT.md)