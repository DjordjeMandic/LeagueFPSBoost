﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LeagueFPSBoost.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
    internal sealed partial class UpdaterActionsSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static UpdaterActionsSettings defaultInstance = ((UpdaterActionsSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new UpdaterActionsSettings())));
        
        public static UpdaterActionsSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool RestartPostUpdateAction_Ran {
            get {
                return ((bool)(this["RestartPostUpdateAction_Ran"]));
            }
            set {
                this["RestartPostUpdateAction_Ran"] = value;
            }
        }
    }
}
