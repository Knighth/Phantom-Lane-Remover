using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
//using ColossalFramework.Steamworks;
using ICities;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace PhantomLaneRemover
{
	public class Loader : LoadingExtensionBase
	{
        public static UIView parentGuiView;
        public static LaneRemoverGUI guiPanel;
        public static bool isGuiRunning = false;
        internal static LoadMode CurrentLoadMode;
        public Loader() { }

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            try
            {
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Reloading config before mapload."); }
                // *reload config values again after map load. This should not be problem atm.
                // *So long as we do this before OnLevelLoaded we should be ok;
                Mod.ReloadConfigValues(false, false);
            }
            catch (Exception ex)
            { Helper.dbgLog("Error:", ex, true); }
        }


        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            CurrentLoadMode = mode;
            try
            {
                if (Mod.DEBUG_LOG_ON && Mod.DEBUG_LOG_LEVEL > 0) { Helper.dbgLog("LoadMode:" + mode.ToString()); }
                if (Mod.IsEnabled == true)
                {
                    // only setup redirect when in a real game
                    if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode==LoadMode.LoadMap ||mode==LoadMode.NewMap )
                    {
                        if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Asset modes not detcted"); }
                        if (Mod.IsGuiEnabled) { SetupGui(); } //setup gui if we're enabled.
                    }
                }
                else
                {
                    //This should never happen.
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("We fired when we were not even enabled active??"); }
                    if (Mod.IsGuiEnabled) { RemoveGui(); }
                }
            }
            catch(Exception ex)
            { Helper.dbgLog("Error:", ex, true); }
        }


        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            try
            {
                if (Mod.IsEnabled & (Mod.IsGuiEnabled | isGuiRunning))
                {
                    RemoveGui();
                }
            }
            catch (Exception ex1)
            {
                Helper.dbgLog("Error: \r\n", ex1, true);
            }

        }


        public override void OnReleased()
        {
            base.OnReleased();
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog ("Releasing Completed."); }
        }

        public static void SetupGui()
        {
            //if(Mod.IsEnabled && Mod.IsGuiEnabled)
            if (Mod.DEBUG_LOG_ON) Helper.dbgLog(" Setting up Gui panel.");
            try
            {
                parentGuiView = null;
                parentGuiView = UIView.GetAView();
                if (guiPanel == null)
                {
                    guiPanel = (LaneRemoverGUI)parentGuiView.AddUIComponent(typeof(LaneRemoverGUI));
                    if (Mod.DEBUG_LOG_ON) Helper.dbgLog(" GUI Setup.");
                    //guiPanel.Hide();
                }
                isGuiRunning = true;
            }
            catch (Exception ex)
            {
                Helper.dbgLog("Error: \r\n", ex,true);
            }

        }

        public static void RemoveGui()
        {

            if (Mod.DEBUG_LOG_ON) Helper.dbgLog(" Removing Gui.");
            try
            {
                if (guiPanel != null)
                {
                    //is this causing on exit exception problem?
                    guiPanel.gameObject.SetActive(false);
                    GameObject.DestroyImmediate(guiPanel.gameObject);
                    guiPanel = null;
                    if (Mod.DEBUG_LOG_ON) Helper.dbgLog("Destroyed GUI objects.");
                }
            }
            catch (Exception ex)
            {
                Helper.dbgLog("Error: ",ex,true);
            }

            isGuiRunning = false;
            if (parentGuiView != null) { parentGuiView = null; } //toast our ref to guiview
        }

	}
}
