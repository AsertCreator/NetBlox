using System.Diagnostics;
using System.Reflection;
using NetBlox.Instances;

namespace NetBlox.Runtime;

public enum SimpleSecurityCapabilityLevel
{
    Default, Inaccessible, LocalUser, RobloxScript, RobloxEngine, Plugin, ElevatedPlugin, WritePlayer, DestroyServices
}

public abstract class SecurityCapability
{
    public abstract bool IsAbleToSetInstanceProperty(Instance instance, string property);
    public abstract bool IsAbleToGetInstanceProperty(Instance instance, string property);
    public abstract bool IsAbleToCallInstanceMethod(Instance instance, string methodName);
    public abstract bool IsAbleToSetInstanceProperty(Instance instance, PropertyInfo property);
    public abstract bool IsAbleToGetInstanceProperty(Instance instance, PropertyInfo property);
    public abstract bool IsAbleToCallInstanceMethod(Instance instance, MethodInfo methodName);
}
public class SimpleSecurityCapability : SecurityCapability
{
    public required SimpleSecurityCapabilityLevel MyLevel { get; init; }
    private static Dictionary<string, SimpleSecurityCapabilityLevel> RequiredCapabilityCache = [];
    private static Dictionary<string, SimpleSecurityCapabilityLevel> RequiredCapabilityForWriteCache = [];

    private static SimpleSecurityCapabilityLevel ResolveForProperty(Instance instance, string property, bool write)
    {
        Type instanceType = instance.GetType();
        string capabilityCacheKey = instanceType.FullName + "$" + property;
        if (write) 
        {
            if (RequiredCapabilityForWriteCache.TryGetValue(capabilityCacheKey, out SimpleSecurityCapabilityLevel level))
                return level;
        }
        else
        {
            if (RequiredCapabilityCache.TryGetValue(capabilityCacheKey, out SimpleSecurityCapabilityLevel level))
                return level;
        }

        PropertyInfo? propertyInfo = instanceType.GetProperty(property);
        if (propertyInfo == null)
        {
            Trace.TraceError("Property of type " + instanceType + " named " + property + " doesn't exist or is private; security failure");
            return SimpleSecurityCapabilityLevel.Inaccessible;
        }
        
        return ResolveForProperty(instance, propertyInfo, write);
    }
    private static SimpleSecurityCapabilityLevel ResolveForMethod(Instance instance, string method)
    {
        Type instanceType = instance.GetType();
        string capabilityCacheKey = instanceType.FullName + "$" + method;
        if (RequiredCapabilityCache.TryGetValue(capabilityCacheKey, out SimpleSecurityCapabilityLevel level))
            return level;

        MethodInfo[] methodInfos = instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        if (methodInfos == null || methodInfos.Length == 0)
        {
            Trace.TraceError("Method of type " + instanceType + " named " + method + " doesn't exist or is private; security failure");
            return SimpleSecurityCapabilityLevel.Inaccessible;
        }

        MethodInfo? candidate = null;

        for (int i = 0; i < methodInfos.Length; i++)
        {
            if (methodInfos[i].IsGenericMethod)
                continue;
            if (candidate == null)
            {
                candidate = methodInfos[i];
                break;
            }
        }

        if (candidate == null)
        {
            Trace.TraceError("Method of type " + instanceType + " named " + method + " has no callable candidates; security failure");
            return SimpleSecurityCapabilityLevel.Inaccessible;
        }

        return ResolveForMethod(instance, candidate);
    }
    private static SimpleSecurityCapabilityLevel ResolveForProperty(Instance instance, PropertyInfo property, bool write)
    {
        Type instanceType = instance.GetType();
        string capabilityCacheKey = instanceType.FullName + "$" + property;
        if (write) 
        {
            if (RequiredCapabilityForWriteCache.TryGetValue(capabilityCacheKey, out SimpleSecurityCapabilityLevel level))
                return level;
        }
        else
        {
            if (RequiredCapabilityCache.TryGetValue(capabilityCacheKey, out SimpleSecurityCapabilityLevel level))
                return level;
        }

        ScriptCallableAttribute? callableAttribute = property.GetCustomAttribute<ScriptCallableAttribute>();
        if (callableAttribute == null)
        {
            Trace.TraceError("Property of type " + instanceType + " named " + property + " isn't ScriptCallable; security failure");
            return SimpleSecurityCapabilityLevel.Inaccessible;
        }

        RequiredCapabilityCache[capabilityCacheKey] = callableAttribute.RequiredLevel;

        if (write)
        {
            SimpleSecurityCapabilityLevel whatImGettin = callableAttribute.RequiredLevel;
            if (callableAttribute.SetValueLevel != SimpleSecurityCapabilityLevel.Default)
                whatImGettin = callableAttribute.SetValueLevel;
            RequiredCapabilityForWriteCache[capabilityCacheKey] = whatImGettin;
            return whatImGettin;
        }
        else
        {
            return callableAttribute.RequiredLevel;
        }
    }
    private static SimpleSecurityCapabilityLevel ResolveForMethod(Instance instance, MethodInfo method)
    {
        Type instanceType = instance.GetType();
        string capabilityCacheKey = instanceType.FullName + "$" + method;
        if (RequiredCapabilityCache.TryGetValue(capabilityCacheKey, out SimpleSecurityCapabilityLevel level))
            return level;

        ScriptCallableAttribute? callableAttribute = method.GetCustomAttribute<ScriptCallableAttribute>();
        if (callableAttribute == null)
        {
            Trace.TraceError("Method of type " + instanceType + " named " + method + " isn't ScriptCallable; security failure");
            return SimpleSecurityCapabilityLevel.Inaccessible;
        }
        RequiredCapabilityCache[capabilityCacheKey] = callableAttribute.RequiredLevel;
        return callableAttribute.RequiredLevel;
    }

    public override bool IsAbleToCallInstanceMethod(Instance instance, string methodName)
    {
        return ResolveForMethod(instance, methodName) == MyLevel;
    }
    public override bool IsAbleToGetInstanceProperty(Instance instance, string property)
    {
        return ResolveForProperty(instance, property, false) == MyLevel;
    }
    public override bool IsAbleToSetInstanceProperty(Instance instance, string property)
    {
        return ResolveForProperty(instance, property, true) == MyLevel;
    }
    public override bool IsAbleToCallInstanceMethod(Instance instance, MethodInfo methodName)
    {
        return ResolveForMethod(instance, methodName) == MyLevel;
    }
    public override bool IsAbleToGetInstanceProperty(Instance instance, PropertyInfo property)
    {
        return ResolveForProperty(instance, property, false) == MyLevel;
    }
    public override bool IsAbleToSetInstanceProperty(Instance instance, PropertyInfo property)
    {
        return ResolveForProperty(instance, property, true) == MyLevel;
    }
}