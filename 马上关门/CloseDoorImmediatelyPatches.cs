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
                // 获取当前门状态
                var currentState = (Door.ControlState)Traverse.Create(__instance).Field("controlState").GetValue();
                if (currentState == nextState) return false; // 状态相同则不处理
                
                // 直接设置门的新状态（跳过队列等待）
                Traverse.Create(__instance).Field("requestedState").SetValue(nextState);
                Traverse.Create(__instance).Field("controlState").SetValue(nextState);
                Traverse.Create(__instance).Method("RefreshControlState").GetValue();
                Traverse.Create(__instance).Method("OnOperationalChanged", new object[0]).GetValue();
                
                // 移除状态提示
                __instance.GetComponent<KSelectable>().RemoveStatusItem(Db.Get().BuildingStatusItems.ChangeDoorControlState);
                
                // 立即执行开门或关门
                if (nextState == Door.ControlState.Opened)
                    __instance.Open();
                else
                    __instance.Close();
                
                return false; // 跳过原始方法
            }
        }

        [HarmonyPatch(typeof(BuildingEnabledButton), "OnMenuToggle")]
        public class BuildingEnabledButton_OnMenuToggle_Patch
        {
            private static bool Prefix(BuildingEnabledButton __instance)
            {
                // 使用Traverse访问私有成员
                var traverse = Traverse.Create(__instance);
                traverse.Field("queuedToggle").SetValue(false);
                traverse.Method("OnToggle").GetValue();
                return false;
            }
        }
    }
}