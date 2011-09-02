# Configuration
$debug = $true
$samtest_path = "C:\SamTest"
$project_path = "C:\MicroFrameworkPK_v4_0"

$openocd_path_bin = ""
$gdb_path_bin= ""

$env:Path = "$samtest_path;$openocd_path_bin;$gdb_path_bin"
cd $project_path
$samtest_config = [xml](get-content SamTest.config.xml)