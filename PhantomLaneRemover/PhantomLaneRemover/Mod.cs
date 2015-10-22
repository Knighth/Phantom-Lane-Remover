using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PhantomLaneRemover
{
     public class Mod : IUserMod
    {
        public static bool DEBUG_LOG_ON = false;
        public static byte DEBUG_LOG_LEVEL = 0;
        internal const ulong MOD_WORKSHOPID = 536250255uL;
        internal const string MOD_OFFICIAL_NAME = "Phantom Lane Remover";  //debug==must match folder name
        internal const string MOD_DLL_NAME = "PhantomLaneRemover";
        internal const string MOD_DESCRIPTION = "Allows you to detect and remove phantom lanes.";
        internal static readonly string MOD_DBG_Prefix = "PhantomLaneRemover"; //same..for now.
        internal const string VERSION_BUILD_NUMBER = "1.2.1-f1 build_004";
        public static readonly string MOD_CONFIGPATH = "PhantomLaneRemover_Config.xml";
        
        public static bool IsEnabled = false;           //tracks if the mod is enabled.
        public static bool IsInited = false;            //tracks if we're inited
        public static bool IsGuiEnabled = false;        //tracks if the gui option is set.
        
        public static float AutoRefreshSeconds = 3.0f;  //why am I storing these here again and not just using mod.config? //oldcode.
        public static bool UseAutoRefreshOption = false;
        public static Configuration config;
        private static bool isFirstEnable = true;


        public string Description
        {
            get
            {
                return MOD_DESCRIPTION;
            }
        }

        public string Name
        {
            get
            {

                return MOD_OFFICIAL_NAME ;

            }
        }


        public void OnEnabled()
        {

            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("fired."); }
            Mod.IsEnabled = true;
            ReloadConfigValues(false, false);
            if (Mod.IsInited == false)
            {
                Helper.dbgLog(" This mod has been set enabled.");
                Mod.init();
            }
        }

        public void OnDisabled()
        {
            if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("fired."); }
            Mod.IsEnabled = false;
            un_init();
            Helper.dbgLog(Mod.MOD_OFFICIAL_NAME + " v" + VERSION_BUILD_NUMBER + " This mod has been set disabled or unloaded.");
        }

         
         
         /// <summary>
         /// Public Constructor on load we grab our config info and init();
         /// </summary>
        public Mod()
		{
            try
            {
                Helper.dbgLog("\r\n" + Mod.MOD_OFFICIAL_NAME + " v" + Mod.VERSION_BUILD_NUMBER + " Mod has been loaded.");
                if (!IsInited)
                {
                    ReloadConfigValues(false, false);
                    isFirstEnable = false;
                    init();
                }
            }
            catch(Exception ex)
            { Helper.dbgLog("[" + MOD_DBG_Prefix + "}", ex, true); }

 
        }
        
         /// <summary>
         /// Called to either initially load, or force a reload our config file var; called by mod initialization and again at mapload. 
         /// </summary>
         /// <param name="bForceReread">Set to true to flush the old object and create a new one.</param>
         /// <param name="bNoReloadVars">Set this to true to NOT reload the values from the new read of config file to our class level counterpart vars</param>
         public static void ReloadConfigValues(bool bForceReread, bool bNoReloadVars)
         {
             try
             {
                 if(isFirstEnable == true)
                 {return;}

                 if (bForceReread)
                 {
                     config = null;
                     if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 1) { Helper.dbgLog("Config wipe requested."); }
                 }
                 config = Configuration.Deserialize(MOD_CONFIGPATH);
                 if (config == null)
                 {
                     config = new Configuration();
                     config.ConfigVersion = Configuration.CurrentVersion;
                     //reset of setting should pull defaults
                     Helper.dbgLog("Existing config was null. Created new one.");
                     Configuration.Serialize(MOD_CONFIGPATH, config); //let's write it.
                 }
                 if (config != null && bNoReloadVars == false) //set\refresh our vars by default.
                 {
                     config.ConfigVersion = Configuration.CurrentVersion;
                     DEBUG_LOG_ON = config.DebugLogging;
                     DEBUG_LOG_LEVEL = config.DebugLoggingLevel;
                     IsGuiEnabled = config.EnableGui;
                     UseAutoRefreshOption = config.UseAutoRefresh;
                     AutoRefreshSeconds = config.AutoRefreshSeconds;
                     if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Vars refreshed"); }
                 }
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog(string.Format("Reloaded Config data ({0}:{1} :{2})", bForceReread.ToString(), bNoReloadVars.ToString(), config.ConfigVersion.ToString())); }
             }
             catch (Exception ex)
             { Helper.dbgLog("Exception while loading config values.", ex, true); }

         }

        internal static void init()
        {

            if (IsInited == false)
            {
                IsInited = true;
                if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Init completed." + DateTime.Now.ToLongTimeString()); }
            }
        }

         internal static void un_init()
         {
             if (IsInited)
             {
                 IsInited = false;
                 if (DEBUG_LOG_ON & DEBUG_LOG_LEVEL >= 2) { Helper.dbgLog("Un-Init triggered."); }
             }
         }


        private void LoggingChecked(bool en)
        {
            DEBUG_LOG_ON = en;
            config.DebugLogging = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

         //called from gui screen.
        public static void UpdateUseAutoRefeshValue(bool en)
        {
            UseAutoRefreshOption = en;
            config.UseAutoRefresh = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        public static void UpdateUseAutoShowOnMapLoad(bool en)
        {
            config.AutoShowOnMapLoad = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }

        public static void UpdateAlternateKeyBinding(bool en)
        {
            config.UseAlternateKeyBinding = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }


        private void OnUseGuiToggle(bool en)
        {
            IsGuiEnabled = en;
            config.EnableGui = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }


        private void OnDumpStatsAtMapEnd(bool en)
        {
            config.DumpStatsOnMapEnd = en;
            Configuration.Serialize(MOD_CONFIGPATH, config);
        }


        private void eventVisibilityChanged(UIComponent component, bool value)
        {
            if (value)
            {
                component.eventVisibilityChanged -= eventVisibilityChanged;
                component.parent.StartCoroutine(DoToolTips(component));
            }
        }

         /// <summary>
         /// Sets up tool tips. Would have been much easier if they would have let us specify the name of the components.
         /// </summary>
         /// <param name="component"></param>
         /// <returns></returns>
        private System.Collections.IEnumerator DoToolTips(UIComponent component)
        {
            yield return new WaitForSeconds(0.500f);
            try
            {
                UICheckBox[] cb = component.GetComponentsInChildren<UICheckBox>(true);
                if (cb != null && cb.Length > 0)
                {
                    for (int i = 0; i < (cb.Length); i++)
                    {
                        switch (cb[i].text)
                        {
                            case "Enable Verbose Logging":
                                cb[i].tooltip = "Enables detailed logging for debugging purposes\n See config file for even more options, unless there are problems you probably don't want to enable this.\n Option must be set before loading game.";
                                break;
                            case "Auto show on map load":
                                cb[i].tooltip = "Enable the info panel to be shown automatically on map load.";
                                break;
                            case "Use alternate key-bindings":
                                cb[i].tooltip = "Enable the alternate keybinding to show the panel\n Default alternate is left-control + left-alt + P\n You may set a custom binding in your config file if you like.";
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    List<UIButton> bb = new List<UIButton>();
                    component.GetComponentsInChildren<UIButton>(true, bb);
                    if ( bb.Count > 0)
                    { bb[0].tooltip = "On windows this will open the config file in notepad for you.\n *PLEASE CLOSE NOTEPAD* when you're done editing the conifg.\n If you don't and close the game steam will think CSL is still running till you do."; }

                }

            }
            catch(Exception ex)
            {
                /* I don't really care.*/
                Helper.dbgLog("", ex, true);
            }
            yield break;
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelper hp = (UIHelper)helper;
            UIScrollablePanel panel = (UIScrollablePanel)hp.self;
            panel.eventVisibilityChanged += eventVisibilityChanged;

            UIHelperBase group = helper.AddGroup("PhantomLaneRemover");
            group.AddCheckbox("Auto show on map load", config.AutoShowOnMapLoad, UpdateUseAutoShowOnMapLoad);
            group.AddCheckbox("Use alternate key-bindings", Mod.config.UseAlternateKeyBinding, UpdateAlternateKeyBinding);
            group.AddCheckbox("Enable Verbose Logging", DEBUG_LOG_ON, LoggingChecked);
            group.AddSpace(20);
           
          
        }


    }

}
