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
	$Source,
	[string]
	$Out,
	[string]
	$Name,
	[string]
	$Version,
	[switch]
	$Unity = $false,
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
		Run-Command -Fatal { & node ..\yarn.js -s install --prefer-offline }
	}

	$doUnity = ""
	if ($Unity) {
		$doUnity = "-u"
	}

	Run-Command -Fatal { & node ..\yarn.js -s run zip -s $Source -o $Out -n $Name -v $Version $doUnity }

} finally {
	Pop-Location
}