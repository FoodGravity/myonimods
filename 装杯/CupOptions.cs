using KSerialization;
using System.Collections.Generic;
using UnityEngine;
using 装杯;

public enum ElementDisplayState
{
    Empty,
    Solid,
    Gas,
    Liquid,
    Multiple
}

public class CupOptions : KMonoBehaviour, ISingleSliderControl, INToggleSideScreenControl, ICheckboxControl
{

    // public string debugtext;
    [MyCmpGet]
    public Storage storage;

    public FilteredStorage filteredStorage;

    public void OnStorageChanged(object data)
    {
        UpdateMeterColor();
        自动操作();

    }

    private void OnFilterChanged(HashSet<Tag> tags)
    {
        nowtags = tags;
        UpdateMeterColor();
        自动操作();
    }
    private void 自动操作()
    {
        if (filteredStorage == null) return;

        if (storage.IsEmpty()) return;

        bool shouldProcess = !需装满;
        if (需装满)
        {
            // 只有在需要装满模式时才检查IsFull
            shouldProcess = filteredStorage.IsFull();
        }
        if (shouldProcess)
        {
            if (autoRemove && SelectedOption != 2)
            {
                OnDeconstruct();
            }
            else if (SelectedOption != 2)
            {
                storage?.DropAll(SelectedOption == 0, SelectedOption == 0);
            }
        }
    }
    public void OnDeconstruct()
    {
        storage?.DropAll(SelectedOption == 0, SelectedOption == 0);
        GetComponent<Cup>().gameObject.DeleteObject();
    }
    // 材质控制
    private MeterController meter2;

    private void UpdateMeterColor()
    {
        if (meter2 == null) return;
        if (storage == null) return;

        if (nowtags != null && nowtags.Count > 0)
        {
            // 颜色混合逻辑
            Color mixedColor = Color.clear;
            int colornum = 0;
            var noColour = new Color(0f, 0f, 0f, 0f); // 无效颜色标记

            foreach (var tag in nowtags)
            {
                var element = ElementLoader.GetElement(tag);
                if (element != null && element.substance != null)
                {
                    Color elementColor = Color.white; // 默认白色

                    // 优先级1: 获取渲染颜色 (substance.colour)
                    var substanceColour = element.substance.colour;
                    // 如果渲染颜色有效且不透明，使用它
                    if (!substanceColour.Equals(noColour) && substanceColour.a > 0f)
                    {
                        elementColor = substanceColour;
                    }
                    // 否则优先级2: 使用UI颜色 (substance.uiColour)
                    else
                    {
                        var uiColour = element.substance.uiColour;
                        if (!uiColour.Equals(noColour) && uiColour.a > 0f)
                        {
                            elementColor = uiColour;
                        }
                        // 优先级3: 默认为白色 (已在前面设置)
                    }

                    mixedColor += elementColor;
                    colornum++;
                }
            }

            if (colornum > 0)
            {
                // 平均混合颜色
                mixedColor /= colornum;
                meter2.SetSymbolTint("meter_level", mixedColor);
            }
            else
            {
                meter2.SetSymbolTint("meter_level", Color.white);
            }
        }
        else
        {
            meter2.SetSymbolTint("meter_level", Color.white);
        }

        meter2.SetPositionPercent(storage.MassStored() / storage.capacityKg);

        // 更新元素状态符号显示
        ElementDisplayState displayState = DetermineDisplayState(nowtags);
        UpdateDisplaySymbols(displayState);
    }

    private ElementDisplayState DetermineDisplayState(HashSet<Tag> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return ElementDisplayState.Empty;
        }

        bool hasSolid = false, hasGas = false, hasLiquid = false;

        foreach (var tag in tags)
        {
            var element = ElementLoader.GetElement(tag);
            if (element != null)
            {
                if (element.IsSolid) hasSolid = true;
                else if (element.IsGas) hasGas = true;
                else if (element.IsLiquid) hasLiquid = true;
                // 对于无法识别（不是固体、气体、液体）的元素，按照固体处理
                else hasSolid = true;
            }
            else
            {
                // 对于ElementLoader无法获取元素信息的物品（如蛋类、装备等），统一按固体处理
                hasSolid = true;
            }
        }

        // 计算类型数量
        int typeCount = (hasSolid ? 1 : 0) + (hasGas ? 1 : 0) + (hasLiquid ? 1 : 0);

        if (typeCount == 0)
        {
            return ElementDisplayState.Empty;
        }
        else if (typeCount > 1)
        {
            return ElementDisplayState.Multiple;
        }
        else if (hasSolid)
        {
            return ElementDisplayState.Solid;
        }
        else if (hasGas)
        {
            return ElementDisplayState.Gas;
        }
        else if (hasLiquid)
        {
            return ElementDisplayState.Liquid;
        }

