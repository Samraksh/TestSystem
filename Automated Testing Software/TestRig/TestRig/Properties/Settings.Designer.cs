﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.17929
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TestRig.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\SamTest\\openocd-bin\\interface\\olimex-arm-usb-tiny-h.cfg")]
        public string OCDInterface {
            get {
                return ((string)(this["OCDInterface"]));
            }
            set {
                this["OCDInterface"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\SamTest\\openocd-bin\\target\\stm32xl.cfg")]
        public string OCDTarget {
            get {
                return ((string)(this["OCDTarget"]));
            }
            set {
                this["OCDTarget"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\SamTest\\openocd-bin\\openocd.exe")]
        public string OCDExe {
            get {
                return ((string)(this["OCDExe"]));
            }
            set {
                this["OCDExe"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\SamTest\\codesourcery")]
        public string CSPath {
            get {
                return ((string)(this["CSPath"]));
            }
            set {
                this["CSPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\DotNet-MF\\MicroFrameworkPK_v4_0")]
        public string MFPath_4_0 {
            get {
                return ((string)(this["MFPath_4_0"]));
            }
            set {
                this["MFPath_4_0"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\SamTest\\Automated Testing Software\\GitBin")]
        public string GitPath {
            get {
                return ((string)(this["GitPath"]));
            }
            set {
                this["GitPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\TestSys")]
        public string TSPath {
            get {
                return ((string)(this["TSPath"]));
            }
            set {
                this["TSPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Test\\Receipts")]
        public string TRPath {
            get {
                return ((string)(this["TRPath"]));
            }
            set {
                this["TRPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\Work\\DotNet-MF\\MicroFrameworkPK_v4_3")]
        public string MFPath_4_3 {
            get {
                return ((string)(this["MFPath_4_3"]));
            }
            set {
                this["MFPath_4_3"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int MFSelection {
            get {
                return ((int)(this["MFSelection"]));
            }
            set {
                this["MFSelection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int HSSelection {
            get {
                return ((int)(this["HSSelection"]));
            }
            set {
                this["HSSelection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int MTSelection {
            get {
                return ((int)(this["MTSelection"]));
            }
            set {
                this["MTSelection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int STSelection {
            get {
                return ((int)(this["STSelection"]));
            }
            set {
                this["STSelection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int GVSelection {
            get {
                return ((int)(this["GVSelection"]));
            }
            set {
                this["GVSelection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int MFPathSelection {
            get {
                return ((int)(this["MFPathSelection"]));
            }
            set {
                this["MFPathSelection"] = value;
            }
        }
    }
}
