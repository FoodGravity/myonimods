// 添加缺失的using语句
using HarmonyLib;
using UnityEngine;
using STRINGS;
using System.Collections.Generic;
using System.Linq;
using KMod;

namespace 擦气
{
    public class MopGas : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            // 添加本地化字符串
            Strings.Add("STRINGS.UI.TOOLS.FILTERLAYERS.擦气.NAME", "擦气");
            Strings.Add("STRINGS.UI.TOOLS.FILTERLAYERS.擦气.TOOLTIP", "启用气体收集模式，允许使用拖把工具收集气体");
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
            { "擦气", ToolParameterMenu.ToggleState.On }
        };
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

        // 修改MopTool的OnDragTool方法来支持气体收集
        [HarmonyPatch(typeof(MopTool), "OnDragTool")]
        public class MopTool_OnDragTool_Patch
        {
            public static bool Prefix(MopTool __instance, int cell, int distFromOrigin)
            {
                if (!Grid.IsValidCell(cell))
                {
                    return false;
                }

                // 检查是否启用了气体收集模式
                bool collectGas = mopToolOptions.ContainsKey("擦气") &&
                                 mopToolOptions["擦气"] == ToolParameterMenu.ToggleState.On;

                bool collectLiquid = mopToolOptions.ContainsKey("擦水") &&
                                    mopToolOptions["擦水"] == ToolParameterMenu.ToggleState.On;

                if (DebugHandler.InstantBuildMode)
                {
                    if (Grid.IsValidCell(cell))
                    {
                        if (collectGas && Grid.Element[cell].IsGas)
                        {
                            Moppable.MopCell(cell, 1000000f, null);
                        }
                        else if (collectLiquid && Grid.Element[cell].IsLiquid)
                        {
                            Moppable.MopCell(cell, 1000000f, null);
                        }
                    }
                    return false;
                }

                GameObject gameObject = Grid.Objects[cell, 8];

                // 处理气体和液体收集 - 合并逻辑
                bool shouldCollect = (collectGas && Grid.Element[cell].IsGas) || (collectLiquid && Grid.Element[cell].IsLiquid);
                if (shouldCollect && !Grid.Solid[cell] && gameObject == null)
                {
                    // 对于气体收集，移除地板和质量限制
                    if (collectGas && Grid.Element[cell].IsGas)
                    {
                        gameObject = (Grid.Objects[cell, 8] = Util.KInstantiate(Assets.GetPrefab(new Tag("MopPlacer"))));
                        Vector3 position = Grid.CellToPosCBC(cell, Grid.SceneLayer.FXFront);
                        float num = -0.15f;
                        position.z += num;
                        gameObject.transform.SetPosition(position);
                        gameObject.SetActive(value: true);
                    }
                    else if (collectLiquid && Grid.Element[cell].IsLiquid)
                    {
                        bool flag = Grid.IsValidCell(Grid.CellBelow(cell)) && Grid.Solid[Grid.CellBelow(cell)];
                        bool flag2 = Grid.Mass[cell] <= MopTool.maxMopAmt;
                        if (flag && flag2)
                        {
                            gameObject = (Grid.Objects[cell, 8] = Util.KInstantiate(Assets.GetPrefab(new Tag("MopPlacer"))));
                            Vector3 position = Grid.CellToPosCBC(cell, Grid.SceneLayer.FXFront);
                            float num = -0.15f;
                            position.z += num;
                            gameObject.transform.SetPosition(position);
                            gameObject.SetActive(value: true);
                        }
                        else
                        {
                            string text = UI.TOOLS.MOP.TOO_MUCH_LIQUID;
                            if (!flag)
                            {
                                text = UI.TOOLS.MOP.NOT_ON_FLOOR;
                            }

                            PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Negative, text, null, Grid.CellToPosCBC(cell, Grid.SceneLayer.FXFront));
                        }
                    }
                    return false;
                }

                if (gameObject != null)
                {
                    Prioritizable component = gameObject.GetComponent<Prioritizable>();
                    if (component != null)
                    {
                        component.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
                    }
                }

                return false;
            }
        }


        // 修改 Moppable.MopCell 支持气体
        [HarmonyPatch(typeof(Moppable), "MopCell")]
        public class Moppable_MopCell_Patch
        {
            public static bool Prefix(int cell, float amount, System.Action<Sim.MassConsumedCallback, object> cb)
            {
                if (Grid.Element[cell].IsLiquid || Grid.Element[cell].IsGas)
                {
                    int callbackIdx = -1;
                    if (cb != null)
                    {
                        callbackIdx = Game.Instance.massConsumedCallbackManager.Add(cb, null, "Moppable").index;
                    }

                    SimMessages.ConsumeMass(cell, Grid.Element[cell].id, amount, 1, callbackIdx);
                }
                return false; // 阻止原方法执行
            }
        }
        // 修改 Moppable.IsThereLiquid 支持气体
        [HarmonyPatch(typeof(Moppable), "IsThereLiquid")]
        public class Moppable_IsThereLiquid_Patch
        {
            public static bool Prefix(Moppable __instance, ref bool __result)
            {
                // 使用反射访问私有字段
                var offsetsField = typeof(Moppable).GetField("offsets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                CellOffset[] offsets = (CellOffset[])offsetsField.GetValue(__instance);

                int cell = Grid.PosToCell(__instance.gameObject);
                bool result = false;
                for (int i = 0; i < offsets.Length; i++)
                {
                    int num = Grid.OffsetCell(cell, offsets[i]);
                    if (Grid.Element[num].IsLiquid || Grid.Element[num].IsGas)
                    {
                        result = true;
                    }
                }
                __result = result;
                return false; // 阻止原方法执行
            }
        }
        // 修改 Moppable.OnReachableChanged 方法来使用更大的可达性检查范围
        [HarmonyPatch(typeof(Moppable), "OnReachableChanged")]
        public class Moppable_OnReachableChanged_Patch
        {
            public static bool Prefix(Moppable __instance, object data, ref MeshRenderer ___childRenderer)
            {
                if (!(___childRenderer != null))
                {
                    return false;
                }

                Material material = ___childRenderer.material;
                bool flag = (bool)data;
                if (material.color == Game.Instance.uiColours.Dig.invalidLocation)
                {
                    return false;
                }

                KSelectable component = __instance.GetComponent<KSelectable>();
                if (flag)
                {
                    material.color = Game.Instance.uiColours.Dig.validLocation;
                    component.RemoveStatusItem(Db.Get().BuildingStatusItems.MopUnreachable);
                    return false;
                }

                // 使用扩展的可达性检查范围，参考移动杂物工具的逻辑
                int cell = Grid.PosToCell(__instance.gameObject);
                bool isReachable = false;

                // 检查当前位置和周围8个方向的可达性
                CellOffset[] reachabilityOffsets = new CellOffset[9]
                {
                    new CellOffset(0, 0),    // 当前位置
                    new CellOffset(-1, 0),   // 左
                    new CellOffset(1, 0),    // 右
                    new CellOffset(0, -1),   // 下
                    new CellOffset(0, 1),    // 上
                    new CellOffset(-1, -1),  // 左下
                    new CellOffset(-1, 1),   // 左上
                    new CellOffset(1, -1),   // 右下
                    new CellOffset(1, 1)     // 右上
                };

                for (int i = 0; i < reachabilityOffsets.Length; i++)
                {
                    int checkCell = Grid.OffsetCell(cell, reachabilityOffsets[i]);
                    if (Grid.IsValidCell(checkCell) && MinionGroupProber.Get().IsReachable(checkCell))
                    {
                        isReachable = true;
                        break;
                    }
                }

                if (isReachable)
                {
                    material.color = Game.Instance.uiColours.Dig.validLocation;
                    component.RemoveStatusItem(Db.Get().BuildingStatusItems.MopUnreachable);
                }
                else
                {
                    component.AddStatusItem(Db.Get().BuildingStatusItems.MopUnreachable, __instance);
                    GameScheduler.Instance.Schedule("Locomotion Tutorial", 2f, delegate
                    {
                        Tutorial.Instance.TutorialMessage(Tutorial.TutorialMessages.TM_Locomotion);
                    });
                    material.color = Game.Instance.uiColours.Dig.unreachable;
                }

                return false; // 阻止原方法执行
            }
        }

        // 存储临时物质数据
        private static Dictionary<Moppable, List<StoredMassInfo>> storedMasses = new Dictionary<Moppable, List<StoredMassInfo>>();

        // 动画状态跟踪 - 记录周期信息以处理卡顿跳过的情况
        private static Dictionary<Moppable, AnimProgressTracker> animProgressTrackers = new Dictionary<Moppable, AnimProgressTracker>();

        public class AnimProgressTracker
        {
            public float lastProgress;  // 上一次记录的进度
            public int cycleCount;      // 已完成的周期数
            public bool hasGenerated;   // 当前周期是否已生成
            public float maxProgressInCycle; // 当前周期的最大进度
        }

        public class StoredMassInfo
        {
            public int elementIdx;  // 原始元素索引
            public float mass;
            public float temperature;
            public int diseaseIdx;
            public int diseaseCount;
        }

        // 修改 Moppable.MopTick 支持气体（只在吸气动画期间运行）
        [HarmonyPatch(typeof(Moppable), "MopTick")]
        public class Moppable_MopTick_Patch
        {
            public static bool Prefix(Moppable __instance, float mopAmount)
            {
                // 检查是否启用了气体收集模式和是否在吸气动画期间
                bool collectGas = mopToolOptions.ContainsKey("擦气") &&
                                 mopToolOptions["擦气"] == ToolParameterMenu.ToggleState.On;

                if (!collectGas)
                {
                    return true; // 使用原始方法
                }

                // 调试：检查MopTick是否被调用
                Debug.Log($"MopTick 被调用 - collectGas: {collectGas}");

                KBatchedAnimController animController = null;

                // 检查工人是否在工作及动画状态
                if (__instance.worker != null)
                {
                    // 从工人获取动画控制器
                    animController = __instance.worker.GetComponent<KBatchedAnimController>();
                    // if (animController != null)
                    // {
                    // HashedString currentAnim = animController.currentAnim;
                    // Debug.Log($"工人动画控制器类型: {animController.GetType().Name}");
                    // Debug.Log($"工作动画哈希值: {currentAnim}");

                    // // 使用实际检测到的哈希值 0x64E60196 作为idle_default
                    // var idleDefaultHash = new HashedString(0x64E60196);
                    // Debug.Log($"idle_default哈希值: {idleDefaultHash}");

                    // if (currentAnim != idleDefaultHash)
                    // {
                    //     Debug.Log($"当前不是idle_default动画，跳过生成检查。当前: {currentAnim} 期望: {idleDefaultHash}");
                    //     return false;
                    // }
                    // Debug.Log("当前是idle_default动画，继续处理");
                    // }
                    // else
                    // {
                    //     Debug.Log("工人没有KBatchedAnimController组件");
                    //     return false;
                    // }
                }
                else
                {
                    Debug.Log("没有工人，跳过动画检查");
                    return false;
                }

                // 初始化进度跟踪器
                if (!storedMasses.ContainsKey(__instance))
                {
                    storedMasses[__instance] = new List<StoredMassInfo>();
                }
                if (!animProgressTrackers.ContainsKey(__instance))
                {
                    animProgressTrackers[__instance] = new AnimProgressTracker
                    {
                        lastProgress = 0f,
                        cycleCount = 0,
                        hasGenerated = false,
                        maxProgressInCycle = 0f
                    };
                }

                // 检查动画播放进度 - 改进版：跨周期检查
                if (animController != null)
                {
                    KAnim.Anim currentAnimData = animController.GetCurrentAnim();
                    Debug.Log($"当前动画名称：{currentAnimData.name},当前动画时长: {currentAnimData.totalTime}");

                    if (currentAnimData != null)
                        {
                            // 获取动画播放进度，并处理循环动画（超过1的情况）
                            float normalizedTime = animController.GetPositionPercent() % 1.0f;
                            var tracker = animProgressTrackers[__instance];
                            bool hasMasses = storedMasses.ContainsKey(__instance) && storedMasses[__instance].Count > 0;

                            // 记录当前进度用于周期检测
                            float prevProgress = tracker.lastProgress;
                            tracker.lastProgress = normalizedTime;

                            // 检测周期变化: 如果进度突然从高值跳到低值，表示新周期开始
                            bool isNewCycle = prevProgress > 0.8f && normalizedTime < 0.2f;

                            // 跨周期生成条件检查
                            bool shouldGenerate = false;
                            string generateReason = "";

                            // 情况1: 当前周期50%以上且未生成
                            if (normalizedTime >= 0.5f && !tracker.hasGenerated)
                            {
                                shouldGenerate = true;
                                generateReason = $"当前周期50% {normalizedTime:F3} >= 50%";
                            }
                            // 情况2: 新周期开始且上周期达到过50%但没来得及生成（跳过检测）
                            else if (isNewCycle && prevProgress >= 0.5f && !tracker.hasGenerated)
                            {
                                shouldGenerate = true;
                                generateReason = $"周期跳过检测 - 上周期达到{prevProgress:F3}但未生成";
                            }

                            // 调试输出
                            Debug.Log($"吸气进度: {normalizedTime:F3} 上次: {prevProgress:F3} 新周期: {isNewCycle} 已生成: {tracker.hasGenerated} 存储数量: {storedMasses[__instance].Count}");

                            // 执行生成
                            if (shouldGenerate && hasMasses)
                            {
                                Debug.Log($"🎉 {generateReason}！开始生成存储数量: {storedMasses[__instance].Count}");
                                GenerateStoredMassesGradually(__instance);
                                tracker.hasGenerated = true; // 标记已生成
                                Debug.Log("✅ 生成完成！");
                            }

                            // 新周期开始时重置生成标志
                            if (isNewCycle)
                            {
                                tracker.hasGenerated = false; // 准备下一周期
                                Debug.Log("🔄 新周期开始，重置生成标志以备下一周期使用");
                            }
                        }
                }

                // 使用反射访问私有字段
                var offsetsField = typeof(Moppable).GetField("offsets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                CellOffset[] offsets = (CellOffset[])offsetsField.GetValue(__instance);

                int cell = Grid.PosToCell(__instance.gameObject);
                for (int i = 0; i < offsets.Length; i++)
                {
                    int num = Grid.OffsetCell(cell, offsets[i]);
                    if (Grid.Element[num].IsLiquid || Grid.Element[num].IsGas)
                    {
                        // 调用原始的 MopCell 方法，让它使用我们修改过的版本
                        Moppable.MopCell(num, mopAmount, (Sim.MassConsumedCallback cb_info, object data) =>
                        {
                            __instance.GetType().GetMethod("OnCellMopped",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                .Invoke(__instance, new object[] { cb_info, data });
                        });
                    }
                }
                return false; // 阻止原方法执行
            }
        }



        // 修改 Moppable.OnSpawn 来支持擦气时的呼吸动画和序列映射
        [HarmonyPatch(typeof(Moppable), "OnSpawn")]
        public class Moppable_OnSpawn_Patch
        {
            public static void Postfix(Moppable __instance)
            {
                // 检查当前的拖把工具模式
                if (MopTool.Instance != null && mopToolOptions.ContainsKey("擦气") &&
                    mopToolOptions["擦气"] == ToolParameterMenu.ToggleState.On)
                {
                    // 如果启用了擦气模式，使用呼吸动画
                    __instance.overrideAnims = new KAnimFile[1] { Assets.GetAnim("anim_idle_breatherdeep_kanim") };

                    // 直接修改工作动画序列，使用呼吸动画的序列名称
                    __instance.workAnims = new HashedString[] { "idle_pre", "idle_default" };
                    __instance.workingPstComplete = new HashedString[] { "idle_pst" };
                    __instance.workingPstFailed = new HashedString[] { "idle_pst" };

                    // 初始化存储字典
                    if (!storedMasses.ContainsKey(__instance))
                    {
                        storedMasses[__instance] = new List<StoredMassInfo>();
                    }
                    if (!animProgressTrackers.ContainsKey(__instance))
                    {
                        animProgressTrackers[__instance] = new AnimProgressTracker
                        {
                            lastProgress = 0f,
                            cycleCount = 0,
                            hasGenerated = false,
                            maxProgressInCycle = 0f
                        };
                    }

                    // 监听动画状态变化
                    var animController = __instance.GetComponent<KBatchedAnimController>();
                    if (animController != null)
                    {
                        animController.onAnimComplete += OnAnimComplete;
                        animController.onAnimEnter += OnAnimEnter;
                    }
                }
            }
        }

        // 动画状态进入事件
        private static void OnAnimEnter(HashedString anim)
        {
            // 当进入idle_default动画时，重置生成标志
            if (anim == "idle_default")
            {
                Debug.Log("🔄 进入idle_default动画，重置生成标志");
                var moppables = animProgressTrackers.Keys.ToList();
                foreach (var moppable in moppables)
                {
                    if (animProgressTrackers.ContainsKey(moppable))
                    {
                        animProgressTrackers[moppable].hasGenerated = false; // 重置为可以生成
                        Debug.Log($"🔄 Moppable {moppable.GetInstanceID()} 生成标志已重置");
                    }
                }
            }
        }

        // 动画完成事件
        private static void OnAnimComplete(HashedString anim)
        {
            // 完成idle_default动画时清理存储（可选）
            if (anim == "idle_default")
            {
                Debug.Log("✅ idle_default动画完成");
            }
        }

        // 逐一生成存储的物质（动画50%时调用）
        private static void GenerateStoredMassesGradually(Moppable moppable)
        {
            if (!storedMasses.ContainsKey(moppable)) return;

            var masses = storedMasses[moppable];
            int cell = Grid.PosToCell(moppable.gameObject);

            foreach (var massInfo in masses)
            {
                try
                {
                    Debug.Log($"尝试生成物质: 元素索引={massInfo.elementIdx} 质量={massInfo.mass}");

                    // 空引用检查
                    if (LiquidSourceManager.Instance == null)
                    {
                        Debug.LogError("❌ LiquidSourceManager.Instance为空！");
                        return;
                    }

                    if (ElementLoader.elements == null || massInfo.elementIdx >= ElementLoader.elements.Count)
                    {
                        Debug.LogError($"❌ 元素索引无效: {massInfo.elementIdx}, 元素数组长度: {ElementLoader.elements?.Count ?? -1}");
                        return;
                    }

                    // 使用和original OnCellMopped完全相同的逻辑
                    Element element = ElementLoader.elements[massInfo.elementIdx];
                    if (element != null && massInfo.mass > 0f)
                    {
                        Debug.Log($"找到元素: {element.name} 创建物质块...");

                        SubstanceChunk substanceChunk = LiquidSourceManager.Instance.CreateChunk(
                            element,
                            massInfo.mass,
                            massInfo.temperature,
                            (byte)massInfo.diseaseIdx,
                            massInfo.diseaseCount,
                            Grid.CellToPosCCC(cell, Grid.SceneLayer.Ore)
                        );

                        if (substanceChunk != null)
                        {
                            Debug.Log($"✅ 物质块逐一生成成功！类型: {element.name} 质量: {massInfo.mass}");

                            // 设置物质块为激活状态（允许堆叠）
                            substanceChunk.gameObject.SetActive(true);
                        }
                        else
                        {
                            Debug.LogError("❌ 物质块生成失败！substanceChunk为null");
                        }
                    }
                    else
                    {
                        Debug.LogError($"⚠️ 元素或质量无效: 元素={element?.name ?? "null"} 质量={massInfo.mass}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("生成物质块异常: " + e.Message + " ElementIdx: " + massInfo.elementIdx + " Mass: " + massInfo.mass);
                    Debug.LogError("异常类型: " + e.GetType().Name);
                    Debug.LogError("StackTrace: " + e.StackTrace);
                }
            }

            // 生成完毕后清空存储
            storedMasses[moppable].Clear();
        }

        // 优化方案：存储元素信息，在动画50%时逐一生成
        [HarmonyPatch(typeof(Moppable), "OnCellMopped")]
        public class Moppable_OnCellMopped_Patch
        {
            public static bool Prefix(Moppable __instance, Sim.MassConsumedCallback mass_cb_info)
            {
                // 检查是否启用了气体收集模式
                bool collectGas = mopToolOptions.ContainsKey("擦气") &&
                                 mopToolOptions["擦气"] == ToolParameterMenu.ToggleState.On;

                if (!collectGas)
                {
                    // 如果不是气体收集模式，使用原始方法
                    return true;
                }

                // 对于气体模式，存储完整的元素信息
                if (__instance != null && mass_cb_info.mass > 0f)
                {
                    // 存储元素信息，包含所有原始数据
                    StoredMassInfo storedInfo = new StoredMassInfo
                    {
                        elementIdx = mass_cb_info.elemIdx,      // 原始元素索引
                        mass = mass_cb_info.mass,                // 质量
                        temperature = mass_cb_info.temperature,  // 温度
                        diseaseIdx = mass_cb_info.diseaseIdx,    // 疾病索引
                        diseaseCount = mass_cb_info.diseaseCount // 疾病数量
                    };

                    // 初始化存储列表
                    if (!storedMasses.ContainsKey(__instance))
                    {
                        storedMasses[__instance] = new List<StoredMassInfo>();
                    }

                    // 存储信息
                    storedMasses[__instance].Add(storedInfo);

                    // 更新amountMopped用于显示
                    var amountMoppedField = typeof(Moppable).GetField("amountMopped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    float currentAmount = (float)amountMoppedField.GetValue(__instance);
                    amountMoppedField.SetValue(__instance, currentAmount + mass_cb_info.mass);

                    Debug.Log($"存储气体: 元素索引={storedInfo.elementIdx} 质量={storedInfo.mass} 存储数量={storedMasses[__instance].Count}");
                }

                return false; // 阻止原始方法执行
            }
        }

        // 处理Moppable清理时的存储清理
        [HarmonyPatch(typeof(Moppable), "OnCleanUp")]
        public class Moppable_OnCleanUp_Patch
        {
            public static void Postfix(Moppable __instance)
            {
                // 清理存储的物质数据
                if (storedMasses.ContainsKey(__instance))
                {
                    storedMasses.Remove(__instance);
                }
                if (animProgressTrackers.ContainsKey(__instance))
                {
                    animProgressTrackers.Remove(__instance);
                }
            }
        }
    }
}
