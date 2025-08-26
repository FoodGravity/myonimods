using System.Collections;
using UnityEngine;

namespace DominoBricks
{
    public class DominoBrick : KMonoBehaviour
    {
        protected override void OnCleanUp()
        {
            TriggerDominoEffect();
            base.OnCleanUp();
        }

        private void TriggerDominoEffect()
        {
            int cell = Grid.PosToCell(transform.position);
            CheckAndTriggerDirection(cell, new CellOffset(-1, 0)); // 左
            CheckAndTriggerDirection(cell, new CellOffset(1, 0));  // 右
            CheckAndTriggerDirection(cell, new CellOffset(0, 1));   // 上
            CheckAndTriggerDirection(cell, new CellOffset(0, -1));  // 下
        }

        private IEnumerator DelayedDestroy()
        {
            var kAnimController = GetComponent<KBatchedAnimController>();
            if (kAnimController != null)
            {
                kAnimController.Play("destroy", KAnim.PlayMode.Once);
                kAnimController.onAnimComplete += (component) => { DestroySelf(); };
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                DestroySelf();
            }
        }

        private void DestroySelf()
        {
            GetComponent<Deconstructable>().ForceDestroyAndGetMaterials();
        }

        private void CheckAndTriggerDirection(int originCell, CellOffset direction)
        {
            int targetCell = Grid.OffsetCell(originCell, direction);
            GameObject targetBuilding = Grid.Objects[targetCell, (int)ObjectLayer.FoundationTile];

            if (targetBuilding != null && targetBuilding.GetComponent<DominoBrick>() != null)
            {
                targetBuilding.GetComponent<DominoBrick>().StartCoroutine("DelayedDestroy");
            }
        }
    }
}