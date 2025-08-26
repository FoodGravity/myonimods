using HarmonyLib;
using KMod;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using static CreaturePoopLoot;
using static STRINGS.DUPLICANTS.ATTRIBUTES;
using static ToolMenu;

namespace ContainmentField
{
    public class ContainmentFieldPatches : UserMod2
    {
        public static Sprite CONTAINMENT_FIELD_ICON_SPRITE;

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            // 尝试从多个位置加载图标
            CONTAINMENT_FIELD_ICON_SPRITE = Assets.GetSprite("icon_tool_containment_field");

            if (CONTAINMENT_FIELD_ICON_SPRITE == null)
            {
                // 尝试从嵌入资源加载
                var imageBytes = Assembly.GetExecutingAssembly().GetManifestResourceStream("ContainmentField.images.image_wirecutter_button.dds");

                if (imageBytes != null)
                {
                    try
                    {
                        byte[] buffer = new byte[imageBytes.Length];
                        imageBytes.Read(buffer, 0, buffer.Length);

                        var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                        texture.LoadRawTextureData(buffer);
                        texture.Apply(true, true);

                        CONTAINMENT_FIELD_ICON_SPRITE = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            32f
                        );
                        CONTAINMENT_FIELD_ICON_SPRITE.name = "icon_tool_containment_field";
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to load embedded icon: {e.Message}");
                        CreateDefaultIcon();
                    }
                }
                else
                {
                    Debug.LogWarning("Embedded icon resource not found, creating default icon");
                    CreateDefaultIcon();
                }
            }
        }

        private void CreateDefaultIcon()
        {
            // 创建纯色默认图标
            var texture = new Texture2D(32, 32);
            Color[] colors = new Color[32 * 32];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.2f, 0.6f, 1f, 0.8f); // 浅蓝色
            }
            texture.SetPixels(colors);
            texture.Apply();

            CONTAINMENT_FIELD_ICON_SPRITE = Sprite.Create(
                texture,
                new Rect(0, 0, 32, 32),
                new Vector2(0.5f, 0.5f),
                32f
            );
            CONTAINMENT_FIELD_ICON_SPRITE.name = "icon_tool_containment_field";
        }

        [HarmonyPatch(typeof(ToolMenu), "CreateBasicTools")]
        public static class ToolMenu_CreateBasicTools_Patch
        {
            public static void Postfix(List<ToolMenu.ToolCollection> ___basicTools)
            {
                // 确保图标已正确加载
                if (ContainmentFieldPatches.CONTAINMENT_FIELD_ICON_SPRITE == null)
                {
                    ContainmentFieldPatches.CONTAINMENT_FIELD_ICON_SPRITE = Assets.GetSprite("icon_wirecutter_button");
                    if (ContainmentFieldPatches.CONTAINMENT_FIELD_ICON_SPRITE == null)
                    {
                        Debug.LogError("Failed to load containment field icon!");
                        return;
                    }
                }

                // 注册图标到资源系统
                string iconName = "icon_tool_containment_field";
                if (Assets.Sprites.ContainsKey((HashedString)iconName))
                    Assets.Sprites.Remove((HashedString)iconName);
                Assets.Sprites.Add((HashedString)iconName, ContainmentFieldPatches.CONTAINMENT_FIELD_ICON_SPRITE);

                // 创建工具集合 - 修改：使用 BuildingUtility1 作为热键
                var toolCollection = new ToolMenu.ToolCollection(
                    "Containment Field",
                    iconName,
                    "Create containment fields",
                    false,
                    Action.BuildingUtility1,
                    false
                );

                // 创建工具信息 - 修改：使用 BuildingUtility1 作为热键
                var toolInfo = new ToolMenu.ToolInfo(
                    "Containment Field",
                    iconName,
                    Action.BuildingUtility1,
                    "ContainmentFieldTool",
                    toolCollection,
                    "Create containment fields",
                    null,
                    null
                );

                if (toolCollection != null)
                {
                    ___basicTools.Add(toolCollection);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerController), "OnPrefabInit")]
    public static class PlayerControllerOnPrefabInit_Patch
    {
        public static void Postfix(PlayerController __instance)
        {
            List<InterfaceTool> interfaceToolList = new List<InterfaceTool>(__instance.tools);
            GameObject gameObject = new GameObject("ContainmentFieldTool");
            gameObject.AddComponent<ContainmentFieldTool>();
            gameObject.transform.SetParent(__instance.gameObject.transform);
            gameObject.gameObject.SetActive(true);
            gameObject.gameObject.SetActive(false);
            interfaceToolList.Add(gameObject.GetComponent<InterfaceTool>());
            __instance.tools = interfaceToolList.ToArray();
        }
    }
}

public class ContainmentFieldTool : DragTool
{
    public static ContainmentFieldTool Instance { get; private set; }
    
    protected override void OnPrefabInit()
    {
        // 初始化基类要求的visualizer组件
        visualizer = Util.KInstantiate(Assets.GetPrefab(SelectTool.Instance.visualizer));
        visualizer.SetActive(false);
        visualizer.transform.SetParent(transform);

        // 初始化自定义可视化组件
        InitializeCustomVisualizer();
        
        base.OnPrefabInit(); // 必须在所有组件初始化后调用基类
        Instance = this;
    }

    private void InitializeCustomVisualizer()
    {
        // 原有初始化逻辑保持不变
        var customVisualizer = new GameObject("ContainmentFieldVisualizer");
        var spriteRenderer = visualizer.AddComponent<SpriteRenderer>();
        spriteRenderer.material = new Material(Shader.Find("Sprites/Default")); // 添加材质
        spriteRenderer.color = new Color(0.5f, 0.5f, 1.0f, 0.3f);
        spriteRenderer.sortingLayerName = "Overlay";
        spriteRenderer.sortingOrder = 100;

        // 确保transform初始化
        visualizer.transform.SetParent(transform);
        visualizer.transform.localPosition = Vector3.zero;
    }

    public override void OnMouseMove(Vector3 cursorPos)
    {
        // 在调用基类前确保基类visualizer就绪
        if (visualizer == null)
        {
            Debug.LogError("Base visualizer not initialized!");
            return;
        }

        try
        {
            base.OnMouseMove(cursorPos); // 现在基类visualizer已初始化
        }
        catch (Exception ex)
        {
            Debug.LogError($"Base OnMouseMove error: {ex.Message}");
        }
    }
}