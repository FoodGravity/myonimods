using Delaunay.LR;
using KSerialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CupOptions : KMonoBehaviour, ISingleSliderControl, ICheckboxControl, INToggleSideScreenControl
{
    public FilteredStorage filteredStorage;

    // 默认储存变量

    [Serialize] private float userMaxCapacity = 0.03f;//用户设置目标容量

    public float UserMaxCapacity//用户设置目标容量
    {
        get => userMaxCapacity;
        set
        {
            userMaxCapacity = value;
            UpdateStorageCapacity();
        }
    }

    // ISingleSliderControl 实现，滑条

    public SingleSliderSideScreen 滑条组件;
    public string SliderTitleKey => "最大容量";
    public string SliderUnits => "千克";

    // public string SliderUnits => Options[SelectedOption].text;
    public int SliderDecimalPlaces(int index) => 3;

    public float GetSliderMax(int index) => 1000f;

    public float GetSliderMin(int index) => 0f;

    public float GetSliderValue(int index) => userMaxCapacity;

    public string GetSliderTooltip(int index) => $"最大容量：{userMaxCapacity:0.###}{SliderUnits}";

    public string GetSliderTooltipKey(int index) => "STRINGS.UI.TOOLTIPS.CUP_CAPACITY_TOOLTIP";

    public void SetSliderValue(float value, int index)
    {
        userMaxCapacity = value;
        UpdateSliderText();
        UpdateStorageCapacity();
        filteredStorage.FilterChanged();
    }

    public void UpdateSliderText()
    {
        if (滑条组件 != null && 滑条组件.sliderSets.Count > 0)
        {
            var sliderSet = 滑条组件.sliderSets[0];
            sliderSet.valueSlider.value = userMaxCapacity;
            sliderSet.numberInput.SetDisplayValue(userMaxCapacity.ToString("0.###"));
        }
    }

    private void UpdateStorageCapacity()
    {
        if (GetComponent<Storage>() is Storage storage)
        { storage.capacityKg = userMaxCapacity; }
        UpdateMeterColor();
    }

    // INToggleSideScreenControl 实现
    public NToggleSideScreen 切换组件;

    public string SidescreenTitleKey => "装满后自动操作";

    public List<LocString> Options => new List<LocString>
{
    "倒出",
    "掉落",
    "无视"
};

    public List<LocString> Tooltips => new List<LocString>
{
    "装满后自动倒出存储的液体",
    "装满后自动掉落存储的液体，可以罐装",
    "装满后不做任何处理"
};

    public string Description => "装满后";

    [Serialize]
    public int SelectedOption { get; set; } = 2;
    public int QueuedOption => SelectedOption;

    public void QueueSelectedOption(int option)
    {

        SelectedOption = option;
    }
    //自动移除勾选框
    [Serialize] public bool autoRemove = false;
    public SingleCheckboxSideScreen 自动移除勾选框;
    public string CheckboxTitleKey => "自动移除";
    public string CheckboxLabel => "自动移除";
    public string CheckboxTooltip => "装满自动掉自动移除杯子";

    public bool GetCheckboxValue() => autoRemove;

    public void SetCheckboxValue(bool value) => autoRemove = value;

    // [Serialize] public bool autoPour = false;
    // [Serialize] public bool autoDrop = false;
    // public GameObject dropGO;
    // public SingleCheckboxSideScreen 自动掉落勾选框;
    // public GameObject pourGO;
    // public SingleCheckboxSideScreen 自动倒出勾选框;
    // private class DropCheckboxControl : MonoBehaviour, ICheckboxControl
    // {
    //     private CupOptions parent;

    //     public void Initialize(CupOptions options) => parent = options;

    //     public string CheckboxTitleKey => "自动掉落";
    //     public string CheckboxLabel => "自动掉落";
    //     public string CheckboxTooltip => "装满自动掉落存储的液体，可以罐装";

    //     public bool GetCheckboxValue() => parent.autoDrop;
    //     public void SetCheckboxValue(bool value)
    //     {
    //         if (value)
    //         {
    //             parent.autoPour = false;
    //             parent.自动倒出勾选框.SetTarget(parent.pourGO);
    //         }
    //         parent.autoDrop = value;

    //     }
    // }

    // private class PourCheckboxControl : MonoBehaviour, ICheckboxControl
    // {
    //     private CupOptions parent;

    //     public void Initialize(CupOptions options) => parent = options;

    //     public string CheckboxTitleKey => "自动倒出";
    //     public string CheckboxLabel => "自动倒出";
    //     public string CheckboxTooltip => "自动倒出存储的液体";

    //     public bool GetCheckboxValue() => parent.autoPour;

    //     public void SetCheckboxValue(bool value)
    //     {
    //         if (value)
    //         {
    //             parent.autoDrop = false;
    //             parent.自动掉落勾选框.SetTarget(parent.dropGO);
    //         }
    //         parent.autoPour = value;

    //     }
    // }

    // private void 销毁所有勾选框()
    // {
    //     自动倒出勾选框 = null;
    //     自动掉落勾选框 = null;
    //     var allCheckboxs = FindObjectsOfType<SingleCheckboxSideScreen>();
    //     foreach (var checkbox in allCheckboxs)
    //     {
    //         var title = checkbox.GetTitle();
    //         if (title.Contains("自动掉落") || title.Contains("自动倒出"))
    //         {
    //             checkbox.DeleteObject();
    //         }
    //     }
    // }

    // private void 复制勾选框()
    // {
    //     if (dropGO == null)
    //     {
    //         dropGO = new GameObject("DropCheckbox");
    //         dropGO.AddComponent<DropCheckboxControl>().Initialize(this);
    //     }

    //     if (pourGO == null)
    //     {
    //         pourGO = new GameObject("PourCheckbox");
    //         pourGO.AddComponent<PourCheckboxControl>().Initialize(this);
    //     }
    //     销毁所有勾选框();

    //     // 在原始Checkbox的下一位创建新勾选框
    //     自动掉落勾选框 = GameObject.Instantiate(自动移除勾选框.gameObject, 自动移除勾选框.transform.parent).GetComponent<SingleCheckboxSideScreen>();
    //     自动掉落勾选框.transform.SetSiblingIndex(自动移除勾选框.transform.GetSiblingIndex() + 1);
    //     自动掉落勾选框?.SetTarget(dropGO);

    //     自动倒出勾选框 = GameObject.Instantiate(自动移除勾选框.gameObject, 自动移除勾选框.transform.parent).GetComponent<SingleCheckboxSideScreen>();
    //     自动倒出勾选框.transform.SetSiblingIndex(自动掉落勾选框.transform.GetSiblingIndex() + 1);
    //     自动倒出勾选框?.SetTarget(pourGO);
    // }

    //ui管理
    private static bool 全局只设置一次切换组件 = true;

    public void 检查ui()
    {
        var allSideScreens = FindObjectsOfType<SideScreenContent>();
        // Transform targetParent = null;

        foreach (var screen in allSideScreens)
        {
            var title = screen.GetTitle();
            if (title.Contains(CheckboxTitleKey))
            {
                自动移除勾选框 = screen.GetComponent<SingleCheckboxSideScreen>();
                // targetParent = screen.transform.parent;
            }
            else if (title.Contains(SidescreenTitleKey))
            {
                切换组件 = screen.GetComponent<NToggleSideScreen>();
                if (全局只设置一次切换组件)
                {
                    刷新切换组件();
                    全局只设置一次切换组件 = false;
                }
            }
            else if (title.Contains(SliderTitleKey))
            {
                滑条组件 = screen.GetComponent<SingleSliderSideScreen>();
                UpdateSliderText();
            }
        }
        // if (自动移除勾选框 != null) 复制勾选框();
    }
    private void 刷新切换组件()
    {
        切换组件.SetTarget(gameObject);
        // 强制刷新按钮状态
        var buttons = 切换组件.GetComponentsInChildren<KToggle>();
        foreach (var button in buttons)
        {
            var buttonText = button.GetComponentInChildren<LocText>();
            if (buttonText != null && buttonText.text == Options[SelectedOption])
            {
                button.isOn = true;
                var imageStates = button.GetComponentsInChildren<ImageToggleState>();
                foreach (var state in imageStates)
                {
                    state.ResetColor();
                }
            }
        }
    }

    public string debugtext;

    //以下为默认函数

    protected override void OnSpawn()
    {
        base.OnSpawn();
        // 其余初始化代码保持不变
        filteredStorage = new FilteredStorage(this, new Tag[0], null, false, Db.Get().ChoreTypes.StorageFetch);
        UpdateStorageCapacity();
        filteredStorage.FilterChanged();


        // 初始化meter控制器
        meter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter",
            Meter.Offset.Infront, Grid.SceneLayer.NoLayer, "meter_frame", "meter_level");


        // 添加对TreeFilterable变化的监听
        var treeFilterable = GetComponent<TreeFilterable>();
        if (treeFilterable != null)
        {
            treeFilterable.OnFilterChanged += OnFilterChanged;
        }


        // 订阅存储变化事件
        Subscribe((int)GameHashes.OnStorageChange, OnStorageChanged);

        // 初始更新颜色
        UpdateMeterColor();
        // 添加颜色更新逻辑
    }

    // 添加新方法处理存储变化
    private void OnStorageChanged(object data)
    {
        UpdateMeterColor();
    }
    private void OnFilterChanged(HashSet<Tag> tags)
    {
        UpdateMeterColor();
    }

    private MeterController meter;

    private void UpdateMeterColor()
    {
        if (meter == null) return;

        var storage = GetComponent<Storage>();
        if (storage == null) return;


        var filterable = GetComponent<TreeFilterable>();
        if (filterable != null && filterable.AcceptedTags.Count > 0)
        {
            // 颜色混合逻辑
            Color mixedColor = Color.clear;
            int validColors = 0;

            foreach (var tag in filterable.AcceptedTags)
            {
                var element = ElementLoader.GetElement(tag);
                if (element != null)
                {
                    mixedColor += element.substance.colour;
                    validColors++;

                }
            }

            if (validColors > 0)
            {
                // 平均混合颜色
                mixedColor /= validColors;
                meter.SetSymbolTint("meter_level", mixedColor);
                debugtext += $"混合了{validColors}种颜色";

            }
            else
            {
                meter.SetSymbolTint("meter_level", Color.white);
            }

        }
        else
        {
            meter.SetSymbolTint("meter_level", Color.white);
            debugtext += "滤网颜色：默认白色";
        }

        // if (storage.Count == 0)
        // {
        //     return;
        // }
        // var firstItem = storage[0];
        // var primaryElement = firstItem.GetComponent<PrimaryElement>();

        // if (primaryElement != null)
        // {
        //     var element = ElementLoader.FindElementByHash(primaryElement.ElementID);
        //     if (element != null)
        //     {
        //         // 先设置颜色
        //         meter.SetSymbolTint("meter_level", element.substance.uiColour);

        //     }
        // }
        meter.SetPositionPercent(storage.MassStored() / storage.capacityKg);
    }

    protected override void OnCleanUp()
    {
        filteredStorage.CleanUp();
        // pourGO?.DeleteObject();
        // dropGO?.DeleteObject();
        // 销毁所有勾选框();
        var treeFilterable = GetComponent<TreeFilterable>();
        if (treeFilterable != null)
        {
            treeFilterable.OnFilterChanged -= OnFilterChanged;
        }

        base.OnCleanUp();
    }
}