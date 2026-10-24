namespace DungeonLife.Rendering
{
    static class GlyphMap
    {
        public static string ToUnicode(int glyph)
        {
            switch (glyph)
            {
                case 11:
                    return "\u2642";
                case 12:
                    return "\u2640";
                case 148:
                    return "\u00f6";
                case 153:
                    return "\u00d6";
                case 176:
                    return "\u2591";
                case 247:
                    return "\u2248";
                default:
                    return ((char)glyph).ToString();
            }
        }
    }
}
