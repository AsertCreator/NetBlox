using System.Reflection;
using System.Runtime.InteropServices;
using NetBlox.Instances;

namespace NetBlox.Runtime;

public class SecurityIdentity
{
    public required int SecurityIdentityNumber { get; init; }
    public required string Name { get; init; }
    public required SecurityCapability[] AllCapabilities { get; init; }

    public static readonly SecurityIdentity SI_Anonymous = new SecurityIdentity()
    {
        SecurityIdentityNumber = 0,
        Name = "Anonymous",
        AllCapabilities = []  
    };
    public static readonly SecurityIdentity SI_LocalGui = new SecurityIdentity()
    {
        SecurityIdentityNumber = 1,
        Name = "LocalGui",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Plugin },
        ]  
    };
    public static readonly SecurityIdentity SI_GameScript = new SecurityIdentity()
    {
        SecurityIdentityNumber = 2,
        Name = "GameScript",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
        ]  
    };
    public static readonly SecurityIdentity SI_ElevatedGameScript = new SecurityIdentity()
    {
        SecurityIdentityNumber = 3,
        Name = "ElevatedGameScript",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
        ]  
    };
    public static readonly SecurityIdentity SI_CommandBar = new SecurityIdentity()
    {
        SecurityIdentityNumber = 4,
        Name = "CommandBar",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Plugin },
        ]  
    };
    public static readonly SecurityIdentity SI_StudioPlugin = new SecurityIdentity()
    {
        SecurityIdentityNumber = 5,
        Name = "StudioPlugin",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Plugin },
        ]  
    };
    public static readonly SecurityIdentity SI_ElevatedStudioPlugin = new SecurityIdentity()
    {
        SecurityIdentityNumber = 6,
        Name = "ElevatedStudioPlugin",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Plugin },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxScript },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.DestroyServices },
        ]  
    };
    public static readonly SecurityIdentity SI_StarterScript = new SecurityIdentity()
    {
        SecurityIdentityNumber = 7,
        Name = "StarterScript",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Plugin },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxScript },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxEngine },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.DestroyServices },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Inaccessible },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.ElevatedPlugin },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.WritePlayer },
        ]  
    };
    public static readonly SecurityIdentity SI_RemoteServerControl = new SecurityIdentity()
    {
        SecurityIdentityNumber = 8,
        Name = "RemoteServerControl",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Plugin },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxScript },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxEngine },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.DestroyServices },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.Inaccessible },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.ElevatedPlugin },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.WritePlayer },
        ]  
    };
    public static readonly SecurityIdentity SI_EngineNetworker = new SecurityIdentity()
    {
        SecurityIdentityNumber = 109,
        Name = "EngineNetworker",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.WritePlayer },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxEngine },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxScript },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.DestroyServices },
        ]  
    };
    public static readonly SecurityIdentity SI_EngineRenderer = new SecurityIdentity()
    {
        SecurityIdentityNumber = 110,
        Name = "EngineRenderer",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxEngine },
        ]  
    };
    public static readonly SecurityIdentity SI_EngineHeartbeat = new SecurityIdentity()
    {
        SecurityIdentityNumber = 111,
        Name = "EngineHeartbeat",
        AllCapabilities = [
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxScript },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.RobloxEngine },
            new SimpleSecurityCapability() { MyLevel = SimpleSecurityCapabilityLevel.LocalUser },
        ]  
    };
    public static readonly SecurityIdentity[] AllowedSecurityIdentites =
    {
        SI_Anonymous, SI_LocalGui, SI_GameScript, SI_ElevatedGameScript, SI_CommandBar, SI_StudioPlugin, SI_ElevatedStudioPlugin,
        SI_StarterScript, SI_RemoteServerControl, 
        SI_EngineNetworker, SI_EngineRenderer, SI_EngineHeartbeat
    };

    public bool RequireSimpleCapability(SimpleSecurityCapabilityLevel level)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
        {
            if (AllCapabilities[i] is not SimpleSecurityCapability capability)
                continue;
            if (capability.MyLevel == level)
                return true;
        }
        return false;
    }

    public bool IsAbleToSetInstanceProperty(Instance instance, PropertyInfo property)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
            if (AllCapabilities[i].IsAbleToSetInstanceProperty(instance, property))
                return true;
        return false;
    }
    public bool IsAbleToGetInstanceProperty(Instance instance, PropertyInfo property)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
            if (AllCapabilities[i].IsAbleToGetInstanceProperty(instance, property))
                return true;
        return false;
    }
    public bool IsAbleToCallInstanceMethod(Instance instance, MethodInfo method)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
            if (AllCapabilities[i].IsAbleToCallInstanceMethod(instance, method))
                return true;
        return false;
    }
    public bool IsAbleToSetInstanceProperty(Instance instance, string property)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
            if (AllCapabilities[i].IsAbleToSetInstanceProperty(instance, property))
                return true;
        return false;
    }
    public bool IsAbleToGetInstanceProperty(Instance instance, string property)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
            if (AllCapabilities[i].IsAbleToGetInstanceProperty(instance, property))
                return true;
        return false;
    }
    public bool IsAbleToCallInstanceMethod(Instance instance, string methodName)
    {
        for (int i = 0; i < AllCapabilities.Length; i++)
            if (AllCapabilities[i].IsAbleToCallInstanceMethod(instance, methodName))
                return true;
        return false;
    }
}