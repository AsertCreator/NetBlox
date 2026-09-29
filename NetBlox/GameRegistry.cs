using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using NetBlox.Instances;
using NetBlox.Runtime;

namespace NetBlox;

public sealed class GameRegistry(GameManager gameManager)
{
    public const ulong SERVICE_ID_BOUNDARY = 1000;

    private Dictionary<ulong, Instance> _trueInstances = [];
    private Dictionary<string, ConstructorInfo> _constructorCache = [];
    private List<Instance> newborns = [];

    private static readonly Type[] defaultConstructor = [typeof(ulong), typeof(GameManager)];
    private static readonly Type instanceRootType = typeof(Instance);
    private static readonly Random random = new Random();

    public T Construct<T>() where T : Instance
    {
        Instance? instance = TryCreateNewDomesticInstanceOfClass(typeof(T).Name);
        if (instance == null || instance is not T t)
            throw new InvalidProgramException("Somehow TryCreateNewDomesticInstanceOfClass returned the wrong type");
        return t;
    }

    public bool TryResolveInstanceById(ulong instanceId, [NotNullWhen(true)] out Instance? instance)
    {
        return _trueInstances.TryGetValue(instanceId, out instance);
    }
    public Instance? TryCreateNewDomesticInstanceOfClass_LUA(string instanceClass)
    {
        ConstructorInfo? constructor = TryResolveConstructorForClass(instanceClass, false);
        if (constructor == null)
            return null;
        Instance newobject = (constructor.Invoke([ GenerateNewId(), gameManager ]) as Instance)!;
        _trueInstances[newobject.InstanceID] = newobject;
        newborns.Add(newobject);
        return newobject;
    }
    public Instance? TryCreateNewDomesticInstanceOfClass(string instanceClass)
    {
        ConstructorInfo? constructor = TryResolveConstructorForClass(instanceClass, false);
        if (constructor == null)
            return null;
        Instance newobject = (constructor.Invoke([ GenerateNewId(), gameManager ]) as Instance)!;
        _trueInstances[newobject.InstanceID] = newobject;
        newborns.Add(newobject);
        return newobject;
    }
    public Instance? TryCreateNewForeignInstanceOfClass(string instanceClass, ulong instanceid)
    {
        ConstructorInfo? constructor = TryResolveConstructorForClass(instanceClass, false);
        if (constructor == null)
            return null;
        Instance newobject = (constructor.Invoke([ instanceid, gameManager ]) as Instance)!;
        newobject.InitializationSettings.IsForeign = true;
        _trueInstances[newobject.InstanceID] = newobject;
        newborns.Add(newobject);
        return newobject;
    }
    public Instance? GetLocalInstanceById(ulong id)
    {
        _trueInstances.TryGetValue(id, out Instance? inst);
        return inst;
    }
    public Instance[] SelectInstances(Func<Instance, bool>? predicate)
    {
        if (predicate == null)
            return _trueInstances.Values.ToArray();
        List<Instance> instances = [];
        for (int i = 0; i < _trueInstances.Count; i++)
        {
            KeyValuePair<ulong, Instance> kvp = _trueInstances.ElementAt(i);
            Instance instance = kvp.Value;
            if (predicate(instance))
                instances.Add(instance);
        }
        return instances.ToArray();
    }
    public void ClearNewborns()
    {
        newborns.Clear();
    }
    public Instance[]? FlushNewborns()
    {
        if (newborns.Count == 0)
            return null;
        Instance[] instances = newborns.ToArray();
        newborns.Clear();
        return instances;
    }
    public int GetInstanceCount()
    {
        return _trueInstances.Count;
    }
    public void Remove(ulong instanceid)
    {
        _trueInstances.Remove(instanceid);
    }

    private ulong GenerateNewId()
    {
        ulong candidate;
        do
            candidate = unchecked((ulong)random.NextInt64() % (ulong.MaxValue - SERVICE_ID_BOUNDARY) + SERVICE_ID_BOUNDARY);
        while (_trueInstances.ContainsKey(candidate));
        return candidate;
    }
    private ConstructorInfo? TryResolveConstructorForClass(string className, bool onlyCreatableClasses)
    {
        if (className == "Instance")
            return null;
        if (_constructorCache.TryGetValue(className, out ConstructorInfo? cachedConstructore))
            return cachedConstructore;
        Type? potentialinstancetype = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == className);
        if (potentialinstancetype == null)
            return null;
        if (!potentialinstancetype.IsAssignableTo(instanceRootType))
            return null;
        if (onlyCreatableClasses)
        {
            if (potentialinstancetype.GetCustomAttribute<CreatableAttribute>() == null)
                return null;
        }
        ConstructorInfo? constructor = potentialinstancetype.GetConstructor(defaultConstructor);
        if (constructor == null)
            return null;
        _constructorCache[className] = constructor;
        return constructor;
    }
}