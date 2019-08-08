<#
.SYNOPSIS
    Takes a bunch of previously prepared folders with unity package assets and creates a package out of them
    Call the run script to prepare folders for this
.DESCRIPTION
    Packages a build of GitHub for Unity
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

Run-Command -Fatal { & node ..\yarn.js run multi -o "$OutputFolder" -n "$PackageName" -v "$Version" $Paths }

} finally {
    Pop-Location
}