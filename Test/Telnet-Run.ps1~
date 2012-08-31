$telnet = [Reflection.Assembly]::LoadFrom($samtest_path+"\Telnet.dll")
$location = "C:\"
$name_of_exe = "RegressionTest"
$name_of_solution = "STM32F10x"

Function MSBuild-Run
{

	$gdb_path_root = $CODESOURCERYPATH
	$mfbase_location = $location + "MicroFrameworkPK_v4_0"
	$build_path = $mfbase_location + "\Solutions\" + $name_of_solution + "\" + $name_of_exe
	$telnetInst = New-Object SamTest.Telnet
	#$telnetInst.Start($BUILDOUTPUT + "RegressionTest.bin")
	$telnetInst.Start()

}

MSBuild-Run
