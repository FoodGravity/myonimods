using KSerialization;
using System.Collections.Generic;
using UnityEngine;
using 装杯;

public class CupOptions : KMonoBehaviour, ISingleSliderControl, INToggleSideScreenControl, ICheckboxControl
{
    [MyCmpGet]
    public Storage storage;

    public FilteredStorage filteredStorage;

    private void OnStorageChanged(object data)
    { UpdateMeterColor(); }

    private void OnFilterChanged(HashSet<Tag> tags)
    {
        nowtags = tags;
        UpdateMeterColor();
    }

    [Serialize] private HashSet<Tag> nowtags;

    // 默认储存变量
    [Serialize] public float userMaxCapacity = 0.03f;//用户设置目标容量

    // ISingleSliderControl 实现，滑条

    public SingleSliderSideScreen 滑条组件;
    public string SliderTitleKey => "装杯滑条组件";
    public string SliderUnits => CupStrings.BUILDINGS.PREFABS.CUP.UI.单位;

    public int SliderDecimalPlaces(int index) => 3;

    public float GetSliderMax(int index) => 1000f;

    public float GetSliderMin(int index) => 0f;

    public float GetSliderValue(int index) => userMaxCapacity;

    public string GetSliderTooltip(int index) => $"{CupStrings.BUILDINGS.PREFABS.CUP.UI.最大容量提示}：{userMaxCapacity:0.###}{SliderUnits}";

    public string GetSliderTooltipKey(int index) => "装杯滑条组件悬浮窗";

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
        if (storage != null)
        { storage.capacityKg = userMaxCapacity; }
        UpdateMeterColor();
    }

    // INToggleSideScreenControl 实现


    public string SidescreenTitleKey => "装满后自动操作";

    public List<LocString> Options => new List<LocString>
{
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.倒出,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.掉落,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.不管,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.需装满
};

    public List<LocString> Tooltips => new List<LocString>
{
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.倒出提示,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.掉落提示,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.不管提示,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.需装满提示
};

    public string Description => 需装满 ? CupStrings.BUILDINGS.PREFABS.CUP.UI.装满后 : CupStrings.BUILDINGS.PREFABS.CUP.UI.随时;

    public bool 需装满 = true;

    [Serialize]
    public int SelectedOption { get; set; } = 2;

    public int QueuedOption => SelectedOption;

    public void QueueSelectedOption(int option)
    {
        if (option == 3)
        {
            需装满 = !需装满;
        }
        else
        {
            SelectedOption = option;
        }
    }

    //自动移除勾选框
    [Serialize] public bool autoRemove = true;

    // public SingleCheckboxSideScreen 自动移除勾选框;
    public string CheckboxTitleKey => "装杯自动移除勾选框";

    public string CheckboxLabel => CupStrings.BUILDINGS.PREFABS.CUP.UI.自动移除;
    public string CheckboxTooltip => CupStrings.BUILDINGS.PREFABS.CUP.UI.自动移除提示;

    public bool GetCheckboxValue() => autoRemove;

    public void SetCheckboxValue(bool value) => autoRemove = value;

    //ui管理

    private void 刷新切换组件(NToggleSideScreen 切换组件)
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

    public void 检查ui()
    {
        if ((滑条组件 == null) && DetailsScreen.Instance != null)
        {
            // 使用反射获取私有字段
            var sideScreensField = typeof(DetailsScreen).GetField("sideScreens",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (sideScreensField != null)
            {
                if (sideScreensField.GetValue(DetailsScreen.Instance) is List<DetailsScreen.SideScreenRef> sideScreens)
                {
                    foreach (var screenRef in sideScreens)
                    {
                        if (screenRef.screenInstance == null) continue;

                        var title = screenRef.screenInstance.GetTitle();
                        if (title.Contains(SidescreenTitleKey))
                        {

                            刷新切换组件(screenRef.screenInstance.GetComponent<NToggleSideScreen>());
                        }
                        else if (title.Contains(SliderTitleKey))
                        {
                            滑条组件 = screenRef.screenInstance.GetComponent<SingleSliderSideScreen>();
                            UpdateSliderText();
                        }
                    }
                }
            }
        }
        // if (切换组件 != null)
        // {
        //     刷新切换组件(切换组件);
        // }
    }

    // 材质控制
    private MeterController meter;

    private void UpdateMeterColor()
    {
        if (meter == null) return;
        if (storage == null) return;

        if (nowtags != null && nowtags.Count > 0)
        {
            // 颜色混合逻辑
            Color mixedColor = Color.clear;
            int colornum = 0;

            foreach (var tag in nowtags)
            {
                var element = ElementLoader.GetElement(tag);
                if (element != null)
                {
                    mixedColor += element.substance.colour;
                    colornum++;
                }
            }

            if (colornum > 0)
            {
                // 平均混合颜色
                mixedColor /= colornum;
                meter.SetSymbolTint("meter_level", mixedColor);
            }
            else
            {
                meter.SetSymbolTint("meter_level", Color.white);
            }
        }
        else
        {
            meter.SetSymbolTint("meter_level", Color.white);
        }

        meter.SetPositionPercent(storage.MassStored() / storage.capacityKg);
    }

    // public string debugtext;

    //以下为默认函数

    protected override void OnSpawn()
    {
        base.OnSpawn();
        if (filteredStorage == null)
        {
            filteredStorage = new FilteredStorage(this, new Tag[0], null, false, Db.Get().ChoreTypes.StorageFetch);
        }
        UpdateStorageCapacity();
        //初始化可以获取到meter、TreeFilterable
        filteredStorage.FilterChanged();

        // 初始化meter控制器
        meter = new MeterController(GetComponent<KBatchedAnimController>(), "meter_target", "meter",
            Meter.Offset.Infront, Grid.SceneLayer.NoLayer, "meter_frame", "meter_level");
        // 添加对TreeFilterable变化的监听
        var treeFilterable = GetComponent<TreeFilterable>();
        if (treeFilterable != null)
        {
            if (nowtags?.Count > 0) treeFilterable.UpdateFilters(nowtags);

            treeFilterable.OnFilterChanged += OnFilterChanged;
        }

        Subscribe((int)GameHashes.OnStorageChange, OnStorageChanged);
        // 初始更新颜色
        UpdateMeterColor();

    }

    protected override void OnCleanUp()
    {
        filteredStorage.CleanUp();

        var treeFilterable = GetComponent<TreeFilterable>();
        if (treeFilterable != null)
        {
            treeFilterable.OnFilterChanged -= OnFilterChanged;
        }

        base.OnCleanUp();
    }
}