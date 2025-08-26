using HarmonyLib;
using KMod;
using TUNING;
using System.Collections.Generic;
using STRINGS;

namespace DominoBricks
{
    public class DominoBricksPatches : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            ModUtil.AddBuildingToPlanScreen("Base", DominoBrickConfig.ID);
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
    public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
    {
        public static void Prefix()
        {
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{DominoBrickConfig.ID.ToUpper()}.NAME", "Domino Brick");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{DominoBrickConfig.ID.ToUpper()}.DESC", "Trigger chain reaction when deconstructed");
            Strings.Add($"STRINGS.BUILDINGS.PREFABS.{DominoBrickConfig.ID.ToUpper()}.EFFECT", "Causes nearby domino bricks to deconstruct");
        }
    }
}