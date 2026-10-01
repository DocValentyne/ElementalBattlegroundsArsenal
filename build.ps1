param(
    [Parameter(Mandatory=$true)][string]$GameDir,
    [string]$ProfileDir = "",
    [switch]$Deploy
)

$ErrorActionPreference = "Stop"

$GameDir = (Resolve-Path $GameDir).Path
$Managed = Join-Path $GameDir "ULTRAKILL_Data\Managed"

if ([string]::IsNullOrWhiteSpace($ProfileDir)) {
    $BepInExRoot = Join-Path $GameDir "BepInEx"
} else {
    if (-not (Test-Path $ProfileDir)) {
        throw "r2modman profile was not found: $ProfileDir"
    }
    $ProfileDir = (Resolve-Path $ProfileDir).Path
    $BepInExRoot = Join-Path $ProfileDir "BepInEx"
}

$BepInExCore = Join-Path $BepInExRoot "core"
$PluginsRoot = Join-Path $BepInExRoot "plugins"

if (-not (Test-Path (Join-Path $Managed "Assembly-CSharp.dll"))) {
    throw "Assembly-CSharp.dll was not found under $Managed"
}
if (-not (Test-Path (Join-Path $BepInExCore "BepInEx.dll"))) {
    throw "BepInEx.dll was not found under $BepInExCore"
}
if (-not (Test-Path (Join-Path $BepInExCore "0Harmony.dll"))) {
    throw "0Harmony.dll was not found under $BepInExCore"
}
if (-not (Test-Path $PluginsRoot)) {
    throw "BepInEx plugins folder was not found under $PluginsRoot"
}

$PluginConfigurator = Get-ChildItem -Path $PluginsRoot -Filter "PluginConfigurator.dll" -File -Recurse -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $PluginConfigurator) {
    throw "PluginConfigurator.dll was not found under $PluginsRoot. Install PluginConfigurator first."
}

$Project = Join-Path $PSScriptRoot "ElementalBattlegrounds.csproj"
$BuiltDll = Join-Path $PSScriptRoot "bin\Release\netstandard2.1\ElementalBattlegrounds.dll"

Write-Host "Building Elemental Battlegrounds Arsenal..."
Write-Host "Game managed assemblies: $Managed"
Write-Host "BepInEx root: $BepInExRoot"
Write-Host "PluginConfigurator: $PluginConfigurator"
Write-Host ""

dotnet build $Project -c Release `
    -p:ManagedDir="$Managed" `
    -p:BepInExCoreDir="$BepInExCore" `
    -p:PluginConfiguratorDll="$PluginConfigurator"

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not (Test-Path $BuiltDll)) {
    throw "Build reported success but the DLL was not found: $BuiltDll"
}

if ($Deploy) {
    # Remove stale duplicate copies only after a successful build.
    Get-ChildItem -Path $PluginsRoot -Filter "ElementalBattlegrounds.dll" -File -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object {
            Write-Host "Removing old copy: $($_.FullName)"
            Remove-Item $_.FullName -Force
        }

    $OutDir = Join-Path $PluginsRoot "ElementalBattlegrounds"
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    $TargetDll = Join-Path $OutDir "ElementalBattlegrounds.dll"
    Copy-Item $BuiltDll $TargetDll -Force
    Write-Host ""
    Write-Host "Deployed ElementalBattlegrounds.dll to:"
    Write-Host "  $TargetDll"
}
