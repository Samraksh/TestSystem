# GDB configuration
#    exe - name of the executable
$gdb_exe = "arm-none-eabi-gdb.exe"
#    path_root - root path
$gdb_path_root = "$samtest_path\codesourcery"
#    path_bin - path to binaries
$gdb_path_bin = "$gdb_path_root\bin\"
#    output_id - OutputDataReceived event SourceIdentifer
$gdb_output_id = "GDB.OutputDataReceived"
#    error_id - ErrorDataReceived event SourceIdentifer
$gdb_error_id = "GDB.ErrorDataReceived"
#    resultrecord_id - gdb result event SourceIdentifer
$gdb_resultrecord_id = "GDB.ResultRecordReceived"
#    asyncrecord_id - gdb async event SourceIdentifer
$gdb_asyncrecord_id = "GDB.AsyncRecordReceived"

# Start-GDB
#   starts an instance of GDB
Function Start-GDB {
    Write-Sam "Starting GDB..."
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.Filename = $gdb_exe
    $process.StartInfo.Arguments = "-quiet -fullname --interpreter=mi2"
    $process.StartInfo.UseShellExecute = $FALSE
    $process.StartInfo.WindowStyle = "Hidden"
    # streams
    $process.StartInfo.RedirectStandardInput = $TRUE
    $process.StartInfo.RedirectStandardOutput = $TRUE
    $process.StartInfo.RedirectStandardError = $TRUE
    # start
    $process.Start() | Out-DBG "GDB.Process.Start()"
    # StandardInput stream
    $input = $process.StandardInput
    # StandardOutput stream
    Register-ObjectEvent $process OutputDataReceived $gdb_output_id { $EventArgs.Data | Out-DBG "GDB.StandardOutput"; if($EventArgs.Data -like "[\^]*") { New-Event $gdb_resultrecord_id -MessageData $EventArgs.Data | Out-DBG "GDB.RegisterEngineEvent.ResultRecordReceived"; } elseif($data -like "[\*\+=]*") { New-Event $gdb_asyncrecord_id -MessageData $EventArgs.Data | Out-DBG "GDB.NewEvent.AsyncRecordReceived"; } } | Out-DBG "GDB.RegisterObjectEvent.StandardOutput"
    #Register-EngineEvent $gdb_resultecord_id { Process-ResultRecord-GDB $Event.MessageData } | Out-DBG "GDB.RegisterEngineEvent.ResultRecordReceived"
    Register-EngineEvent $gdb_asyncrecord_id { Process-AsyncRecord-GDB $Event.MessageData } | Out-DBG "GDB.RegisterEngineEvent.AsyncRecordReceived"
    $process.BeginOutputReadLine()
    # StandardError stream
    Register-ObjectEvent $process ErrorDataReceived $gdb_error_id { $EventArgs.Data | Out-DBG "GDB.StandardError"; } | Out-DBG "GDB.RegisterObjectEvent.StandardError"
    $process.BeginErrorReadLine()
    # Export handles up to global scope
    Set-Variable gdb_process $process -Scope Global
    Set-Variable gdb_input $input -Scope Global
    Write-Sam "GDB was started."
}

# Kill-GDB
#   kills GDB and unregisters associated events
Function Kill-GDB {
    Write-Sam "Killing GDB..."
    Unregister-Event $gdb_output_id
    Unregister-Event $gdb_error_id
    $gdb_process.Kill()
}

# Write-GDB
#   writes to the GDB process and awaits a resultrecord
#   @param - the line to write
#   @param - the timeout of the wait event; @default - 10
Function Write-GDB {
    param($line)
    $gdb_input.WriteLine($line)
    Wait-ResultRecord-GDB
}

Function Wait-ResultRecord-GDB {
    param($timeout=-1)
    $resultevent = Wait-Event $gdb_resultrecord_id -Timeout $timeout
    Process-ResultRecord-GDB $resultevent.MessageData
    Remove-Event $gdb_resultrecord_id
}

Function Process-ResultRecord-GDB {
    param($string)
    $resultrecord = Parse-ResultRecord-GDB $string
    Set-Variable gdb_resultrecord $resultrecord -Scope Global
}

Function Wait-AsyncRecordKey-GDB {
    param($key, $timeout=-1)
    Write-Sam "waiting for asynchronous key: "" $key ""..."
    do {
        $asyncrecord = Get-AsyncRecord-GDB
    } while(-not $asyncrecord.Contains($key));
    $asyncrecord
    Write-Sam "asynchronous key found."
}