        return ElementDisplayState.Empty;
    }

    private void UpdateDisplaySymbols(ElementDisplayState state)
    {
        if (meter2 == null || meter2.meterController == null)
        {
            return;
        }

        // 获取建筑物的动画控制器
        var buildingController = GetComponent<KBatchedAnimController>();
        if (buildingController == null)
        {
            return;
        }

        // 先隐藏所有符号
        buildingController.SetSymbolVisiblity("mat_solid", false);
        buildingController.SetSymbolVisiblity("mat_gas", false);
        buildingController.SetSymbolVisiblity("mat_liquid", false);
        buildingController.SetSymbolVisiblity("mat_multiple", false);

        // 显示对应状态的符号
        switch (state)
        {
            case ElementDisplayState.Solid:
                buildingController.SetSymbolVisiblity("mat_solid", true);
                break;
            case ElementDisplayState.Gas:
                buildingController.SetSymbolVisiblity("mat_gas", true);
                break;
            case ElementDisplayState.Liquid:
                buildingController.SetSymbolVisiblity("mat_liquid", true);
                break;
            case ElementDisplayState.Multiple:
                buildingController.SetSymbolVisiblity("mat_multiple", true);
                break;
        }
    }

    [Serialize] private HashSet<Tag> nowtags;



    // 自动桶装设置
    [Serialize] public bool 允许桶罐装 = true;

    // 状态更新方法
    private void 设置禁止标签()
    {
        if (filteredStorage != null)
        {
            if (允许桶罐装)
            {
                filteredStorage.RemoveForbiddenTag(GameTags.LiquidSource);
                filteredStorage.RemoveForbiddenTag(GameTags.GasSource);
            }
            else
            {
                filteredStorage.AddForbiddenTag(GameTags.LiquidSource);
                filteredStorage.AddForbiddenTag(GameTags.GasSource);

            }
            filteredStorage.FilterChanged();
        }
    }

    // 自动桶装切换方法
    public void 切换允许桶装()
    {
        允许桶罐装 = !允许桶罐装;
        设置禁止标签();
        自动操作();
    }



    // 最大容量
    [Serialize] public float userMaxCapacity = 20000f;


    // ISingleSliderControl 实现，滑条

    private SingleSliderSideScreen 滑条组件;
    public string SliderTitleKey => "装杯滑条组件";
    public string SliderUnits => GameUtil.GetCurrentMassUnit();

    public int SliderDecimalPlaces(int index) => 3;

    public float GetSliderMax(int index) => 20000f;

    public float GetSliderMin(int index) => 0f;

    public float GetSliderValue(int index) => userMaxCapacity;

    public string GetSliderTooltip(int index) => CupStrings.BUILDINGS.PREFABS.CUP.UI.最大容量提示 + ": " + $"{userMaxCapacity:0.###}{SliderUnits}";

    public string GetSliderTooltipKey(int index) => "装杯滑条组件悬浮窗";

    public void SetSliderValue(float value, int index)
    {
        userMaxCapacity = value;
        UpdateSliderText();
        UpdateStorageCapacity();
        filteredStorage.FilterChanged();
        自动操作();
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

    private NToggleSideScreen 切换组件;
    public string SidescreenTitleKey => "装满后自动操作";

    public List<LocString> Options => new List<LocString>
{
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.倒出,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.掉落,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.不管,
};

    public List<LocString> Tooltips => new List<LocString>
{
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.倒出提示,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.掉落提示,
        CupStrings.BUILDINGS.PREFABS.CUP.UI.ACTIONS.TOOLTIPS.不管提示,
};
    public string Description => 需装满文本(需装满) + "-" + CupStrings.BUILDINGS.PREFABS.CUP.UI.自动操作;
    public string 需装满文本(bool 输入需装满)
    {
        return 输入需装满 ? (CupStrings.BUILDINGS.PREFABS.CUP.UI.装满 + "(" + Options[SelectedOption] + ")") :
        (CupStrings.BUILDINGS.PREFABS.CUP.UI.随时 + "(" + Options[SelectedOption] + ")");
    }

    [Serialize]
    public bool 需装满 = true;
    public void 切换需装满()
    {
        需装满 = !需装满;
        切换组件?.SetTarget(gameObject);
        if (!需装满 && autoRemove)
        {
            autoRemove = false;
            自动移除组件.SetTarget(gameObject);

        }
        自动操作();
    }
    [Serialize]
    private bool allowItemRemovalValue = false;

    public bool 允许提取物品
    {
        get { return allowItemRemovalValue; }
        set
        {
            if (value == allowItemRemovalValue) return;
            allowItemRemovalValue = value;
            if (storage != null)
            {
                storage.allowItemRemoval = value;
                storage.RenotifyAll();
            }
        }
    }

    [Serialize]
    public int SelectedOption { get; set; } = 2;

    public int QueuedOption => SelectedOption;

    public void QueueSelectedOption(int option)
    {
        SelectedOption = option;
        Game.Instance.userMenu.Refresh(gameObject);
        自动操作();
    }

    //自动移除勾选框
    private SingleCheckboxSideScreen 自动移除组件;
    [Serialize] public bool autoRemove = true;
    public string CheckboxTitleKey => "装杯自动移除勾选框";

    public string CheckboxLabel => CupStrings.BUILDINGS.PREFABS.CUP.UI.自动移除;
    public string CheckboxTooltip => CheckboxLabel;

    public bool GetCheckboxValue() => autoRemove;

    public void SetCheckboxValue(bool value) { autoRemove = value; 自动操作(); }



    //ui管理
    public void 检查ui()
    {
        // 如果组件已经找到，直接返回
        if (滑条组件 != null && 切换组件 != null && 自动移除组件 != null)

            return;

        if (DetailsScreen.Instance == null)
            return;

        var allSideScreens = DetailsScreen.Instance.GetComponentsInChildren<SideScreenContent>(true);

        foreach (var sideScreen in allSideScreens)
        {
            if (sideScreen == null || !sideScreen.gameObject.activeInHierarchy)
                continue;

            var title = sideScreen.GetTitle();

            // 使用更精确的匹配条件
            if (title.Contains(SidescreenTitleKey) && 切换组件 == null)
            {
                切换组件 = sideScreen.GetComponent<NToggleSideScreen>();
                if (切换组件 != null)
                {
                    刷新切换组件状态();
                }
            }
            else if (title.Contains(SliderTitleKey) && 滑条组件 == null)
            {
                滑条组件 = sideScreen.GetComponent<SingleSliderSideScreen>();
                if (滑条组件 != null)
                {
                    UpdateSliderText();
                }
            }
            else if (title.Contains(CheckboxTitleKey) && 自动移除组件 == null)
            {
                自动移除组件 = sideScreen.GetComponent<SingleCheckboxSideScreen>();
            }


            // 如果两个组件都找到了，就可以停止查找
            if (滑条组件 != null && 切换组件 != null && 自动移除组件 != null)

                break;
        }
    }
    public static bool 需要刷新切换组件 = true;
    private void 刷新切换组件状态()
    {
        if (!需要刷新切换组件) { return; }
        if (切换组件 == null) return;
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
        需要刷新切换组件 = false;
    }





    //以下为默认函数
    private StatusItem 桶装状态项;
    protected override void OnSpawn()
    {
        base.OnSpawn();

        // 初始化状态项
        桶装状态项 = new StatusItem("CupBottleOption", "", "", "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.None.ID);
        桶装状态项.resolveStringCallback = (str, data) =>
            允许桶罐装 ? CupStrings.BUILDINGS.PREFABS.CUP.UI.启用桶罐装 : CupStrings.BUILDINGS.PREFABS.CUP.UI.禁用桶罐装;
        桶装状态项.resolveTooltipCallback = (str, data) =>
            允许桶罐装 ? CupStrings.BUILDINGS.PREFABS.CUP.UI.启用桶罐装提示 : CupStrings.BUILDINGS.PREFABS.CUP.UI.禁用桶罐装提示;

        GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, 桶装状态项, this);


        if (filteredStorage == null)
        {
            filteredStorage = new FilteredStorage(this, new Tag[0], null, false, Db.Get().ChoreTypes.StorageFetch);
        }
        // 禁用FilteredStorage的meter，避免与我们自定义的meter冲突
        filteredStorage.SetHasMeter(false);
        设置禁止标签();


        // 初始化meter控制器 - 设置meter_target在最前面，meter_level在最后面
        // 使用BuildingFront使meter_target在building前面，使用Front使meter_level在最后面
        meter2 = new MeterController(
            GetComponent<KBatchedAnimController>(),
            "meter_target",
            "meter",
            Meter.Offset.Behind,
            Grid.SceneLayer.NoLayer,
            "meter_frame",
            "meter_level"
        );
        // // 额外为meter_level设置更低的渲染层级，使其在最后面
        // if (meter2 != null && meter2.meterController != null)
        // {
        //     meter2.meterController.sceneLayer = Grid.SceneLayer.WorldSelection; // 最后面渲染
        // }
        // 添加对TreeFilterable变化的监听
        var treeFilterable = GetComponent<TreeFilterable>();
        if (treeFilterable != null)
        {
            if (nowtags?.Count > 0) treeFilterable.UpdateFilters(nowtags);

            treeFilterable.OnFilterChanged += OnFilterChanged;
        }


        UpdateStorageCapacity();


        if (storage != null && storage.allowItemRemoval != 允许提取物品)
        {
            storage.allowItemRemoval = 允许提取物品;
            storage.RenotifyAll();
        }
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
