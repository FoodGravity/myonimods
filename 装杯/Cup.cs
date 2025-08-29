using UnityEngine;
using 装杯;
public class Cup : KMonoBehaviour, ISim1000ms
{
    private static readonly EventSystem.IntraObjectHandler<Cup> OnRefreshUserMenuDelegate =
        new EventSystem.IntraObjectHandler<Cup>((component, data) => component.OnRefreshUserMenu(data));

    // private static readonly EventSystem.IntraObjectHandler<Cup> OnUIRefreshDelegate =
    //      new EventSystem.IntraObjectHandler<Cup>((component, data) => component.OnUIRefresh(data));

    private static readonly EventSystem.IntraObjectHandler<Cup> OnDeconstructDelegate =
        new EventSystem.IntraObjectHandler<Cup>((component, data) => component.OnDeconstruct());

    private static readonly EventSystem.IntraObjectHandler<Cup> OnCopySettingsDelegate =
        new EventSystem.IntraObjectHandler<Cup>((component, data) => component.OnCopySettings(data));

    [MyCmpGet]
    public CupOptions options;
    // public static CupOptions nowCup;
    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        // gameObject.Subscribe<Cup>(1980521255, Cup.OnUIRefreshDelegate);
        gameObject.Subscribe<Cup>(493375141, Cup.OnRefreshUserMenuDelegate);
        gameObject.Subscribe<Cup>(-111137758, Cup.OnRefreshUserMenuDelegate);//StatusChange
        gameObject.Subscribe<Cup>(-790448070, Cup.OnDeconstructDelegate);
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);

        // 确保CupOptions组件存在
        if (options == null)
        {
            options = gameObject.AddComponent<CupOptions>();
        }
    }

    // public void OnUIRefresh(object data)
    // {
    //     //没卵用
    // }

    private void OnRefreshUserMenu(object data)
    {

        // nowCup = options;
        if (this.HasTag(GameTags.Stored))
            return;

        // 添加移除按钮
        Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
            "action_deconstruct",
            CupStrings.BUILDINGS.PREFABS.CUP.UI.移除,
            new System.Action(OnDeconstruct),
            tooltipText: CupStrings.BUILDINGS.PREFABS.CUP.UI.移除提示), 0.0f);



        if (options != null)
        {
            string tooltip = options.允许桶罐装 ?
                CupStrings.BUILDINGS.PREFABS.CUP.UI.禁用桶罐装提示 :
                CupStrings.BUILDINGS.PREFABS.CUP.UI.启用桶罐装提示;
            foreach (BuildingDef buildingDef in Assets.BuildingDefs)
            {
                if (buildingDef.BuildingComplete.HasTag(GameTags.LiquidSource) || buildingDef.BuildingComplete.HasTag(GameTags.GasSource))
                {
                    tooltip += "\n" + buildingDef.Name;
                }

            }
            Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
                "action_bottler_delivery",
                options.允许桶罐装 ?
                    CupStrings.BUILDINGS.PREFABS.CUP.UI.禁用桶罐装 :
                    CupStrings.BUILDINGS.PREFABS.CUP.UI.启用桶罐装,
                options.切换允许桶装,
                tooltipText: tooltip), 0.4f);

            Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
                "action_empty_contents",
                CupStrings.BUILDINGS.PREFABS.CUP.UI.切换 + options.需装满文本(!options.需装满),
                options.切换需装满,
                tooltipText: CupStrings.BUILDINGS.PREFABS.CUP.UI.切换 + options.需装满文本(!options.需装满)
                ), 0.4f);
        }
        options?.检查ui();
        // if (options != null)
        // {
        //     options.检查ui();
        //     // if (options.debugtext != null)
        //     // {
        //     //     Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
        //     //          "a",
        //     //          options.debugtext,
        //     //          null,
        //     //          tooltipText: options.debugtext), 0.0f);
        //     // }
        // }
    }



    public void OnDeconstruct()
    {
        options?.storage?.DropAll(options.SelectedOption == 0, options.SelectedOption == 0);
        gameObject.DeleteObject();
    }

    public void OnCopySettings(object data)
    {
        var source = data as GameObject;
        if (source != null)
        {
            var sourceCup = source.GetComponent<Cup>();
            if (sourceCup != null && sourceCup.options != null)
            {
                options.userMaxCapacity = sourceCup.options.userMaxCapacity;
                options.SetSliderValue(options.userMaxCapacity, 0);
                options.autoRemove = sourceCup.options.autoRemove;
                options.SelectedOption = sourceCup.options.SelectedOption;
                options.需装满 = sourceCup.options.需装满;
                options.允许桶罐装 = sourceCup.options.允许桶罐装;
            }
        }
    }

    public void Sim1000ms(float dt)
    {
        if (options == null || options.filteredStorage == null)
            return;

        bool shouldProcess = !options.需装满 || options.filteredStorage.IsFull();

        if (shouldProcess)
        {
            if (options.autoRemove && options.SelectedOption != 2)
            {
                OnDeconstruct();
            }
            else if (options.SelectedOption != 2)
            {
                options.storage?.DropAll(options.SelectedOption == 0, options.SelectedOption == 0);
            }
        }
    }
}