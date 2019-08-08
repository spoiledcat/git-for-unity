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
	[string[]]
	$PackageName,
	[string[]]
	$Version,
	[string[]]
	$Sources,
	[string[]]
	$Extras,
	[string[]]
	$Ignores,
	[string[]]
	$BaseInstall,
	[switch]
	$DontPackage,
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

	if ($DontPackage) {
		Run-Command -Fatal { & node ..\yarn.js start -o $OutputFolder -n $PackageName -s $Sources -v $Version -i $Ignores -e $Extras -t $BaseInstall -k }
	} else {
		Run-Command -Fatal { & node ..\yarn.js start -o $OutputFolder -n $PackageName -s $Sources -v $Version -i $Ignores -e $Extras -t $BaseInstall }
	}

} finally {
	Pop-Location
}