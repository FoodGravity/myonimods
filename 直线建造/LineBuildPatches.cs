
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using Rendering.World;

namespace 直线建造
{
    /// 直线建造状态管理类
    /// 存储拖拽建造过程中的所有状态信息
    public static class LineBuildState
    {
        /// 拖拽起始单元格的索引，-1表示未开始拖拽
        public static int dragStartCell = -1;
        public static int lastCell = -1;
        /// 拖拽轴向，0表示未确定，1表示X轴，2表示Y轴
        public static int dragAxis = 0;

        /// 是否正在进行直线拖拽建造
        public static bool isDragging = false;

        /// <summary>
        /// 检查当前建筑是否支持直线建造（砖块或梯子）
        /// </summary>
        public static bool IsSupportedBuilding(BuildingDef def)
        {
            if (def == null) return false;
            return def.PrefabID.Contains("Tile") ||
                   def.PrefabID.Contains("Ladder") ||
                   def.PrefabID.Contains("Pole");

        }
    }

    /// 修补DragTool的OnLeftClickDown方法
    /// 在鼠标左键按下时初始化直线建造状态
    [HarmonyPatch(typeof(DragTool), "OnLeftClickDown")]
    public class DragTool_OnLeftClickDown_Patch
    {

        static void Postfix(DragTool __instance, Vector3 cursor_pos)
        {
            // 只对BuildTool且是支持的建筑类型生效
            if (__instance is BuildTool tool && LineBuildState.IsSupportedBuilding(Traverse.Create(tool).Field<BuildingDef>("def").Value))
            {
                // 将鼠标位置转换为网格单元格索引
                int cell = Grid.PosToCell(cursor_pos);

                // 初始化直线建造状态
                LineBuildState.dragStartCell = cell;        // 记录起始单元格
                LineBuildState.dragAxis = 0;                // 重置轴向
                LineBuildState.isDragging = true;           // 标记开始拖拽

                // 在起始位置建造第一个建筑
                if (Grid.IsValidCell(cell) && Grid.IsVisible(cell))
                {
                    // 调用原BuildTool的OnDragTool方法来建造建筑
                    Traverse.Create(__instance).Method("OnDragTool", cell, 0).GetValue();
                    LineBuildState.lastCell = cell;
                }
            }
        }
    }

    /// 修补DragTool的AddDragPoints方法
    /// 替换原有的拖拽点添加逻辑，实现直线建造
    [HarmonyPatch(typeof(DragTool), "AddDragPoints")]
    public class DragTool_AddDragPoints_Patch
    {
        /// 沿着指定轴向建造建筑的辅助方法
        private static int BuildAlongAxis(DragTool instance, int lastCoord, int currentCoord, int fixedCoord, bool isXAxis)
        {
            int delta = currentCoord - lastCoord;
            int steps = Mathf.Abs(delta);
            int direction = delta > 0 ? 1 : -1;
            int targetCoord = lastCoord + steps * direction;

            for (int i = 1; i <= steps; i++)
            {
                int stepCoord = lastCoord + i * direction;
                int stepCell = isXAxis ? Grid.XYToCell(stepCoord, fixedCoord) : Grid.XYToCell(fixedCoord, stepCoord);
                if (Grid.IsValidCell(stepCell) && Grid.IsVisible(stepCell))
                {
                    Traverse.Create(instance).Method("OnDragTool", stepCell, 0).GetValue();
                }
            }

            return isXAxis ? Grid.XYToCell(targetCoord, fixedCoord) : Grid.XYToCell(fixedCoord, targetCoord);
        }

        static bool Prefix(DragTool __instance, Vector3 cursorPos)
        {
            // 只对BuildTool且正在直线拖拽且是支持的建筑类型时生效
            if (!(__instance is BuildTool tool) ||
                !LineBuildState.isDragging ||
                !LineBuildState.IsSupportedBuilding(Traverse.Create(tool).Field<BuildingDef>("def").Value))
                return true;

            // 获取当前单元格
            int currentCell = Grid.PosToCell(cursorPos);

            // 如果还在同一个单元格内，不进行建造
            if (currentCell == LineBuildState.lastCell) return false;

            // 将单元格坐标转换为XY坐标进行计算
            Grid.CellToXY(LineBuildState.dragStartCell, out int startX, out int startY);
            Grid.CellToXY(currentCell, out int currentX, out int currentY);

            // 确定主拖拽轴向（只在轴向未确定时执行）
            if (LineBuildState.dragAxis == 0)
            {
                // 计算从起始点到当前点的偏移量
                int deltaX = currentX - startX;
                int deltaY = currentY - startY;

                // 比较X和Y方向的移动距离，确定主要轴向
                if (Mathf.Abs(deltaX) >= Mathf.Abs(deltaY))
                {
                    LineBuildState.dragAxis = 1; // X轴
                }
                else
                {
                    LineBuildState.dragAxis = 2; // Y轴
                }
            }

            // 获取上次单元格的坐标
            Grid.CellToXY(LineBuildState.lastCell, out int lastX, out int lastY);

            // 根据轴向补齐中间缺失的建筑，并计算目标单元格
            int targetCell;
            if (LineBuildState.dragAxis == 1)
            {
                // 沿着X轴建造
                targetCell = BuildAlongAxis(__instance, lastX, currentX, lastY, true);
            }
            else
            {
                // 沿着Y轴建造
                targetCell = BuildAlongAxis(__instance, lastY, currentY, lastX, false);
            }

            // 更新上次单元格为目标单元格
            LineBuildState.lastCell = targetCell;

            return false; // 返回false阻止原AddDragPoints方法执行
        }
    }

    /// 修补DragTool的OnLeftClickUp方法
    /// 在鼠标左键释放时清理直线建造状态
    [HarmonyPatch(typeof(DragTool), "OnLeftClickUp")]
    public class DragTool_OnLeftClickUp_Patch
    {
        /// Postfix方法：在原方法执行后执行
        /// 清理所有直线建造相关的状态变量，为下次拖拽做准备
        /// <param name="__instance">DragTool实例</param>
        static void Postfix(DragTool __instance)
        {
            // 只对BuildTool进行清理
            if (__instance is BuildTool)
            {
                // 重置所有状态变量到初始状态
                LineBuildState.dragStartCell = -1;         // 重置起始单元格
                LineBuildState.lastCell = -1;             // 重置上次单元格
                LineBuildState.dragAxis = 0;               // 重置轴向
                LineBuildState.isDragging = false;         // 标记停止拖拽
            }
        }
    }
}
