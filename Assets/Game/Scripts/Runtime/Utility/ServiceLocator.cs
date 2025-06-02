using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<System.Type, object> services = new();    public static void Register<T>(T service)
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            return;
        }
        services[type] = service;
    }

    public static void Unregister<T>()
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
            services.Remove(type);
    }    public static T Get<T>()
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
            return (T)service;

        return default;
    }

    public static bool Has<T>()
    {
        return services.ContainsKey(typeof(T));
    }
}
