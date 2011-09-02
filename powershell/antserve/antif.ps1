$antifdll_path = "C:\\Program Files (x86)\\RockyLogic\\Ant\\antif.dll"

Function RL_Error {
    param($ret)
    Write-Host "RL Error: $ret"
}

Function RL_Info {
    param([Int32]$AntType=[Int32]111)
    $signature = @"
    [StructLayout(LayoutKind.Sequential)]
    public class TModParams {
      public int     Channels;               // Ant8:8, Ant16:16, Ant18e:18, BBX34:32
      public int     Depth;                  // 3K for Ant8, 2K for Ant16
      public int     PatternRecognizers;     // 2
      public int     XFunctions;             // 2
      public int     StateMachineFunctions;  // 8
      public int     MaxCounter;             // 1023
      public int     BytesWide;              //
      public int     ThresoldGroups;         //
      public int     FCchannels;             // >0 if frequency counter supported
      public int     ModCaps;                // misc capability flags in all modes
      public int     LAcaps;                 // misc capability flags in LA mode
      public int     FCcaps;                 // misc capability flags in FC mode
      public int     SlowestClockIx;
      public int     FastestClockIx;
      public int[]   Dummy;
    }
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_Info(Int32 AntType, [In, Out, MarshalAs(UnmanagedType.LPStruct)] TModParams pInfo, Int32 InfoBytes);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_Info -PassThru
    $pInfo = New-Object $type[1]
    $pInfo.Channels = 10;
    [Int32]$InfoBytes = [Int32]88
    if(($ret = $type[0]::_RL_Info($AntType, $pInfo, $InfoBytes)) -ne 0) { RL_Error $ret }
    $pInfo
}

Function RL_Initialize {
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_Initialize(ref IntPtr h);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_Initialize -PassThru
    [IntPtr]$h = [IntPtr]::Zero
    if(($ret = $type::_RL_Initialize([ref]$h)) -ne 0) { RL_Error $ret }
    Set-Variable antif_handle $h -Scope 1
}

Function RL_Finalize {
    param([IntPtr]$h=$antif_handle)
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_Finalize(IntPtr h);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_Finalize -PassThru
    if(($ret = $type::_RL_Finalize($h) -ne 0)) { RL_Error $ret }
}

Function RL_ConnectToServer {
    param([String]$anthost=[String]"127.0.0.1", [Int32]$port=[Int32]8279, [IntPtr]$h=$antif_handle)
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_ConnectToServer(IntPtr h, String Host, Int32 Port);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_ConnectToServer -PassThru
    if(($ret = $type::_RL_ConnectToServer($h, $anthost, $port)) -ne 0) { RL_Error $ret }
}

Function RL_DisconnectFromServer {
    param([IntPtr]$h=$antif_handle)
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_DisconnectFromServer(IntPtr h);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_DisconnectFromServer -PassThru
    if(($ret = $type::_RL_DisconnectFromServer($h)) -ne 0) { RL_Error $ret }
}

Function RL_QueryServerID {
    param([System.Text.StringBuilder]$s=(New-Object System.Text.StringBuilder 1024), [Int32]$len=[Int32]1024, [IntPtr]$h=$antif_handle)
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_QueryServerID(IntPtr h, StringBuilder s, Int32 len);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_QueryServerID -Using System.Text -PassThru
    if(($ret = $type::_RL_QueryServerID($h, $s, $len)) -ne 0) { RL_Error $ret }
    $s.ToString()
}

Function RL_ModCount {
    param([Int32]$pCount=[Int32]0, [IntPtr]$h=$antif_handle)
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_ModCount(IntPtr h, ref Int32 pCount);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_ModCount -PassThru
    if(($ret = $type::_RL_ModCount($h, [ref]$pCount)) -ne 0) { RL_Error $ret }
    $pCount
}

Function RL_ModInfo {
    param([Int32]$index=[Int32]2, [System.Text.StringBuilder]$name=(New-Object System.Text.StringBuilder 8), [System.Text.StringBuilder]$flags=(New-Object System.Text.StringBuilder 3), [IntPtr]$h=$antif_handle)
    $signature = @"
    [DllImport("$antifdll_path", CallingConvention=CallingConvention.Cdecl)]
    public static extern Int32 _RL_ModInfo(IntPtr h, Int32 index, StringBuilder name, StringBuilder flags);
"@
    $type = Add-Type -MemberDefinition $signature -Name RL_AntInfo -Using System.Text -PassThru
    if(($ret = $type::_RL_ModInfo($h, $index, $name, $flags)) -ne 0) { RL_Error $ret }
    $name.ToString()
    $flags.ToString()
}


