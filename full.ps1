# Configuration
$debug = $true
$samtest_path = "C:\SamTest"
$project_path = "C:\MicroFrameworkPK_v4_0"

$openocd_path_root = "$samtest_path\openocd-0.4.0"
$openocd_path_bin = "$openocd_path_root\bin"
$gdb_path_root = "$samtest_path\codesourcery"
$gdb_path_bin = "$gdb_path_root\bin"

#cd $samtest_path
#$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
#&$compiler /debug /target:library /lib:C:\SamTest\powershell /out:SamTest.dll /r:SaleaeDeviceSdkDotNet.dll *.cs

$env:Path = "$samtest_path;$openocd_path_bin;$gdb_path_bin"
cd $project_path

$samtest_config = ([xml](get-content SamTest.config.xml)).SamTest
$test_dir = $samtest_config.project.testdir
if(!$test_dir.EndsWith("\")) { $test_dir += "\" }
$tests = get-item "$($test_dir)*" | where-object { $_.Mode -eq "d----" }

# import DLLs
$saleae = [Reflection.Assembly]::LoadFrom($samtest_path+"\powershell\SaleaeDeviceSdkDotNet.dll")
$samtest = [Reflection.Assembly]::LoadFrom($samtest_path+"\SamTest.dll")

$suiteInst = new-object SamTest.SuiteInstance
$suiteInst.StartAll()
$suiteInst.Compile($gdb_path_root, $samtest_config.project.root, $samtest_config.project.proj)

#TODO: remove insts - once a test fails its over
$insts = @{}
foreach($test in $tests) {
    $path = "$($test.ToString())"
    if(!$path.EndsWith("\")) { $path += "\" }
    $config_file = get-item "$($path)*" | where-object { $_.Extension -eq ".xml" }
    if($config_file) {
        $config = ([xml](get-content $config_file)).Test
        $path+$($config.file)
        #add-type -path $path$($config.file) -referencedassemblies $samtest.Location, $saleae.Location, System.Xml
        $testInst = new-object $($config.class)($path, $config)
        #$testInst = new-object SamTest.TestInstance($path, $config)
        $insts += @{$config.name=$testInst}
    }
}