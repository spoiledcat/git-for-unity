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
    $PathToPackage,
    [string]
    $OutputFolder,
    [string]
    $PackageName,
    [string]
    $Version,
    [string]
    $Ignores,
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

Run-Command -Fatal { & node ..\yarn.js install --prefer-offline }
Run-Command -Fatal { & node ..\yarn.js start --path "$PathToPackage" --out "$OutputFolder" --name "$PackageName" --version "$Version" --ignores "$Ignores" }

} finally {
    Pop-Location
}