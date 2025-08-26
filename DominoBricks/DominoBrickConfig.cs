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
                10,//����ֵ
                1f,//ʩ��ʱ��
                new float[] { 1f },//��������
                new string[] { "BuildableRaw&Metal&BuildingWood" },
                1600f,
                BuildLocationRule.Tile,
                BUILDINGS.DECOR.PENALTY.TIER0,
                NOISE_POLLUTION.NONE
            );

            // ������¹ؼ�����
            buildingDef.IsFoundation = true;  // ����Ϊ�ػ�ש��
            buildingDef.TileLayer = ObjectLayer.FoundationTile;
            buildingDef.SceneLayer = Grid.SceneLayer.TileMain;
            buildingDef.ObjectLayer = ObjectLayer.FoundationTile;
            buildingDef.UseStructureTemperature = false;  // ʹ�������¶�

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);

            // ��ӱ�Ҫ�����
            var occupier = go.AddOrGet<SimCellOccupier>();
            occupier.movementSpeedMultiplier = 1.0f;
            occupier.notifyOnMelt = true;
            occupier.doReplaceElement = true;  // �����滻Ԫ��

            go.AddOrGet<TileTemperature>();  // �¶ȴ���
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
            go.AddOrGet<AnimTileable>();  // ȷ��ש�鶯����ȷ
            go.AddOrGet<DominoBrick>();  // ����ŵ��Ϊ

            // ��ӵذ��ǩ
            go.GetComponent<KPrefabID>().AddTag(GameTags.FloorTiles);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            // ȷ��ש����Ϊ��ȷ
            go.AddOrGet<SimCellOccupier>().doReplaceElement = true;
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;

            // ȷ��������Ϊ�ذ�ʹ��
            go.AddOrGet<KPrefabID>().AddTag(GameTags.FloorTiles);
        }
    }
}