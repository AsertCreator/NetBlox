using System.Reflection;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Network;

namespace NetBlox.Runtime.Bridges;

// this is such a slop how would y'all even understand this
public static class InstanceBridge
{
    public class InstanceClassCacheMethod
    {
        public required string Name;
        public required Type[] Parameters;
        public required Type ReturnType;
        public required MethodInfo Method;
        public required DynValue Callback;
        public required ScriptCallableAttribute ScriptCallable;

        public override string ToString()
        {
            return "Method - " + Name;
        }
    }
    public class InstanceClassCacheProperty
    {
        public required string Name;
        public required Type PropertyType;
        public required PropertyInfo Property;
        public required ScriptCallableAttribute ScriptCallable;
        public required bool CanBeReplicated;
        public required bool IsReadOnly;
        public required int? NetworkOrdinal;

        public override string ToString()
        {
            return "Property - " + Name + " - " + PropertyType;
        }
    }
    public class InstanceClassCache
    {
        public string Name;
        public Dictionary<string, InstanceClassCacheMethod> Methods;
        public Dictionary<string, InstanceClassCacheProperty> Properties;
        public InstanceClassCacheProperty[] ReplicationOrderedProperties;
        public bool CanBeReplicated;
        public bool CanChildrenBeReplicated;
        public bool IsService;
        public GameManager GameManager;
        public Type TargetType;
        public Type? ParentType;

        private InstanceClassCache(GameManager gameManager, Type type)
        {
            Name = type.Name;
            Methods = new Dictionary<string, InstanceClassCacheMethod>();
            Properties = new Dictionary<string, InstanceClassCacheProperty>();
            ReplicationOrderedProperties = [];
            GameManager = gameManager;
            TargetType = type;
            IsService = type.GetCustomAttribute<ServiceAttribute>() != null;
            CanChildrenBeReplicated = true;
            if (IsService)
                CanChildrenBeReplicated = false;
            CanChildrenBeReplicated = CanChildrenBeReplicated || type.GetCustomAttribute<ReplicateChildrenAttribute>() != null;
            CanBeReplicated = type.GetCustomAttribute<NotReplicatedAttribute>() == null;

            if (TargetType.Name != "Instance")
                ParentType = type.BaseType;
            else
                ParentType = null;
        }

