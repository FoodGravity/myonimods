using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace 手压气泵
{
    public class Patches
    { 
        // 修改手压泵配置，添加气体标签
        [HarmonyPatch(typeof(LiquidPumpingStationConfig), "ConfigureBuildingTemplate")]
        public class LiquidPumpingStationConfig_ConfigureBuildingTemplate_Patch
        {
            public static void Postfix(GameObject go, Tag prefab_tag)
            {
                go.AddTag(GameTags.GasSource);
            }
        }

        // 修改手压泵扫描逻辑，支持气体并移除质量阈值
        [HarmonyPatch(typeof(LiquidPumpingStation), "Sim200ms")]
        public class LiquidPumpingStation_Sim200ms_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                // 查找质量阈值 1f 并替换为 0f (完全移除阈值)
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 1f)
                    {
                        // 检查是否是质量阈值比较
                        if (i + 2 < codes.Count &&
                            (codes[i + 1].opcode == OpCodes.Ble_Un || codes[i + 1].opcode == OpCodes.Ble))
                        {
                            // 替换为 0f，允许检测任何质量的气体/液体
                            codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0f);
                        }
                    }
                }

                return codes;
            }
        }



        // 替换IsLiquid检查为支持气体的检查
        [HarmonyPatch(typeof(LiquidPumpingStation), "Sim200ms")]
        public class LiquidPumpingStation_Sim200ms_Transpiler_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                // 查找IsLiquid检查并替换为气体和液体的检查
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt &&
                        codes[i].operand.ToString().Contains("IsLiquid"))
                    {
                        // 替换为调用我们自己的检查方法
                        codes[i] = new CodeInstruction(OpCodes.Call,
                            AccessTools.Method(typeof(LiquidPumpingStation_Sim200ms_Transpiler_Patch), "IsLiquidOrGas"));
                    }
                }

                return codes;
            }

            // 辅助方法：检查元素是否为液体或气体
            public static bool IsLiquidOrGas(Element element)
            {
                return element.IsLiquid || element.IsGas;
            }
        }



    }
}
