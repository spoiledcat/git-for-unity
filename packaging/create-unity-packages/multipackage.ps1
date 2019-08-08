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
    [string[]]
    $Paths,
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
write-output $env:NODE_OPTIONS

Run-Command -Fatal { & node --max-old-space-size=4096 ..\yarn.js run multi --out "$OutputFolder" --name "$PackageName" --version "$Version" $Paths }

} finally {
    Pop-Location
}