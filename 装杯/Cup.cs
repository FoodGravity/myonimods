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
            CupStrings.BUILDINGS.PREFABS.CUP.UI.移除,
            new System.Action(OnDeconstruct),
            tooltipText: CupStrings.BUILDINGS.PREFABS.CUP.UI.移除提示), 0.0f);


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

            // storage?.DropAll(options.autoPour, options.autoPour);
            options.storage?.DropAll(options.SelectedOption == 0, options.SelectedOption == 0);

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
                options.userMaxCapacity = sourceCup.options.userMaxCapacity;
                options.autoRemove = sourceCup.options.autoRemove;

                options.SelectedOption = sourceCup.options.SelectedOption;
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