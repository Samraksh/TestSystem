cd C:\SamTest
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
&$compiler /target:library /lib:C:\SamTest\logic /out:SamTest.dll /r:SaleaeDeviceSdkDotNet.dll *.cs

cd server
&$compiler /target:library /r:System.Management.Automation.dll /r:System.Web.Extensions.dll /out:Server.dll *.cs