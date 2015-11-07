using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ICities;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;
using System.Diagnostics;
namespace PhantomLaneRemover
{
    public class LaneRemoverGUI : UIPanel
    {
        public static readonly string cacheName = "LaneRemoverGUI";
        public static LaneRemoverGUI instance;
        private const string DTMilli = "MM/dd/yyyy hh:mm:ss.fff tt";
        private const string sVALUE_PLACEHOLDER = "[00000]  |  [00000]";
        private const string TAG_VALUE_PREFIX = "LaneRemoverGUI_Value_";
        private const string TAG_TEXT_PREFIX = "LaneRemoverGUI_Text_";
        private const string sVALUE_FSTRING1 = "   {0}    |    [{1}]";
        private const string sVALUE_FSTRING2 = "   {0}";
        private const string sVALUE_FSTRING3 = "   {0}    |    {1}";
        private static readonly float WIDTH = 600f;
        private static readonly float HEIGHT = 400f;
        private static readonly float HEADER = 40f;
        private static readonly float SPACING = 10f;
        private static readonly float SPACING22 = 22f;
        private static bool isRefreshing = false;  //Used basically as a safety lock.
        private bool CoCheckStatsDataEnabled = false;   //These tell us if certain coroutine is running. 
        private bool CoDisplayRefreshEnabled = false;

        private bool bUseAlternateKeys = false;
        private Helper.KeycodeData AlternateBindingData;
        private object[] _tmpNetData;
        private object[] _tmpCitzData;
        private object[] _tmpotherdata;
        private object[] _tmpotherdata2;
        private object[] MaxsizesInt;
        private object[] LimitsizesInt;
        private Dictionary<string, UILabel> _txtControlContainer = new Dictionary<string, UILabel>(16);
        private Dictionary<string,UILabel> _valuesControlContainer = new Dictionary<string,UILabel>(16);

        UIDragHandle m_DragHandler; //lets us move the panel around.
        UIButton m_closeButton; //our close button
        UILabel m_title;
        UIButton m_refresh;  //our manual refresh button
        UILabel m_AutoRefreshChkboxText; //label that get assigned to the AutoRefreshCheckbox.
        UICheckBox m_AutoRefreshCheckbox; //Our AutoRefresh checkbox

        UILabel m_HeaderDataText;
        UILabel m_NetSegmentsText;
        UILabel m_NetSegmentsValue;
        UILabel m_NetNodesText;
        UILabel m_NetNodesValue;
        UILabel m_NetLanesText;
        UILabel m_NetLanesValue;
        UILabel m_CitizenUnitsText;
        UILabel m_CitizenUnitsValue;

        
        UILabel m_AdditionalText1Text;
        UIButton m_LogdataButton;
        UIButton m_ClearDataButton;
        UILabel m_MessageText;
        UIButton m_CheckCitizenUnitsButton;
        UIButton m_FixCitizenUnitsButton;

//        private Stopwatch MyPerfTimer = new Stopwatch();
        private ItemClass.Availability CurrentMode;
        private int NumPhantomLanesDetected = 0;
        private int NumPhantomLanesRemoved = 0;
        private int NumPhantomCUDetected = 0;
        private int NumPhantomCURemoved = 0;



