namespace BorkelRNVG.Helpers
{
    public static class Category
    {
        public static readonly string miscCategory = Format(0, "Miscellaneous");
        public static readonly string globalCategory = Format(1, "Global");
        public static readonly string gatingCategory = Format(2, "Gating");
        public static readonly string illuminationCategory = Format(3, "Illumination");

        public static string Format(int order, string category) => $"{order:00}. {category}";
    }
}
