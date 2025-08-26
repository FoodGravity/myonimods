using UnityEngine;
using System.Collections.Generic;

public class ContainmentFieldComponent : KMonoBehaviour
{
    [MyCmpReq]
    private KBoxCollider2D collider;

    private bool isFrozen = false;
    private List<GameObject> containedObjects = new List<GameObject>();
    private MeshRenderer visualRenderer;
    private Material visualMaterial;
    private Bounds containmentBounds;

    // 状态枚举
    public enum FieldState
    { Building, Frozen, Cleaning }

    private FieldState currentState = FieldState.Building;

    protected override void OnSpawn()
    {
        base.OnSpawn();
        SetupVisuals();
        CalculateBounds();
    }

    private void SetupVisuals()
    {
        // 创建视觉表现
        GameObject visual = new GameObject("ContainmentVisual");
        visual.transform.parent = transform;

        // 添加网格过滤器和渲染器
        MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
        visualRenderer = visual.AddComponent<MeshRenderer>();

        // 创建材质
        visualMaterial = new Material(Shader.Find("Klei/Transparent"));
        visualMaterial.color = new Color(0.5f, 0.5f, 1.0f, 0.3f); // 初始蓝色半透明

        visualRenderer.material = visualMaterial;

        // 创建网格
        UpdateVisualMesh();
    }

    private void CalculateBounds()
    {
        // 计算最小封闭边界
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        foreach (var obj in containedObjects)
        {
            if (obj != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    min = Vector3.Min(min, renderer.bounds.min);
                    max = Vector3.Max(max, renderer.bounds.max);
                }
            }
        }

        containmentBounds = new Bounds();
        containmentBounds.SetMinMax(min, max);

        // 确保最小尺寸
        if (containmentBounds.size.x < 1) containmentBounds.size = new Vector3(1, containmentBounds.size.y, containmentBounds.size.z);
        if (containmentBounds.size.y < 1) containmentBounds.size = new Vector3(containmentBounds.size.x, 1, containmentBounds.size.z);
    }

    private void UpdateVisualMesh()
    {
        // 创建或更新视觉表现
        GameObject visual = new GameObject("ContainmentVisual");
        visual.transform.parent = transform;

        // 添加SpriteRenderer而不是MeshRenderer
        SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();

        // 创建2D纹理
        Texture2D texture = new Texture2D(
            Mathf.CeilToInt(containmentBounds.size.x),
            Mathf.CeilToInt(containmentBounds.size.y)
        );

        // 根据状态设置颜色
        Color32[] colors = new Color32[texture.width * texture.height];
        Color stateColor;
        switch (currentState)
        {
            case FieldState.Building:
                stateColor = new Color(0.5f, 0.5f, 1.0f, 0.3f);
                break;

            case FieldState.Frozen:
                stateColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                break;

            case FieldState.Cleaning:
                stateColor = new Color(0.5f, 1.0f, 0.5f, 0.5f);
                break;

            default:
                stateColor = Color.clear;
                break;
        }

        // 填充纹理
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = stateColor;
        }

        texture.SetPixels32(colors);
        texture.Apply();

        // 创建精灵
        spriteRenderer.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        // 设置渲染层级
        spriteRenderer.sortingLayerName = "Overlay";
        spriteRenderer.sortingOrder = 100;

        // 调整位置和大小
        visual.transform.position = containmentBounds.center;
        visual.transform.localScale = containmentBounds.size;
    }

    public void FreezeArea()
    {
        if (!isFrozen)
        {
            isFrozen = true;
            currentState = FieldState.Frozen;

            // 冻结区域内所有物体
            foreach (var obj in containedObjects)
            {
                if (obj != null)
                {
                    // 暂停物理模拟
                    Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.isKinematic = true;

                    // 暂停动画
                    Animator animator = obj.GetComponent<Animator>();
                    if (animator != null) animator.enabled = false;

                    // 暂停其他组件
                    KMonoBehaviour kmb = obj.GetComponent<KMonoBehaviour>();
                    if (kmb != null) kmb.enabled = false;
                }
            }

            UpdateVisualState();
        }
    }

    public void UnfreezeArea()
    {
        if (isFrozen)
        {
            isFrozen = false;
            currentState = FieldState.Building;

            // 解冻区域内所有物体
            foreach (var obj in containedObjects)
            {
                if (obj != null)
                {
                    // 恢复物理模拟
                    Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.isKinematic = false;

                    // 恢复动画
                    Animator animator = obj.GetComponent<Animator>();
                    if (animator != null) animator.enabled = true;

                    // 恢复其他组件
                    KMonoBehaviour kmb = obj.GetComponent<KMonoBehaviour>();
                    if (kmb != null) kmb.enabled = true;
                }
            }

            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (visualMaterial == null) return;

        // 根据状态更新视觉表现
        switch (currentState)
        {
            case FieldState.Building:
                visualMaterial.color = new Color(0.5f, 0.5f, 1.0f, 0.3f); // 亮蓝色半透明
                break;

            case FieldState.Frozen:
                visualMaterial.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // 灰色半透明
                break;

            case FieldState.Cleaning:
                visualMaterial.color = new Color(0.5f, 1.0f, 0.5f, 0.5f); // 绿色半透明
                break;
        }
    }

    public void AddObjectToContainment(GameObject obj)
    {
        if (!containedObjects.Contains(obj))
        {
            containedObjects.Add(obj);
            CalculateBounds();
            UpdateVisualMesh();
        }
    }

    public void RemoveObjectFromContainment(GameObject obj)
    {
        if (containedObjects.Contains(obj))
        {
            containedObjects.Remove(obj);
            CalculateBounds();
            UpdateVisualMesh();
        }
    }

    public bool IsAirtight()
    {
        // 检查边界是否完全气密
        // 这里需要实现具体的气密性检查逻辑
        return true;
    }
}