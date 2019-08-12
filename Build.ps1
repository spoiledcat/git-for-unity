Param(
	[switch]
	$Trace = $false
)


Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\scripts\helpers.ps1 | out-null

& {
	Trap {
		Write-Output "Setting version failed"
		Write-Output "Error: $_"
		exit 0
	}


	Run-Command -Fatal { .\hMSBuild.bat /t:restore /verbosity:minimal }
	Run-Command -Fatal { .\hMSBuild.bat /verbosity:minimal }

}