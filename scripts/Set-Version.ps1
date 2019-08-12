Param(
	[string]
	$NewVersion = ''
	,
	[switch]
	$BumpMajor = $false
	,
	[switch]
	$BumpMinor = $false
	,
	[switch]
	$BumpPatch = $false
	,
	[switch]
	$BumpBuild = $false
	,
	[int]
	$BuildNumber = -1
	,
	[switch]
	$Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) { Set-PSDebug -Trace 1 }

. $PSScriptRoot\helpers.ps1 | out-null

& {
	Trap {
		Write-Output "Setting version failed"
		Write-Output "Error: $_"
		exit 0
	}

	if ($NewVersion -eq '') {
		if (!$BumpMajor -and !$BumpMinor -and !$BumpPatch -and !$BumpBuild){
			Die -1 "You need to indicate which part of the version to update via -BumpMajor/-BumpMinor/-BumpPatch/-BumpBuild flags or a custom version via -NewVersion"
		}
	}

	$source = Get-Content -Raw $PSScriptRoot\TheVersion.cs
	Add-Type -TypeDefinition "$source"

	function Read-Version([string]$versionFile) {
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$version = $versionjson.version
		$parsed = [TheVersion]::Parse("$version")
		$parsed
	}

	function Write-Version([string]$versionFile, [TheVersion]$version) {
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$versionjson.version = $version.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	function Set-Version([string]$versionFile, [string]$newValue) {
		$parsed = [TheVersion]::Parse("$newValue")
		Write-Version versionFile $parsed
	}

	function Bump-Version([string]$versionFile,
		[bool]$bumpMajor, [bool] $bumpMinor,
		[bool]$bumpPatch, [bool] $bumpBuild,
		[int]$newValue = -1)
	{
		$versionjson = Get-Content $versionFile | ConvertFrom-Json
		$version = $versionjson.version
		$parsed = [TheVersion]::Parse("$version")

		if ($bumpMajor) {
			if ($newValue -ge 0) {
				$newVersion = $parsed.SetMajor($newValue)
			} else {
				$newVersion = $parsed.bumpMajor()
			}
		} elseif ($bumpMinor) {
			if ($newValue -ge 0) {
				$newVersion = $parsed.SetMinor($newValue)
			} else {
				$newVersion = $parsed.BumpMinor()
			}
		} elseif ($bumpPatch) {
			if ($newValue -ge 0) {
				$newVersion = $parsed.SetPatch($newValue)
			} else {
				$newVersion = $parsed.BumpPatch()
			}
		} elseif ($bumpBuild) {
			if ($newValue -ge 0) {
				$newVersion = $parsed.SetBuild($newValue)
			} else {
				$newVersion = $parsed.BumpBuild()
			}
		}

		$versionjson.version = $newVersion.Version
		ConvertTo-Json $versionjson | Set-Content $versionFile
	}

	if ($NewVersion -ne '') {
		$versionFile = "$rootDirectory\src\com.unity.git.ui\version.json"
		Set-Version $versionFile $NewVersion

		$versionFile = "$rootDirectory\src\com.unity.git.api\version.json"
		Set-Version $versionFile $NewVersion
	} else {
		$versionFile = "$rootDirectory\src\com.unity.git.ui\version.json"
		Bump-Version $versionFile $BumpMajor $BumpMinor $BumpPatch $BumpBuild $BuildNumber

		$versionFile = "$rootDirectory\src\com.unity.git.api\version.json"
		Bump-Version $versionFile $BumpMajor $BumpMinor $BumpPatch $BumpBuild $BuildNumber
	}

}