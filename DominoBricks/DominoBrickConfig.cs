using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace DominoBricks
{
    public class DominoBrickConfig : IBuildingConfig
    {
        public const string ID = "DominoBrick";

        public override BuildingDef CreateBuildingDef()
        {
            var buildingDef = BuildingTemplates.CreateBuildingDef(
                ID,
                1, 1,
                "domino_brick_kanim",
                10,//生命值
                1f,//施工时长
                new float[] { 1f },//建造消耗
                new string[] { "BuildableRaw&Metal&BuildingWood" },
                1600f,
                BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE
            );

            // 添加以下关键设置
            buildingDef.IsFoundation = true;  // 设置为地基砖块
            buildingDef.TileLayer = ObjectLayer.FoundationTile;
            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.ObjectLayer = ObjectLayer.FoundationTile;
            buildingDef.UseStructureTemperature = false;  // 使用自身温度

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);

            // 添加必要的组件
            var occupier = go.AddOrGet<SimCellOccupier>();
            occupier.movementSpeedMultiplier = 1.0f;
            occupier.notifyOnMelt = true;
            occupier.doReplaceElement = true;  // 允许替换元素

            go.AddOrGet<TileTemperature>();  // 温度传递
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
            go.AddOrGet<AnimTileable>();  // 确保砖块动画正确
            go.AddOrGet<DominoBrick>();  // 多米诺行为

            // 添加地板标签
            go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // 确保砖块行为正确
            go.AddOrGet<SimCellOccupier>().doReplaceElement = true;
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;

            // 确保可以作为地板使用
            go.AddOrGet<KPrefabID>().AddTag(GameTags.FloorTiles);
        }
    }
}