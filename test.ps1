# the runspace for the script has already been initialized
$samtest_config = ([xml](get-content SamTest.config.xml)).SamTest
$test_dir = $samtest_config.project.testdir
if(!$test_dir.EndsWith("\")) { $test_dir += "\" }
$tests = get-item "$($test_dir)*" | where-object { $_.Mode -eq "d----" }

$suiteInst = new-object SamTest.SuiteInstance
$suiteInst.StartTemp($project_path)
#$suiteInst.Compile($gdb_path_root, $samtest_config.project.root, $samtest_config.project.proj)
$suiteInst.PrepareGDB($samtest_config.project.output, $samtest_config.project.entry)

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
        $testInst._Setup()
        $testInst._Execute()
        $testInst._Teardown()
        $insts += @{$config.name=$testInst}
    }
}