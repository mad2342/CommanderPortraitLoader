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



namespace CommanderPortraitLoader {
    [HarmonyPatch(typeof(RenderedPortraitResult), "get_Item")]
    public static class RenderedPortraitResult_get_Item_Patch {
        static void Postfix(RenderedPortraitResult __instance, ref Texture2D __result) {
            if (!string.IsNullOrEmpty(__instance.settings.Description.Icon)) {
                try {
                    Texture2D texture2D = new Texture2D(2, 2);
                    byte[] array = File.ReadAllBytes($"{ CommanderPortraitLoader.ModDirectory}/Portraits/" + __instance.settings.Description.Icon + ".png");
                    texture2D.LoadImage(array);
                    __result = texture2D;
                }
                catch (Exception e) {
                    Logger.LogError(e);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SGCharacterCreationWidget), "CreatePilot")]
    public static class SGCharacterCreationWidget_CreatePilot_Patch {

        static void Postfix(ref SGCharacterCreationWidget __instance, ref Pilot __result) {
            try {
                if (!string.IsNullOrEmpty(__result.pilotDef.PortraitSettings.Description.Icon)) {

                    PilotDef pilotDef = new PilotDef(new HumanDescriptionDef(__result.Description.Id, __result.Description.Callsign, __result.Description.FirstName, __result.Description.LastName,
                        __result.Description.Callsign, __result.Description.Gender, Faction.NoFaction, __result.Description.Age, __result.Description.Details, __result.pilotDef.PortraitSettings.Description.Icon),
                        __result.Gunnery, __result.Piloting, __result.Guts, __result.Tactics, 0, 3, false, 0, string.Empty, Helper.GetAbilities(__result.Gunnery, __result.Piloting, __result.Guts, __result.Tactics), AIPersonality.Undefined, 0, __result.pilotDef.PilotTags, 0, 0);

                    pilotDef.PortraitSettings = null;
                    pilotDef.SetHiringHallStats(true, false, true, false);

                    __result = new Pilot(pilotDef, "commander", false);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(PilotDef), "GetPortraitSprite")]
    public static class PilotDef_GetPortraitSprite_Patch {
        static void Postfix(ref PilotDef __instance, ref Sprite __result) {
            try {
                if (__result == null) {
                    Texture2D texture2D2 = new Texture2D(2, 2);
                    byte[] data = File.ReadAllBytes($"{ CommanderPortraitLoader.ModDirectory}/Portraits/" + __instance.Description.Icon + ".png");
                    texture2D2.LoadImage(data);
                    Sprite sprite = new Sprite();
                    sprite = Sprite.Create(texture2D2, new Rect(0f, 0f, (float)texture2D2.width, (float)texture2D2.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, Vector4.zero);
                    __result = sprite;
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(PilotDef), "GetPortraitSpriteThumb")]
    public static class PilotDef_GetPortraitSpriteThumb_Patch {
        static void Postfix(ref PilotDef __instance, ref Sprite __result) {
            try {
                if (__result == null) {
                    Texture2D texture2D2 = new Texture2D(2, 2);
                    byte[] data = File.ReadAllBytes($"{ CommanderPortraitLoader.ModDirectory}/Portraits/" + __instance.Description.Icon + ".png");
                    texture2D2.LoadImage(data);
                    Sprite sprite = new Sprite();
                    sprite = Sprite.Create(texture2D2, new Rect(0f, 0f, (float)texture2D2.width, (float)texture2D2.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, Vector4.zero);
                    __result = Helper.DownsampleSprite(sprite);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilitiesPatch {
        public static void Postfix(ref VersionManifest __result) {
            try {
                var addendum = VersionManifestUtilities.ManifestFromCSV($"{ CommanderPortraitLoader.ModDirectory}/VersionManifest.csv");
                foreach (var entry in addendum.Entries) {
                    __result.AddOrUpdate(entry.Id, entry.FilePath, entry.Type, entry.AddedOn, entry.AssetBundleName, entry.IsAssetBundlePersistent);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }

    // BEN: Make sure custom portraits don't get used by PilotGenerator
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

    [HarmonyPatch(typeof(SimGameState), "AddPilotToRoster", new Type[] { typeof(PilotDef), typeof(bool), typeof(bool) })]
    public static class SimGameState_AddPilotToRoster_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int foundIndex = -1;
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldarg_2)
                {
                    foundIndex = i;
                    break;
                }
            }
            if (foundIndex > -1)
            {
                codes[foundIndex].opcode = OpCodes.Nop;
                codes[foundIndex + 1].opcode = OpCodes.Nop;
            }
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(SGCharacterCreationPortraitSelectionPanel), "GetRandomizedSortOrder", new Type[] { typeof(Int32) })]
    public static class SGCharacterCreationPortraitSelectionPanel_PopulateList_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator ilGenerator, IEnumerable<CodeInstruction> instructions)
        {
            int newarrIndex = -1;
            int callvirtIndex = -1;
            int newobjIndex = -1;
            var jump1 = ilGenerator.DefineLabel();
            var jump2 = ilGenerator.DefineLabel();
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Newarr)
                {
                    newarrIndex = i;
                    break;
                }
            }
            for (int j = 0; j < codes.Count; j++)
            {
                if (codes[j].opcode == OpCodes.Callvirt)
                {
                    callvirtIndex = j;
                    break;
                }
            }
            for (int k = 0; k < codes.Count; k++)
            {
                if (codes[k].opcode == OpCodes.Newobj)
                {
                    newobjIndex = k;
                    break;
                }
            }
            codes.Insert(0, new CodeInstruction(OpCodes.Ldarg_1));
            codes.Insert(1, new CodeInstruction(OpCodes.Newarr, codes[newarrIndex + 1].operand));
            codes.Insert(2, new CodeInstruction(OpCodes.Stloc_1));
            codes.Insert(3, new CodeInstruction(OpCodes.Newobj, codes[newobjIndex + 3].operand));
            codes.Insert(4, new CodeInstruction(OpCodes.Stloc_2));
            codes.Insert(5, new CodeInstruction(OpCodes.Ldc_I4_0));
            codes.Insert(6, new CodeInstruction(OpCodes.Stloc_3));
            codes.Insert(7, new CodeInstruction(OpCodes.Br, jump1));

            codes.Insert(8, new CodeInstruction(OpCodes.Ldloc_1) { labels = new List<Label>() { jump2 } });
            codes.Insert(9, new CodeInstruction(OpCodes.Ldloc_3));
            codes.Insert(10, new CodeInstruction(OpCodes.Ldloc_3));
            codes.Insert(11, new CodeInstruction(OpCodes.Stelem_I4));
            codes.Insert(12, new CodeInstruction(OpCodes.Ldloc_2));
            codes.Insert(13, new CodeInstruction(OpCodes.Ldloc_3));
            codes.Insert(14, new CodeInstruction(OpCodes.Callvirt, codes[callvirtIndex + 14].operand));
            codes.Insert(15, new CodeInstruction(OpCodes.Ldloc_3));
            codes.Insert(16, new CodeInstruction(OpCodes.Ldc_I4_1));
            codes.Insert(17, new CodeInstruction(OpCodes.Add));
            codes.Insert(18, new CodeInstruction(OpCodes.Stloc_3));

            codes.Insert(19, new CodeInstruction(OpCodes.Ldloc_3) { labels = new List<Label>() { jump1 } });
            codes.Insert(20, new CodeInstruction(OpCodes.Ldarg_1));
            codes.Insert(21, new CodeInstruction(OpCodes.Blt, jump2));

            codes.Insert(22, new CodeInstruction(OpCodes.Ldloc_1));
            codes.Insert(23, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Array), "Reverse", new Type[] { typeof(Array) })));
            codes.Insert(24, new CodeInstruction(OpCodes.Ldloc_1));
            codes.Insert(25, new CodeInstruction(OpCodes.Ret));
            return codes.AsEnumerable();
        }
    }

    // BEN: Disable customize button in Barracks
    [HarmonyPatch(typeof(SGBarracksDossierPanel), "SetPilot")]
    public static class SGBarracksDossierPanel_SetPilot_Patch {
        public static void Postfix(ref SGBarracksDossierPanel __instance, Pilot p) {
            try {
                // BEN: Would also work (but assumes the player actually has used a custom portrait, generated ones can't be customized anymore too)
                //if (p.IsPlayerCharacter) {
                //    __instance.SetCustomizeButtonEnabled(false);
                //}

                if (p.pilotDef.PortraitSettings == null) {
                    __instance.SetCustomizeButtonEnabled(false);
                }
            }
            catch (Exception e) {
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
                    string selectedRoninId = __result.Description.Id; Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] selectedRoninId: " + selectedRoninId);
                    List<String> blacklistedRonins = new List<string>
                    {
                        "pilot_sim_starter_behemoth",
                        "pilot_sim_starter_dekker",
                        "pilot_sim_starter_glitch",
                        "pilot_sim_starter_medusa",
                        "pilot_backer_Test9",
                        "pilot_backer_Nick",
                        "pilot_backer_Fielding",
                        "pilot_backer_Keane",
                        "pilot_backer_Saada",
                        "pilot_backer_Slipais",
                        "pilot_ronin_Squire",
                        "pilot_backer_Bixby"
                    };
                    string commanderIcon = __instance.Commander.pilotDef.PortraitSettings.Description.Icon; Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] commanderIcon: " + commanderIcon);
                    
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
                        backerId = "pilot_backer_" + backerId; Logger.LogLine("[SimGameState_GetUnusedRonin_POSTFIX] backerId: " + backerId);

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

    // BEN: Disable customize button for character creation
    /*
    [HarmonyPatch(typeof(SGCharacterCreationNameAndAppearanceScreen), "OnAddedToHierarchy")]
    public static class SGCharacterCreationNameAndAppearanceScreen_OnAddedToHierarchy_Patch
    {
        public static void Postfix(ref SGCharacterCreationNameAndAppearanceScreen __instance) {
            try {
                if (__instance.PortraitSelection.selectedPortrait.portraitSettings == null)
                {
                    __instance.portraitCustomizationButton.SetState(ButtonState.Disabled, false);
                }
            }
            catch (Exception e) {
                Logger.LogError(e);
            }
        }
    }
    */
}
