using HarmonyLib;
using UnityEngine;
using STRINGS;
using System.Collections.Generic;
using KMod;

namespace 擦气
{
    // 定义目标流体类型枚举（只有气体和液体两种选择）
    public enum TargetFluidType
    {
        Gas,
        Liquid
    }

    // Moppable模式的组件，用于标记MopPlacer应该处理哪种类型的流体
    public class MoppableModeComponent : MonoBehaviour
    {
        public TargetFluidType TargetType { get; set; }

        private void Awake()
        {
            TargetType = TargetFluidType.Liquid; // 默认以液体模式运行
        }
    }

    // 用于在MopCell调用期间传递当前Moppable的模式信息
    public static class CurrentMoppableContext
    {
        public static TargetFluidType CurrentMode { get; set; } = TargetFluidType.Liquid;
    }

    public class MouthGasCarrierMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            // 添加本地化字符串
            Strings.Add("STRINGS.UI.TOOLS.FILTERLAYERS.吸气.NAME", "吸气");
            Strings.Add("STRINGS.UI.TOOLS.FILTERLAYERS.吸气.TOOLTIP", "启用气体收集模式，允许使用拖把工具收集气体");
            Strings.Add("STRINGS.UI.TOOLS.FILTERLAYERS.擦水.NAME", "擦水");
            Strings.Add("STRINGS.UI.TOOLS.FILTERLAYERS.擦水.TOOLTIP", "启用液体收集模式，允许使用拖把工具收集液体");
        }
    }

    public class MouthCarrierPatches
    {
        // 存储MopTool的选项状态
        private static Dictionary<string, ToolParameterMenu.ToggleState> mopToolOptions = new Dictionary<string, ToolParameterMenu.ToggleState>
        {
            { "擦水", ToolParameterMenu.ToggleState.Off },
            { "吸气", ToolParameterMenu.ToggleState.On }
        };

        // 修改MopTool的OnDragTool方法来支持气体收集
        // 液体模式直接跳过补丁让原版处理，气体模式才拦截处理
        [HarmonyPatch(typeof(MopTool), "OnDragTool")]
        public class MopTool_OnDragTool_Patch
        {
            public static bool Prefix(MopTool __instance, int cell, int distFromOrigin)
            {
                // 检查是否启用了气体收集模式
                bool collectGas = mopToolOptions.ContainsKey("吸气") &&
                                 mopToolOptions["吸气"] == ToolParameterMenu.ToggleState.On;

                // 如果没有启用气体模式，直接让原版逻辑处理（包括液体模式）
                if (!collectGas)
                {
                    return true; // 返回true让原版OnDragTool方法执行
                }

                // 启用了气体模式 - 只处理气体情况
                if (!Grid.IsValidCell(cell) || !Grid.Element[cell].IsGas)
                {
                    return false; // 不是气体或无效网格，让其他逻辑处理（如优先级设置）
                }

                // 处理气体收集
                if (DebugHandler.InstantBuildMode)
                {
                    // 建造模式：快速处理气体，不需要经过MopCell的模式检查
                    if (Grid.Element[cell].IsGas)
                    {
                        int callbackIdx = -1;
                        SimMessages.ConsumeMass(cell, Grid.Element[cell].id, 1000000f, 1, callbackIdx);
                    }
                    return false;
                }

                // 正常模式：气体收集不需要地板要求，直接创建处理气体
                GameObject gasMopPlacer = Grid.Objects[cell, 8];
                if (!Grid.Solid[cell] && gasMopPlacer == null)
                {
                    gasMopPlacer = (Grid.Objects[cell, 8] = Util.KInstantiate(Assets.GetPrefab(new Tag("MopPlacer"))));
                    Vector3 position = Grid.CellToPosCBC(cell, Grid.SceneLayer.FXFront);
                    float num = -0.15f;
                    position.z += num;
                    gasMopPlacer.transform.SetPosition(position);
                    gasMopPlacer.SetActive(value: true);

                    // 标记为气体模式
                    var gasMode = gasMopPlacer.AddComponent<MoppableModeComponent>();
                    gasMode.TargetType = TargetFluidType.Gas;

                    return false; // 处理完毕，不让原版方法执行
                }

                // 处理优先级设置
                if (gasMopPlacer != null)
                {
                    Prioritizable component = gasMopPlacer.GetComponent<Prioritizable>();
                    if (component != null)
                    {
                        component.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
                    }
                }

                return false;
            }
        }

        // 修改MopTool的OnActivateTool方法来显示选项菜单
        [HarmonyPatch(typeof(MopTool), "OnActivateTool")]
        public class MopTool_OnActivateTool_Patch
        {
            public static void Postfix(MopTool __instance)
            {
                ToolMenu.Instance.PriorityScreen.Show();
                if (ToolMenu.Instance.toolParameterMenu != null)
                {
                    ToolMenu.Instance.toolParameterMenu.PopulateMenu(mopToolOptions);
                }
            }
        }

        // 修改MopTool的OnDeactivateTool方法来清理选项菜单
        [HarmonyPatch(typeof(MopTool), "OnDeactivateTool")]
        public class MopTool_OnDeactivateTool_Patch
        {
            public static void Postfix()
            {
                ToolMenu.Instance.PriorityScreen.Show(show: false);
                if (ToolMenu.Instance.toolParameterMenu != null)
                {
                    ToolMenu.Instance.toolParameterMenu.ClearMenu();
                }
            }
        }


        // MopTick补丁：气体模式拦截处理，液体模式让原版处理
        [HarmonyPatch(typeof(Moppable), "MopTick")]
        public class Moppable_MopTick_Patch
        {
            public static bool Prefix(Moppable __instance, float mopAmount)
            {
                // 检查是否有MoppableModeComponent来确定应该处理的类型
                var modeComponent = __instance.GetComponent<MoppableModeComponent>();

                // 如果没有MoppableModeComponent（原版液体罐）或者目标是液体模式，让原版MopTick执行
                if (modeComponent == null || modeComponent.TargetType == TargetFluidType.Liquid)
                {
                    return true; // 返回true让原版MopTick方法执行
                }

                // 气体模式：直接消耗气体并生成气罐
                if (modeComponent.TargetType == TargetFluidType.Gas)
                {
                    // 使用反射访问私有字段
                    var offsetsField = typeof(Moppable).GetField("offsets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var amountMoppedField = typeof(Moppable).GetField("amountMopped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    CellOffset[] offsets = (CellOffset[])offsetsField.GetValue(__instance);
                    float currentAmountMopped = (float)amountMoppedField.GetValue(__instance);

                    int cell = Grid.PosToCell(__instance.gameObject);
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        int num = Grid.OffsetCell(cell, offsets[i]);
                        // 气体模式只处理气体元素
                        if (Grid.Element[num].IsGas && Grid.Mass[num] > 0f)
                        {
                            // 计算实际消耗量（不能超过该格子的气体量）
                            float actualAmount = Mathf.Min(mopAmount, Grid.Mass[num]);

                            // 获取气体元素信息
                            Element element = Grid.Element[num];
                            float temperature = Grid.Temperature[num];
                            int diseaseIdx = (Grid.DiseaseIdx[num] != 255) ? Grid.DiseaseIdx[num] : 0;
                            int diseaseCount = Grid.DiseaseCount[num];

                            // 消耗气体
                            SimMessages.ConsumeMass(num, element.id, actualAmount, 1, -1);

                            // 直接生成气罐（绕过MopCell的回调机制）
                            LiquidSourceManager.Instance.CreateChunk(
                                element,
                                actualAmount,
                                temperature,
                                (byte)diseaseIdx,
                                diseaseCount,
                                Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore)
                            );

                            // 更新已消耗总量
                            currentAmountMopped += actualAmount;
                        }
                    }

                    // 更新amountMopped字段
                    amountMoppedField.SetValue(__instance, currentAmountMopped);

                    return false; // 阻止原方法执行，使用我们的处理结果
                }

                // 其他未预期的模式，返回true让原版执行
                return true;
            }
        }

        // IsThereLiquid补丁：液体模式让给原版，气体模式才截断
        [HarmonyPatch(typeof(Moppable), "IsThereLiquid")]
        public class Moppable_IsThereLiquid_Patch
        {
            public static bool Prefix(Moppable __instance, ref bool __result)
            {
                // 检查是否有MoppableModeComponent来确定应该处理的类型
                var modeComponent = __instance.GetComponent<MoppableModeComponent>();
                TargetFluidType targetType = (modeComponent != null) ? modeComponent.TargetType : TargetFluidType.Liquid;

                // 如果是液体模式或者没有MoppableModeComponent（原版），让原版IsThereLiquid执行
                if (modeComponent == null || targetType == TargetFluidType.Liquid)
                {
                    return true; // 返回true让原版IsThereLiquid方法执行
                }

                // 气体模式，我们进行补丁处理
                if (targetType == TargetFluidType.Gas)
                {
                    // 保持原版代码结构，只扩展检测条件
                    int cell = Grid.PosToCell(__instance.gameObject);
                    bool flag = false;

                    // 使用反射获取offsets数组
                    var offsets = __instance.GetType().GetField("offsets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .GetValue(__instance) as CellOffset[];

                    for (int index = 0; index < offsets.Length; ++index)
                    {
                        int i = Grid.OffsetCell(cell, offsets[index]);

                        // 气体模式只检测气体
                        bool isValidElement = Grid.Element[i].IsGas;

                        if (isValidElement)
                        {
                            flag = true;
                            break;
                        }
                    }

                    __result = flag;
                    return false; // 阻止原方法执行，适用我们处理后的结果
                }

                // 其他未预期的模式，返回true让原版执行
                return true;
            }
        }

        // 修改Moppable的OnReachableChanged方法来设置擦气时的两种蓝色图标
        [HarmonyPatch(typeof(Moppable), "OnReachableChanged")]
        public class Moppable_OnReachableChanged_Patch
        {
            public static bool Prefix(Moppable __instance, object data, ref MeshRenderer ___childRenderer)
            {
                // 检查是否为气体模式（通过MoppableModeComponent判断）
                var modeComponent = __instance.GetComponent<MoppableModeComponent>();
                if (modeComponent != null && modeComponent.TargetType == TargetFluidType.Gas)
                {
                    // 气体模式：我们自己处理视觉效果
                    if (!(___childRenderer != null))
                    {
                        return false;
                    }

                    Material material = ___childRenderer.material;
                    bool isReachable = (bool)data;

                    if (material.color == Game.Instance.uiColours.Dig.invalidLocation)
                    {
                        return false;
                    }

                    KSelectable selectComponent = __instance.GetComponent<KSelectable>();

                    if (isReachable)
                    {
                        material.color = new Color(1f/255f, 183f/255f, 255f/255f); // 吸气模式：可达时的亮蓝1,183,255
                        selectComponent.RemoveStatusItem(Db.Get().BuildingStatusItems.MopUnreachable);
                    }
                    else
                    {
                        selectComponent.AddStatusItem(Db.Get().BuildingStatusItems.MopUnreachable, __instance);
                        GameScheduler.Instance.Schedule("Locomotion Tutorial", 2f, delegate
                        {
                            Tutorial.Instance.TutorialMessage(Tutorial.TutorialMessages.TM_Locomotion);
                        });
                        material.color = new Color(54f/255f, 111f/255f, 134f/255f); // 吸气模式：不可达时的暗蓝54, 111, 134
                    }

                    return false; // 阻止原方法执行，因为我们已处理完毕
                }

                // 非气体模式：让原版方法执行
                return true;
            }
        }

    }
}
