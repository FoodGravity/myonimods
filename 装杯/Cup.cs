using UnityEngine;

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
        // nowCup = options;
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
            "移除",
            new System.Action(OnDeconstruct),
            tooltipText: "移除杯子，东西掉出来"), 0.0f);

        Game.Instance.userMenu.AddButton(gameObject, new KIconButtonMenu.ButtonInfo(
             "a",
             Localization.GetLocale()?.Code,
             null,
             tooltipText: Localization.GetLocale()?.Code), 0.0f);

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
        // 使用CupOptions的FilteredStorage来处理物品掉落
        if (options != null)
        {
            var storage = GetComponent<Storage>();
            // storage?.DropAll(options.autoPour, options.autoPour);
            storage?.DropAll(options.SelectedOption == 0, options.SelectedOption == 0);

        }
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
                options.UserMaxCapacity = sourceCup.options.UserMaxCapacity;
                options.autoRemove = sourceCup.options.autoRemove;
                // options.autoDrop = sourceCup.options.autoDrop;
                // options.autoPour = sourceCup.options.autoPour;
                options.SelectedOption = sourceCup.options.SelectedOption;
            }
        }
    }

    public void Sim1000ms(float dt)
    {
        if (options == null || options.filteredStorage == null)
            return;

        if (options.filteredStorage.IsFull())
        {
            if (options.autoRemove)
            {
                OnDeconstruct();
            }
            // else if (options.autoDrop || options.autoPour)
            // {
            //     var storage = GetComponent<Storage>();
            //     storage?.DropAll(options.autoPour, options.autoPour);
            // }
            else if (options.SelectedOption != 2)
            {
                var storage = GetComponent<Storage>();
                storage?.DropAll(options.SelectedOption == 0, options.SelectedOption == 0);
            }
        }
    }
}