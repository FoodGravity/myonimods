using HarmonyLib;
using KMod;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
{
    public static void Prefix()
    {
        // 添加建筑名称、描述和效果的本地化字符串
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.NAME", "装杯");
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.DESC", "一个容器");
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.EFFECT", "最多存储1吨");
    }
}

//复制建筑模式标记
public static class CopyBuildingFlag
{
    public static bool IsCopyBuildingMode = false;
    public static Building SourceBuilding = null;
}

[HarmonyPatch(typeof(MaterialSelectionPanel), "SelectSourcesMaterials")]
public static class MaterialSelectionPanel_SelectSourcesMaterials_Patch
{
    public static void Prefix(Building building)
    {
        CopyBuildingFlag.IsCopyBuildingMode = true;
        CopyBuildingFlag.SourceBuilding = building;
    }
}

// 新增即时建造补丁
[HarmonyPatch(typeof(BuildingDef), "Instantiate")]
public static class BuildingDef_Instantiate_Patch
{
    public static bool Prefix(
        Vector3 pos,
        Orientation orientation,
        IList<Tag> selected_elements,
        int layer,
        BuildingDef __instance,
        ref GameObject __result)
    {
        if (__instance.name != CupConfig.ID)
            return true;

        bool isCopied = CopyBuildingFlag.IsCopyBuildingMode;
        Building sourceBuilding = CopyBuildingFlag.SourceBuilding;
        CopyBuildingFlag.IsCopyBuildingMode = false;
        CopyBuildingFlag.SourceBuilding = null;

        // 判断当前是否为复制建筑工具
        var 源材质 = selected_elements[0];

        if (selected_elements.Count > 0)
            selected_elements[0] = TagManager.Create("Vacuum");

        __result = __instance.Build(Grid.PosToCell(pos), orientation, null, selected_elements, 293.15f, false, GameClock.Instance.GetTime());

        if (__result != null)
        {
            Cup cup = __result.GetComponent<Cup>();
            if (cup != null && cup.options != null)
            {
                if (isCopied && sourceBuilding != null)
                {
                    // 这里可以复制源建筑的设置
                    // cup.options.debugtext = "复制建筑 " + 源材质 + "，源建筑ID: " + sourceBuilding.GetInstanceID();
                    // cup.OnCopySettings(sourceBuilding.gameObject);

                    // 使用协程实现延迟执行
                    Global.Instance.StartCoroutine(DelayedFilterSync(cup, sourceBuilding));
                }
                else
                {
                    // cup.options.debugtext = "新建筑" + 源材质;
                }
            }
        }
        return false;
    }

    private static IEnumerator<float> DelayedFilterSync(Cup cup, Building sourceBuilding)
    {
        // 等待一帧
        yield return 0f;

        // cup.options.debugtext += "开始执行延迟";
        // var storage = cup.GetComponent<Storage>();
        //        if (storage != null)
        // {
        cup.Trigger((int)GameHashes.CopySettings, sourceBuilding.gameObject);
        // }
    }
}

// 新增UI相关补丁
[HarmonyPatch(typeof(ProductInfoScreen), "SetMaterials")]
public static class ProductInfoScreen_SetMaterials_Patch
{
    public static void Postfix(BuildingDef def, ref ProductInfoScreen __instance)
    {
        if (def.name == CupConfig.ID)
            __instance.materialSelectionPanel.gameObject.SetActive(false);
    }
}

[HarmonyPatch(typeof(ResourceRemainingDisplayScreen), "GetString")]
public static class ResourceRemainingDisplayScreen_Patch
{
    public static string Postfix(string __result)
    {
        if (BuildTool.Instance.GetComponent<BuildToolHoverTextCard>().currentDef.name == CupConfig.ID)
            return "无需材料";
        return __result;
    }
}

public class CupPatches : UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        // 先注册字符串
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.NAME", "装杯");
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.DESC", "一个容器");
        Strings.Add($"STRINGS.BUILDINGS.PREFABS.{CupConfig.ID.ToUpper()}.EFFECT", "最多存储1吨");
        // 再添加建筑到计划屏幕
        ModUtil.AddBuildingToPlanScreen("Base", CupConfig.ID);
    }
}