using ICities;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.Packaging;
using ColossalFramework.Steamworks;
using ColossalFramework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace PhantomLaneRemover
{
    public class Helper
    {
        public struct KeycodeData
        {
            public byte NumOfCodes;
            public KeyCode kCode1;
            public KeyCode kCode2;
            public KeyCode kCode3;
        }

/*
        [Flags]
        public enum DumpOption : byte
        {
            None = 0,
            Default = 1,
            MapLoaded = 2,
            OptionsOnly = 4,
            DebugInfo = 8,
            UseSeperateFile = 16,
            VehicleData = 32,
            GUIActive = 64,
            ExtendedInfo = 128,
            All = 255
        }
 */ 
//        private const string DumpStatsHeader = "\r\n---------- CSL Show More Limits StatsDump ----------\r\n";
//        private const string DumpVersion = "ModVersion: {0}   Current DateTime: {1}\r\n";

//        private const string dbgDumpstr1 = "\r\n----- Debug info -----\r\n";
//        private const string dbgDumpstr2 = "DebugEnabled: {0}  DebugLogLevel: {1}  isGuiEnabled {2}  AutoRefreshEnabled {3} \r\n";   
//        private const string dbgDumpstr3 = "IsEnabled: {4}  IsInited: {5}  isGuiRunning {6} \r\n";
//        private const string dbgDumpstr4 = "UseAutoRefreshOption: {7}  AutoRefreshSeconds: {8}  GUIOpacity: {9} \r\n";
//        private const string dbgDumpstr5 = "CheckStatsEnabled: {10}  UseCustomLogfile: {11}  DumpLogOnMapEnd: {12} \r\n";
//        private const string dbgDumpstr6 = "UseCustomDumpFile: {13}  DumpFileFullpath: {14}\r\nCustomLogfilePath: {15}  \r\n";
//        private const string dbgDumpstrGUIExtra1 = "IsAutoRefreshActive {3}  CoroutineCheckStats: {5}  CoRoutineDisplayData: {6} \r\n";
//        private const string dbgDumpstrGUIExtra2 = "NewGameAppVersion: {0}  CityName: {1}  Paused: {2}  Mode:{3}\r\n";
//        private const string dbgDumpPaths = "Path Info: \r\n AppBase: {0} \r\n AppExe: {1} \r\n Mods: {2} \r\n Saves: {3} \r\n gContent: {4} \r\n AppLocal: {5} \r\n";

//        private const string dbgGameVersion = "UnityProd: {0}  UnityPlatform: {1} \r\nProductName: {2}  ProductVersion: {3}  ProductVersionString: {4}\r\n";

        private const string sbgMapLimits1 = "#NetSegments: {0} | {1}   #NetNodes: {2} | {3}  #NetLanes: {4} | {5} \r\n";
        private const string sbgMapLimits2 = "#Buildings: {0} | {1}  #ZonedBlocks: {2} | {3} \r\n";
        private const string sbgMapLimits3 = "#Transportlines: {4}  #UserProps: {5}  #PathUnits: {6} \r\n#Areas: {8}  #Districts: {9} #Tress: {10}\r\n#BrokenAssets: {7}\r\n";
        private const string sbgMapLimits4 = "#Citizens: {0}  #Families: {1}  #ActiveCitzenAgents: {2} \r\n";
        private const string sbgMapLimits5 = "#Vehicles: {1}  #ParkedCars: {0} \r\n";


//        private static object[] tmpVer;
//        private static object[] tmpVehc;
//        private static object[] tmpPaths;
//        private static object[] tmpdbg;
//        private static object[] tmpGuiExtra;
//        private static object[] tmpGuiExtra2;

        //should be enough for most log messages and we want this guy in the HFHeap.
        private static StringBuilder logSB = new System.Text.StringBuilder(512);


        internal static int LogLaneData(bool bREMOVE = false)
        {
            uint i = 1;
            StringBuilder laneSB;
            if (bREMOVE & Mod.DEBUG_LOG_ON )
            { laneSB = new StringBuilder(524288); }
            else
            { laneSB = new StringBuilder(4096); }

            try
            {
                NetManager nMgr = Singleton<NetManager>.instance;
                laneSB.AppendLine("\r\n-------- Begin Lane Data ----------\r\n");
                ushort cSegment =0;
                int tmpLaneCreatedCount = 0;
                int tmpLaneNotCreatedCount = 0;
                int tmpSegmentCreatedCount = 0;
                int tmpSegmentNotCreatedCount = 0;
                int tmpNoRealSegmentListed = 0;
                int tmpRealSegmentListed = 0;
                int tmpHasRealNode = 0;
                int tmpHasNoRealNode = 0;
                int tmpReleasedCount = 0;
                int tmpHasNextLane = 0;
                int tmpHasNoNextLane = 0;
                
                for (i = 1; i < nMgr.m_lanes.m_size; i++)
                {
                    cSegment = 0;
                    if((nMgr.m_lanes.m_buffer[i].m_flags & (ushort)NetLane.Flags.Created ) == (ushort)NetLane.Flags.Created)
                    {
                        tmpLaneCreatedCount++;
                        cSegment = nMgr.m_lanes.m_buffer[i].m_segment;
                        if (cSegment > 0)
                        {
                            tmpRealSegmentListed++;
                            if (isSegmentCreated(nMgr, cSegment))
                            {
                                tmpSegmentCreatedCount++;
                            }
                            else
                            {
                                tmpSegmentNotCreatedCount++;
                            }
                        }
                        else
                        { 
                            tmpNoRealSegmentListed++;
                            if(nMgr.m_lanes.m_buffer[i].m_nodes > 0)
                            {
                                tmpHasRealNode++;
                            }
                            else
                            {
                                tmpHasNoRealNode++;
                            }

                            if (nMgr.m_lanes.m_buffer[i].m_nextLane > 0)
                            {
                                tmpHasNextLane++;
                            }
                            else 
                            {
                                tmpHasNoNextLane++;
                            }

                            if (bREMOVE == true & (nMgr.m_lanes.m_buffer[i].m_nodes == 0))
                            {
                                //Array32<NetLane> tmpABCD = new Array32<NetLane>(1);
                                uint tmpunUsedCount = (uint)typeof(Array32<NetLane>).GetField("m_unusedCount", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(nMgr.m_lanes);

                                if(Mod.DEBUG_LOG_ON)
                                {
                                    laneSB.AppendLine("Releasing lane with index# " + i.ToString() + "  nextlane=" + nMgr.m_lanes.m_buffer[i].m_nextLane.ToString() +
                                    " ItemCount =" + nMgr.m_lanes.ItemCount().ToString() + " unusedCount == " + tmpunUsedCount.ToString());
                                }
                                nMgr.m_lanes.m_buffer[i] = default(NetLane);
                                nMgr.m_lanes.ReleaseItem(i);

                                tmpReleasedCount++ ;
                                nMgr.m_laneCount = (int)(nMgr.m_lanes.ItemCount() - 1u);
                            }
                        }
                    }
                    else
                    {
                        tmpLaneNotCreatedCount++;
                    }

                }

                if (bREMOVE == false)
                {
                    laneSB.AppendFormat("There were {0} lanes marked as created, and {1} lanes marked not created.\r\n",
                        tmpLaneCreatedCount.ToString(), tmpLaneNotCreatedCount.ToString());
                    laneSB.AppendFormat("Of the {0} lanes marked as created, {1} had segmentid's > 0 , {2} did not and are suspect.\r\n",
                        tmpLaneCreatedCount.ToString(), tmpRealSegmentListed.ToString(), tmpNoRealSegmentListed.ToString());
                    laneSB.AppendFormat("Of {0} 'created' lanes with segmentid's > 0\r\n there were {1} lanes with segments that are marked created (good), and {2} linked to segmentid's not marked as created (bad).\r\n",
                        tmpRealSegmentListed.ToString(), tmpSegmentCreatedCount.ToString(), tmpSegmentNotCreatedCount.ToString());
                    laneSB.AppendFormat("Of {0} lanes with 'NoRealSegment but created'\r\n There were {1} lanes with Nodes > 0 , and {2} lanes linked to node 0 (likely bad).\r\n",
                        tmpNoRealSegmentListed.ToString(), tmpHasRealNode.ToString(), tmpHasNoRealNode.ToString());
                    laneSB.AppendFormat("Of {0} lanes with 'NoRealSegment but created'\r\n There were {1} lanes with NextLane > 0 , and {2} lanes linked to NextLane 0 (tails).\r\n",
                        tmpNoRealSegmentListed.ToString(), tmpHasNextLane.ToString(), tmpHasNoNextLane.ToString());
                }
                else 
                {
                    laneSB.AppendFormat("We just released {0} lanes that appeared to be phantoms lanes.\r\n",
                        tmpReleasedCount.ToString());
                }
                laneSB.AppendLine("\r\n-------- End Lane Data ----------");
                dbgLog(laneSB.ToString());
                if (bREMOVE == false)
                {
                    return tmpNoRealSegmentListed;
                }
                else 
                {
                    return tmpReleasedCount;
                }
            }
            catch (Exception ex)
            { dbgLog("Error in lane data: " + i.ToString() + "\r\n"+ laneSB.ToString(), ex, true); }
            return 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="bREMOVE"> flag to trigger actual removal</param>
        /// <returns></returns>
        internal static int LogAndFixCitizenUnits(bool bREMOVE = false)
        {
            uint i = 1;
            StringBuilder cuSB;
            if (bREMOVE & Mod.DEBUG_LOG_ON)
            { cuSB = new StringBuilder(524288); }
            else
            { cuSB = new StringBuilder(4096); }

            try
            {
                int tmpCitizenUnitsCreated = 0;
                int tmpCitizenUnitsWithLinks = 0;
                int tmpCitizenUnitsWithoutLinks = 0;
                int tmpCitizenUnitsWithBadLinks = 0;
                int tmpCitizenUnitsReleased = 0;
                CitizenManager cm = Singleton<CitizenManager>.instance;
                VehicleManager vm = Singleton<VehicleManager>.instance;
                BuildingManager bm = Singleton<BuildingManager>.instance;
                bool bFlag = false;

                cuSB.AppendLine("\r\n-------- Begin Citizen Unit Data ----------");
                for (i = 1; i < cm.m_units.m_buffer.Length; i++)
                {
                    bFlag = false;
                    if ((cm.m_units.m_buffer[i].m_flags & CitizenUnit.Flags.Created) == CitizenUnit.Flags.Created)
                    {
                        tmpCitizenUnitsCreated++;
                        CitizenUnit cu = cm.m_units.m_buffer[i];
                        if (cu.m_building == 0 && cu.m_goods == 0 && cu.m_vehicle == 0)
                        {
                            tmpCitizenUnitsWithoutLinks++;
                            if (bREMOVE)
                            {
                                if (Mod.DEBUG_LOG_ON)
                                { cuSB.AppendLine(string.Concat("Releasing citizen unit number: ",i)); }
                                cm.m_units.m_buffer[i] = default(CitizenUnit);
                                cm.ReleaseUnits(i);
                                cm.m_unitCount = (int)(cm.m_units.ItemCount() - 1u);
                                tmpCitizenUnitsReleased++;
                            }
                        }
                        else
                        {
                            tmpCitizenUnitsWithLinks++;
                            if (cu.m_building > 0) 
                            {
                                if (!isBuildingCreated(bm, cu.m_building))
                                {
                                    tmpCitizenUnitsWithBadLinks++;
                                    continue;
                                }
 
                            }

                            if (cu.m_vehicle > 0)
                            {
                                if (!isVehicleCreated(vm, cu.m_vehicle))
                                {
                                    tmpCitizenUnitsWithBadLinks++;
                                    continue;
                                }
                            }
                            if (bFlag && Mod.DEBUG_LOG_ON)
                            { cuSB.AppendLine(string.Concat("Citizen unit number: ", i," is linked to vehc or bldg that is not created.")); }

                        }

                    }
 
                }
                cuSB.AppendLine(string.Format("There were {0} Citizen Units marked as created.",tmpCitizenUnitsCreated.ToString()));
                cuSB.AppendLine(string.Format("There were {0} Citizen Units marked as having links to other objects.",tmpCitizenUnitsWithLinks.ToString()));
                cuSB.AppendLine(string.Format("There were {0} Citizen Units marked as having links to *NO* objects.", tmpCitizenUnitsWithoutLinks.ToString()));
                cuSB.AppendLine(string.Format("There were {0} Citizen Units marked as having links to objects that are not created themselves.", tmpCitizenUnitsWithBadLinks.ToString()));
                if (bREMOVE)
                {
                    cuSB.AppendLine(string.Format("Of the {0} Citizen Units that had NO objects, {1} were successfully reset and released.",tmpCitizenUnitsWithoutLinks.ToString(), tmpCitizenUnitsReleased.ToString()));
                }

                cuSB.AppendLine("\r\n-------- End CitizenUnit Data ----------");
                dbgLog(cuSB.ToString());
                if(bREMOVE)
                {
                    return tmpCitizenUnitsReleased;
                }
                else
                {
                    return tmpCitizenUnitsWithoutLinks;

                }
 
            }
            catch (Exception ex)
            { Helper.dbgLog("Error in reviewing CitizenUnit data: " + i.ToString()+"\r\n" + cuSB.ToString(), ex, true); }
            return 0;
        }


        private static bool isBuildingCreated(BuildingManager bMgr, ushort iBuildingIndex)
        {
            if ((bMgr.m_buildings.m_buffer[iBuildingIndex].m_flags & Building.Flags.Created) == Building.Flags.Created)
            {
                return true;
            }
            return false;
 
        }

        private static bool isVehicleCreated(VehicleManager vMgr, ushort iVehicleIndex)
        {
            if ((vMgr.m_vehicles.m_buffer[iVehicleIndex].m_flags & Vehicle.Flags.Created) == Vehicle.Flags.Created )
            {
                return true;
            }
            return false;
        }

        private static bool isSegmentCreated(NetManager nMgr, ushort iSegmentIndex) 
        {
            if ((nMgr.m_segments.m_buffer[iSegmentIndex].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.Created)
            {
                return true;
            }
            return false;

        }

      


        /// <summary>
        /// Adds building and network limit information into the string builder stream... we added other data why not this.
        /// </summary>
        /// <param name="sb">an already created stringbuilder object.</param>
        public static object[] AddLimitData(byte bObjFlag=0,StringBuilder sb = null)
        {
            try
            {
                object[] tmpdata;
                if (bObjFlag == 0 & sb != null)
                {
                    sb.AppendLine("\r\n----- Map Counter and Object Limit Data -----\r\n");
                }
                if (bObjFlag == 0 || bObjFlag == 2)
                {
                    NetManager tMgr = Singleton<NetManager>.instance;
                    tmpdata = new object[]{tMgr.m_segmentCount.ToString(), tMgr.m_segments.ItemCount().ToString(),
                    tMgr.m_nodeCount.ToString(), tMgr.m_nodes.ItemCount().ToString(), tMgr.m_laneCount.ToString(),
                    tMgr.m_lanes.ItemCount().ToString()};
                    if (bObjFlag == 2)
                    { return tmpdata; }
                    sb.AppendFormat(sbgMapLimits1, tmpdata);
                }

                if (bObjFlag == 0 || bObjFlag == 4)
                {
                    CitizenManager cMgr = Singleton<CitizenManager>.instance;
                    tmpdata = new object[] {cMgr.m_citizens.ItemCount().ToString(),cMgr.m_units.ItemCount().ToString(),
                    cMgr.m_instances.ItemCount().ToString() };
                    if (bObjFlag == 4)
                    { return tmpdata; }
                    sb.AppendFormat(sbgMapLimits4, tmpdata);
                }

                if (bObjFlag == 0 || bObjFlag == 8)
                {
                    tmpdata = new object[]{Singleton<BuildingManager>.instance.m_buildingCount.ToString(),
                    Singleton<BuildingManager>.instance.m_buildings.ItemCount().ToString(), Singleton<ZoneManager>.instance.m_blockCount.ToString(),
                    Singleton<ZoneManager>.instance.m_blocks.ItemCount(),Singleton<TransportManager>.instance.m_lines.ItemCount().ToString(),
                    Singleton<PropManager>.instance.m_props.ItemCount(),Singleton<PathManager>.instance.m_pathUnits.ItemCount(),
                    Singleton<LoadingManager>.instance.m_brokenAssets,Singleton<GameAreaManager>.instance.m_areaCount.ToString(),
                    Singleton<DistrictManager>.instance.m_districts.ItemCount().ToString(),Singleton<TreeManager>.instance.m_treeCount.ToString()};
                    if (bObjFlag == 8)
                    { return tmpdata; }
                    sb.AppendFormat(sbgMapLimits2, tmpdata);
                    sb.AppendFormat(sbgMapLimits3, tmpdata);
                }

                if (bObjFlag == 0 || bObjFlag == 16)
                {
                    tmpdata = new object[] { Singleton<VehicleManager>.instance.m_parkedCount.ToString(),
                Singleton<VehicleManager>.instance.m_vehicleCount.ToString(),Singleton<VehicleManager>.instance.m_vehicles.ItemCount().ToString()};
                    if (bObjFlag == 16)
                    { return tmpdata; }
                    sb.AppendFormat(sbgMapLimits5, tmpdata);
                }
                


            }
            catch(Exception ex)
            {
                dbgLog("Error:\r\n",ex,true);
            }
            return null;

        }



        /// <summary>
        /// Our LogWrapper...used everywhere.
        /// </summary>
        /// <param name="sText">Text to log</param>
        /// <param name="ex">An Exception - if not null it's basic data will be printed.</param>
        /// <param name="bDumpStack">If an Exception was passed do you want the stack trace?</param>
        /// <param name="bNoIncMethod">If for some reason you don't want the method name prefaced with the log line.</param>
        public static void dbgLog(string sText, Exception ex = null, bool bDumpStack = false, bool bNoIncMethod = false) 
        {
            try
            {
                logSB.Length = 0;
                string sPrefix = string.Concat("[", Mod.MOD_DBG_Prefix);
                if (bNoIncMethod) { string.Concat(sPrefix, "] "); }
                else
                {
                    System.Diagnostics.StackFrame oStack = new System.Diagnostics.StackFrame(1); //pop back one frame, ie our caller.
                    sPrefix = string.Concat(sPrefix, ":", oStack.GetMethod().DeclaringType.Name, ".", oStack.GetMethod().Name, "] ");
                }
                logSB.Append(string.Concat(sPrefix, sText));

                if (ex != null)
                {
                    logSB.Append(string.Concat("\r\nException: ", ex.Message.ToString()));
                }
                if (bDumpStack)
                {
                    logSB.Append(string.Concat("\r\nStackTrace: ", ex.ToString()));
                }
                if (Mod.config != null && Mod.config.UseCustomLogFile == true)
                {
                    string strPath = System.IO.Directory.Exists(Path.GetDirectoryName(Mod.config.CustomLogFilePath)) ? Mod.config.CustomLogFilePath.ToString() : Path.Combine(DataLocation.executableDirectory.ToString(), Mod.config.CustomLogFilePath);
                    using (StreamWriter streamWriter = new StreamWriter(strPath, true))
                    {
                        streamWriter.WriteLine(logSB.ToString());
                    }
                }
                else 
                {
                    Debug.Log(logSB.ToString());
                }
            }
            catch (Exception Exp)
            {
                Debug.Log(string.Concat("[CSLShowMoreLimits.Helper.dbgLog()] Error in log attempt!  ", Exp.Message.ToString()));
            }
            logSB.Length = 0;
            if (logSB.Capacity > 8192)
            { logSB.Capacity = 4096; }

        }
    }

}
