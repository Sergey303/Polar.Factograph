using System.Text;
using System.Text.RegularExpressions;

namespace Polar.Factograph.Api.Authentication;

public static class LocalLoginName
{
    private static readonly Regex Pattern = new(
        "^[\\p{L}\\p{Nd}][\\p{L}\\p{Nd}._-]{1,61}[\\p{L}\\p{Nd}_-]$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string Canonicalize(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ArgumentException("Введите логин.", nameof(login));
        }

        string canonical = login.Trim().Normalize(NormalizationForm.FormKC);
        if (!Pattern.IsMatch(canonical))
        {
            throw new ArgumentException(
                "Логин должен содержать от 3 до 63 букв, цифр, точек, знаков подчёркивания или дефисов, начинаться с буквы или цифры и не заканчиваться точкой.",
                nameof(login));
        }

        return canonical;
    }

    public static string Normalize(string login) =>
        NormalizeCanonical(Canonicalize(login));

    public static string NormalizeCanonical(string canonicalLogin) =>
        canonicalLogin.ToUpperInvariant();

    public static string ToFogFileName(string canonicalLogin) =>
        Canonicalize(canonicalLogin) + ".fog";
}
