cd C:\Main\Work\Projects\Testing\TestInfraStructure\SamTest\SamTest
$buildpath = "C:\Main\Work\Projects\Testing\TestInfraStructure\SamTest\SamTest"
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
&$compiler /target:library /lib:$buildpath\logic /lib:$buildpath\textile /out:SamTest.dll /r:SaleaeDeviceSdkDotNet.dll /r:Textile.dll *.cs

#cd server
#&$compiler /target:library /r:System.Management.Automation.dll /r:System.Web.Extensions.dll /out:SamServer.dll *.cs