using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<System.Type, object> services = new();

    public static void Register<T>(T service)
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
        {
            Debug.LogWarning($"Service of type {type} is already registered.");
            return;
        }
        services[type] = service;
        Debug.Log($"Service of type {type} registered successfully.");
    }

    public static void Unregister<T>()
    {
        var type = typeof(T);
        if (services.ContainsKey(type))
            services.Remove(type);
    }

    public static T Get<T>()
    {
        var type = typeof(T);
        if (services.TryGetValue(type, out var service))
            return (T)service;

        Debug.LogError($"Service of type {type} is not registered.");
        return default;
    }

    public static bool Has<T>()
    {
        return services.ContainsKey(typeof(T));
    }
}
