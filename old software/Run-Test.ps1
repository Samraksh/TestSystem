#######################################################################################################################
# Name : Run-Test
# Author : Nived.Sivadas@samraksh.com
# Description : Single point of entry for the testing framework
# History : Seminal Version
#
#######################################################################################################################

# 
# Function Name : Call-Info
# Description : Display options to the user 
#
#

$config_file = ""
$test_name = ""
$location = ""
$stage = 0
$mfbase_location = ""
$name_of_exe = "RegressionTest"
$name_of_solution = "STM32F10x"
$build_path = ""
$samtest_path = Get-Location
$gdb_path_root = $CODESOURCERYPATH
#$saleae = [Reflection.Assembly]::LoadFrom($samtest_path.Path+"\logic\SaleaeDeviceSdkDotNet.dll")
#$textile = [Reflection.Assembly]::LoadFrom($samtest_path.Path+"\textile\Textile.dll")
#$samtest = [Reflection.Assembly]::LoadFrom($samtest_path.Path+"\SamTest.dll")
#$suiteInst = New-Object SamTest.SuiteInstance

Function Call-Info
{
	
	Write-Host "==========================================================================================="
	Write-Host ""	
	Write-Host "Syntax : Run-Test -cf <config_file>"
	Write-Host "Syntax : Run-Test -t <Name_of_Test> -l <Location_Of_Source> -o <name_of_output_exe> -s <solution>"
	Write-Host "OPTIONS"
	Write-Host "-cf config_file"
	Write-Host "`tSpecify the configuration file to use or specify the input as command line arguments"
	Write-Host ""
	Write-Host "-t Name_of_Test"
	Write-Host "`tSpecify the name of the test"
	Write-Host "`n"
	Write-Host "-l Location_Of_Source"
	Write-Host "`tSpecify the location of the Microframework root folder Ex: If C:\MF, specify C:\"
	Write-Host "-o Name_Of_Output_Exe"
	Write-Host "`tSpecify the name of the output to be loaded (RegressionTest | NativeSample >, this version does not support creation of new executables, if not specified RegressionTest assumed by default" 
	Write-Host "-s Name_Of_Solution"
	Write-Host "`tSpecify the solution to be built. Assumed as STM32F10x if not specified"
	Write-Host "============================================================================================"
}

Function Launch-Build
{

	Write-Host "`n`nBuilding Test and generating executable !!!!"

	$mfbase_location = $location + "MicroFrameworkPK_v4_0"
    $build_path = $mfbase_location + "\Solutions\" + $name_of_solution + "\" + $name_of_exe 
    $output_path = "BuildOutput/THUMB2/GCC4.2/le/FLASH/debug/" + $name_of_solution + "/bin/" + $name_of_exe + ".axf"
    cd $mfbase_location
    $set_env = $mfbase_location + "\setenv_gcc.cmd " + $gdb_path_root
    Invoke-Expression $set_env
    $cmd = "C:\WINDOWS\Microsoft.NET\Framework\v3.5\msbuild.exe" + " /t:build " + $build_path + "\" + $name_of_exe + ".proj"  
    Invoke-Expression $cmd
	#$entry = "ApplicationEntryPoint"
	#Write-Host $build_path
	#cd $mfbase_location
	#Write-Host $gdb_path_root	
	#$suiteInst.startBuild($mfbase_location)
	#$suiteInst.Compile($gdb_path_root,$build_path,$name_of_exe + ".proj")
        #$suiteInst.killBuild();

	Write-Host "`n`nBuild Complete !!!!"



}

Function Launch-GDB
{

	Write-Host "`n`nAttempting to Load code on Test Subject !!!!"

	
	$mfbase_location = $location + "MicroFrameworkPK_v4_0"
        $build_path = $mfbase_location + "\Solutions\" + $name_of_solution + "\" + $name_of_exe 
        $output_path = "BuildOutput/THUMB2/GCC4.2/le/FLASH/debug/" + $name_of_solution + "/bin/" + $name_of_exe + ".axf"
	$entry = "ApplicationEntryPoint"
	Write-Host $output_path

	if($env:OPENOCDPATH -eq "")
	{
		Write-Host "OpenOCD not found !!!!"
		Write-Host "Exiting !!!"
		Exit
	}
	$openocdexe = $OPENOCDPATH + "/bin/openocd-0.5.0.exe "


	Write-Host "`nLaunching OpenOCD Server"
	Invoke-Item $openocdexe " -f  $OPENOCDPATH/interface/olimex-arm-usb-tiny-h.cfg -f  $OPENOCDPATH/target/stm32f1x.cfg"  
	
	#cd $mfbase_location
	#Write-Host $gdb_path_root	
	#$suiteInst.startGDB($mfbase_location)
	#$suiteInst.PrepareGDB($output_path,$entry)
        #$suiteInst.killBuild();

	Write-Host "`n`nLoad Complete !!!!"



}

#
# Name : Launch-Stage1
# Description : Will be reponsible for building the test with the Microframework
#
#

Function Launch-Stage1
{
	Launch-Build
	#Launch-GDB

}

Function Launch-StateMachine
{
	if($stage -eq 0)
	{
		Launch-Stage1
	}
	elseif($stage -eq 1)
	{
		Launch-Stage2
	}
	elseif($stage -eq 2)
	{
		Launch-Stage3	
	}

}

if ($args.Count -eq 0)
{
	Write-Host "Error : Insufficient Number of Arguments"
	Call-Info
}
else
{
    Write-Host "Check Point 1"
	#Process-Arguments $args
	for($i = 0; $i -lt $args.Length; $i++)
	{
		Switch($args[$i])
		{
			"-cf" 
			{
				$i++;
				$config_file = $args[$i];
			}
			"-t" 
			{
				$i++;
				$test_name = $args[$i];
			}
			"-l"
			{
				$i++;
				$location = $args[$i++];
			}
			"-o"
			{
				$i++;
				$name_of_exe = $args[$i++];

			}
			default
			{
				Write-Host "Error : Invalid Arguments"
				Call-Info
				Exit
			}
		}
	}	

	Launch-StateMachine

}
