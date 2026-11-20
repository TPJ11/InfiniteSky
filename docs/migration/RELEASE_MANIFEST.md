# Local Release Candidate Manifest

This manifest records a local, unpublished candidate package. It does not assert release readiness or migration acceptance.

| Field | Value |
|---|---|
| Candidate identifier | `infinite-sky-dotnet10-prompt25-rc1` |
| Candidate status | `NOT_READY` / not approved for publication or deployment |
| Payload source revision | To be filled with the commit used for the clean-checkout publish |
| Manifest/documentation revision | Recorded in Git after the artifact hash is finalized |
| Target RID | `win-x64` only |
| Publish mode | Framework-dependent; requires .NET 10 runtime |
| Configuration | `Release`; no trimming or Native AOT |
| Publish command | `dotnet publish src\InfiniteSky.Server\InfiniteSky.Server.csproj --configuration Release --runtime win-x64 --self-contained false --no-restore --output <publish-dir>` |
| Package archive | To be recorded after publish and clean-directory smoke test |
| Archive size | Pending |
| SHA-256 | Pending |
| Required external runtime | `Microsoft.NETCore.App` 10.0 servicing runtime |
| Required external services | MongoDB for persistent authentication/character/guild paths; production MongoDB version not established |
| Required external game data | Authorized server-side data supplied separately; not included |
| Included proprietary client/game assets | None |
| Secrets / production database | None |
| Automated baseline | 188 passed, 0 failed, 2 opt-in MongoDB tests skipped; final result recorded in `MIGRATION_STATUS.md` |
| Original-client compatibility | Unverified |
| Release readiness | `NOT_READY` |

The archive must include the published server output, safe `appsettings.json` defaults, root `LICENSE`, deployment/operations/known-issues instructions, and [THIRD-PARTY-NOTICES.md](../../THIRD-PARTY-NOTICES.md). It must not include development settings, databases, credentials, test account data, `.IMG`/`.WREGION`/client assets, `bin/obj`, or historical unproven publish output. Update this manifest only with measured values from the actual artifact.