Function Process-AsyncRecord-GDB {
    param($string)
    $asyncrecord = Parse-ResultRecord-GDB $string
    Set-Variable gdb_asyncrecord $asyncrecord -Scope Global
}

Function Parse-ResultRecord-GDB {
    param($string)
    $resultrecord = @{}
    #$string = $string.TrimStart('^')
    $resultclass, $string = Parse-ResultClass-GDB $string
    $result_hash = @{}
    while(!(IsNullOrEmpty($string))) {
        if($string.StartsWith(',')) { $string = $string.TrimStart(',');  }
        $result, $string = Parse-Result-GDB $string
        $result_hash += $result
    }
    $resultrecord = @{$resultclass=$result_hash}
    $resultrecord
}

Function Parse-ResultClass-GDB {
    param($string)
    $string.Split(",", 2) # $resultclass, $string
}

Function Parse-Result-GDB {
    param($string)
    $result = @{}
    $variable, $string = $string.Split("=", 2)
    $value, $string = Parse-Value-GDB $string
    $result += @{$variable=$value}
    $result
    $string
}

Function Parse-Value-GDB {
    param($string)
    if($string.StartsWith('{')) {
        $value = @{}
        $string = $string.TrimStart('{')
        if($string.StartsWith('}')) { $null; $string.TrimStart('}'); return }
        do {
            if($string.StartsWith(',')) { $string = $string.TrimStart(',');  }
            $result, $string = Parse-Result-GDB $string
            $value += $result
        } while($string.StartsWith(','))
        $value
        $string.TrimStart('}')
    } elseif($string.StartsWith("[")) {
        $value = @{}
        $string = $string.TrimStart('[')
        if($string.StartsWith(']')) { $null; $string.TrimStart(']'); return }
        do {
            if($string.StartsWith(',')) { $string = $string.TrimStart(',');  }
            $result, $string = Parse-Value-GDB $string
            $value += $result
        } while($string.StartsWith(','))
        $value
        $string.TrimStart(']')
    } elseif($string.StartsWith('"')) {
        $string = $string.TrimStart('"')
        $string.Split('"', 2)
    } else {
        Parse-Result-GDB $string
    }
}

Function Get-ResultRecord-GDB {
    $gdb_resultrecord
}

Function Get-AsyncRecord-GDB {
    $gdb_asyncrecord
}

Function Quit-GDB {
    Write-Sam "Quitting GDB..."
    Write-GDB "-gdb-exit"
}

Function Connect-GDB {
    Write-Sam "connecting to target..."
    # specify executable and symbol file(s)
    Write-GDB "-file-exec-and-symbols $axf_full"
    # connect
    Write-GDB "-target-select remote localhost:3333"
    # reset init
    Write-GDB "monitor reset init"
}

Function Load-GDB {
    # unlock flash
    Write-Sam "clearing flash..."
    Write-GDB "monitor stm32x unlock 0"
    # reset init
    Write-GDB "monitor reset init"
    # load
    Write-Sam "loading..."
    Write-GDB "-target-download"
}

Function Continue-GDB {
    Write-Sam "continuing..."
    Write-GDB "-exec-continue"
}

Function Jump-GDB {
    param($location)
    Write-Sam "jump to: $location"
    Write-Sam "continuing..."
    Write-GDB "-exec-jump $location"
}

Function Next-GDB {
    Write-Sam "next..."
    Write-GDB "-exec-next"
}

Function Insert-BP-GDB {
    param($location)
    Write-GDB "-break-insert $location"
    $res = Get-ResultRecord-GDB
    $res["^done"]["bkpt"]["number"]
}

Function List-BP-GDB {
    Write-GDB "-break-list"
}

Function Get-Info-BP-GDB {
    param($bp)
    Write-GDB "-break-info $bp"
}

Function Enable-BP-GDB {
    param($bp)
    Write-GDB "-break-enable $bp"
}

Function Disable-BP-GDB {
    param($bp)
    Write-GDB "-break-disable $bp"
}

Function BreakAfter-BP-GDB {
    param($bp, $count=1)
    Write-GDB "-break-after $bp $count"
}

Function Delete-BP-GDB {
    param($bp)
    Write-GDB "-break-delete $bp"
}
