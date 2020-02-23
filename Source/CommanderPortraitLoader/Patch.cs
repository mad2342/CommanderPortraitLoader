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
                    Logger.Debug(string.Format("[SaveGameRequestResource_POSTFIX] Set Icon for Pilot {0}: {1}", (object)__instance.Description.Callsign, (object)__instance.Description.Icon));
                }
            }
            if (!string.IsNullOrEmpty(__instance.Description.Icon))
            {
                // Issue a Load request for any custom sprites 
                try
                {
                    Logger.Debug(string.Format("[SaveGameRequestResource_POSTFIX] Issuing Load Request Icon for Pilot {0}: {1}", (object)__instance.Description.Callsign, (object)__instance.Description.Icon));
                    loadRequest.AddBlindLoadRequest(BattleTechResourceType.Sprite, __instance.Description.Icon, new bool?(false));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
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

                    //---
                    //byte[] array = File.ReadAllBytes($"{ CommanderPortraitLoader.ModDirectory}/Portraits/Commander/" + __instance.settings.Description.Icon + ".png");

                    // BEN: Read path from Description.Details
                    Logger.Debug("[RenderedPortraitResult_get_Item_POSTFIX] Read path from Description.Details: " + __instance.settings.Description.Details);
                    //---

                    byte[] array = File.ReadAllBytes($"{ CommanderPortraitLoader.ModDirectory}" + __instance.settings.Description.Details);

                    texture2D.LoadImage(array);
                    __result = texture2D;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
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
                        Logger.Debug("[SGBarracksMWCustomizationPopup_LoadPortraitSettings_PREFIX] Fetching PortraitSetting for: " + __instance.pilot.pilotDef.Description.Icon);
                        string filePath = CommanderPortraitLoader.PortraitSettingsDirectory + __instance.pilot.pilotDef.Description.Icon + ".json";
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
                Logger.Error(e);
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
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        public static void Postfix(ref VersionManifest __result)
        {
            try
            {
                string filePath = CommanderPortraitLoader.PortraitSettingsDirectory;
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

                    if (!CommanderPortraitLoader.blacklistedPortraits.Contains(preset.Description.Id))
                    {
                        CommanderPortraitLoader.blacklistedPortraits.Add(preset.Description.Id);
                        Logger.Debug("[VersionManifestUtilities_LoadDefaultManifest_POSTFIX] blacklistedPortraits: " + preset.Description.Id);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    // BEN: Add PortraitSettings of all potential Commander portraits AND pilots in Memorial Wall to blacklist
    [HarmonyPatch(typeof(PilotGenerator), "GetPortraitForGenderAndAge")]
    public static class PilotGenerator_GetPortraitForGenderAndAge_Patch
    {
        public static void Prefix(PilotGenerator __instance, ref List<string> blackListedIDs, SimGameState ___Sim)
        {
            try
            {
                foreach (string id in blackListedIDs)
                {
                    Logger.Debug("[PilotGenerator_GetPortraitForGenderAndAge_PREFIX] blackListedIDs: " + id);
                }

                /*
                if (___Sim.Commander != null)
                {
                    // BEN: Note that the other (not chosen) PortraitSettings can still be potentially picked for random pilots. Need some mechanism to blacklist by tag or similar...
                    blackListedIDs.Add(___Sim.Commander.Description.Icon);
                    Logger.Debug("[PilotGenerator_GetPortraitForGenderAndAge_PREFIX] Added to blackListedIDs: " + ___Sim.Commander.Description.Icon);
                }
                */

                /*
                string filePath = CommanderPortraitLoader.PortraitSettingsDirectory;
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
                    blackListedIDs.Add(preset.Description.Id);
                    Logger.Debug("[PilotGenerator_GetPortraitForGenderAndAge_PREFIX] Added to blackListedIDs: " + preset.Description.Id);
                }
                */

                blackListedIDs.AddRange(CommanderPortraitLoader.blacklistedPortraits);
                Logger.Debug("[PilotGenerator_GetPortraitForGenderAndAge_PREFIX] Adding CommanderPortraitLoader.blacklistedPortraits to blackListedIDs...");

                // Blacklist dead pilots too so no "twins of the dead" will arise...
                foreach (Pilot pilot in ___Sim.Graveyard)
                {
                    if (pilot.pilotDef.PortraitSettings != null)
                    {
                        blackListedIDs.Add(pilot.pilotDef.PortraitSettings.Description.Id);
                        Logger.Debug("[PilotGenerator_GetPortraitForGenderAndAge_PREFIX] Added dead pilot to blackListedIDs: " + pilot.pilotDef.PortraitSettings.Description.Id);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        // Info
        public static void Postfix(PilotGenerator __instance, List<string> blackListedIDs)
        {
            try
            {
                foreach (string id in blackListedIDs)
                {
                    Logger.Info("[PilotGenerator_GetPortraitForGenderAndAge_POSTFIX] blackListedIDs: " + id);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }

    // BEN: Disable customize button in Barracks
    [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot")]
    public static class SGBarracksDossierPanel_SetPilot_Patch
    {
        public static void Postfix(ref SGBarracksDossierPanel __instance, Pilot p)
        {
            try
            {
                if (p.pilotDef.PortraitSettings == null)
                {
                    __instance.SetCustomizeButtonEnabled(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
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
                    Logger.Debug("[SimGameState_GetUnusedRonin_POSTFIX] selectedRoninId: " + selectedRoninId);

                    string commanderIcon = __instance.Commander.Description.Icon;
                    Logger.Debug("[SimGameState_GetUnusedRonin_POSTFIX] Commander.Description.Icon: " + commanderIcon);

                    string backerId = "";

                    if (commanderIcon != null && commanderIcon != "" && commanderIcon.Contains("backer"))
                    {
                        Logger.Debug("[SimGameState_GetUnusedRonin_POSTFIX] Commander seems to use portrait of some backer...");
                        backerId = Regex.Replace(commanderIcon, "guiTxrPort_backer", "");
                        backerId = Regex.Replace(backerId, "f_", "");
                        backerId = Regex.Replace(backerId, "m_", "");
                        backerId = Regex.Replace(backerId, "_utr", "");
                        backerId = "pilot_backer_" + backerId;
                        Logger.Debug("[SimGameState_GetUnusedRonin_POSTFIX] ...backerId: " + backerId);
                    }

                    if (backerId == selectedRoninId)
                    {
                        Logger.Debug("[SimGameState_GetUnusedRonin_POSTFIX] selectedRoninId: " + selectedRoninId + " is using the same portrait as the commander. Nulling it.");
                        __result = null;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
