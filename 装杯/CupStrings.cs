namespace 装杯
{
    public static class CupStrings
    {
        public static bool 是中文() => Localization.GetLocale()?.Code == "zh";


        public static class BUILDINGS
        {
            public static class PREFABS
            {
                public static class CUP
                {
                    public static LocString NAME => 是中文() ? "装杯" : "Cup";
                    public static LocString DESC => 是中文() ? "一个可以装东西的杯子" : "A container that can hold things";
                    public static LocString EFFECT => 是中文() ? "能装1吨" : "Can hold 1 ton";
                    public static LocString 无需材料 => 是中文() ? "无需材料" : "No materials needed";

                    public static class UI
                    {
                        public static LocString 移除 => 是中文() ? "移除" : "Remove";
                        public static LocString 移除提示 => 是中文() ? "移除杯子，东西掉出来" : "Remove the cup, things drop out";
                        public static LocString 自动操作 => 是中文() ? "自动操作" : "AutoAction";
                        public static LocString 最大容量提示 => 是中文() ? "设置杯子的最大容量" : "Set the maximum capacity of the cup";
                        public static LocString 装满 => 是中文() ? "装满后" : "When full";
                        public static LocString 随时 => 是中文() ? "随时" : "Any time";
                        public static LocString 自动移除 => 是中文() ? "自动移除(在自动操作后)" : "AutoRemove(After AutoAction)";
                        public static LocString 启用桶罐装 => 是中文() ? "启用桶/罐装" : "Enable bucket/bottling";
                        public static LocString 启用桶罐装提示 => 是中文() ? "若启用，复制人会直接将从以下来源获取桶、罐运送到这个建筑：" : "If enabled, the copier will directly transfer buckets and bottles from the following sources to this building:";
                        public static LocString 禁用桶罐装 => 是中文() ? "禁用桶/罐装" : "Disable bucket/bottling";
                        public static LocString 禁用桶罐装提示 => 是中文() ? "若禁用，复制人不会直接将从以下来源获取桶、罐运送到这个建筑：" : "If disabled, the copier will not directly transfer buckets and bottles from the following sources to this building:";
                        public static LocString 切换 => 是中文() ? "切换到" : "Switch to";
                        public static class ACTIONS
                        {
                            public static LocString 倒出 => 是中文() ? "倒出" : "Pour";
                            public static LocString 掉落 => 是中文() ? "掉落" : "Drop";
                            public static LocString 不管 => 是中文() ? "不管" : "Ignore";


                            public static class TOOLTIPS
                            {
                                public static LocString 倒出提示 => 是中文() ? "装满后自动倒出存储的液体" : "Automatically pour out stored liquid when full";
                                public static LocString 掉落提示 => 是中文() ? "装满后自动掉落存储的液体，可以罐装" : "Automatically drop stored liquid when full, can be bottled";
                                public static LocString 不管提示 => 是中文() ? "装满后不做任何处理" : "Do nothing when full";

                            }
                        }
                    }
                }
            }
        }
    }
}