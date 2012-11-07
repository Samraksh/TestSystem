$OpenOCD = [Reflection.Assembly]::LoadFrom($samtest_path+"\OpenOCD.dll")

Function Openocd-Run
{
	$ocdInst = New-Object SamTest.OpenOCD
	$ocdInst.Start()

}

Openocd-Run
	
