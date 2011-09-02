Add-Type -Path C:\SamTest\powershell\Tester.cs
cd C:\SamTest\powershell
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
&$compiler /debug /target:library /lib:C:\SamTest\powershell /out:Tester.dll /r:SaleaeDeviceSdkDotNet.dll *.cs
$saleae = [Reflection.Assembly]::LoadFrom("C:\SamTest\powershell\SaleaeDeviceSdkDotNet.dll")
$tester = [Reflection.Assembly]::LoadFrom("C:\SamTest\powershell\Tester.dll")
$face = new-object Tester.TestInstance
Register-ObjectEvent $face Connected -SourceIdentifier TestInstance.Connected
Register-ObjectEvent $face ParseDone -SourceIdentifier TestInstance.ParseDone

[AppDomain]::CurrentDomain.GetAssemblies()

cd C:\Users\Nick\Desktop\SaleaeDeviceSdk-1.1.9\C#.NET\ConsoleDemo
$compiler = "$env:windir/Microsoft.NET/Framework/v2.0.50727/csc"
&$compiler /debug /target:library /out:ConsoleDemo.dll /r:SaleaeDeviceSdkDotNet.dll *.cs
$saleae = [Reflection.Assembly]::LoadFile("C:\Users\Nick\Desktop\SaleaeDeviceSdk-1.1.9\C#.NET\ConsoleDemo\SaleaeDeviceSdkDotNet.dll")
$tester = [Reflection.Assembly]::LoadFile("C:\Users\Nick\Desktop\SaleaeDeviceSdk-1.1.9\C#.NET\ConsoleDemo\ConsoleDemo.dll")
$face = new-object Tester.TestInstance

