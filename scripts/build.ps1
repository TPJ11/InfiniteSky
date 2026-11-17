[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

dotnet restore InfiniteSky.sln
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet build InfiniteSky.sln --no-restore
exit $LASTEXITCODE