        public static InstanceClassCache Construct(ScriptContext ctx, Type type)
        {
            InstanceClassCache classCache = new InstanceClassCache(ctx.GameManager, type);

            if (!type.IsAssignableTo(typeof(Instance)))
                throw new ArgumentException("InstanceClassCache cannot be constructued for a non-Instance class");

            MethodInfo[] allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            PropertyInfo[] allProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            for (int i = 0; i < allMethods.Length; i++)
            {
                MethodInfo methodInfo = allMethods[i];
                ScriptCallableAttribute? scriptCallable = methodInfo.GetCustomAttribute<ScriptCallableAttribute>();

                if (scriptCallable == null)
                    continue;

                Type[] parameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();

                DynValue newCallback = DynValue.NewCallback((x, y) =>
                {
                    UserData instUD = y[0].CheckType(type.Name + ":" + methodInfo.Name, DataType.UserData).UserData;
                    SecurityIdentity? identity = ctx.GameManager.GameScheduler.GetCurrentSecurityIdentity();
                    string methodName = type.Name + ":" + methodInfo.Name;

                    if (instUD.Descriptor is not InstanceBridgeDescriptor instDesc)
                        throw new InvalidOperationException("Cannot call method " + methodName + " on a non-Instance type");
                    
                    Instance? instance = ctx.GameManager.GameRegistry.GetLocalInstanceById(instDesc.Value);
                    if (instance == null)
                        throw new InvalidOperationException("wow");
                    
                    if (!instance.GetType().IsAssignableTo(type))
                        throw new InvalidOperationException("Cannot call method " + methodName + ": Instance class mismatch");

                    if (identity != null)
                    {
                        if (!identity.IsAbleToCallInstanceMethod(instance, methodInfo))
                            throw new InvalidOperationException("Cannot call method " + methodName);

                        object?[] marshalled = LuaMarshal.MarshalLuaToClrArray(methodName, ctx, y.GetArray(1), parameterTypes);
                        object? returnval = methodInfo.Invoke(instance, marshalled);
                        DynValue luaReturn = LuaMarshal.MarshalClrToLua(methodName, ctx, returnval);

                        return luaReturn;
                    }
                    throw new InvalidOperationException(methodName + " method call went terribly wrong");   
                });

                InstanceClassCacheMethod cacheMethod = new InstanceClassCacheMethod()
                {
                    Name = methodInfo.Name,
                    Method = methodInfo,
                    Parameters = parameterTypes,
                    ReturnType = methodInfo.ReturnType,
                    Callback = newCallback,
                    ScriptCallable = scriptCallable  
                };

                classCache.Methods[cacheMethod.Name] = cacheMethod;
            }

            List<InstanceClassCacheProperty> replicatableProperties = [];

            for (int i = 0; i < allProperties.Length; i++)
            {
                PropertyInfo propertyInfo = allProperties[i];
                ScriptCallableAttribute? scriptCallable = propertyInfo.GetCustomAttribute<ScriptCallableAttribute>();

                if (scriptCallable == null)
                    continue;

                bool readOnly = propertyInfo.SetMethod == null;
                bool canBeReplicated = true;
                
                if (!readOnly && !propertyInfo.SetMethod!.IsPublic)
                    readOnly = true;
                
                if (propertyInfo.PropertyType == typeof(LuaEvent))
                {
                    canBeReplicated = false;
                    readOnly = true;
                }

                if (propertyInfo.GetCustomAttribute<NotReplicatedAttribute>() != null)
                    canBeReplicated = false;                

                InstanceClassCacheProperty cacheProperty = new InstanceClassCacheProperty()
                {
                    Name = propertyInfo.Name,
                    Property = propertyInfo,
                    PropertyType = propertyInfo.PropertyType,
                    IsReadOnly = readOnly,
                    CanBeReplicated = canBeReplicated,
                    ScriptCallable = scriptCallable,
                    NetworkOrdinal = null
                };

                classCache.Properties[cacheProperty.Name] = cacheProperty;
                if (canBeReplicated && !readOnly)
                    replicatableProperties.Add(cacheProperty);
            }

            replicatableProperties.Sort((a, b) => a.Name.CompareTo(b.Name));

            classCache.ReplicationOrderedProperties = replicatableProperties.ToArray();

            for (int i = 0; i < classCache.ReplicationOrderedProperties.Length; i++)
                classCache.ReplicationOrderedProperties[i].NetworkOrdinal = i;

            return classCache;
        }
        public InstanceClassCacheProperty? TryGetProperty(string name)
        {
            if (Properties.TryGetValue(name, out InstanceClassCacheProperty? property))
                return property;
            InstanceClassCache? parentCache = RetrieveParentCache();
            if (parentCache != null)
                return parentCache.TryGetProperty(name);
            return null;
        }
        public InstanceClassCacheMethod? TryGetMethod(string name)
        {
            if (Methods.TryGetValue(name, out InstanceClassCacheMethod? method))
                return method;
            InstanceClassCache? parentCache = RetrieveParentCache();
            if (parentCache != null)
                return parentCache.TryGetMethod(name);
            return null;
        }
        public InstanceClassCache? RetrieveParentCache()
        {
            if (ParentType == null)
                return null;
            ScriptContext scriptContext = GameManager.RootModel.GetService<ScriptContext>();
            return scriptContext.ResolveInstanceClassCacheForType(ParentType);
        }

        public override string ToString()
        {
            return "InstanceClassCache - " + Name;
        }
    }

    public class InstanceBridgeDescriptor : IUserDataDescriptor
    {
        public string Name => nameof(Instance);
        public Type Type => typeof(Instance);
        public GameManager GameManager;
        public ulong Value;

        public InstanceBridgeDescriptor(Instance value)
        {
            Value = value.InstanceID;
            GameManager = value.GameManager;
        }
        public InstanceBridgeDescriptor(GameManager gm, ulong value)
        {
            Value = value;
            GameManager = gm;
        }

        private Instance ThrowIfZombieInstance()
        {
            if (GameManager.GameRegistry.TryResolveInstanceById(Value, out Instance? instance))
                return instance;
            throw new ScriptRuntimeException("WEE WOO WEE WOO W");
        }

        public InstanceClassCache ResolveForType(Type type)
        {
            ScriptContext scriptContext = GameManager.RootModel.GetService<ScriptContext>();
            return scriptContext.ResolveInstanceClassCacheForType(type);
        }

        public string AsString(object obj)
        {
            Instance thisInstance = ThrowIfZombieInstance();
            return thisInstance.GetFullName();
        }
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            Instance thisInstance = ThrowIfZombieInstance();
            string name = index.CheckType("instance index", DataType.String).String;
            Type type = thisInstance.GetType();
            InstanceClassCache classCache = ResolveForType(type);
            SecurityIdentity? securityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity();

            if (securityIdentity == null)
                throw new InvalidOperationException("wtf");
            
