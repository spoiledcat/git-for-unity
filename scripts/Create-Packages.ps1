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
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

& {
    Trap {
        Write-Output "Creating packages failed"
        Write-Output "Error: $_"
        exit -1
    }

    if ($Version -eq '') {
        Die -1 "You need to pass the -Version parameter"
    }

    $artifactDir="$rootDirectory\artifacts"
    $tmpDir="$rootDirectory\obj"
    $packageDir="$rootDirectory\build\packages"
    $srcDir="$rootDirectory\src"
    $packagingScriptsDir="$rootDirectory\packaging\create-unity-packages"

    Write-Output "Cleaning up previous build artifacts..."
    Remove-Item $tmpDir -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item $artifactDir -Force -Recurse -ErrorAction SilentlyContinue

    $pkgName="com.unity.git.api"
    $pkgSrcDir="$packageDir\$pkgName"
    $extrasDir="$srcDir\extras\$pkgName"
    $ignorefile="$srcDir\$pkgName\.npmignore"
    $baseInstall="Packages\$pkgName"
    $outDir=$artifactDir

    Write-Verbose "$packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall"
    Write-Output "Packaging $pkgName..."
    Run-Command -Fatal { & $packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall -Tmp $tmpDir }

    $pkgName="com.unity.git.ui"
    $pkgSrcDir="$packageDir\$pkgName"
    $extrasDir="$srcDir\extras\$pkgName"
    $ignorefile="$srcDir\$pkgName\.npmignore"
    $baseInstall="Packages\$pkgName"
    $outDir=$artifactDir

    Write-Verbose "$packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall"
    Write-Output "Packaging $pkgName..."
    Run-Command -Fatal { & $packagingScriptsDir\run.ps1 $pkgSrcDir $outDir $pkgName $Version $extrasDir $ignorefile $baseInstall -Tmp $tmpDir }
}