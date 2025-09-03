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



        // 直接修改ResolveString方法，确保菜单显示所有检测到的元素
        [HarmonyPatch(typeof(LiquidPumpingStation), "ResolveString")]
        public class LiquidPumpingStation_ResolveString_Patch
        {
            public static bool Prefix(LiquidPumpingStation __instance, string base_string, ref string __result)
            {
                // 获取私有字段
                var infosField = AccessTools.Field(typeof(LiquidPumpingStation), "infos");
                var infoCountField = AccessTools.Field(typeof(LiquidPumpingStation), "infoCount");

                var infosArray = infosField.GetValue(__instance);
                int infoCount = (int)infoCountField.GetValue(__instance);

                string text = "";
                for (int i = 0; i < infoCount; i++)
                {
                    // 使用反射访问数组元素和字段
                    var liquidInfo = ((System.Array)infosArray).GetValue(i);

                    // 获取amount和element字段
                    var amountField = liquidInfo.GetType().GetField("amount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var elementField = liquidInfo.GetType().GetField("element", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    float amount = (float)amountField.GetValue(liquidInfo);
                    Element element = (Element)elementField.GetValue(liquidInfo);

                    // 显示所有检测到的元素，不管source是否为null
                    if (amount > 0f)  // 只显示有质量的元素
                    {
                        text = text + "\n" + element.name + ": " + GameUtil.GetFormattedMass(amount);
                    }
                }

                __result = base_string.Replace("{Liquids}", text);
                return false; // 跳过原方法
            }
        }

        // 修改手压泵扫描逻辑，支持气体并移除质量阈值
        [HarmonyPatch(typeof(LiquidPumpingStation), "Sim200ms")]
        public class LiquidPumpingStation_Sim200ms_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count; i++)
                {
                    // 替换所有1f为0f
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 1f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0f);
                    }
                    // 替换IsLiquid检查为IsLiquidOrGas
                    else if (codes[i].opcode == OpCodes.Callvirt &&
                             codes[i].operand.ToString().Contains("IsLiquid"))
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call,
                            AccessTools.Method(typeof(LiquidPumpingStation_Sim200ms_Patch), "IsLiquidOrGas"));
                    }
                }

                return codes;
            }

            public static bool IsLiquidOrGas(Element element)
            {
                return element?.IsLiquid == true || element?.IsGas == true;
            }
        }

        // 修改ConsumeMass方法，移除最小消耗限制
        [HarmonyPatch]
        public class WorkSession_ConsumeMass_Patch
        {
            public static System.Reflection.MethodBase TargetMethod()
            {
                // 使用AccessTools获取私有嵌套类的私有方法
                var workSessionType = typeof(LiquidPumpingStation).GetNestedType("WorkSession", System.Reflection.BindingFlags.NonPublic);
                return AccessTools.Method(workSessionType, "ConsumeMass");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                // 查找Mathf.Max(a, 1f)并替换为Mathf.Max(a, 0f)
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 1f)
                    {
                        // 检查是否是Mathf.Max的第二个参数
                        if (i > 0 && i + 1 < codes.Count &&
                            codes[i - 1].opcode == OpCodes.Ldloc_S &&
                            codes[i + 1].opcode == OpCodes.Call &&
                            codes[i + 1].operand.ToString().Contains("Max"))
                        {
                            // 替换为0f，移除最小消耗限制
                            codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 0f);
                        }
                    }
                }

                return codes;
            }
        }



    }
}
