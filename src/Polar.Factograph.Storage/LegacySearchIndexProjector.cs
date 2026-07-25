using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Polar.Factograph.Storage;

public sealed record SearchIndexProjection(
    IReadOnlyList<PolarDbNameSearchRow> NameRows,
    IReadOnlyList<PolarDbWordSearchRow> WordRows);

/// <summary>
/// Projects legacy searchable RDF literals into exact keys suitable for Polar.DB.Typed external indexes.
/// </summary>
public sealed class LegacySearchIndexProjector
{
    public const string NamePredicate = "http://fogid.net/o/name";
    public const string AliasPredicate = "http://fogid.net/o/alias";
    public const string DescriptionPredicate = "http://fogid.net/o/description";
    public const string DocumentContentPredicate = "http://fogid.net/o/doc-content";

    private static readonly HashSet<string> NamePredicates = new(
        [NamePredicate, AliasPredicate],
        StringComparer.Ordinal);

    private static readonly HashSet<string> WordPredicates = new(
        [NamePredicate, AliasPredicate, DescriptionPredicate, DocumentContentPredicate],
        StringComparer.Ordinal);

    private static readonly char[] WordDelimiters =
    [
        ' ', '\r', '\n', '\t', ',', '.', ':', '-', '!', '?', '"', '\'', '=', '\\', '|', '/',
        '(', ')', '[', ']', '{', '}', ';', '*', '<', '>'
    ];

    public SearchIndexProjection Project(ProjectedResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        List<PolarDbNameSearchRow> nameRows = new();
        List<PolarDbWordSearchRow> wordRows = new();

        foreach (TripleRow triple in resource.Triples.Where(triple =>
                     triple.ObjectKind == TripleObjectKind.Literal))
        {
            string language = triple.Language ?? string.Empty;

            if (NamePredicates.Contains(triple.Predicate))
            {
                foreach (string searchKey in CreateNameSearchKeys(triple.ObjectValue))
                {
                    nameRows.Add(new PolarDbNameSearchRow(
                        StableGuid(
                            "name-search",
                            triple.TripleId.ToString("N", CultureInfo.InvariantCulture),
                            searchKey),
                        searchKey,
                        triple.Subject,
                        triple.Predicate,
                        triple.ObjectValue,
                        language,
                        triple.SourceCassetteId));
                }
            }

            if (WordPredicates.Contains(triple.Predicate))
            {
                foreach (string word in NormalizeSearchWords(triple.ObjectValue))
                {
                    wordRows.Add(new PolarDbWordSearchRow(
                        StableGuid(
                            "word-search",
                            triple.TripleId.ToString("N", CultureInfo.InvariantCulture),
                            word),
                        word,
                        triple.Subject,
                        triple.Predicate,
                        triple.ObjectValue,
                        language,
                        triple.SourceCassetteId));
                }
            }
        }

        return new SearchIndexProjection(nameRows, wordRows);
    }

    public static string NormalizeNameQuery(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return string.Join(' ', Tokenize(value));
    }

    public static IReadOnlyList<string> NormalizeSearchWords(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Tokenize(value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<string> CreateNameSearchKeys(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string[] words = Tokenize(value);
        if (words.Length == 0)
        {
            return Array.Empty<string>();
        }

        HashSet<string> keys = new(StringComparer.Ordinal);
        string phrase = string.Join(' ', words);
        AddPrefixes(phrase, keys);

        foreach (string word in words)
        {
            AddPrefixes(word, keys);
        }

        return keys.Order(StringComparer.Ordinal).ToArray();
    }

    private static string[] Tokenize(string value) => value
        .Normalize(NormalizationForm.FormKC)
        .ToUpperInvariant()
        .Split(WordDelimiters, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void AddPrefixes(string value, ISet<string> target)
    {
        for (int length = 1; length <= value.Length; length++)
        {
            target.Add(value[..length]);
        }
    }

    private static Guid StableGuid(params string[] parts)
    {
        string canonical = string.Join("\u001F", parts);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return new Guid(hash.AsSpan(0, 16));
    }
}