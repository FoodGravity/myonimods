using UnityEngine;
using STRINGS;
using TUNING;
using System.Collections.Generic;

namespace ContainmentField
{
    public class ContainmentFieldConfig : IBuildingConfig
    {
        public const string ID = "ContainmentField";

        public override BuildingDef CreateBuildingDef()
        {
            float[] materialAmounts = new float[1] { 100f };

            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(
                id: ID,
                width: 1,
                height: 1,
                anim: "containment_field_kanim",  // 需要确保这个动画文件存在
                hitpoints: 1000,//生命
                construction_time: 1f,//施工时长
                construction_mass: new float[] { 1f },
                construction_materials: new string[] { "BuildableRaw&Metal&BuildingWood" },
                melting_point: 1600f,
                build_location_rule: BuildLocationRule.Anywhere,
                decor: DECOR.BONUS.TIER0,
                noise: NOISE_POLLUTION.NONE
            );

            buildingDef.AudioCategory = "Metal";
            buildingDef.AudioSize = "small";
            buildingDef.Overheatable = true;
            buildingDef.RequiresPowerInput = true;
            buildingDef.EnergyConsumptionWhenActive = 120f;
            buildingDef.ExhaustKilowattsWhenActive = 1f;
            buildingDef.SelfHeatKilowattsWhenActive = 1f;
            buildingDef.ViewMode = OverlayModes.Power.ID;
            buildingDef.SceneLayer = Grid.SceneLayer.Building;

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<BuildingComplete>().isManuallyOperated = false;
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
        }
    }
}