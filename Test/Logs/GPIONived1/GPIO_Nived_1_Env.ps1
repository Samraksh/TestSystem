###############################################################################
# The Samraksh Company
# This is an auto generated file from tool TestRig 
###############################################################################


mkdir C:\Test\Logs\GPIONived1
Copy-Item C:\Test\TestRig\TestRig\msbuild.cs -destination C:\Test\Logs\GPIONived1
Copy-Item C:\Test\TestRig\TestRig\GDB.cs -destination C:\Test\Logs\GPIONived1
Copy-Item C:\Test\TestRig\TestRig\GDBCommand.cs -destination C:\Test\Logs\GPIONived1
cd C:\Test\Logs\GPIONived1
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
&$compiler /target:library /out:GDB.dll GDB.cs GDBCommand.cs
&$compiler /target:library /out:MsBuild.dll msbuild.cs
