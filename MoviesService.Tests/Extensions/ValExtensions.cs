namespace MoviesService.Tests.Extensions;

public static class ValExtensions
{
    public static int ToInt(object num)
    {
        return num.As<int>();
    }

    public static bool ToBool(object boolean)
    {
        return boolean.As<bool>();
    }

    public static string ToString(object boolean)
    {
        return boolean.As<string>();
    }
}