using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Tests;

public sealed class LocalLoginNameTests
{
    [Theory]
    [InlineData("Сергей", "Сергей", "СЕРГЕЙ", "Сергей.fog")]
    [InlineData(" пользователь-1 ", "пользователь-1", "ПОЛЬЗОВАТЕЛЬ-1", "пользователь-1.fog")]
    [InlineData("Anna_2", "Anna_2", "ANNA_2", "Anna_2.fog")]
    public void Unicode_login_is_canonicalized_and_kept_in_fog_filename(
        string source,
        string canonical,
        string normalized,
        string fileName)
    {
        Assert.Equal(canonical, LocalLoginName.Canonicalize(source));
        Assert.Equal(normalized, LocalLoginName.Normalize(source));
        Assert.Equal(fileName, LocalLoginName.ToFogFileName(canonical));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("логин.")]
    [InlineData("лог ин")]
    [InlineData("логин/два")]
    public void Unsafe_filename_login_is_rejected(string login)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => LocalLoginName.Canonicalize(login));

        Assert.Contains("Логин должен содержать", exception.Message);
    }

    [Fact]
    public void Login_comparison_is_case_insensitive_after_normalization()
    {
        Assert.Equal(
            LocalLoginName.Normalize("Сергей"),
            LocalLoginName.Normalize("сЕРГЕЙ"));
    }
}
