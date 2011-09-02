# Configuration
$debug = $true
$samtest_path = "C:\SamTest"
$project_path = "C:\MicroFrameworkPK_v4_0"

$openocd_path_bin = "$samtest_path\openocd-0.4.0\bin\"
$gdb_path_bin = "$samtest_path\codesourcery\bin\"

$env:Path = "$samtest_path;$openocd_path_bin;$gdb_path_bin"
cd $project_path

$samtest_config = [xml](get-content SamTest.config.xml)
$tests = get-item "$($samtest_config.SamTest.project.testdir)\*" | where-object { $_.Mode -eq "d----" }

$saleae = [Reflection.Assembly]::LoadFrom("C:\SamTest\powershell\SaleaeDeviceSdkDotNet.dll")
$samtest = [Reflection.Assembly]::LoadFrom("C:\SamTest\SamTest.dll")

$openocd = [SamTest.OpenOCD]::Instance
$gdb = [SamTest.GDB]::Instance
$msbuild = [SamTest.MSBuild]::Instance
$logic = [SamTest.Logic]::Instance
$parser = [SamTest.Parser]::Instance

$insts = @{}
foreach($test in $tests) {
    $path = "$($test.ToString())\"
    $config_file = get-item "$($path)*" | where-object { $_.Extension -eq ".xml" }
    if($config_file) {
        $config = [xml](get-content $config_file)
        #Add-Type $config.controlFile
        #$testInst = new-object $config.controlClass($path, $config.Test)
        $testInst = new-object SamTest.TestInstance($path, $config.Test)
        $insts += @{$config.Test.name=$testInst}
    }
}