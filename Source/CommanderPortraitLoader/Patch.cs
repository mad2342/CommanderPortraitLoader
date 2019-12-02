using System;
using Harmony;
using BattleTech;
using System.IO;
using BattleTech.Portraits;
using UnityEngine;
using BattleTech.UI;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text.RegularExpressions;
using BattleTech.Data;

namespace CommanderPortraitLoader {

    [HarmonyPatch(typeof(PilotDef), "SaveGameRequestResource", typeof(LoadRequest))]
    class PilotDef_SaveGameRequestResource_Patch
    {
        public static void Postfix(PilotDef __instance, LoadRequest loadRequest)
        {
            if (__instance.PortraitSettings != null)
            {
                // perform this here on the first save load after character creation
                // this avoids all sorts of exceptions and problems with the character customization UIs
                // this also means we only need to do this in one patch instead of many
                if (!string.IsNullOrEmpty(__instance.PortraitSettings.Description.Icon))
                {
                    __instance.Description.SetIcon(__instance.PortraitSettings.Description.Icon);
                    __instance.PortraitSettings = null;
                    Logger.LogLine(string.Format("Applying Hardset Icon to Pilot: {0}, {1}", (object)__instance.Description.Callsign, (object)__instance.Description.Icon));
                }
            }
            if (!string.IsNullOrEmpty(__instance.Description.Icon))
            {
                //Logger.LogLine(string.Format("Loading Pilot: {0}, {1}", (object)__instance.Description.Callsign, (object)__instance.Description.Icon));
                // Issue a Load request for any custom sprites 
                try
                {
                    Logger.LogLine(string.Format("Issuing  Load Request Icon for Pilot: {0}, {1}", (object)__instance.Description.Callsign, (object)__instance.Description.Icon));
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.Sprite, __instance.Description.Icon, new bool?(false));
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
    }

    [HarmonyPatch(typeof(RenderedPortraitResult), "get_Item")]
    public static class RenderedPortraitResult_get_Item_Patch {
        static void Postfix(RenderedPortraitResult __instance, ref Texture2D __result)
        {
            if (!string.IsNullOrEmpty(__instance.settings.Description.Icon))
            {
                try
                {
                    Texture2D texture2D = new Texture2D(2, 2);
                    byte[] array = File.ReadAllBytes($"{ CommanderPortraitLoader.ModDirectory}/Portraits/" + __instance.settings.Description.Icon + ".png");
                    texture2D.LoadImage(array);
                    __result = texture2D;
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SGBarracksMWCustomizationPopup), "Save")]
    public static class SGBarracksMWCustomizationPopup_Save_Patch
    {
        static void Postfix(ref SGBarracksMWCustomizationPopup __instance)
        {
            if (!string.IsNullOrEmpty(__instance.pilot.pilotDef.Description.Icon))
            {
                __instance.pilot.pilotDef.PortraitSettings = null;
            }
        }
    }

    [HarmonyPatch(typeof(SGBarracksMWCustomizationPopup), "LoadPortraitSettings")]
    public static class SGBarracksMWCustomizationPopup_LoadPortraitSettings_Patch
    {
        static void Prefix(ref SGBarracksMWCustomizationPopup __instance, ref PortraitSettings portraitSettingsData)
        {
            try
            {
                if (portraitSettingsData == null)
                {
                    if (!string.IsNullOrEmpty(__instance.pilot.pilotDef.Description.Icon))
                    {
                        string filePath = $"{ CommanderPortraitLoader.ModDirectory}/Jsons/" + __instance.pilot.pilotDef.Description.Icon + ".json";
                        if (File.Exists(filePath))
                        {
                            portraitSettingsData = new PortraitSettings();
                            using (StreamReader r = new StreamReader(filePath))
                            {
                                string json = r.ReadToEnd();
                                portraitSettingsData.FromJSON(json);
                            }
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

    [HarmonyPatch(typeof(SGBarracksMWDetailPanel), "CustomizePilot")]
    public static class SGBarracksMWDetailPanel_CustomizePilot_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int startIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld)
                {
                    startIndex = i;
                    break;
                }
            }
            if (startIndex > -1)
            {
                codes.RemoveRange(startIndex + 1, 8);
                codes.Insert(startIndex + 1, new CodeInstruction(OpCodes.Ldc_I4_0));
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilitiesPatch
    {
        public static void Postfix(ref VersionManifest __result)
        {
            try
            {
                string filePath = $"{ CommanderPortraitLoader.ModDirectory}/Jsons/";
                DirectoryInfo d1 = new DirectoryInfo(filePath);
                FileInfo[] f1 = d1.GetFiles("*.json");

                PortraitSettings preset = new PortraitSettings();
                foreach (FileInfo info in f1)
                {
                    using (StreamReader r = new StreamReader(info.FullName))
                    {
                        string json = r.ReadToEnd();
                        preset.FromJSON(json);
                    }
                    __result.AddOrUpdate(preset.Description.Id, info.FullName, "PortraitSettings", DateTime.Now, null, false);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    // BEN: Make sure custom portraits don't get used by PilotGenerator
    /*
    [HarmonyPatch(typeof(PilotGenerator), "GetPortraitForGenderAndAge")]
    public static class PilotGenerator_GetPortraitForGenderAndAge_Patch
    {
        public static void Prefix(ref PilotGenerator __instance, ref List<string> blackListedIDs)
        {
            try
            {
                var addendum = VersionManifestUtilities.ManifestFromCSV($"{ CommanderPortraitLoader.ModDirectory}/VersionManifest.csv");
                foreach (var entry in addendum.Entries)
                {
                    blackListedIDs.Add(entry.Id);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
    */

    // BEN: Disable customize button in Barracks
    [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot")]
    public static class SGBarracksDossierPanel_SetPilot_Patch
    {
        public static void Postfix(ref SGBarracksDossierPanel __instance, Pilot p)
        {
            try
            {
                // BEN: Would also work (but assumes the player actually has used a custom portrait, generated ones can't be customized anymore too)
                //if (p.IsPlayerCharacter) {
                //    __instance.SetCustomizeButtonEnabled(false);
                //}

                if (p.pilotDef.PortraitSettings == null)
                {
                    __instance.SetCustomizeButtonEnabled(false);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(SimGameState), "GetUnusedRonin")]
    public static class SimGameState_GetUnusedRonin_Patch
    {
        public static void Postfix(ref SimGameState __instance, ref PilotDef __result)
        {
            try
            {
                if (__result != null)
                {
                    string selectedRoninId = __result.Description.Id;
                    Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] selectedRoninId: " + selectedRoninId);

                    List<string> blacklistedRonins = new List<string>();

                    string commanderIcon = __instance.Commander.Description.Icon;
                    Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] commanderIcon: " + commanderIcon);
                    
                    // Test
                    //string commanderIcon = "f_guiTxrPort_backerHuxley_utr";

                    if (commanderIcon != null && commanderIcon != "" && commanderIcon.Contains("backer"))
                    {
                        Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] Commander seems to use portrait of some backer. Will build ID and blacklist it");
                        string backerId;
                        backerId = Regex.Replace(commanderIcon, "guiTxrPort_backer", "");
                        backerId = Regex.Replace(backerId, "f_", "");
                        backerId = Regex.Replace(backerId, "m_", "");
                        backerId = Regex.Replace(backerId, "_utr", "");
                        backerId = "pilot_backer_" + backerId;
                        Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] backerId: " + backerId);

                        blacklistedRonins.Add(backerId);
                    }

                    if (blacklistedRonins.Contains(selectedRoninId))
                    {
                        Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] selectedRoninId: " + selectedRoninId + " is blacklisted!");
                        __result = null;
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
