using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace PhantomLaneRemover
{
    public class Configuration
    {
        [XmlIgnoreAttribute]
        public static readonly uint CurrentVersion = 1;  //running version

        public uint ConfigVersion = 1;    //saved version incase we ever need to migrate or wipe config values.
        public string ConfigReserved = ""; //reserved value again for possible migration\upgrade data or some unknown use.
        public bool DebugLogging = false;   
        public byte DebugLoggingLevel = 0;  //detail: 1 basically very similar to just on+0 ; 2 = Very detailed; 3+ extreme only meant for me during dev...if that. 
        public bool EnableGui = true;
        public bool UseAutoRefresh = true;
        public bool AutoShowOnMapLoad = true;
        public float AutoRefreshSeconds = 3.0f;
        public float GuiOpacity = 1.0f;
        public bool DumpStatsOnMapEnd = false;
        public bool CheckStatsForLimitsEnabled= true; //applies to guimode only
        public float StatsCheckEverySeconds = 15.0f; //~5-6x per second or every ~200 miliseconds (it's not exact but should be close).
        public bool UseCustomLogFile = true;
        public string CustomLogFilePath = "PhantomLaneRemover_Log.txt";
        public bool UseAlternateKeyBinding = false;
        public string AlternateKeyBindingCode = "LeftControl,LeftAlt,P";

        public Configuration() { }
        public static bool isCurrentVersion(uint iVersion)
        {
            if(iVersion != CurrentVersion)
            {
                return false;
            }
            return true;
        }

        public static Helper.KeycodeData getAlternateKeyBindings(String sTheText)
        {
            Helper.KeycodeData kcData = new Helper.KeycodeData();
            kcData.NumOfCodes = 0;
            kcData.kCode1 = KeyCode.None;
            kcData.kCode2 = KeyCode.None;
            kcData.kCode3 = KeyCode.None;
            try
            {
                string[] sArray = sTheText.Split(',');
                byte ilen = (byte)sArray.Length;
                if (ilen <= 1)
                { return kcData; }

                if (ilen == 2)
                {
                    kcData.kCode1 = (KeyCode)Enum.Parse(typeof(KeyCode), sArray[0].ToString());
                    kcData.kCode2 = (KeyCode)Enum.Parse(typeof(KeyCode), sArray[1].ToString());
                    kcData.NumOfCodes = 2;
                }
                else
                {
                    kcData.kCode1 = (KeyCode)Enum.Parse(typeof(KeyCode), sArray[0].ToString());
                    kcData.kCode2 = (KeyCode)Enum.Parse(typeof(KeyCode), sArray[1].ToString());
                    kcData.kCode3 = (KeyCode)Enum.Parse(typeof(KeyCode), sArray[2].ToString());
                    kcData.NumOfCodes = 3;
                }
                if (Mod.DEBUG_LOG_ON)
                { Helper.dbgLog("Alternate Keys bound: " + kcData.NumOfCodes.ToString()); }

            }
            catch (Exception ex)
            {
                Helper.dbgLog(ex.Message.ToString(), ex, true);
            }

            if ((kcData.kCode1 == KeyCode.None) || kcData.kCode2 == KeyCode.None)
            {
                kcData.kCode1 = KeyCode.LeftControl; kcData.kCode2 = KeyCode.LeftAlt; kcData.kCode3 = KeyCode.L;
                kcData.NumOfCodes = 3;
                Helper.dbgLog("Alternate Keys enabled but used incorrectly, using default alternate.");
            }
            return kcData;

        }

        public static void Serialize(string filename, Configuration config)
        {
            var serializer = new XmlSerializer(typeof(Configuration));
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch (System.IO.IOException ex1)
            {
                Helper.dbgLog("Filesystem or IO Error: \r\n", ex1, true);
            }
            catch (Exception ex1)
            {
                Helper.dbgLog(ex1.Message.ToString() + "\r\n", ex1, true);
            }
        }

        public static Configuration Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (Configuration)serializer.Deserialize(reader);
                    ValidateConfig(ref config);
                    return config;
                }
            }
            
            catch(System.IO.FileNotFoundException ex4)
            {
                Helper.dbgLog("File not found. This is expected if no config file. \r\n",ex4,false);
            }

            catch (System.IO.IOException ex1)
            {
                Helper.dbgLog("Filesystem or IO Error: \r\n",ex1,true);
            }
            catch (Exception ex1)
            {
                Helper.dbgLog(ex1.Message.ToString() + "\r\n",ex1,true);
            }

            return null;
        }

        /// <summary>
        /// Constrain certain values read in from the config file that will either cause issue or just make no sense. 
        /// </summary>
        /// <param name="tmpConfig"> An instance of an initialized Configuration object (byref)</param>

        public static void ValidateConfig(ref Configuration tmpConfig)
        {
            if (tmpConfig.GuiOpacity > 1.0f | tmpConfig.GuiOpacity < 0.10f) tmpConfig.GuiOpacity = 1.0f;
            if (tmpConfig.AutoRefreshSeconds > 60.0f | tmpConfig.AutoRefreshSeconds < 1.0f) tmpConfig.AutoRefreshSeconds=3.0f;
            if (tmpConfig.StatsCheckEverySeconds > 180.1f | tmpConfig.StatsCheckEverySeconds < 3.00f) tmpConfig.StatsCheckEverySeconds = 60.0f;
        }
    }
}
