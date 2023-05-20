namespace Engi.Substrate;

public static class SubscriptionConventions
{
    public static string GetName(Type type)
    {
        if (type.BaseType?.IsGenericType != true
            || type.BaseType.GetGenericTypeDefinition() != typeof(SubscriptionProcessingBase<>))
        {
            throw new ArgumentException(
                "Only subscription types must be passed.",
                nameof(type));
        }

        return $"SubscriptionProcessor<{type.Name}>";
    }
}