            InstanceClassCacheProperty? property = classCache.TryGetProperty(name);
            if (property != null)
            {
                if (!securityIdentity.IsAbleToGetInstanceProperty(thisInstance, property.Property))
                    throw new InvalidOperationException("Cannot index property " + type.Name + "." + property.Name);
                object? data = property.Property.GetValue(thisInstance);
                return LuaMarshal.MarshalClrToLua(property.Name + " index", GameManager.RootModel.GetService<ScriptContext>(), data);
            }
            
            InstanceClassCacheMethod? method = classCache.TryGetMethod(name);
            if (method != null)
                return method.Callback;

            Instance? childInstance = thisInstance.FindFirstChild(name);
            if (childInstance != null)
                return DynValue.NewUserData(PushUserData(GameManager, childInstance));
            
            throw new ArgumentException("Couldn't find a property, method or child Instance named \"" + name + "\"");
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            Instance thisInstance = ThrowIfZombieInstance();
            string name = index.CheckType("instance newindex", DataType.String).String;
            Type type = thisInstance.GetType();
            InstanceClassCache classCache = ResolveForType(type);
            InstanceClassCacheProperty? property = classCache.TryGetProperty(name);
            SecurityIdentity securityIdentity = GameManager.GameScheduler.GetCurrentSecurityIdentity()!;

            if (property != null)
            {
                if (property.IsReadOnly)
                    throw new InvalidOperationException("Cannot set read-only property " + type.Name + "." + property.Name);
                if (!securityIdentity.IsAbleToSetInstanceProperty(thisInstance, property.Property))
                    throw new InvalidOperationException("Cannot set property " + type.Name + "." + property.Name);
                object? data = LuaMarshal.MarshalLuaToClr(property.Name + " newindex", GameManager.RootModel.GetService<ScriptContext>(),
                    value, property.PropertyType);
                property.Property.SetValue(thisInstance, data);

                ReplicationAgent? agent = thisInstance.ReplicationAgentIfServer;
                if (agent != null)
                    agent.DeltaAddReplicatablePropertyChange(thisInstance, name, data);

                return true;
            }

            throw new Exception("Couldn't find a property named \"" + name + "\"");
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                throw new ScriptRuntimeException("Cannot add Instance");
            }
            else if (metaname == "__sub")
            {
                throw new ScriptRuntimeException("Cannot subtract Instance");
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply Instance");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide Instance");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not InstanceBridgeDescriptor instBD)
                        return DynValue.False;
                    if (instBD.Value == Value)
                        return DynValue.True;
                    return DynValue.False;
                });
            }
            return DynValue.Nil;
        }
    }

    public static void Setup(GameManager gameManager)
    {
        ScriptContext scriptContext = gameManager.RootModel.GetService<ScriptContext>();
        SecurityIdentity securityIdentity = gameManager.GameScheduler.GetCurrentSecurityIdentity()!;
        Table instancemodule = scriptContext.AllocateTableInRegistry(nameof(InstanceBridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table instancemeta = scriptContext.AllocateTableInRegistry(nameof(InstanceBridge) + "_meta", out _);
            instancemeta["__index"] = instancemeta;
            instancemeta["__metatable"] = "Locked";
            instancemeta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 1)
                {
                    string className = args[0].CheckType("Instance.new", DataType.String).String;
                    
                    Instance? instance = gameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass_LUA(className);
                    if (instance == null)
                        throw new ScriptRuntimeException("Unable to create Instance of type \"" + className + "\"");

                    return DynValue.NewUserData(PushUserData(gameManager, instance));
                }
                else if (args.Count == 2)
                {
                    string className = args[0].CheckType("Instance.new", DataType.String).String;
                    UserData instBDUD = args[1].CheckType("Instance.new", DataType.UserData).UserData;

                    if (instBDUD.Descriptor is not InstanceBridgeDescriptor descriptor)
                        throw new ScriptRuntimeException("Instance.new requires 1 string argument and an optional Instance argument");
                    
                    Instance? instance = gameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass_LUA(className);
                    if (instance == null)
                        throw new ScriptRuntimeException("Unable to create Instance of type \"" + className + "\"");
                    
                    instance.Parent = gameManager.GameRegistry.GetLocalInstanceById(descriptor.Value);

                    return DynValue.NewUserData(PushUserData(gameManager, instance));
                }
                throw new ScriptRuntimeException("Instance.new requires 1 string argument and an optional Instance argument");
            });
            instancemodule.MetaTable = instancemeta;
        }

        script.Globals["Instance"] = instancemodule;
    }
    public static UserData PushUserData(GameManager gameManager, Instance value)
    {
        var obj = new InstanceBridgeDescriptor(value);
        return UserData.Create(obj, obj).UserData;
    }
    public static UserData PushUserData(GameManager gameManager, ulong value)
    {
        var obj = new InstanceBridgeDescriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}