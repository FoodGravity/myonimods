using HarmonyLib;
using UnityEngine;

namespace 墙式马桶优先级
{
    public class WallToiletPrioritizablePatches
    {
        [HarmonyPatch(typeof(WallToiletConfig))]
        [HarmonyPatch("ConfigureBuildingTemplate")]
        public static class WallToiletConfig_ConfigureBuildingTemplate_Patch
        {
            public static void Postfix(GameObject go)
            {
                // 为墙式马桶添加优先级组件
                Prioritizable.AddRef(go);
            }
        }
    }
}