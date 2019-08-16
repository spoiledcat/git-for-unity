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
	$Src,
	[string]
	$Out,
	[switch]
	$Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
	Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

Push-Location $rootDirectory

try {

	if (!(Test-Path 'node_modules')) {
		Invoke-Command -Fatal { & node ..\yarn.js -s install --prefer-offline }
	}

	Invoke-Command -Fatal { & node ..\yarn.js -s update -s $Src -o $Out }

} finally {
	Pop-Location
}