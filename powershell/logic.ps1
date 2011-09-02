$logic_connect_id = "Logic.OnLogicConnect"
$logic_disconnect_id = "Logic.OnDisconnect"
$logic_read_id = "Logic.OnReadData"
$logic_write_id = "Logic.OnWriteData"
$logic_error_id = "Logic.OnError"
$logic_data_buffer = @()

function Start-Logic {
	$saleae = [Reflection.Assembly]::LoadFile("C:\SamTest\powershell\SaleaeDeviceSdkDotNet.dll")
	$devices = New-Object SaleaeDeviceSdkDotNet.MSaleaeDevices
	Register-ObjectEvent $devices OnLogicConnect $logic_connect_id { OnConnect-Logic $Event.SourceArgs } | Out-DBG "Logic.RegisterObjectEvent.OnLogicConnect"
	Register-ObjectEvent $devices OnDisconnect $logic_disconnect_id { OnDisconnect-Logic $Event.SourceArgs } | Out-DBG "Logic.RegisterObjectEvent.OnDisconnect"
    $devices.BeginConnect()
	Set-Variable logic_devices $devices -Scope Global
    Set-Variable logic_saleae $saleae -Scope Global
}

function Kill-Logic {
	$logic_devices = $null;
	$logic_saleae = $null;
	Unregister-Event $logic_connect_id
	Unregister-Event $logic_disconnect_id
}

function OnConnect-Logic {
	param($CBdata)
	$device_id = $CBdata[0]
	$logic = $CBdata[1]
	Write-Sam "Logic connected $device_id"
	Register-ObjectEvent $logic OnReadData $logic_read_id { OnReadData-Logic $Event.SourceArgs } | Out-DBG "Logic.RegisterObjectEvent.OnReadData"
	Register-ObjectEvent $logic OnWriteData $logic_write_id { OnWriteData-Logic $Event.SourceArgs } | Out-DBG "Logic.RegisterObjectEvent.OnWriteData"
	Register-ObjectEvent $logic OnError $logic_error_id { OnError-Logic $Event.SourceArgs } | Out-DBG "Logic.RegisterObjectEvent.OnError"
	$logic.SampleRateHz = 250000;
	Set-Variable logic_logic $logic -Scope Global
}

function OnDisconnect-Logic {
	param($device_id)
	Write-Sam "Logic disconnected $device_id"
	Unregister-Event $logic_read_id
	Unregister-Event $logic_write_id
	Unregister-Event $logic_error_id
}

function OnReadData-Logic {
	param($CBdata)
	$device_id = $CBdata[0]
	$data = @($CBdata[1])
	#$logic_data_buffer = $data
    Write-Sam "$data"#"$($data.Length), $data"
}

function OnWriteData-Logic {
	param($CBdata)
	$device_id = $CBdata[0]
	$data = $CBdata[1]
}

function OnError-Logic {
	param($device_id)
	Write-Sam "Logic error $device_id"
}

function Read-Logic {
	$logic_logic.ReadStart()
	Write-Sam "Read started..."
}

function Write-Logic {
	$logic_logic.WriteStart()
	Write-Sam "Write-Started"
}

function Read-Byte-Logic {
	$logic_logic.GetInput()
}

function Write-Byte-Logic {
	param($byte)
	$logic_logic.SetOutput($byte)
	Write-Sam "Byte $byte written"
}

function Set-SampleRate-Logic {
	param($rate)
	$logic_logic.SampleRateHz = $rate
	Write-Sam "Rate set to $rate"
}

function Stop-Logic {
	if($logic_logic.IsStreaming()) {
		$logic_logic.Stop()
		Write-Sam "Logic is now stopped."
	} else {
		Write-Sam "Logic was not streaming. Could not stop."
	}
}