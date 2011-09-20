# Configuration
$debug = $true
$samtest_path = "C:\SamTest"
$project_path = "C:\MicroFrameworkPK_v4_0"

$openocd_path_root = "$samtest_path\openocd-0.4.0"
$openocd_path_bin = "$openocd_path_root\bin"
$gdb_path_root = "$samtest_path\codesourcery"
$gdb_path_bin = "$gdb_path_root\bin"
$git_path = "$samtest_path\git\cmd"

#cd $samtest_path
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
#&$compiler /debug /target:library /lib:C:\SamTest\powershell /out:SamTest.dll /r:SaleaeDeviceSdkDotNet.dll *.cs

$env:Path = "$samtest_path;$openocd_path_bin;$gdb_path_bin;$git_path"
cd $project_path

# import DLLs
$saleae = [Reflection.Assembly]::LoadFrom($samtest_path+"\logic\SaleaeDeviceSdkDotNet.dll")
$textile = [Reflection.Assembly]::LoadFrom($samtest_path+"\textile\Textile.dll")
$samtest = [Reflection.Assembly]::LoadFrom($samtest_path+"\SamTest.dll")

[SamTest.SuiteInstance]::StartPerm()