[CmdletBinding()]
Param(
	[string]
	$Manifest,
	[switch]
	$Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

Get-Content $Manifest | ConvertFrom-Json | Get-ObjectMembers | % {
	$key = $_.Key
	$val = $_.Value
	$val | % {
		Push-AppveyorArtifact $_.path -FileName (Split-Path $_.path -leaf) -DeploymentName $key
		$hasmd5 = $_.PSObject.Properties.Name -contains 'md5Path'
		if ($hasmd5) {
			Push-AppveyorArtifact $_.md5Path -FileName (Split-Path $_.md5Path -leaf) -DeploymentName $key
		}
	}
}
Push-AppveyorArtifact $Manifest -FileName (Split-Path $Manifest -leaf) -DeploymentName manifest
