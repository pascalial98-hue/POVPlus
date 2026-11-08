param(
    [Parameter(Mandatory = $true)][string]$DalamudLibPath,
    [string]$DotNet = 'dotnet',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$referenceRoot = (Resolve-Path -LiteralPath $DalamudLibPath).Path
foreach ($dll in @('Dalamud.dll','Dalamud.Bindings.ImGui.dll','FFXIVClientStructs.dll','InteropGenerator.Runtime.dll','Lumina.dll','Lumina.Excel.dll','Newtonsoft.Json.dll')) {
    if (-not (Test-Path -LiteralPath (Join-Path $referenceRoot $dll))) { throw "Missing reference: $dll" }
}
Push-Location $repoRoot
try {
    & $Python -X utf8 scripts/verify_localization.py
    if ($LASTEXITCODE -ne 0) { throw 'Localization validation failed.' }
    & $DotNet build src/PovPlus.csproj -c Release --nologo "-p:DalamudLibPath=$referenceRoot"
    if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
    & $DotNet run --project tests/PovPlus.Tests.csproj -c Release --no-launch-profile "-p:DalamudLibPath=$referenceRoot"
    if ($LASTEXITCODE -ne 0) { throw 'Regression checks failed.' }
    & $Python -X utf8 scripts/package.py
    if ($LASTEXITCODE -ne 0) { throw 'Packaging failed.' }
} finally { Pop-Location }
