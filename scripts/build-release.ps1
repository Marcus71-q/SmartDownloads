param([Parameter(Mandatory=$true)][ValidatePattern('^https://')][string]$ServerUrl)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$artifacts=Join-Path $workspace 'artifacts'
$resolvedCandidate=[IO.Path]::GetFullPath($artifacts)
if(-not $resolvedCandidate.StartsWith($workspace+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe artifacts path.'}
if(Test-Path -LiteralPath $resolvedCandidate){Remove-Item -LiteralPath $resolvedCandidate -Recurse -Force}
New-Item -ItemType Directory -Path $resolvedCandidate|Out-Null
$publish=Join-Path $resolvedCandidate 'SmartDownloads-win-x64'
dotnet publish (Join-Path $workspace 'SmartDownloads\SmartDownloads.csproj') -c Release -r win-x64 --self-contained true -p:DebugType=None -p:SmartDownloadsServerUrl=$ServerUrl -o $publish
if($LASTEXITCODE -ne 0){throw 'Publish failed.'}
Copy-Item -LiteralPath (Join-Path $workspace 'PRIVACY.md') -Destination $publish
Copy-Item -LiteralPath (Join-Path $workspace 'LICENSE') -Destination $publish
Compress-Archive -LiteralPath $publish -DestinationPath (Join-Path $artifacts 'SmartDownloads-win-x64.zip') -CompressionLevel Optimal
Write-Output (Join-Path $artifacts 'SmartDownloads-win-x64.zip')
