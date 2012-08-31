cd C:\Test
$buildpath = "C:\Test"
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
&$compiler /target:library /out:OpenOCD.dll OpenOCD.cs
&$compiler /target:library /out:GDB.dll GDB.cs GDBCommand.cs
&$compiler /target:library /out:MsBuild.dll msbuild.cs

#cd server
#&$compiler /target:library /r:System.Management.Automation.dll /r:System.Web.Extensions.dll /out:SamServer.dll *.cs
