###############################################################################
# The Samraksh Company
# This is an auto generated file from tool TestRig 
###############################################################################
Echo "Executing GPIO" | Out-File -Append GPIO_Nived_1_Out.log


Echo "Stage 1 : Launching Build" | Out-File GPIO_Nived_1_Out.log


$msbuild = [Reflection.Assembly]::LoadFrom("msbuild.dll")
$gdb = [Reflection.Assembly]::LoadFrom("GDB.dll")


$msbuildInst = new-object TestRig.msbuild
$gdbInst = new-object TestRig.gdb


$testPath = "C:\MicroFrameworkPK_v4_0\Solutions\STM32F10x\RegressionTest"


$buildProjName = "RegressionTest.proj"


$codeSourceryPath = "C:\codesourcery"


$mfInstallationPath = "C:\MicroFrameworkPK_v4_0"
Echo "Stage 1 : Setting Environment" | Out-File -Append GPIO_Nived_1_Out.log
$msbuildInst.Init($testPath,$buildProjName,$codeSourceryPath,$mfInstallationPath)


$msbuildInst.Start()


$msbuildInst.SetEnv()
Echo "Stage 1 : Cleaning Project" | Out-File -Append GPIO_Nived_1_Out.log


$msbuildInst.Clean()


Echo "Stage 1 : Building Project" | Out-File -Append GPIO_Nived_1_Out.log
$msbuildInst.Build()
Echo "Stage 1 : Build Complete" | Out-File -Append GPIO_Nived_1_Out.log


Echo "Stage 2: Launching Deployment" | Out-File -Append GPIO_Nived_1_Out.log


$gdbInst.Init()


$gdbInst.Start()


$gdbInst.Load()
