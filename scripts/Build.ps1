[CmdletBinding()]
Param(
	[switch]
	$Trace = $false
)


Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

Invoke-Command -Fatal { common\nuget restore }
Invoke-Command -Fatal { .\hMSBuild.bat /t:restore /verbosity:minimal }
Invoke-Command -Fatal { .\hMSBuild.bat /verbosity:minimal }
