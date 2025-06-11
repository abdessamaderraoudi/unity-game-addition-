using System;
using System.Text;

public static class ArabicFixer
{
    public static string Fix(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remplace les caractères typiques (simplifié ici)
        text = text.Replace("ا", "ﺍ")
                   .Replace("ب", "ﺏ")
                   .Replace("ت", "ﺕ")
                   .Replace("ث", "ﺙ")
                   .Replace("ج", "ﺝ")
                   .Replace("ح", "ﺡ")
                   .Replace("خ", "ﺥ")
                   .Replace("د", "ﺩ")
                   .Replace("ذ", "ﺫ")
                   .Replace("ر", "ﺭ")
                   .Replace("ز", "ﺯ")
                   .Replace("س", "ﺱ")
                   .Replace("ش", "ﺵ")
                   .Replace("ص", "ﺹ")
                   .Replace("ض", "ﺽ")
                   .Replace("ط", "ﻁ")
                   .Replace("ظ", "ﻅ")
                   .Replace("ع", "ﻉ")
                   .Replace("غ", "ﻍ")
                   .Replace("ف", "ﻑ")
                   .Replace("ق", "ﻕ")
                   .Replace("ك", "ﻙ")
                   .Replace("ل", "ﻝ")
                   .Replace("م", "ﻡ")
                   .Replace("ن", "ﻥ")
                   .Replace("ه", "ﻩ")
                   .Replace("و", "ﻭ")
                   .Replace("ي", "ﻱ");

        // Inverser le texte pour l’affichage RTL
        char[] chars = text.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}