        /// <summary>
        /// Function gets called by unity on every single frame.
        /// We just check for our key combo, maybe there is a better way to register this with the game?
        /// </summary>
        public override void Update()
        {
            if (bUseAlternateKeys)
            {
                if (AlternateBindingData.NumOfCodes == 2)
                {
                    if (Input.GetKey(AlternateBindingData.kCode1) && Input.GetKeyDown(AlternateBindingData.kCode2))
                    { this.ProcessVisibility(); }
                }
                else
                {
                    if (Input.GetKey(AlternateBindingData.kCode1) && Input.GetKeyDown(AlternateBindingData.kCode2) && Input.GetKey(AlternateBindingData.kCode3))
                    { this.ProcessVisibility(); }
                }
            }
            else
            {

                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.P) && Input.GetKeyDown(KeyCode.L))
                {
                    this.ProcessVisibility();
                }
            }
            base.Update();
        }


        /// <summary>
        /// Gets called upon the base UI component's creation. Basically it's the constructor...but not really.
        /// </summary>
        public override void Start()
        {
            base.Start();
            LaneRemoverGUI.instance = this;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog(string.Concat("Attempting to create our display panel.  ",DateTime.Now.ToString(DTMilli).ToString()));
            this.size = new Vector2(WIDTH, HEIGHT);
            this.backgroundSprite = "MenuPanel";
            this.canFocus = true;
            this.isInteractive = true;
            this.BringToFront();
            this.relativePosition = new Vector3((Loader.parentGuiView.fixedWidth / 2) - 200, (Loader.parentGuiView.fixedHeight / 2) - 300);
            this.opacity = Mod.config.GuiOpacity;
            this.cachedName = cacheName;
            CurrentMode = Singleton<ToolManager>.instance.m_properties.m_mode;
            //DragHandler
            m_DragHandler = this.AddUIComponent<UIDragHandle>();
            m_DragHandler.target = this;
            //Title UILabel
            m_title = this.AddUIComponent<UILabel>();
            m_title.text = "Phantom Lane Remover"; //spaces on purpose
            m_title.relativePosition = new Vector3(WIDTH / 2 - (m_title.width / 2) - 25f, (HEADER / 2) - (m_title.height / 2));
            m_title.textAlignment = UIHorizontalAlignment.Center;
            //Close Button UIButton
            m_closeButton = this.AddUIComponent<UIButton>();
            m_closeButton.normalBgSprite = "buttonclose";
            m_closeButton.hoveredBgSprite = "buttonclosehover";
            m_closeButton.pressedBgSprite = "buttonclosepressed";
            m_closeButton.relativePosition = new Vector3(WIDTH - 35, 5, 10);
            m_closeButton.eventClick += (component, eventParam) =>
            {
                this.Hide();
            };

            if (!Mod.config.AutoShowOnMapLoad)
            {
                this.Hide();
            }
            DoOnStartup();
            if (Mod.DEBUG_LOG_ON) Helper.dbgLog(string.Concat("Display panel created. ",DateTime.Now.ToString(DTMilli).ToString()));
        }

        /// <summary>
        /// Our initialize stuff; call after panel\form setup.
        /// </summary>
        private void DoOnStartup()
        {
            try
            {
                FetchMaxSizeLimitSizeData();
                CreateTextLabels();
                FetchValueLabelData();
                CreateDataLabels();
                NumPhantomLanesDetected = 0;
                NumPhantomLanesRemoved = 0;
                NumPhantomCUDetected = 0;
                NumPhantomCURemoved = 0;

                if (Mod.config.UseAlternateKeyBinding)
                {
                    AlternateBindingData = Configuration.getAlternateKeyBindings(Mod.config.AlternateKeyBindingCode);
                    if (AlternateBindingData.NumOfCodes > 1) { bUseAlternateKeys = true; }
                }

                if (Mod.config.CheckStatsForLimitsEnabled)
                {
                    this.StartCoroutine(CheckForStatsStatus());
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("CheckForStatsStatus coroutine started."); }
                }
                if (m_AutoRefreshCheckbox.isChecked)
                {
                    this.StartCoroutine(RefreshDisplayDataWrapper());
                    if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("RefreshDisplayDataWrapper coroutine started."); }
                }
                else
                {
                    RefreshDisplayData(); //at least run once.
                }
            }
            catch (Exception ex)
            {
                Helper.dbgLog("GUI DoOnStarup failed.", ex, true);
            }

        }

        private void FetchMaxSizeLimitSizeData()
        {
            try
            {
                VehicleManager vMgr = Singleton<VehicleManager>.instance;
                NetManager nMgr = Singleton<NetManager>.instance;
                CitizenManager cMgr = Singleton<CitizenManager>.instance;

                MaxsizesInt = new object[] { nMgr.m_segments.m_size, nMgr.m_nodes.m_size, nMgr.m_lanes.m_size,
                Singleton<BuildingManager>.instance.m_buildings.m_size,Singleton<ZoneManager>.instance.m_blocks.m_size,
                vMgr.m_vehicles.m_size,vMgr.m_parkedVehicles.m_size,cMgr.m_citizens.m_size ,cMgr.m_units.m_size,
                cMgr.m_instances.m_size,Singleton<TransportManager>.instance.m_lines.m_size,Singleton<PathManager>.instance.m_pathUnits.m_size,
                Singleton<GameAreaManager>.instance.m_areaGrid.Count(),Singleton<DistrictManager>.instance.m_districts.m_size,
                Singleton<TreeManager>.instance.m_trees.m_size,Singleton<PropManager>.instance.m_props.m_size};

                LimitsizesInt = new object[]{
                CheckMode() ? (int)36352 : (int)16384,
                CheckMode() ? (int)32256 : (int)16384,
                CheckMode() ? (int)258048 : (int)131072, //lanes
                CheckMode() ? (int)48640 : (int)8192, //build
                CheckMode() ? (int)48640 : (int)16384, //zoneblk
                (int)16384, //vehc
                (int)32767, //parked
                (int)1048575, //cit
                (int)524287, //units
                (int)65535,  //agents
                (int)249,
                (int)262143, //pathunits
                (int) Singleton<GameAreaManager>.instance.m_maxAreaCount, //areas
                (int)126,  //districts
                Singleton<TreeManager>.instance.m_trees.m_size > 262144 ? (int)(Singleton<TreeManager>.instance.m_trees.m_size - 10u) : (CheckMode() ? (int)262139 : (int)250000), //trees
                CheckMode() ? (int)65531 : (int)50000
                };
            }
            catch (Exception ex)
            { Helper.dbgLog("FetchMaxSizleData failed. ", ex, true); }
        }

        private void FetchValueLabelData()
        {
            try
            {

                _tmpNetData = Helper.AddLimitData(2);
                _tmpCitzData = Helper.AddLimitData(4);
                _tmpotherdata = Helper.AddLimitData(8);
                _tmpotherdata2 = Helper.AddLimitData(16);
            }
            catch (Exception ex)
            {
                Helper.dbgLog("fetchvalues died. ", ex, true);
            }
        }

        /// <summary>
        /// Returns true if we are in game, false if we are in map editor, assumes loader never loads this form in asset mode.
        /// </summary>
        /// <returns>true|False ; true if game, false if mapeditor</returns>
        private bool CheckMode()
        {
            if(Loader.CurrentLoadMode == LoadMode.LoadMap || Loader.CurrentLoadMode == LoadMode.NewMap)
            {
                return false;
            }
            return true;

        }

        /// <summary>
        /// Create and setup up default text and stuff for our Text UILabels;
        /// </summary>
        private void CreateTextLabels() 
        {
            m_HeaderDataText = this.AddUIComponent<UILabel>();
            m_HeaderDataText.textScale = 0.825f;
            m_HeaderDataText.text = string.Concat("Object Type     [#maxsize]  |  [#defaultLimit]:         #In Use          [  |  #itemCount]");
            m_HeaderDataText.tooltip = string.Concat("Maxsize is the max size of the holding array, defaultlimit is the number the game or map by default is restricting you too.",
                "\n#InUse: is the number you are currently using.\n",
                "Optionally #itemCount is shown as checksum, it should never be more than one higher than #InUse if shown.");
            m_HeaderDataText.relativePosition = new Vector3(SPACING, 50f);
            m_HeaderDataText.autoSize = true;

            m_NetSegmentsText = this.AddUIComponent<UILabel>();
            m_NetSegmentsText.text = string.Format("Net Segments     [{0}]]  |  [{1}]:", MaxsizesInt[0].ToString(), LimitsizesInt[0].ToString());
            m_NetSegmentsText.tooltip = "You can think of segments as road sections, but they are used for far more then just roads.\n Each segment is typically connected in a chain of other segments to a 'node'.";
            m_NetSegmentsText.relativePosition = new Vector3(SPACING, (m_HeaderDataText.relativePosition.y + SPACING22));
            m_NetSegmentsText.autoSize = true;
            m_NetSegmentsText.name = TAG_TEXT_PREFIX + "0";


            m_NetNodesText = this.AddUIComponent<UILabel>();
            m_NetNodesText.relativePosition = new Vector3(SPACING, (m_NetSegmentsText.relativePosition.y + SPACING22));
            m_NetNodesText.text = string.Format("Net Nodes     [{0}]  |  [{1}]:", MaxsizesInt[1].ToString(), LimitsizesInt[1].ToString());
            m_NetNodesText.tooltip = "The number of Nodes. Think of nodes sort of like intersections, or the point that the first segment of a path connects too,\n Each node typically contains zero or more segments.";
            m_NetNodesText.autoSize = true;
            m_NetNodesText.name = TAG_TEXT_PREFIX + "1";

            m_NetLanesText = this.AddUIComponent<UILabel>();
            m_NetLanesText.relativePosition = new Vector3(SPACING, (m_NetNodesText.relativePosition.y + SPACING22));
            m_NetLanesText.text = string.Format("Net Lanes     [{0}]  |  [{1}]:",MaxsizesInt[2].ToString(),LimitsizesInt[2].ToString()) ;
            m_NetLanesText.tooltip = "The number of lanes. Lanes are used by more than just roads and rail.\n Things like ped.paths, bike paths, transportlines and similar things create and use them too.\nIt's not alway logical,most roads with only two lanes by default will get assigned six lanes,\n These account sidewalks and other such 'lanes' types";
            m_NetLanesText.autoSize = true;
            m_NetLanesText.name = TAG_TEXT_PREFIX + "2";

            m_CitizenUnitsText = this.AddUIComponent<UILabel>();
            m_CitizenUnitsText.relativePosition = new Vector3(SPACING, (m_NetLanesText.relativePosition.y + SPACING22));
            m_CitizenUnitsText.text = string.Format("Citizen Units     [{0}]  |  [{1}]:", MaxsizesInt[8].ToString(), LimitsizesInt[8].ToString());
            m_CitizenUnitsText.tooltip = "The number of Citizen Units.\nCitizen units are used to group,transport, and otherwise account for citizens moving around your city.\n For instance there will be more or one CU per vehicle, the more passengers the more CU's.";
            m_CitizenUnitsText.autoSize = true;
            m_CitizenUnitsText.name = TAG_TEXT_PREFIX + "3";

            
            m_AutoRefreshCheckbox = this.AddUIComponent<UICheckBox>();
            m_AutoRefreshCheckbox.relativePosition = new Vector3((SPACING), (m_CitizenUnitsText.relativePosition.y + 30f));

            m_AutoRefreshChkboxText = this.AddUIComponent<UILabel>();
            m_AutoRefreshChkboxText.relativePosition = new Vector3(m_AutoRefreshCheckbox.relativePosition.x + m_AutoRefreshCheckbox.width + (SPACING * 3), (m_AutoRefreshCheckbox.relativePosition.y) + 5f);
            //m_AutoRefreshChkboxText.text = "Use Auto Refresh";
            m_AutoRefreshChkboxText.tooltip = "Enables these stats to update every few seconds \n Default is 5 seconds.";
            //m_AutoRefreshChkboxText.autoSize = true;

            m_AutoRefreshCheckbox.height = 16;
            m_AutoRefreshCheckbox.width = 16;
            m_AutoRefreshCheckbox.label = m_AutoRefreshChkboxText;
            m_AutoRefreshCheckbox.text = string.Concat("Use AutoRefresh  (", Mod.AutoRefreshSeconds.ToString("f1"), " sec)");

            UISprite uncheckSprite = m_AutoRefreshCheckbox.AddUIComponent<UISprite>();
            uncheckSprite.height = 20;
            uncheckSprite.width = 20;
            uncheckSprite.relativePosition = new Vector3(0, 0);
            uncheckSprite.spriteName = "check-unchecked";
            uncheckSprite.isVisible = true;

            UISprite checkSprite = m_AutoRefreshCheckbox.AddUIComponent<UISprite>();
            checkSprite.height = 20;
            checkSprite.width = 20;
            checkSprite.relativePosition = new Vector3(0, 0);
            checkSprite.spriteName = "check-checked";

            m_AutoRefreshCheckbox.checkedBoxObject = checkSprite;
            m_AutoRefreshCheckbox.isChecked = Mod.UseAutoRefreshOption;
            m_AutoRefreshCheckbox.isEnabled = true;
            m_AutoRefreshCheckbox.isVisible = true;
            m_AutoRefreshCheckbox.canFocus = true;
            m_AutoRefreshCheckbox.isInteractive = true;
            //can't use this? m_AutoRefreshCheckbox.autoSize = true;  
            m_AutoRefreshCheckbox.eventCheckChanged += (component, eventParam) => { AutoRefreshCheckbox_OnCheckChanged(component, eventParam); };
            //AutoRefreshCheckbox_OnCheckChanged;

            m_AdditionalText1Text = this.AddUIComponent<UILabel>();
            m_AdditionalText1Text.relativePosition = new Vector3(m_AutoRefreshCheckbox.relativePosition.x + m_AutoRefreshCheckbox.width + SPACING, (m_AutoRefreshCheckbox.relativePosition.y) + 25f);
            m_AdditionalText1Text.width = 300f;
            m_AdditionalText1Text.height = 50f;
            m_AdditionalText1Text.textScale = 0.875f;
            //m_AdditionalText1Text.autoSize = true;
            //m_AdditionalText1Text.wordWrap = true;
            m_AdditionalText1Text.text = "* Use CTRL + (P & L) to show again. \n  More options available in PhantomLaneRemover_Config.xml";

            m_refresh = this.AddUIComponent<UIButton>();
            m_refresh.size = new Vector2(120, 24);
            m_refresh.text = "Manual Refresh";
            m_refresh.tooltip = "Use to manually refresh the data. \n (use when auto enabled is off)";
            m_refresh.textScale = 0.800f;
            m_refresh.normalBgSprite = "ButtonMenu";
            m_refresh.hoveredBgSprite = "ButtonMenuHovered";
            m_refresh.pressedBgSprite = "ButtonMenuPressed";
            m_refresh.disabledBgSprite = "ButtonMenuDisabled";
            //m_refresh.relativePosition = m_closeButton.relativePosition + new Vector3(-60 - SPACING, 6f);
            m_refresh.relativePosition = m_AutoRefreshChkboxText.relativePosition + new Vector3((m_AutoRefreshChkboxText.width + SPACING * 2), -5f);
            m_refresh.eventClick += (component, eventParam) =>
            {
                FetchValueLabelData();
                RefreshDisplayData();
                CheckStatsForColorChange();
            };


            m_LogdataButton = this.AddUIComponent<UIButton>();
            m_LogdataButton.size = new Vector2(180, 34);
            m_LogdataButton.text = "Detect Phantom Lanes";
            m_LogdataButton.tooltip = "Use to detect and log information about any phantom lanes in your map.";
            m_LogdataButton.textScale = 0.875f;
            m_LogdataButton.normalBgSprite = "ButtonMenu";
            m_LogdataButton.hoveredBgSprite = "ButtonMenuHovered";
            m_LogdataButton.pressedBgSprite = "ButtonMenuPressed";
            m_LogdataButton.disabledBgSprite = "ButtonMenuDisabled";
            m_LogdataButton.relativePosition = m_AutoRefreshCheckbox.relativePosition + new Vector3(0f, 90f);
            m_LogdataButton.eventClick += (component, eventParam) => { ProcessOnLogButton(); };

            m_ClearDataButton = this.AddUIComponent<UIButton>();
            m_ClearDataButton.size = new Vector2(80, 34);
            m_ClearDataButton.text = "Fix Lanes";
            m_ClearDataButton.tooltip = "Use to actually clean up the phantom lanes.";
            m_ClearDataButton.textScale = 0.875f;
            m_ClearDataButton.normalBgSprite = "ButtonMenu";
            m_ClearDataButton.hoveredBgSprite = "ButtonMenuHovered";
            m_ClearDataButton.pressedBgSprite = "ButtonMenuPressed";
            m_ClearDataButton.disabledBgSprite = "ButtonMenuDisabled";
            m_ClearDataButton.relativePosition = m_LogdataButton.relativePosition + new Vector3((m_LogdataButton.width + SPACING * 3), 0f);
            m_ClearDataButton.eventClick += (component, eventParam) => { ProcessOnCopyButton(); };
            m_ClearDataButton.isVisible = false;


            m_CheckCitizenUnitsButton  = this.AddUIComponent<UIButton>();
            m_CheckCitizenUnitsButton.size = new Vector2(200, 34);
            m_CheckCitizenUnitsButton.text = "Detect Phantom CitizenUnits";
            m_CheckCitizenUnitsButton.tooltip = "Use to detect and log information about any phantom CitizenUnits in your map.";
            m_CheckCitizenUnitsButton.textScale = 0.875f;
            m_CheckCitizenUnitsButton.normalBgSprite = "ButtonMenu";
            m_CheckCitizenUnitsButton.hoveredBgSprite = "ButtonMenuHovered";
            m_CheckCitizenUnitsButton.pressedBgSprite = "ButtonMenuPressed";
            m_CheckCitizenUnitsButton.disabledBgSprite = "ButtonMenuDisabled";
            m_CheckCitizenUnitsButton.relativePosition = m_LogdataButton.relativePosition + new Vector3(0f, 40f);
            m_CheckCitizenUnitsButton.eventClick += (component, eventParam) => { ProcessCheckCUButton(); };

            m_FixCitizenUnitsButton = this.AddUIComponent<UIButton>();
            m_FixCitizenUnitsButton.size = new Vector2(120, 34);
            m_FixCitizenUnitsButton.text = "Fix CitizenUnits";
            m_FixCitizenUnitsButton.tooltip = "Use to actually clean up the phantom CitizenUnits.";
            m_FixCitizenUnitsButton.textScale = 0.800f;
            m_FixCitizenUnitsButton.normalBgSprite = "ButtonMenu";
            m_FixCitizenUnitsButton.hoveredBgSprite = "ButtonMenuHovered";
            m_FixCitizenUnitsButton.pressedBgSprite = "ButtonMenuPressed";
            m_FixCitizenUnitsButton.disabledBgSprite = "ButtonMenuDisabled";
            m_FixCitizenUnitsButton.relativePosition = m_CheckCitizenUnitsButton.relativePosition + new Vector3((m_CheckCitizenUnitsButton.width + SPACING * 3), 0f);
            m_FixCitizenUnitsButton.eventClick += (component, eventParam) => { ProcessFixCUButton(); };
            m_FixCitizenUnitsButton.isVisible = false;

            m_MessageText = this.AddUIComponent<UILabel>();
            m_MessageText.relativePosition = m_CheckCitizenUnitsButton.relativePosition + new Vector3(0f, SPACING22 * 2);
            m_MessageText.text = "Detection results";
            m_MessageText.textScale = 0.875f;
            m_MessageText.tooltip = "The results of a the last phantom lane or citizen unit detection button press.";
            m_MessageText.autoSize = true;
            m_MessageText.name = TAG_TEXT_PREFIX + "4";

        }


        /// <summary>
        /// Event handler for clicking on AutoRefreshbutton.
        /// </summary>
        /// <param name="UIComp">The triggering UIComponent</param>
        /// <param name="bValue">The Value True|False (Checked|Unchecked)</param>

        private void AutoRefreshCheckbox_OnCheckChanged(UIComponent UIComp, bool bValue)
        {
            if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("AutoRefreshButton was toggled to: " + bValue.ToString()); }
            Mod.UpdateUseAutoRefeshValue(bValue);
            if (bValue == true)
            {
                byte bflag = 0;
                if (!CoDisplayRefreshEnabled) { this.StartCoroutine(RefreshDisplayDataWrapper()); bflag += 2; }
                if (!CoCheckStatsDataEnabled) { this.StartCoroutine(CheckForStatsStatus()); bflag += 4; }
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Starting all coroutines that were not already started " + 
                    bValue.ToString() + " bflag=" + bflag.ToString()); }
            }
            else
            {
                this.StopAllCoroutines();
                ResetAllCoroutineState(false); //cleanup
                if (Mod.DEBUG_LOG_ON) { Helper.dbgLog("Stopping all coroutines: " + bValue.ToString()); }
            }
            return;
        }

        /// <summary>
        /// Sadly needed to reset state of Coroutines after forced stop.
        /// </summary>
        /// <param name="bStatus">True|False</param>
        private void ResetAllCoroutineState(bool bStatus)
        {
            CoCheckStatsDataEnabled = bStatus;
            CoDisplayRefreshEnabled = bStatus;
        }

        /// <summary>
        /// Function to check if we need to reset the stats, ment to check only every so often..like once a minute
        /// or modify it 
        /// </summary>
        private IEnumerator CheckForStatsStatus()
        {
            if (CoCheckStatsDataEnabled == true)
            {
                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog(" CheckkForStatsStatus* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 copy at a time.

            while (Mod.config.CheckStatsForLimitsEnabled & this.isVisible )
            {
                CoCheckStatsDataEnabled = true;
                CheckStatsForColorChange();
                if (Mod.DEBUG_LOG_ON) Helper.dbgLog(string.Concat("CheckStats fired. will fire again in 60 seconds.", DateTime.Now.ToString(DTMilli)));
                yield return new WaitForSeconds(Mod.config.StatsCheckEverySeconds);
            }
            CoCheckStatsDataEnabled = false;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("CheckkForStatsStatus coroutine exited due to CheckStatsForLimitsEnabled = false or visibility change.");
            yield break;
        }




        /// <summary>
        /// Primary coroutine function to update the more static (seconds) information display.
        /// as there really is no need to update this more then once per second.
        /// </summary>
        private IEnumerator RefreshDisplayDataWrapper() 
        {
            if (CoDisplayRefreshEnabled == true)
            {
                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("Refresh vehicleData* coroutine exited; Only one allowed at a time.");
                yield break;
            } //ensure only 1 active. 
            while (isRefreshing == false && this.isVisible == true && m_AutoRefreshCheckbox.isChecked)
            {
//                MyPerfTimer.Reset();
//                MyPerfTimer.Start();

                CoDisplayRefreshEnabled  = true;
                FetchValueLabelData();
                RefreshDisplayData();
//                MyPerfTimer.Stop();
//                Helper.dbgLog(string.Concat("Refresh took this many ticks:", MyPerfTimer.ElapsedTicks.ToString(), ":",MyPerfTimer.ElapsedMilliseconds.ToString()));
                yield return new WaitForSeconds(Mod.config.AutoRefreshSeconds);
            }
            CoDisplayRefreshEnabled = false;
            if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL > 0) Helper.dbgLog("Refresh vehicleData coroutine exited due to AutoRefresh disabled, visiblity change, or already refreshing.");
            yield break;
        }


        /// <summary>
        /// Function refreshes the display data. mostly called from coroutine timer.
        /// </summary>
        private void RefreshDisplayData()
        {
            isRefreshing = true; //safety lock so we never get more then one of these, probably don't need after co-routine refactor.
            try
            {

                m_NetSegmentsValue.text = String.Format(sVALUE_FSTRING1, _tmpNetData[0], _tmpNetData[1]);
                m_NetNodesValue.text = String.Format(sVALUE_FSTRING1, _tmpNetData[2], _tmpNetData[3]);
                m_NetLanesValue.text = String.Format(sVALUE_FSTRING1, _tmpNetData[4], _tmpNetData[5]);
                m_CitizenUnitsValue.text = String.Format(sVALUE_FSTRING2, _tmpCitzData[1]);

                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 3) Helper.dbgLog("Refreshing display data completed. " + DateTime.Now.ToString(DTMilli));
            }
            catch (Exception ex)
            {
                isRefreshing = false;
                Helper.dbgLog("ERROR during RefreshDisplayData. ",ex,true);
            }
            isRefreshing = false;

        }


        /// <summary>
        /// Checks stats against 10% of their limits, turns them orange.
        /// </summary>
        private void CheckStatsForColorChange()
        {
            try
            {
                Color32 cGreen = new Color32(0, 204, 0, 255);
                Color32 cOrange = new Color32(255, 204, 0, 255);
                m_HeaderDataText.color = cGreen;
                m_NetSegmentsValue.textColor  = isWithin10Percent(int.Parse(_tmpNetData[0].ToString()), (int)LimitsizesInt[0], 0.1f) ? cOrange : cGreen;
                m_NetNodesValue.textColor = isWithin10Percent(int.Parse(_tmpNetData[2].ToString()), (int)LimitsizesInt[1]) ? cOrange : cGreen;
                m_NetLanesValue.textColor = isWithin10Percent(int.Parse(_tmpNetData[4].ToString()), (int)LimitsizesInt[2]) ? cOrange : cGreen;
                m_CitizenUnitsValue.textColor = isWithin10Percent(int.Parse(_tmpCitzData[1].ToString()), (int)LimitsizesInt[8]) ? cOrange : cGreen;
                if (Mod.DEBUG_LOG_ON & Mod.DEBUG_LOG_LEVEL >= 2)
                {
                    Helper.dbgLog("Completed CheckStatsForColorChange. " + DateTime.Now.ToString(DTMilli));
                }
            }
            catch (Exception ex)
            { Helper.dbgLog("Error :", ex, true); }


        }

        private bool isWithin10Percent(int ival,int imaxlimit,float fPercent = 0.1f)
        {
            if (ival > (imaxlimit * (1.0f - fPercent)))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Creates all our UILabels that store data that changes\gets refreshed.
        /// </summary>
        private void CreateDataLabels() 
        {
            m_NetSegmentsValue = this.AddUIComponent<UILabel>();
            m_NetSegmentsValue.text = sVALUE_PLACEHOLDER;
            m_NetSegmentsValue.relativePosition = new Vector3(m_NetSegmentsText.relativePosition.x + m_NetSegmentsText.width + (SPACING * 5), m_NetSegmentsText.relativePosition.y);
            m_NetSegmentsValue.autoSize = true;
            m_NetSegmentsValue.tooltip = "";
            m_NetSegmentsValue.name = TAG_VALUE_PREFIX + "0";

            m_NetNodesValue = this.AddUIComponent<UILabel>();
            m_NetNodesValue.relativePosition = new Vector3(m_NetSegmentsValue.relativePosition.x , m_NetNodesText.relativePosition.y);
            m_NetNodesValue.autoSize = true;
            m_NetNodesValue.text = sVALUE_PLACEHOLDER;
            m_NetNodesValue.name = TAG_VALUE_PREFIX + "1";

            m_NetLanesValue = this.AddUIComponent<UILabel>();
            m_NetLanesValue.relativePosition = new Vector3(m_NetNodesValue.relativePosition.x , m_NetLanesText.relativePosition.y);
            m_NetLanesValue.autoSize = true;
            m_NetLanesValue.text = sVALUE_PLACEHOLDER;
            m_NetLanesValue.name = TAG_VALUE_PREFIX + "2";

            m_CitizenUnitsValue = this.AddUIComponent<UILabel>();
            m_CitizenUnitsValue.relativePosition = new Vector3(m_NetLanesValue.relativePosition.x, m_CitizenUnitsText.relativePosition.y);
            m_CitizenUnitsValue.autoSize = true;
            m_CitizenUnitsValue.text = sVALUE_PLACEHOLDER;
            m_CitizenUnitsValue.name = TAG_VALUE_PREFIX + "3";
        }

        /// <summary>
        /// Handle action for Hide\Show events.
        /// </summary>
        private void ProcessVisibility()
        {
            if (!this.isVisible)
            {
                this.Show();
                if (!CoCheckStatsDataEnabled ) { this.StartCoroutine(CheckForStatsStatus()); }
                if (!CoDisplayRefreshEnabled) { this.StartCoroutine(RefreshDisplayDataWrapper()); }
                //we do not touch the Resetting of StatsData; that's left to autorefresh on\off only atm.
            }
            else
            {
                this.Hide();
                //we don't have to stop the two above coroutines, 
                //should do that themselves via their own visibility checks.
            }
            if (NumPhantomLanesDetected == 0)
            { m_ClearDataButton.isVisible = false; }
            else { m_ClearDataButton.isVisible = true; }

            if(NumPhantomCUDetected == 0)
            {m_FixCitizenUnitsButton.isVisible = false;}
            else
            {m_FixCitizenUnitsButton.isVisible = true;}
        
        }

        //yes I should rename it ... resused from CSLShowMoreLimits.
        private void ProcessOnCopyButton()
        {
            try
            {
               NumPhantomLanesRemoved = Helper.LogLaneData(true);
               NumPhantomLanesDetected = 0;
               m_ClearDataButton.isVisible = false;
               m_MessageText.textColor = new Color32(0, 204, 0, 255); //greenish
               m_MessageText.text = String.Format("{0} phantom lanes were successfully cleaned up. Save this cleaned version.", NumPhantomLanesRemoved.ToString());
            }
            catch (Exception ex)
            {
                Helper.dbgLog("Error while clearing lanes.", ex, true);
            }
        }


        /// <summary>
        /// Handles action for pressing Detect Phantom Lanes Button...yes I need to rename this.
        /// </summary>
        private void ProcessOnLogButton()
        {
            NumPhantomLanesDetected = 0;
            NumPhantomLanesDetected = Helper.LogLaneData();
            if (NumPhantomLanesDetected > 0)
            {
                m_ClearDataButton.isVisible = true;
                m_MessageText.textColor = new Color32(255, 204, 0, 255); //yellowish
            }
            else
            { 
                m_MessageText.textColor = new Color32(0, 204, 0, 255); //greenish
            }
            m_MessageText.text = String.Format("There were {0} phantom lanes detected. " + (NumPhantomLanesDetected > 0 ? "Press 'Fix Lanes' to correct.":""), NumPhantomLanesDetected.ToString());
        }


        private void ProcessCheckCUButton()
        {
            NumPhantomCUDetected = 0;
            NumPhantomCUDetected = Helper.LogAndFixCitizenUnits();
            if(NumPhantomCUDetected > 0 )
            {
                m_FixCitizenUnitsButton.isVisible = true;
                m_MessageText.textColor = new Color32(255,204,0,255); //yellowish
            }
            else
            { 
                m_MessageText.textColor = new Color32(0, 204, 0, 255); //greenish
            }
            m_MessageText.text = String.Format("There were {0} phantom CitizenUnits detected. " + (NumPhantomCUDetected > 0 ? "Press 'Fix CitizenUnits' to correct.":""), NumPhantomCUDetected.ToString());

        }
        private void ProcessFixCUButton()
        {
            try
            {
                NumPhantomCURemoved = Helper.LogAndFixCitizenUnits(true);
                NumPhantomCUDetected = 0;
                m_FixCitizenUnitsButton.isVisible = false;
                m_MessageText.textColor = new Color32(0, 204, 0, 255); //greenish
                m_MessageText.text = String.Format("{0} phantom CitizenUnits were successfully cleaned up. Save this cleaned version.", NumPhantomCURemoved.ToString());
            }
            catch (Exception ex)
            {
                Helper.dbgLog("Error while clearing Citizen Units.", ex, true);
            }
        }

    }
}
