namespace lab1;

public static class NameHashFunction
{
    private static readonly Dictionary<char, int> CharGroup = BuildCharGroup();

    private static Dictionary<char, int> BuildCharGroup()
    {
        var map = new Dictionary<char, int>();

        void Add(string chars, int group)
        {
            foreach (var c in chars)
            {
                map[char.ToLowerInvariant(c)] = group;
                map[char.ToUpperInvariant(c)] = group;
            }
        }

        Add("аб", 1);
        Add("вгдеє", 2);
        Add("жзиіїй", 3);
        Add("кл", 4);
        Add("мно", 5);
        Add("пр", 6);
        Add("сту", 7);
        Add("фхцч", 8);
        Add("шщюя", 9);
        Add("ьъ", 5);

        Add("ab", 1);
        Add("cdef", 2);
        Add("ghij", 3);
        Add("kl", 4);
        Add("mno", 5);
        Add("pqr", 6);
        Add("stu", 7);
        Add("vwx", 8);
        Add("yz", 9);

        return map;
    }

    public static long Compute(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0L;

        string lower = name.ToLowerInvariant().Trim();
        int len = Math.Min(lower.Length, 99);

        long prefix = 0;
        for (int i = 0; i < 3; i++)
        {
            int g = (i < lower.Length && CharGroup.TryGetValue(lower[i], out int grp)) ? grp : 0;
            prefix = prefix * 10 + g;
        }

        const long Base = 31L;
        const long Mod = 9_999_991L;
        long rolling = 0;
        for (int i = 3; i < lower.Length; i++) rolling = (rolling * Base + lower[i]) % Mod;

        long suffix = len;

        return prefix * 1_000_000_000L + rolling * 100L + suffix;
    }

    public static string Explain(string name)
    {
        string lower = name.ToLowerInvariant().Trim();
        var prefixParts = new List<string>();
        for (int i = 0; i < 3; i++)
        {
            if (i < lower.Length)
            {
                CharGroup.TryGetValue(lower[i], out int g);
                prefixParts.Add($"'{lower[i]}'→{g}");
            }
            else prefixParts.Add("_→0");
        }

        long hash = Compute(name);
        return $"Ім'я: \"{name}\" | Перші 3: [{string.Join(", ", prefixParts)}] | Хеш: {hash}";
    }
}