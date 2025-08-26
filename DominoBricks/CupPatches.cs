using HarmonyLib;
using KMod;

[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
{
    public static void Prefix()
    {
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.NAME", "Cup");
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.DESC", "A container for holding liquids");
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.EFFECT", "Stores up to 500g of liquid");
    }
}

public class CupPatches : UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        ModUtil.AddBuildingToPlanScreen("Base", CupConfig.ID);
    }
}