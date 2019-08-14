[CmdletBinding()]
Param(
	[string]
	$Manifest,
	[string]
	$Out,
	[switch]
	$Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

Remove-Item "$Out" -Force -Recurse -ErrorAction SilentlyContinue
New-Item -itemtype Directory -Path "$Out" -Force -ErrorAction SilentlyContinue

$from = Split-Path $Manifest
Get-Content $Manifest | ConvertFrom-Json | Get-ObjectMembers | % {
	$filename = $_.Key
	Copy-Item "$from\$filename" "$Out"
}
Copy-Item "$Manifest" "$Out\packages.json"
