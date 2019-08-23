<#
.SYNOPSIS
    Packages a build of GitHub for Unity
.DESCRIPTION
    Packages a build of GitHub for Unity
#>

[CmdletBinding()]

Param(
    [string]
    $PackageName,
    [string]
    $PackagePath,
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

$packagingScriptsDir="$rootDirectory\packaging\create-unity-packages"
$artifactsDir="$rootDirectory\artifacts"
$original="$rootDirectory\artifacts\manifest-$PackageName.json"
$combined="$rootDirectory\artifacts\combined-$PackageName.json"
$upmjson="$rootDirectory\upm-ci~\packages\packages.json"

Copy-Item $original $combined
Invoke-Command -Fatal { & upm-ci package pack --package-path $PackagePath }
Copy-Item $upmjson $original
Invoke-Command -Fatal { & $packagingScriptsDir\update.ps1 $combined $upmjson }
Copy-Item $upmjson $combined
Get-FileHash "$artifactsDir\$PackageName-*-preview.tgz" | % { Set-Content "$($_.Path).md5" $_.Hash }