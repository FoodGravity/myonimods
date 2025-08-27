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

                    public static class UI
                    {
                        public static LocString REMOVE_BUTTON => 是中文() ? "移除" : "Remove";
                        public static LocString REMOVE_TOOLTIP => 是中文() ? "移除杯子，东西掉出来" : "Remove the cup, things drop out";
                        public static LocString MAX_CAPACITY_TOOLTIP => 是中文() ? "设置杯子的最大容量" : "Set the maximum capacity of the cup";
                        public static LocString AUTO_ACTION => 是中文() ? "装满后" : "When full";
                        public static LocString AUTO_REMOVE => 是中文() ? "自动移除" : "Auto remove";
                        public static LocString AUTO_REMOVE_TOOLTIP => 是中文() ? "装满后自动移除杯子" : "Automatically remove the cup when full";
                        public static LocString SLIDER_UNITS => 是中文() ? "千克" : "kg";

                        public static class ACTIONS
                        {
                            public static LocString POUR_OUT => 是中文() ? "倒出" : "Pour out";
                            public static LocString DROP => 是中文() ? "掉落" : "Drop";
                            public static LocString IGNORE => 是中文() ? "不管" : "Ignore";

                            public static class TOOLTIPS
                            {
                                public static LocString POUR_OUT => 是中文() ? "装满后自动倒出存储的液体" : "Automatically pour out stored liquid when full";
                                public static LocString DROP => 是中文() ? "装满后自动掉落存储的液体，可以罐装" : "Automatically drop stored liquid when full, can be bottled";
                                public static LocString IGNORE => 是中文() ? "装满后不做任何处理" : "Do nothing when full";
                            }
                        }
                    }
                }
            }
        }
    }
}