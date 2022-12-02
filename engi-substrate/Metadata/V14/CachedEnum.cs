using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace Engi.Substrate.Metadata.V14;

public static class CachedEnum<T>
    where T : struct, IConvertible
{
    private static readonly ConcurrentDictionary<T, string> Cache = new();

    public static string GetEnumMemberValue(T value)
    {
        return Cache.GetOrAdd(value, _ =>
        {
            var enumMember = typeof(T)
                .GetTypeInfo()
                .DeclaredMembers
                .Single(x => x.Name == value.ToString())
                .GetCustomAttribute<EnumMemberAttribute>(false);

            return enumMember?.Value ?? value.ToString()!;
        });
    }
}