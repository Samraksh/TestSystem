$msbuild = [Reflection.Assembly]::LoadFrom($samtest_path+"\MsBuild.dll")
$location = "C:\"
$name_of_exe = "RegressionTest"
$name_of_solution = "STM32F10x"

Function MSBuild-Run
{

	$gdb_path_root = $CODESOURCERYPATH
	$mfbase_location = $location + "MicroFrameworkPK_v4_0"
	$build_path = $mfbase_location + "\Solutions\" + $name_of_solution + "\" + $name_of_exe
	$msbuildInst = New-Object SamTest.MSBuild
	$msbuildInst.Start($mfbase_location)
	$msbuildInst.SetEnv($gdb_path_root)
	$msbuildInst.Clean($build_path, $name_of_exe + ".proj")
	$msbuildInst.Build($build_path,$name_of_exe + ".proj")

}

MSBuild-Run
