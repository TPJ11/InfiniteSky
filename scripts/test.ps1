[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

dotnet test InfiniteSky.sln --no-restore --logger "console;verbosity=minimal"
exit $LASTEXITCODE
