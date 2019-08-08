<#
.SYNOPSIS
    Packages a build of GitHub for Unity
.DESCRIPTION
    Packages a build of GitHub for Unity
.PARAMETER PathToPackage
    Path to the Package folder that contains all the binaries and meta files
    <root>\unity\PackageProject
.PARAMETER OutputFolder
    Folder to put the package files
.PARAMETER PackageName
    Name of the package (usually github-for-unity-[version]). The script will add
    the appropriate extensions to the generated files.
#>

[CmdletBinding()]

Param(
    [string]
    $OutputFolder,
    [string]
    $PackageName,
    [string]
    $Version,
    [string]
    $Path1,
    [string]
    $Extras1,
    [string]
    $Ignores1,
    [string]
    $Path2,
    [string]
    $Extras2,
    [string]
    $Ignores2,
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

Push-Location $scriptsDirectory

try {

if (!(Test-Path 'node_modules')) {
	Run-Command -Fatal { & node ..\yarn.js install --prefer-offline }
}

Run-Command -Fatal { & node ..\yarn.js run multi --out "$OutputFolder" --name "$PackageName" --version "$Version" --path1 "$Path1" --extras1 "$Extras1" --ignores1 "$Ignores1" --path2 "$Path2" --extras2 "$Extras2" --ignores2 "$Ignores2" }

} finally {
    Pop-Location
}