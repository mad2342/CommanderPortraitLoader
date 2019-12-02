using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BattleTech.Portraits;



namespace CommanderPortraitLoader {
    public static class CommanderPortraitLoader {

        internal static string ModDirectory;
        public static bool disableCreatePilotPatch;

        // BEN: Debug (0: nothing, 1: errors, 2:all)
        internal static int DebugLevel = 2;

        public static void Init(string directory, string settingsJSON) {
            var harmony = HarmonyInstance.Create("de.mad.CommanderPortraitLoader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModDirectory = directory;
            disableCreatePilotPatch = true;
            CreateJsons();
        }

        public static void CreateJsons()
        {
            try
            {
                //Create a path for the Json files if it does not already exist
                string jsonPath = $"{ CommanderPortraitLoader.ModDirectory}/Jsons/";
                Directory.CreateDirectory(jsonPath);

                string filePath = $"{ CommanderPortraitLoader.ModDirectory}/Portraits/";
                DirectoryInfo d1 = new DirectoryInfo(filePath);
                FileInfo[] f1 = d1.GetFiles("*.png");
                foreach (FileInfo info in f1)
                {
                    if (!File.Exists(info.FullName.Replace(".png", ".json")))
                    {
                        PortraitSettings portrait = new PortraitSettings();

                        // BEN: Make portraits appear for correct gender settings via filename
                        //portait.headMesh = 0.5f;
                        if (info.FullName.Contains("f_"))
                        {
                            portrait.headMesh = 0.9f;
                        }
                        if (info.FullName.Contains("m_"))
                        {
                            portrait.headMesh = 0.1f;
                        }

                        portrait.Randomize(true);
                        portrait.Description.SetName(info.Name.Replace(".png", ""));
                        portrait.Description.SetID(info.Name.Replace(".png", ""));
                        portrait.Description.SetIcon(info.Name.Replace(".png", ""));
                        portrait.isCommander = true;
                        using (StreamWriter writer = new StreamWriter(jsonPath + info.Name.Replace(".png", ".json"), false))
                        {
                            writer.WriteLine(portrait.ToJSON());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}