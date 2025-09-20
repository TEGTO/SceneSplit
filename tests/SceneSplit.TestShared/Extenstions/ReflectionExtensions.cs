using System.Reflection;

namespace SceneSplit.TestShared.Extenstions;

public static class ReflectionExtensions
{
    public static T? GetFieldValue<T>(this object obj, string name)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var field = obj.GetType().GetField(name, bindingFlags);
        return (T?)field?.GetValue(obj);
    }

    public static T? GetStaticFieldValue<T>(this Type type, string name)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var field = type.GetField(name, bindingFlags);
        return (T?)field?.GetValue(null);
    }
}