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

$srcDir = Join-Path $rootDirectory 'build\packages'
$targetDir = Join-Path $rootDirectory 'upm-ci~\packages'

Remove-Item "$targetDir\*" -Force -ErrorAction SilentlyContinue
New-Item -itemtype Directory -Path $targetDir -Force -ErrorAction SilentlyContinue

Get-ChildItem -Directory $srcDir | % {
    if (Test-Path "$srcDir\$($_)\package.json") {
        try {
            Push-Location (Join-Path $srcDir $_.Name)
            $package = Invoke-Command -Fatal { & npm pack -q }
            $package = "$package".Trim()
            $tgt = Join-Path $targetDir $package
            Move-Item $package $tgt -Force
            Copy-Item "package.json" (Join-Path $targetDir "$package.json") -Force

            Write-Output "Created package $tgt\$package"
        } finally {
            Pop-Location
        }
    }
}

Write-Output "Writing packages.json"

try {
    Push-Location $targetDir

    $file = "packages.json"
    $count = 0

    Set-Content $file "{"

    Get-ChildItem "*.tgz" | % {
        if ($count -gt 0) {
            Add-Content $file ","
        }
        $json = Get-Content "$($_.Name).json"
        Add-Content $file """$($_.Name)"": "
        Add-Content $file $json
        $count = $count + 1
    }

    Add-Content $file "}"

} finally {
    Pop-Location
}
