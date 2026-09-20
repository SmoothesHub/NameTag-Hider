param(
    [string]$GtagPath = "C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag"
)

$ErrorActionPreference = "Stop"

$Managed = Join-Path $GtagPath "Gorilla Tag_Data\Managed"
$BepCore = Join-Path $GtagPath "BepInEx\core"
$Libs = Join-Path $PSScriptRoot "libs"

New-Item -ItemType Directory -Force -Path $Libs | Out-Null

$managedDlls = @(
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.IMGUIModule.dll",
    "UnityEngine.InputLegacyModule.dll",
    "UnityEngine.UI.dll"
)

foreach ($dll in $managedDlls) {
    $src = Join-Path $Managed $dll
    if (!(Test-Path $src)) {
        throw "Missing: $src"
    }

    Copy-Item $src (Join-Path $Libs $dll) -Force
}

$bep = Join-Path $BepCore "BepInEx.dll"
if (!(Test-Path $bep)) {
    throw "Missing: $bep"
}

Copy-Item $bep (Join-Path $Libs "BepInEx.dll") -Force

dotnet build (Join-Path $PSScriptRoot "NameTag-Hider.csproj") -c Release

Write-Host ""
Write-Host "Built:"
Write-Host (Join-Path $PSScriptRoot "bin\Release\NameTag-Hider.dll")
