
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace 直线建造
{
    public static class LineBuildState
    {
        public static int dragStartCell = -1;
        public static int dragDirectionX = 0;
        public static int dragDirectionY = 0;
        public static bool isDragging = false;
        public static int lastBuiltDistance = -1;
        public static HashSet<int> builtCells = new HashSet<int>();
        public static bool directionLocked = false;
    }

    [HarmonyPatch(typeof(DragTool), "OnLeftClickDown")]
    public class DragTool_OnLeftClickDown_Patch
    {
        static void Postfix(DragTool __instance, Vector3 cursor_pos)
        {
            if (__instance is BuildTool)
            {
                int cell = Grid.PosToCell(cursor_pos);
                LineBuildState.dragStartCell = cell;
                LineBuildState.dragDirectionX = 0;
                LineBuildState.dragDirectionY = 0;
                LineBuildState.isDragging = true;
                LineBuildState.lastBuiltDistance = -1;
                LineBuildState.builtCells.Clear();
                LineBuildState.directionLocked = false;

                // 在起始位置建造第一个建筑
                if (Grid.IsValidCell(cell) && Grid.IsVisible(cell))
                {
                    Traverse.Create(__instance).Method("OnDragTool", cell, 0).GetValue();
                    LineBuildState.builtCells.Add(cell);
                }
            }
        }
    }

    [HarmonyPatch(typeof(DragTool), "AddDragPoints")]
    public class DragTool_AddDragPoints_Patch
    {
        static bool Prefix(DragTool __instance, Vector3 cursorPos, Vector3 previousCursorPos)
        {
            if (!(__instance is BuildTool))
            {
                return true;
            }

            if (!LineBuildState.isDragging || LineBuildState.dragStartCell == -1)
            {
                return true;
            }

            int currentCell = Grid.PosToCell(cursorPos);
            int startCell = LineBuildState.dragStartCell;

            // 如果还在同一个单元格内，不处理
            if (currentCell == startCell)
            {
                return false;
            }

            int startX, startY, currentX, currentY;
            Grid.CellToXY(startCell, out startX, out startY);
            Grid.CellToXY(currentCell, out currentX, out currentY);

            int deltaX = currentX - startX;
            int deltaY = currentY - startY;

            // 计算当前移动方向
            int currentDirectionX = 0;
            int currentDirectionY = 0;

            if (Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
            {
                currentDirectionX = (deltaX > 0) ? 1 : -1;
                currentDirectionY = 0;
            }
            else if (Mathf.Abs(deltaY) > Mathf.Abs(deltaX))
            {
                currentDirectionX = 0;
                currentDirectionY = (deltaY > 0) ? 1 : -1;
            }
            else if (Mathf.Abs(deltaX) > 0 || Mathf.Abs(deltaY) > 0)
            {
                if (Mathf.Abs(deltaX) >= Mathf.Abs(deltaY))
                {
                    currentDirectionX = (deltaX > 0) ? 1 : -1;
                    currentDirectionY = 0;
                }
                else
                {
                    currentDirectionX = 0;
                    currentDirectionY = (deltaY > 0) ? 1 : -1;
                }
            }

            // 如果方向还没锁定，确定主要拖动方向
            if (!LineBuildState.directionLocked)
            {
                if (currentDirectionX != 0)
                {
                    LineBuildState.dragDirectionX = currentDirectionX;
                    LineBuildState.dragDirectionY = 0;
                    LineBuildState.directionLocked = true;
                }
                else if (currentDirectionY != 0)
                {
                    LineBuildState.dragDirectionX = 0;
                    LineBuildState.dragDirectionY = currentDirectionY;
                    LineBuildState.directionLocked = true;
                }
            }

            // 根据锁定方向计算目标距离
            int targetDistance = 0;
            if (LineBuildState.dragDirectionX != 0)
            {
                targetDistance = deltaX * LineBuildState.dragDirectionX;
            }
            else if (LineBuildState.dragDirectionY != 0)
            {
                targetDistance = deltaY * LineBuildState.dragDirectionY;
            }

            // 如果距离没变，说明在次轴方向移动，不需要建造
            if (targetDistance == LineBuildState.lastBuiltDistance)
            {
                return false;
            }

            // 建造从起始点到目标距离的所有位置
            int maxDist = Mathf.Abs(targetDistance);
            for (int dist = 1; dist <= maxDist; dist++)
            {
                int actualDist = dist * ((targetDistance >= 0) ? 1 : -1);
                int targetX = startX + LineBuildState.dragDirectionX * actualDist;
                int targetY = startY + LineBuildState.dragDirectionY * actualDist;
                int targetCell = Grid.XYToCell(targetX, targetY);

                if (Grid.IsValidCell(targetCell) && Grid.IsVisible(targetCell) && !LineBuildState.builtCells.Contains(targetCell))
                {
                    Traverse.Create(__instance).Method("OnDragTool", targetCell, Mathf.Abs(actualDist)).GetValue();
                    LineBuildState.builtCells.Add(targetCell);
                }
            }

            LineBuildState.lastBuiltDistance = targetDistance;
            return false;
        }
    }

    [HarmonyPatch(typeof(DragTool), "OnLeftClickUp")]
    public class DragTool_OnLeftClickUp_Patch
    {
        static void Postfix(DragTool __instance)
        {
            if (__instance is BuildTool)
            {
                LineBuildState.dragStartCell = -1;
                LineBuildState.dragDirectionX = 0;
                LineBuildState.dragDirectionY = 0;
                LineBuildState.isDragging = false;
                LineBuildState.lastBuiltDistance = -1;
                LineBuildState.builtCells.Clear();
                LineBuildState.directionLocked = false;
            }
        }
    }
}
