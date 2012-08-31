# the runspace for the script has already been initialized
$samtest_config = ([xml](get-content SamTest.config.xml)).SamTest
$test_dir = $samtest_config.project.testdir
if(!$test_dir.EndsWith("\")) { $test_dir += "\" }
# get all potential tests by directory
$tests = get-item "$($test_dir)*" | where-object { $_.Mode -eq "d----" }

# new suite instance
$suiteInst = new-object SamTest.SuiteInstance
# start temp programs
$suiteInst.StartTemp($project_path)
# feedback directory
# $commit_id = "abc123"
$feedback_root = $samtest_path + "\server\tests\$commit_id"
try {
    mkdir $feedback_root
} catch {
    "this commit has already been run"
    exit
}
$suiteInst.SetFeedback($feedback_root)
# git checkout
if($checkout) {
    $suiteInst.Checkout($project_path, $commit_id)
}
# msbuild project
$suiteInst.Compile($gdb_path_root, $samtest_config.project.root, $samtest_config.project.proj)
# connect and load to target
$suiteInst.PrepareGDB($samtest_config.project.output, $samtest_config.project.entry)

#TODO: remove insts - once a test fails its over
$insts = @{}
foreach($test in $tests) {
    # current path
    $path = "$($test.ToString())"
    if(!$path.EndsWith("\")) { $path += "\" }
    # see if a config.xml file exists
    $config_file = get-item "$($path)*" | where-object { $_.Extension -eq ".xml" }
    if($config_file) {
        # get the contents of the config.xml file
        $config = ([xml](get-content $config_file)).Test
        # $path+$($config.file) # this is the cs file
        # invoke the compiler
        &$compiler /target:library /lib:C:\SamTest /out:$path\test.dll /r:SamTest.dll $path*.cs
        [Reflection.Assembly]::LoadFrom($path+"\test.dll")
        #add-type -path $path$($config.file) -referencedassemblies $samtest.Location, System.Xml
        # instantiate new test object
        $testInst = new-object $($config.class)($suiteInst.feedback, $path, $config)
        # run through test
        $testInst._Setup()
        $testInst._Execute()
        $testInst._Teardown()
        # append instance to test array
        $insts += @{$config.name=$testInst}
    }
}
# generate HTML results
$suiteInst.feedback.HTML()