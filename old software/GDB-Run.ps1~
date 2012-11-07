$GDB = [Reflection.Assembly]::LoadFrom($samtest_path+"\GDB.dll")

$axf = "C:\\MicroFrameworkPK_v4_0\\BuildOutput\\THUMB2\\GCC4.2\\le\\FLASH\\debug\\STM32F10x\\bin\\RegressionTest.axf"

Function GDB-Run
{
	$gdbInst = New-Object SamTest.GDB
	$gdbInst.Start()
	Write-Host $axf
	$gdbInst.Load($axf)

}

GDB-Run
