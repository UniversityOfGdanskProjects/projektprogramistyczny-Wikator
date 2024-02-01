namespace MoviesService.DataAccess.Extensions;

public static class EnumExtensions
{
    public static string ToCamelCaseString(this Enum value)
    {
        var enumString = value.ToString();
        return char.ToLowerInvariant(enumString[0]) + enumString[1..];
    }
}