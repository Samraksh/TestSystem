# openOCD configuration
#        exe - name of the executable
$openocd_exe = "openocd.exe"
#        interface_cfg - specify the interface config file
#$openocd_interface_cfg = "olimex-arm-usb-ocd.cfg"
$openocd_interface_cfg = "olimex-jtag-tiny.cfg"
#        board_cfg - specify the board config file
$openocd_board_cfg = "stm3210e_eval.cfg"
#        path_root - root path
$openocd_path_root = "$samtest_path\openocd-0.4.0"
#        path_bin - path to binaries
$openocd_path_bin = "$openocd_path_root\bin\"
#        path_tcl - path to tcl (tool command language) files, these are openocd's .cfg files
$openocd_path_tcl = "$openocd_path_root\tcl\"
#        output_id - OutputDataReceived event SourceIdentifer
$openocd_output_id = "OpenOCD.OutputDataReceived"
#        error_id - ErrorDataReceived event SourceIdentifer
$openocd_error_id = "OpenOCD.ErrorDataReceived"
#        info_id - InfoReceived event SourceIdentifer
$openocd_info_id = "OpenOCD.InfoReceived"

# Start-OpenOCD
#   starts an instance of OpenOCD
Function Start-OpenOCD {
    Write-Sam "Starting OpenOCD..."
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.Filename = $openocd_exe
    $process.StartInfo.Arguments = "-f interface\$openocd_interface_cfg -f board\$openocd_board_cfg"
    $process.StartInfo.UseShellExecute = $FALSE
    $process.StartInfo.WindowStyle = "Hidden"
    # streams
    $process.StartInfo.RedirectStandardInput = $TRUE
    $process.StartInfo.RedirectStandardOutput = $TRUE
    $process.StartInfo.RedirectStandardError = $TRUE
    # start
    $process.Start() | Out-DBG "OpenOCD.Process.Start()"
    # StandardInput stream
    $input = $process.StandardInput
    # StandardOutput stream
    Register-ObjectEvent $process OutputDataReceived $openocd_output_id { $EventArgs.Data | Out-DBG "OpenOCD.StandardOutput" } | Out-DBG "OpenOCD.RegisterObjectEvent.StandardOutput"
    $process.BeginOutputReadLine()
    # StandardError stream
    Register-ObjectEvent $process ErrorDataReceived $openocd_error_id { $EventArgs.Data | Out-DBG "OpenOCD.StandardError"; if($EventArgs.Data -like "Open On-Chip Debugger 0.4.0*") { New-Event $openocd_info_id -MessageData $EventArgs.Data | Out-DBG "OpenOCD.NewEvent.InfoReceived"; } } | Out-DBG "OpenOCD.RegisterObjectEvent.StandardError"
    $process.BeginErrorReadLine()
    # Export handles up one level
    Set-Variable openocd_process $process -Scope Global
    Set-Variable openocd_input $input -Scope Global
    Wait-Info-OpenOCD
    Write-Sam "OpenOCD was started."
}

# Kill-OpenOCD
#   kills OpenOCD and unregisters associated events
Function Kill-OpenOCD {
    Write-Sam "Killing OpenOCD..."
    Unregister-Event $openocd_output_id
    Unregister-Event $openocd_error_id
    $openocd_process.Kill()
    Write-Sam "OpenOCD was killed."
}

Function Wait-Info-OpenOCD {
    param($timeout=-1)
	try {
		$resultevent = Wait-Event $openocd_info_id -Timeout $timeout
		Remove-Event $openocd_info_id
	} catch {
		Throw "The Wait-Event $openocd_info_id failed."
	}
}