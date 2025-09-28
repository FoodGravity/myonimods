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

        [HarmonyPatch(typeof(BuildingEnabledButton), "OnMenuToggle")]
        public class BuildingEnabledButton_OnMenuToggle_Patch
        {
            private static bool Prefix(BuildingEnabledButton __instance)
            {
                // 保存当前优先级
                var building = __instance.GetComponent<Building>();
                var operational = building?.GetComponent<Operational>();
                var prioritizable = operational?.GetComponent<Prioritizable>();
                var savedPriority = prioritizable?.GetMasterPriority().priority_value;
                
                // 使用Traverse访问私有成员
                var traverse = Traverse.Create(__instance);
                traverse.Field("queuedToggle").SetValue(false);
                traverse.Method("OnToggle").GetValue();
                
                // 恢复优先级
                if (savedPriority.HasValue && prioritizable != null)
                {
                    prioritizable.SetMasterPriority(new PrioritySetting(
                        PriorityScreen.PriorityClass.basic, 
                        savedPriority.Value));
                }
                
                // 刷新用户菜单
                Game.Instance.userMenu.Refresh(__instance.gameObject);
                
                return false;
            }
        }
    }
}