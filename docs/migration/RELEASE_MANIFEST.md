# Local Release Candidate Manifest

This manifest records a local, unpublished candidate package. It does not assert release readiness or migration acceptance.

| Field | Value |
|---|---|
| Candidate identifier | `infinite-sky-dotnet10-prompt25-rc1` |
| Candidate status | `NOT_READY` / not approved for publication or deployment |
| Payload source revision | `82047a23ca7e22813262924af13cc95cd905b23d` |
| Manifest/documentation revision | This manifest is committed separately after the payload; its containing Git commit records the final documentation revision |
| Build environment | Windows 11 Pro x64 (`10.0.26300`) |
| .NET SDK | `10.0.401` |
| Tested .NET runtime | `10.0.12` |
| Target RID | `win-x64` only; no other RID is claimed |
| Publish mode | Framework-dependent; requires .NET 10 runtime |
| Configuration | `Release`; no trimming or Native AOT |
| Restore command | `dotnet restore InfiniteSky.sln --locked-mode` |
| Publish command | `dotnet publish src\InfiniteSky.Server\InfiniteSky.Server.csproj --configuration Release --runtime win-x64 --self-contained false --no-restore --output <publish-dir>` |
| Package archive | `artifacts\release-candidate\infinite-sky-dotnet10-prompt25-rc1-win-x64.zip` |
| Archive size | 3,066,194 bytes |
| SHA-256 | `DB7BD8A9BC1A843D897773714D9D0439F16AD5BE6474EC16DDD1CA2EF063F67D` |
| Archive payload | 158 files; 7,899,160 uncompressed bytes; 137 Markdown documents |
| Required external runtime | `Microsoft.NETCore.App` 10.0 servicing runtime |
| Required external services | MongoDB for persistent authentication/character/guild paths; production MongoDB version not established |
| Required external game data | Authorized server-side data supplied separately; not included |
| Included proprietary client/game assets | None |
| Secrets / production database / test credentials | None |
| Automated tests | 188 passed, 0 failed, 2 opt-in MongoDB integration tests skipped |
| Clean-checkout validation | Locked restore, Release build (0 warnings/errors), tests, format verification, and `win-x64` publish succeeded |
| Extracted-package smoke | Started from the extracted archive and remained running for 5 seconds; networking/database disabled, no zones configured, world readiness explicitly degraded; no client or persistence flow exercised |
| Original-client compatibility | Unverified |
| Release readiness | `NOT_READY` |

The archive contains the published server output, safe `appsettings.json` defaults, root `LICENSE`, `THIRD-PARTY-NOTICES.md`, `README.txt`, and repository Markdown documentation except `docs/RELEASE_NOTES.md` and this manifest, which remain alongside the archive in the repository to avoid self-referential hash metadata. The archive's copy of `FINAL_MIGRATION_REPORT.md` refers to the separately maintained manifest rather than embedding the archive hash. It excludes PDB/XML build artifacts, development/production-specific appsettings, databases, credentials, test account data, `.IMG`/`.WREGION`/client assets, `bin/obj`, and the historical unproven Linux output.

The archive hash and size are measured from the local ZIP; they are not a byte-for-byte reproducibility claim. NuGet package archives did not contain separate license text files for the exact runtime package versions. The notice inventory records package metadata, but owner/legal review remains necessary before any distribution. The artifact has not been published externally, deployed, or approved.
