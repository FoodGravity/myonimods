using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace 手压气泵
{
    public class Patches
    {
        // 修改手压泵尺寸为1x2
        [HarmonyPatch(typeof(LiquidPumpingStationConfig), "CreateBuildingDef")]
        public class LiquidPumpingStationConfig_CreateBuildingDef_Patch
        {
            public static void Postfix(ref BuildingDef __result)
            {
                __result.WidthInCells = 1;
                __result.HeightInCells = 2;
            }
        }

        // 修改手压泵配置，添加气体标签
        [HarmonyPatch(typeof(LiquidPumpingStationConfig), "ConfigureBuildingTemplate")]
        public class LiquidPumpingStationConfig_ConfigureBuildingTemplate_Patch
        {
            public static void Postfix(GameObject go, Tag prefab_tag)
            {
                go.AddTag(GameTags.GasSource);
            }
        }

        // 修改手压泵扫描逻辑，支持气体
        [HarmonyPatch(typeof(LiquidPumpingStation), "Sim200ms")]
        public class LiquidPumpingStation_Sim200ms_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                // 查找IsLiquid检查
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt &&
                        codes[i].operand.ToString().Contains("IsLiquid"))
                    {
                        // 替换为气体或液体的检查
                        codes[i] = new CodeInstruction(OpCodes.Callvirt,
                            AccessTools.Method(typeof(Element), "get_IsGas"));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Or));
                        i++; // 跳过插入的指令
                    }
                }

                return codes;
            }
        }

        // 修改IsLiquidAccessible方法，使其也接受气体
        [HarmonyPatch(typeof(LiquidPumpingStation), "IsLiquidAccessible")]
        public class LiquidPumpingStation_IsLiquidAccessible_Patch
        {
            public static bool Prefix(Element element, ref bool __result)
            {
                __result = element.IsLiquid || element.IsGas;
                return false; // 跳过原方法
            }
        }
    }
}
