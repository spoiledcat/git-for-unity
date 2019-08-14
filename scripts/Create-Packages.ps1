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
    [string]
    $TmpDir = '',
    [switch]
    $SkipUnity = $false,
    [switch]
    $SkipPackman = $false,
    [switch]
    $SkipUpm = $false,
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

    $tmpDir=$TmpDir
    if ($tmpDir -eq '') {
        $tmpDir="$rootDirectory\obj"
    } elseif (![System.IO.Path]::IsPathRooted($tmpDir)) {
        $tmpDir="$rootDirectory\$tmpDir"
    }

    $artifactDir="$rootDirectory\artifacts"
    $packageDir="$rootDirectory\build\packages"
    $srcDir="$rootDirectory\src"
    $packagingScriptsDir="$rootDirectory\packaging\create-unity-packages"

    Write-Output "Cleaning up previous build artifacts from $tmpDir..."
    Remove-Item $tmpDir -Force -Recurse -ErrorAction SilentlyContinue
    Remove-Item $artifactDir -Force -Recurse -ErrorAction SilentlyContinue

    $pkgName="com.unity.git.api"
    $pkgSrcDir="$packageDir\$pkgName"
    $ignorefile="$srcDir\$pkgName\.npmignore"
    $baseInstall="Packages\$pkgName"
    $outDir=$artifactDir

    Write-Verbose "$packagingScriptsDir\run.ps1 -Source $pkgSrcDir -Out $artifactDir -Name $pkgName -Version $Version $extrasDir -Ignore $ignorefile -BaseInstall $baseInstall"
    Write-Output "Packaging $pkgName..."
    Run-Command -Fatal { & $packagingScriptsDir\run.ps1 -Source $pkgSrcDir -Out $artifactDir -Name $pkgName -Version $Version -Ignore $ignorefile -BaseInstall $baseInstall -Tmp $tmpDir -SkipUnity:$SkipUnity -SkipPackman:$SkipPackman -SkipUpm:$SkipUpm }

    $pkgName="com.unity.git.ui"
    $pkgSrcDir="$packageDir\$pkgName"
    $ignorefile="$srcDir\$pkgName\.npmignore"
    $baseInstall="Packages\$pkgName"
    $outDir=$artifactDir

    Write-Verbose "$packagingScriptsDir\run.ps1 -Source $pkgSrcDir -Out $artifactDir -Name $pkgName -Version $Version $extrasDir -Ignore $ignorefile -BaseInstall $baseInstall"
    Write-Output "Packaging $pkgName..."
    Run-Command -Fatal { & $packagingScriptsDir\run.ps1 -Source $pkgSrcDir -Out $artifactDir -Name $pkgName -Version $Version -Ignore $ignorefile -BaseInstall $baseInstall -Tmp $tmpDir -SkipUnity:$SkipUnity -SkipPackman:$SkipPackman -SkipUpm:$SkipUpm }
}