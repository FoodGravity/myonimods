using HarmonyLib;
using UnityEngine;
using STRINGS;
using System.Collections.Generic;
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
            { "擦水", ToolParameterMenu.ToggleState.On },
            { "擦气", ToolParameterMenu.ToggleState.Off }
        };

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

        // 修改 Moppable.MopTick 支持气体
        [HarmonyPatch(typeof(Moppable), "MopTick")]
        public class Moppable_MopTick_Patch
        {
            public static bool Prefix(Moppable __instance, float mopAmount)
            {
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
        // // 修改 Moppable.OnReachableChanged 方法来使用更大的可达性检查范围
        // [HarmonyPatch(typeof(Moppable), "OnReachableChanged")]
        // public class Moppable_OnReachableChanged_Patch
        // {
        //     public static bool Prefix(Moppable __instance, object data, ref MeshRenderer ___childRenderer)
        //     {
        //         if (!(___childRenderer != null))
        //         {
        //             return false;
        //         }

        //         Material material = ___childRenderer.material;
        //         bool flag = (bool)data;
        //         if (material.color == Game.Instance.uiColours.Dig.invalidLocation)
        //         {
        //             return false;
        //         }

        //         KSelectable component = __instance.GetComponent<KSelectable>();
        //         if (flag)
        //         {
        //             material.color = Game.Instance.uiColours.Dig.validLocation;
        //             component.RemoveStatusItem(Db.Get().BuildingStatusItems.MopUnreachable);
        //             return false;
        //         }

        //         // 使用扩展的可达性检查范围，参考移动杂物工具的逻辑
        //         int cell = Grid.PosToCell(__instance.gameObject);
        //         bool isReachable = false;

        //         // 检查当前位置和周围8个方向的可达性
        //         CellOffset[] reachabilityOffsets = new CellOffset[9]
        //         {
        //             new CellOffset(0, 0),    // 当前位置
        //             new CellOffset(-1, 0),   // 左
        //             new CellOffset(1, 0),    // 右
        //             new CellOffset(0, -1),   // 下
        //             new CellOffset(0, 1),    // 上
        //             new CellOffset(-1, -1),  // 左下
        //             new CellOffset(-1, 1),   // 左上
        //             new CellOffset(1, -1),   // 右下
        //             new CellOffset(1, 1)     // 右上
        //         };

        //         for (int i = 0; i < reachabilityOffsets.Length; i++)
        //         {
        //             int checkCell = Grid.OffsetCell(cell, reachabilityOffsets[i]);
        //             if (Grid.IsValidCell(checkCell) && MinionGroupProber.Get().IsReachable(checkCell))
        //             {
        //                 isReachable = true;
        //                 break;
        //             }
        //         }

        //         if (isReachable)
        //         {
        //             material.color = Game.Instance.uiColours.Dig.validLocation;
        //             component.RemoveStatusItem(Db.Get().BuildingStatusItems.MopUnreachable);
        //         }
        //         else
        //         {
        //             component.AddStatusItem(Db.Get().BuildingStatusItems.MopUnreachable, __instance);
        //             GameScheduler.Instance.Schedule("Locomotion Tutorial", 2f, delegate
        //             {
        //                 Tutorial.Instance.TutorialMessage(Tutorial.TutorialMessages.TM_Locomotion);
        //             });
        //             material.color = Game.Instance.uiColours.Dig.unreachable;
        //         }

        //         return false; // 阻止原方法执行
        //     }
        // }

        // // 修改 Moppable.OnSpawn 来支持擦气时的呼吸动画
        // [HarmonyPatch(typeof(Moppable), "OnSpawn")]
        // public class Moppable_OnSpawn_Patch
        // {
        //     public static void Postfix(Moppable __instance)
        //     {
        //         // 检查当前的拖把工具模式
        //         if (MopTool.Instance != null && mopToolOptions.ContainsKey("擦气") &&
        //             mopToolOptions["擦气"] == ToolParameterMenu.ToggleState.On)
        //         {
        //             // 如果启用了擦气模式，使用呼吸动画
        //             __instance.overrideAnims = new KAnimFile[1] { Assets.GetAnim("anim_idle_breathdeep_kanim") };
        //         }
        //         else
        //         {
        //             // 否则使用默认的拖把动画（包括搽水模式）
        //             __instance.overrideAnims = new KAnimFile[1] { Assets.GetAnim("anim_mop_dirtywater_kanim") };
        //         }
        //     }
        // }
    }
}
