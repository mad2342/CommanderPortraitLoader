using System;
using System.Reflection;
using System.IO;
using Harmony;
using BattleTech.Portraits;
using System.Collections.Generic;

namespace CommanderPortraitLoader {
    public static class CommanderPortraitLoader {

        internal static string ModDirectory;
        internal static string LogPath;

        public static bool disableCreatePilotPatch;
        public static List<string> blacklistedPortraits = new List<string>();

        // BEN: Debug (0: nothing, 1: errors, 2:all)
        internal static int DebugLevel = 2;

        public static void Init(string directory, string settingsJSON) {
            var harmony = HarmonyInstance.Create("de.mad.CommanderPortraitLoader");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModDirectory = directory;
            LogPath = Path.Combine(ModDirectory, "CommanderPortraitLoader.log");
            File.CreateText(CommanderPortraitLoader.LogPath);

            disableCreatePilotPatch = true;
            CreateJsons();
        }

        public static void CreateJsons()
        {
            try
            {
                //Create a path for the Json files if it does not already exist
                string jsonPath = $"{ CommanderPortraitLoader.ModDirectory}/PortraitSettings/";
                Directory.CreateDirectory(jsonPath);

                string filePath = $"{ CommanderPortraitLoader.ModDirectory}/Portraits/Commander/";
                DirectoryInfo d1 = new DirectoryInfo(filePath);
                FileInfo[] f1 = d1.GetFiles("*.png");
                foreach (FileInfo info in f1)
                {
                    PortraitSettings portrait = new PortraitSettings();

                    // BEN: Make portraits appear for correct gender settings via filename
                    //portrait.headMesh = 0.5f;
                    if (info.Name.Contains("f_"))
                    {
                        portrait.headMesh = 0.9f;
                    }
                    if (info.Name.Contains("m_"))
                    {
                        portrait.headMesh = 0.1f;
                    }

                    //---
                    //portrait.Randomize(true);
                    //portrait.Description.SetName(info.Name.Replace(".png", ""));
                    //portrait.Description.SetID(info.Name.Replace(".png", ""));
                    //portrait.Description.SetIcon(info.Name.Replace(".png", ""));

                    // BEN: Set Description via BaseDescriptionDef to be able to save path in Description.Details itself
                    string id = info.Name.Replace(".png", "");
                    string path = "/Portraits/Commander/" + info.Name;
                    portrait.Description = new BattleTech.BaseDescriptionDef(id, id, path, id);
                    //---

                    portrait.isCommander = true;
                    using (StreamWriter writer = new StreamWriter(jsonPath + info.Name.Replace(".png", ".json"), false))
                    {
                        writer.WriteLine(portrait.ToJSON());
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