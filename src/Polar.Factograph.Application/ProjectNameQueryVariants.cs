using System.Text;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed record ProjectNameQueryVariant(string Key, int Rank);

internal static class ProjectNameQueryVariants
{
    private const string EnglishKeyboard = "qwertyuiop[]asdfghjkl;'zxcvbnm,./`";
    private const string RussianKeyboard = "йцукенгшщзхъфывапролджэячсмитьбю.ё";

    private static readonly IReadOnlyDictionary<char, string> CyrillicToLatin =
        new Dictionary<char, string>
        {
            ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
            ['е'] = "e", ['ё'] = "yo", ['ж'] = "zh", ['з'] = "z", ['и'] = "i",
            ['й'] = "y", ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n",
            ['о'] = "o", ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t",
            ['у'] = "u", ['ф'] = "f", ['х'] = "kh", ['ц'] = "ts", ['ч'] = "ch",
            ['ш'] = "sh", ['щ'] = "shch", ['ъ'] = "", ['ы'] = "y", ['ь'] = "",
            ['э'] = "e", ['ю'] = "yu", ['я'] = "ya"
        };

    private static readonly (string Latin, string Cyrillic)[] LatinSequences =
    [
        ("shch", "щ"), ("sch", "щ"),
        ("yur", "юр"),
        ("yo", "ё"), ("jo", "ё"), ("io", "ё"),
        ("zh", "ж"), ("kh", "х"), ("ts", "ц"), ("ch", "ч"), ("sh", "ш"),
        ("yu", "ю"), ("ju", "ю"), ("iu", "ю"),
        ("ya", "я"), ("ja", "я"), ("ia", "я"),
        ("ye", "е"), ("je", "е"),
        ("iy", "ий"), ("ii", "ий"), ("ei", "ей"), ("ey", "ей"),
        ("ay", "ай"), ("oy", "ой"), ("uy", "уй")
    ];

    private static readonly IReadOnlyDictionary<char, char> LatinCharacters =
        new Dictionary<char, char>
        {
            ['a'] = 'а', ['b'] = 'б', ['c'] = 'к', ['d'] = 'д', ['e'] = 'е',
            ['f'] = 'ф', ['g'] = 'г', ['h'] = 'х', ['i'] = 'и', ['j'] = 'й',
            ['k'] = 'к', ['l'] = 'л', ['m'] = 'м', ['n'] = 'н', ['o'] = 'о',
            ['p'] = 'п', ['q'] = 'к', ['r'] = 'р', ['s'] = 'с', ['t'] = 'т',
            ['u'] = 'у', ['v'] = 'в', ['w'] = 'в', ['x'] = 'кс', ['y'] = 'ы',
            ['z'] = 'з'
        };

    public static IReadOnlyList<ProjectNameQueryVariant> Create(string query)
    {
        ArgumentNullException.ThrowIfNull(query);
        Dictionary<string, int> variants = new(StringComparer.Ordinal);
        Add(variants, query, 300);

        string keyboard = SwapKeyboardLayout(query);
        if (!string.Equals(keyboard, query, StringComparison.Ordinal))
        {
            Add(variants, keyboard, 220);
        }

        if (query.Any(IsCyrillic))
        {
            Add(variants, TransliterateCyrillic(query), 160);
        }

        if (query.Any(IsLatin))
        {
            Add(variants, TransliterateLatin(query), 160);
        }

        return variants
            .Select(pair => new ProjectNameQueryVariant(pair.Key, pair.Value))
            .OrderByDescending(variant => variant.Rank)
            .ThenBy(variant => variant.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private static void Add(IDictionary<string, int> variants, string value, int rank)
    {
        string key = LegacySearchIndexProjector.NormalizeNameQuery(value);
        if (key.Length == 0)
        {
            return;
        }

        if (!variants.TryGetValue(key, out int existing) || rank > existing)
        {
            variants[key] = rank;
        }
    }

    private static string SwapKeyboardLayout(string value)
    {
        StringBuilder result = new(value.Length);
        foreach (char source in value)
        {
            bool upper = char.IsUpper(source);
            char lower = char.ToLowerInvariant(source);
            int englishIndex = EnglishKeyboard.IndexOf(lower, StringComparison.Ordinal);
            int russianIndex = RussianKeyboard.IndexOf(lower, StringComparison.Ordinal);
            char mapped = englishIndex >= 0
                ? RussianKeyboard[englishIndex]
                : russianIndex >= 0
                    ? EnglishKeyboard[russianIndex]
                    : source;
            result.Append(upper ? char.ToUpperInvariant(mapped) : mapped);
        }
        return result.ToString();
    }

    private static string TransliterateCyrillic(string value)
    {
        StringBuilder result = new(value.Length * 2);
        foreach (char source in value.Normalize(NormalizationForm.FormKC))
        {
            char lower = char.ToLowerInvariant(source);
            if (CyrillicToLatin.TryGetValue(lower, out string? mapped))
            {
                result.Append(mapped);
            }
            else
            {
                result.Append(source);
            }
        }
        return result.ToString();
    }

    private static string TransliterateLatin(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        StringBuilder result = new(normalized.Length);
        for (int index = 0; index < normalized.Length;)
        {
            bool matched = false;
            foreach ((string latin, string cyrillic) in LatinSequences)
            {
                if (normalized.AsSpan(index).StartsWith(latin, StringComparison.Ordinal))
                {
                    result.Append(cyrillic);
                    index += latin.Length;
                    matched = true;
                    break;
                }
            }

            if (matched)
            {
                continue;
            }

            char source = normalized[index];
            if (LatinCharacters.TryGetValue(source, out char mapped))
            {
                result.Append(mapped);
            }
            else
            {
                result.Append(source);
            }
            index++;
        }
        return result.ToString();
    }

    private static bool IsCyrillic(char value) =>
        value is >= '\u0400' and <= '\u04ff';

    private static bool IsLatin(char value) =>
        value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
