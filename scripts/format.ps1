[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

dotnet format InfiniteSky.sln --verify-no-changes --no-restore
exit $LASTEXITCODE
