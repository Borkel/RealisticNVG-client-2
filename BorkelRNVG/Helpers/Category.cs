namespace BorkelRNVG.Helpers
{
    public static class Category
    {
        public static readonly string miscCategory = Format(0, "Miscellaneous");
        public static readonly string globalCategory = Format(1, "Global");
        public static readonly string gainControlCategory = Format(2, "Gain control");
        public static readonly string illuminationCategory = Format(3, "Illumination");
        public static readonly string sightDimmingCategory = Format(4, "NVG Sight Dimmer - Vultify");

        public static string Format(int order, string category) => $"{order:00}. {category}";
    }
}
