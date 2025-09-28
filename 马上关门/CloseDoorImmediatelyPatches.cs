using HarmonyLib;
using UnityEngine;
using Klei.AI;

namespace 马上关门
{
    public class CloseDoorImmediatelyPatches
    {
        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch("Initialize")]
        public class Db_Initialize_Patch
        {
            public static void Postfix()
            {
                Debug.Log("[马上关门] Mod initialized!");
            }
        }

        [HarmonyPatch(typeof(Door), "QueueStateChange")]
        public class Door_QueueStateChange_Patch
        {
            private static bool Prefix(Door __instance, Door.ControlState nextState)
            {
                var currentState = (Door.ControlState)Traverse.Create(__instance).Field("controlState").GetValue();
                if (currentState == nextState)
                    return false;

                Traverse.Create(__instance).Field("requestedState").SetValue(nextState);
                Traverse.Create(__instance).Field("controlState").SetValue(nextState);
                Traverse.Create(__instance).Method("RefreshControlState").GetValue();
                Traverse.Create(__instance).Method("OnOperationalChanged", new object[0]).GetValue();
                
                __instance.GetComponent<KSelectable>().RemoveStatusItem(Db.Get().BuildingStatusItems.ChangeDoorControlState);
                
                if (nextState == Door.ControlState.Opened)
                    __instance.Open();
                else
                    __instance.Close();
                
                return false;
            }
        }

        // [HarmonyPatch(typeof(BuildingEnabledButton), "OnMenuToggle")]
        // public class BuildingEnabledButton_OnMenuToggle_Patch
        // {
        //     private static bool Prefix(BuildingEnabledButton __instance)
        //     {
        //         // 保存当前优先级
        //         var building = __instance.GetComponent<Building>();
        //         var operational = building?.GetComponent<Operational>();
        //         var savedPriority = operational?.GetComponent<Prioritizable>()?.GetMasterPriority().priority_value;
                
        //         // 执行原始切换逻辑
        //         __instance.HandleToggle();
                
        //         // 强制刷新UI状态
        //         var kSelectable = __instance.GetComponent<KSelectable>();
        //         if (kSelectable != null)
        //         {
        //             kSelectable.IsSelected = !kSelectable.IsSelected;
        //             kSelectable.IsSelected = !kSelectable.IsSelected;
        //         }
                
        //         // 恢复优先级
        //         if (savedPriority.HasValue && operational != null)
        //         {
        //             operational.GetComponent<Prioritizable>()?.SetMasterPriority(new PrioritySetting(
        //                 PriorityScreen.PriorityClass.basic, 
        //                 savedPriority.Value));
        //         }
                
        //         return false;
        //     }
        // }
    }
}