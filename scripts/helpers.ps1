Add-Type -AssemblyName "System.Core"
try {
Add-Type -TypeDefinition @"
public class ScriptException : System.Exception
{
    public int ExitCode { get; private set; }
    public ScriptException(string message, int exitCode) : base(message)
    {
        this.ExitCode = exitCode;
    }
}
"@
} catch {}

New-Module -ScriptBlock {
    $rootDirectory = Split-Path ($PSScriptRoot)
    $scriptsDirectory = Join-Path $rootDirectory "scripts"
    Export-ModuleMember -Variable scriptsDirectory,rootDirectory
}

New-Module -ScriptBlock {
    function Die([int]$exitCode, [string]$message, [object[]]$output) {
        #$host.SetShouldExit($exitCode)
        $ret = ""
        if ($output) {
            $ret = $output | %{ "`r`n$_" }
            Write-Host $ret
            $message += ". See output above."
        }
        $hash = @{
            Message = $message
            ExitCode = $exitCode
            Output = $ret
        }
        Throw (New-Object -TypeName ScriptException -ArgumentList $message,$exitCode)
        #throw $message
    }


    function Invoke-Command([scriptblock]$Command, [switch]$Fatal, [switch]$Quiet) {
        $output = ""

        $exitCode = 0

        if ($Quiet) {
            $output = & $command 2>&1 | %{ "$_" }
        } else {
            & $command
        }

        if (!$? -and $LastExitCode -ne 0) {
            $exitCode = $LastExitCode
        } elseif ($? -and $LastExitCode -ne 0) {
            $exitCode = $LastExitCode
        }

        if ($exitCode -ne 0) {
            if (!$Fatal) {
                Write-Host "``$Command`` failed" $output
            } else {
                Die $exitCode "``$Command`` failed" $output
            }
        }
        $output
    }

    function Invoke-Process([int]$Timeout, [string]$Command, [string[]]$Arguments, [switch]$Fatal = $false)
    {
        $args = ($Arguments | %{ "`"$_`"" })
        [object[]] $output = "$Command " + $args
        $exitCode = 0
        $outputPath = [System.IO.Path]::GetTempFileName()
        $process = Start-Process -PassThru -NoNewWindow -RedirectStandardOutput $outputPath $Command ($args | %{ "`"$_`"" })
        Wait-Process -InputObject $process -Timeout $Timeout -ErrorAction SilentlyContinue
        if ($process.HasExited) {
            $output += Get-Content $outputPath
            $exitCode = $process.ExitCode
        } else {
            $output += "Process timed out. Backtrace:"
            $output += Get-DotNetStack $process.Id
            $exitCode = 9999
        }
        Stop-Process -InputObject $process
        Remove-Item $outputPath
        if ($exitCode -ne 0) {
            if (!$Fatal) {
                Write-Host "``$Command`` failed" $output
            } else {
                Die $exitCode "``$Command`` failed" $output
            }
        }
        $output
    }

    function New-TempDirectory {
        $path = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
        New-Item -Type Directory $path
    }

    function Get-ObjectMembers {
        [CmdletBinding()]
        Param(
            [Parameter(Mandatory=$True, ValueFromPipeline=$True)]
            [PSCustomObject]$obj
        )
        $obj | Get-Member -MemberType NoteProperty | ForEach-Object {
            $key = $_.Name
            [PSCustomObject]@{Key = $key; Value = $obj."$key"}
        }
    }

    Export-ModuleMember -Function Die,Invoke-Command,Invoke-Process,New-TempDirectory,Get-ObjectMembers
}