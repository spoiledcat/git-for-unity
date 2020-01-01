[CmdletBinding()]
Param(
	[string]
	$OutDir = '',
	[switch]
	$Yamato = $false,
	[switch]
	$Trace = $false
)


Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

$packageDir="$rootDirectory\packages"
$testDir="$rootDirectory\build\tests\bin"
$nunit="$packageDir\NUnit.ConsoleRunner.3.10.0\tools\nunit3-console.exe"
$nure="$packageDir\Nure.1.2.0\tools\nure.exe"
$xml="$testDir\TestResult.xml"
$outDir="$rootDirectory\test-results\"

if ($OutDir -ne '') {
	$outDir = $OutDir
}

$TimeoutDuration = 5*60

$args = Get-ChildItem -Recurse "$testDir\*.Tests.dll" | % { $_.FullName }
$args += "--where:cat!=DoNotRunOnAppVeyor"

Write-Verbose "$nunit $args"

Push-Location $testDir

try {
	Invoke-Process -Fatal $TimeoutDuration $nunit $args

	if ($Yamato) {
		Invoke-Command { & $nure $xml --html -o $outDir }
	}
} finally {
	Pop-Location
}
