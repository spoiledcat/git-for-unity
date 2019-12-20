[CmdletBinding()]
Param(
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

$packagesDir = Join-Path $rootDirectory 'build\packages'
$upmDir = Join-Path $rootDirectory 'build\upm'
$srcDir = Join-Path $rootDirectory 'src'

New-Item -itemtype Directory -Path $upmDir -Force -ErrorAction SilentlyContinue

# the loops I have to go throught to get upm to do the right thing...
Get-ChildItem -Directory $srcDir | % {
    if (Test-Path "$srcDir\$($_)\package.json") {
        Write-Output "Packing $($_.Name)"

        $pkgdir = Join-Path $upmDir $_.Name

        $src = Join-Path $packagesDir $_.Name
        $target = $upmDir
        Copy-Item $src $target -Recurse -Force -ErrorAction SilentlyContinue

        $src = "$srcDir\$($_.Name)\Tests.meta"
        $target = $pkgdir
        Copy-Item $src $target -Force -ErrorAction SilentlyContinue

        $testsdir = Join-Path $pkgdir "Tests"
        New-Item -itemtype Directory -Path $testsdir -Force -ErrorAction SilentlyContinue

        $src = "$packagesDir\$($_.Name).tests\*"
        $target = "$testsdir\"
        Copy-Item $src $target -Recurse -Force -ErrorAction SilentlyContinue

        Invoke-Command -Fatal { & upm-ci package pack --package-path $pkgdir }
    }
}
