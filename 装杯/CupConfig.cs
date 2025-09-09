using TUNING;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class CupConfig : IBuildingConfig
{
    public const string ID = "Cup";

    public override BuildingDef CreateBuildingDef()
    {
        //string id,
        //int width, int height,
        //string anim,
        //int hitpoints,
        //float construction_time,
        //float[] construction_mass,
        //string[] construction_materials,
        //float melting_point,
        //BuildLocationRule build_location_rule,
        //EffectorValues decor,
        //EffectorValues noise,
        //float temperature_modification_mass_scale = 0.2f)

        BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
            "Cup",// 建筑名称
            1, 1,//宽高
            "cup_kanim",//动画文件
            30,//生命
            3f,//施工时长
            new float[1] { 1f },//建造消耗
            MATERIALS.ANY_BUILDABLE,//材料
            9999f,//融点
            BuildLocationRule.Anywhere,//建造规则
            BUILDINGS.DECOR.PENALTY.TIER0,//装饰度
            NOISE_POLLUTION.NONE//噪音
            );
        buildingDef.Floodable = false;
        buildingDef.AudioCategory = "Metal";
        buildingDef.Overheatable = false;
        buildingDef.Repairable = false;
        buildingDef.Disinfectable = false;
        buildingDef.Invincible = true;

        buildingDef.SceneLayer = Grid.SceneLayer.SceneMAX;
        buildingDef.ObjectLayer = ObjectLayer.AttachableBuilding;

        return buildingDef;
    }

    public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
    {
        SoundEventVolumeCache.instance.AddVolume("cup_kanim", "StorageLocker_Hit_metallic_low", NOISE_POLLUTION.NOISY.TIER1);
        Prioritizable.AddRef(go);
        Storage storage = go.AddOrGet<Storage>();
        storage.showInUI = true;
        //storage.allowItemRemoval = false;
        storage.showDescriptor = true;



        // storage.storageFilters = STORAGEFILTERS.LIQUIDS.Concat(STORAGEFILTERS.GASES).ToList();
        // storage.storageFilters = STORAGEFILTERS.NOT_EDIBLE_SOLIDS
        // .Concat(STORAGEFILTERS.FOOD)
        // .Concat(STORAGEFILTERS.LIQUIDS)
        // .Concat(STORAGEFILTERS.GASES)
        // .ToList();


        // 设置杯子的存储过滤器，包含游戏中几乎所有类型的物品
        storage.storageFilters = STORAGEFILTERS.NOT_EDIBLE_SOLIDS  // 不可食用固体（如金属、矿石、工业产品等）
            .Concat(STORAGEFILTERS.FOOD)                           // 食物和烹饪原料          
            .Concat(STORAGEFILTERS.BAGABLE_CREATURES)              // 可装袋生物
            .Concat(STORAGEFILTERS.SWIMMING_CREATURES)             // 游泳生物
            .Concat(STORAGEFILTERS.LIQUIDS)                        // 液体
            .Concat(STORAGEFILTERS.GASES)                          // 气体
            .Union(new List<Tag> { GameTags.CropSeed })            //作物种子
            // .Concat(STORAGEFILTERS.PAYLOADS)                       // 轨道炮payload（字符串标签）
            // .Concat(new List<Tag> { GameTags.RailGunPayloadEmptyable }) // Interplanetary Payload的实际标签
            .ToList();
        storage.storageFullMargin = 0.0f;
        storage.fetchCategory = Storage.FetchCategory.GeneralStorage;
        storage.showCapacityStatusItem = true;
        storage.showCapacityAsMainStatus = true;
        // 添加必要组件
        go.AddOrGet<Cup>();
        go.AddOrGet<CupOptions>();
        go.AddOrGetDef<RocketUsageRestriction.Def>().restrictOperational = false;

        // 移除不需要的组件
        Object.Destroy(go.AddOrGet<Reconstructable>());
        Object.Destroy(go.AddOrGet<Deconstructable>());
        // 修改侧边栏添加方式
    }

    public override void DoPostConfigureComplete(GameObject go)
    {
        go.AddOrGetDef<StorageController.Def>();
    }
}
