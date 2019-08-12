<#
.SYNOPSIS
    Packages a build of GitHub for Unity
.DESCRIPTION
    Packages a build of GitHub for Unity
#>

[CmdletBinding()]

Param(
    [string]
    $Version,
    [switch]
    $Trace = $false,
    [switch]
    $Verbose = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null


$artifactDir="$rootDirectory\artifacts"
$tmpDir="$rootDirectory\tmp"
$packageDir="$rootDirectory\build\packages"
$srcDir="$rootDirectory\src"
$packagingScriptsDir="$rootDirectory\packaging\create-unity-packages"

$pkgName="com.unity.git.api"
$pkgSrcDir="$packageDir\$pkgName"
$extrasDir="$srcDir\extras\$pkgName"
$ignorefile="$srcDir\$pkgName\.npmignore"
$baseInstall="Packages\$pkgName"
$outDir=$artifactDir

if ($Verbose) {
    Write-Output "$packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall"
}
Run-Command -Fatal -Quiet { & $packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall }

$pkgName="com.unity.git.ui"
$pkgSrcDir="$packageDir\$pkgName"
$extrasDir="$srcDir\extras\$pkgName"
$ignorefile="$srcDir\$pkgName\.npmignore"
$baseInstall="Packages\$pkgName"
$outDir=$artifactDir

if ($Verbose) {
    Write-Output "$packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall"
}
Run-Command -Fatal { & $packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall }
