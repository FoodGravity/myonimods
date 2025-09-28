using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]

// // 用户菜单补丁
[HarmonyPatch(typeof(ExteriorWallConfig), "DoPostConfigureComplete")]
public static class ExteriorWallConfig_DoPostConfigureComplete_Patch
{
    public static void Postfix(GameObject go)
    {
        go.AddOrGet<CopySkinComponent>();
        go.AddOrGet<CopyBuildingSettings>();
    }
}

// 复制皮肤组件
public class CopySkinComponent : KMonoBehaviour
{
    protected override void OnSpawn()
    {
        base.OnSpawn();
        Subscribe((int)GameHashes.CopySettings, OnCopySettings);
    }

    private void OnCopySettings(object data)
    {
        var source = data as GameObject;
        if (source != null)
        {
            Debug.Log($"[CopySkin] 开始复制皮肤设置，源对象: {source.name}");
            
            // 获取源对象的皮肤ID
            var sourceFacade = source.GetComponent<BuildingFacade>();
            var targetFacade = GetComponent<BuildingFacade>();
            
            if (sourceFacade != null && targetFacade != null)
            {
                string currentFacade = sourceFacade.CurrentFacade;
                if (!string.IsNullOrEmpty(currentFacade))
                {
                    // 获取皮肤资源并应用到目标
                    var facadeResource = Db.GetBuildingFacades().TryGet(currentFacade);
                    if (facadeResource != null)
                    {
                        targetFacade.ApplyBuildingFacade(facadeResource);
                        // Debug.Log($"[CopySkin] 成功复制皮肤设置: {currentFacade}");
                    }
                    // else
                    // {
                    //     Debug.Log($"[CopySkin] 找不到皮肤资源: {currentFacade}");
                    // }
                }
            }
        }
    }
}
