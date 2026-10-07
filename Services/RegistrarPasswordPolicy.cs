namespace Ascendia.Services;

public static class RegistrarPasswordPolicy
{
    public const int MinimumLength = 12;

    public static bool IsStrong(string? password)
    {
        return password is { Length: >= MinimumLength }
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character => !char.IsLetterOrDigit(character));
    }
}
