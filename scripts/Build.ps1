Param(
	[switch]
	$Trace = $false
)


Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

Run-Command -Fatal { .\hMSBuild.bat /t:restore /verbosity:minimal }
Run-Command -Fatal { .\hMSBuild.bat /verbosity:minimal }
