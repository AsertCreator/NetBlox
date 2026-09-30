using System.Diagnostics;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime.Bridges;

namespace NetBlox.Network;

public class ReplicationAgent
{
    public readonly GameManager GameManager;
    public bool InitialReplicationReceived { get; private set; }

    public Queue<byte[]> PendingDeltaReplicationFrames = [];

    private struct DeltaReplicationInstanceCreationItem
    {
        public string ClassName;
        public ulong ParentId;
        public ulong InstanceId;
    }
    private struct DeltaReplicationInstanceDestructionItem
    {
        public ulong InstanceId;
    }
    private struct DeltaReplicationInstanceReparentedItem
    {
        public ulong ParentId;
        public ulong InstanceId;
    }

    private Dictionary<ulong, Dictionary<string, object?>> deltaBufferPropertyChanges = [];
    private Dictionary<ulong, DeltaReplicationInstanceCreationItem> deltaBufferNewInstances = [];
    private Dictionary<ulong, DeltaReplicationInstanceDestructionItem> deltaBufferRemovedInstances = [];
    private Dictionary<ulong, DeltaReplicationInstanceReparentedItem> deltaBufferReparentedInstances = [];

    public ReplicationAgent(GameManager gameManager)
    {
        GameManager = gameManager;
    }

    public byte[] ConstructInitialReplicationFrame()
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        ScriptContext scriptContext = GameManager.RootModel.GetService<ScriptContext>();

        void WalkReplicableInstanceTreeFrom(Instance instance)
        {
            bw.Write(instance.InstanceID);
            bw.Write(instance.parentid);
            bw.Write(instance.ClassName);

            InstanceBridge.InstanceClassCache cache = scriptContext.ResolveInstanceClassCacheForType(instance.GetType());

            void WriteReplicatedProperties(InstanceBridge.InstanceClassCache levelCache)
            {
                InstanceBridge.InstanceClassCacheProperty[] replicatedProperties = levelCache.ReplicationOrderedProperties;

                bw.Write(replicatedProperties.Length);

                for (int i = 0; i < replicatedProperties.Length; i++)
                {
                    InstanceBridge.InstanceClassCacheProperty property = replicatedProperties[i];
                    object? value = property.Property.GetValue(instance);

                    NetworkMarshal.MarshalClrToNetwork(GameManager, bw, property.PropertyType, value);
                }

                if (levelCache.ParentType != null)
                {
                    InstanceBridge.InstanceClassCache parentCache = scriptContext.ResolveInstanceClassCacheForType(levelCache.ParentType);
                    WriteReplicatedProperties(parentCache);
                }
            }

            WriteReplicatedProperties(cache);
            
            Instance[] instances = instance.GetChildren();

            if (cache.CanChildrenBeReplicated)
            {
                bw.Write(instances.Length);

                for (int i = 0; i < instances.Length; i++)
                {
                    Instance child = instances[i];
                    InstanceBridge.InstanceClassCache childcache = scriptContext.ResolveInstanceClassCacheForType(child.GetType());

                    if (!childcache.CanBeReplicated)
                    {
                        bw.Write(false);
                        continue;
                    }

                    bw.Write(true);

                    WalkReplicableInstanceTreeFrom(child);
                }
            }
            else
            {
                bw.Write(0);
            }
        }

        WalkReplicableInstanceTreeFrom(GameManager.RootModel);

        Trace.TraceInformation("Constructed an initial replication frame, size=" + ms.Length);

        return ms.ToArray();
    }
    public void ApplyInitialReplicationFrame(byte[] frame)
    {
        using MemoryStream ms = new MemoryStream(frame);
        using BinaryReader br = new BinaryReader(ms);

        Trace.TraceInformation("Applying an initial replication frame, size=" + frame.Length);

        ScriptContext context = GameManager.RootModel.GetService<ScriptContext>();

        void ReadInstance()
        {
            ulong instanceId = br.ReadUInt64();
            ulong parentId = br.ReadUInt64();
            string className = br.ReadString();

            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(instanceId);
            if (instance == null)
            {
                instance = GameManager.GameRegistry.TryCreateNewForeignInstanceOfClass(className, instanceId);
                if (instance == null)
                    throw new NetworkException("Cannot create an instance of type " + className + ", aborting...");    
            }

            instance.Parent = GameManager.GameRegistry.GetLocalInstanceById(parentId);

            InstanceBridge.InstanceClassCache cache = context.ResolveInstanceClassCacheForType(instance.GetType());

            void ReadReplicatedProperties(InstanceBridge.InstanceClassCache levelCache)
            {
                InstanceBridge.InstanceClassCacheProperty[] replicatedProperties = levelCache.ReplicationOrderedProperties;

                int length = br.ReadInt32();

                for (int i = 0; i < replicatedProperties.Length && i < length; i++)
                {
                    InstanceBridge.InstanceClassCacheProperty property = replicatedProperties[i];
                    object? value = NetworkMarshal.MarshalNetworkToClr(GameManager, br, property.PropertyType);

                    property.Property.SetValue(instance, value);
                }

                if (levelCache.ParentType != null)
                {
                    InstanceBridge.InstanceClassCache parentCache = context.ResolveInstanceClassCacheForType(levelCache.ParentType);
                    ReadReplicatedProperties(parentCache);
                }
            }

            ReadReplicatedProperties(cache);

            int childrenCount = br.ReadInt32();

            for (int i = 0; i < childrenCount; i++)
            {
                bool available = br.ReadBoolean();
                if (available)
                    ReadInstance();
            }
        }

        ReadInstance();

        InitialReplicationReceived = true;

        // if we had initial replication frame consumed, we may begin parsing the deltareplicationframes

        GameSchedulerTask task = GameManager.GameScheduler.Schedule("Delta Replication Handler", GameScheduler.SchedulerPhase.Network, DoDeltaReplicationLoop);
        task.Identity = Runtime.SecurityIdentity.SI_EngineNetworker;
    }

    public void DeltaAddReplicatablePropertyChange(Instance instance, string propertyName, object? newValue)
    {
        if (!deltaBufferPropertyChanges.TryGetValue(instance.InstanceID, out Dictionary<string, object?>? v))
        {
            v = new Dictionary<string, object?>();
            deltaBufferPropertyChanges[instance.InstanceID] = v;
        }

        v[propertyName] = newValue;
    }
    public void DeltaAddNewInstance(Instance instance)
    {
        DeltaReplicationInstanceCreationItem item = default;
        item.ClassName = instance.ClassName;
        item.InstanceId = instance.InstanceID;
        item.ParentId = instance.parentid;

        deltaBufferNewInstances[instance.InstanceID] = item;
    }
    public void DeltaRemoveInstance(Instance instance)
    {
        DeltaReplicationInstanceDestructionItem item = default;
        item.InstanceId = instance.InstanceID;

        if (deltaBufferNewInstances.ContainsKey(instance.InstanceID))
        {
            deltaBufferNewInstances.Remove(instance.InstanceID);
            return;
        }

        deltaBufferRemovedInstances[instance.InstanceID] = item;
    }
    public void DeltaReparentInstance(Instance instance)
    {
        DeltaReplicationInstanceReparentedItem item = default;
        item.InstanceId = instance.InstanceID;
        item.ParentId = instance.parentid;

        deltaBufferReparentedInstances[instance.InstanceID] = item;
    }
    public byte[] CommitDeltaReplication()
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        ScriptContext scriptContext = GameManager.RootModel.GetService<ScriptContext>();

        bw.Write(deltaBufferNewInstances.Count);

        foreach (KeyValuePair<ulong, DeltaReplicationInstanceCreationItem> mpair in deltaBufferNewInstances)
        {
            bw.Write(mpair.Value.InstanceId);
            bw.Write(mpair.Value.ParentId);
            bw.Write(mpair.Value.ClassName);

            Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(mpair.Value.InstanceId);
            if (instance == null)
                continue;

            InstanceBridge.InstanceClassCache? cache = scriptContext.ResolveInstanceClassCacheForType(instance.GetType());

            do
            {
                for (int i = 0; i < cache.ReplicationOrderedProperties.Length; i++)
                {
                    InstanceBridge.InstanceClassCacheProperty property = cache.ReplicationOrderedProperties[i];
                    object? value = property.Property.GetValue(instance);

                    NetworkMarshal.MarshalClrToNetwork(GameManager, bw, property.PropertyType, value);
                }

                cache = cache.RetrieveParentCache();
            }
            while (cache != null);
        }

        bw.Write(deltaBufferReparentedInstances.Count);

        foreach (KeyValuePair<ulong, DeltaReplicationInstanceReparentedItem> mpair in deltaBufferReparentedInstances)
        {
            bw.Write(mpair.Value.InstanceId);
            bw.Write(mpair.Value.ParentId);
        }

        bw.Write(deltaBufferRemovedInstances.Count);

        foreach (KeyValuePair<ulong, DeltaReplicationInstanceDestructionItem> mpair in deltaBufferRemovedInstances)
        {
            bw.Write(mpair.Value.InstanceId);
        }

        bw.Write(deltaBufferPropertyChanges.Count);

        foreach (KeyValuePair<ulong, Dictionary<string, object?>> mpair in deltaBufferPropertyChanges)
        {
            Instance? changeKey = GameManager.GameRegistry.GetLocalInstanceById(mpair.Key);
            Dictionary<string, object?> changeValue = mpair.Value;

            if (changeKey == null)
            {
                bw.Write(false);
                continue;
            }

            InstanceBridge.InstanceClassCache cache = scriptContext.ResolveInstanceClassCacheForType(changeKey.GetType());

            bw.Write(true);
            bw.Write(changeKey.InstanceID);
            bw.Write(changeValue.Count);

            foreach (KeyValuePair<string, object?> pair in changeValue)
            {
                InstanceBridge.InstanceClassCacheProperty property = cache.TryGetProperty(pair.Key)!;

                bw.Write(property.NetworkOrdinal!.Value);
                NetworkMarshal.MarshalClrToNetwork(GameManager, bw, property.PropertyType, pair.Value);
            }
        }

        ForceFlushDeltaBuffer();

        return ms.ToArray();
    }
    public void ForceFlushDeltaBuffer()
    {
        deltaBufferNewInstances.Clear();
        deltaBufferPropertyChanges.Clear();
        deltaBufferRemovedInstances.Clear();
        deltaBufferReparentedInstances.Clear();
    }

    private SchedulerTaskResult DoDeltaReplicationLoop(GameSchedulerTask task)
    {
        if (GameManager.ShuttingDown)
            return SchedulerTaskResult.CompletedSuccess;

        if (GameManager.NetworkMode == NetworkMode.Client)
        {
            byte[]? frame = null;

            lock (PendingDeltaReplicationFrames)
            {
                if (PendingDeltaReplicationFrames.Count == 0)
                {
                    int networkFps = 60;
                    if (GameManager.GameRenderer != null)
                        networkFps = GameManager.GameRenderer.PreferredFPS;
                    task.WaitingTimeTarget = GameManager.TimestampInTheFuture(TimeSpan.FromMilliseconds(1000f / networkFps));
                    return SchedulerTaskResult.NotCompleted;
                }

                frame = PendingDeltaReplicationFrames.Dequeue();
            }

            ScriptContext scriptContext = GameManager.RootModel.GetService<ScriptContext>();

            using MemoryStream ms = new MemoryStream(frame);
            using BinaryReader br = new BinaryReader(ms);

            int deltaBufferNewInstancesCount = br.ReadInt32();

            for (int i = 0; i < deltaBufferNewInstancesCount; i++)
            {
                ulong instanceId = br.ReadUInt64();
                ulong parentId = br.ReadUInt64();
                string className = br.ReadString();

                Instance? newparent = GameManager.GameRegistry.GetLocalInstanceById(parentId);
                Instance? instance = GameManager.GameRegistry.TryCreateNewForeignInstanceOfClass(className, instanceId);
                instance?.Parent = newparent;

                if (instance == null)
                    continue;

                InstanceBridge.InstanceClassCache? cache = scriptContext.ResolveInstanceClassCacheForType(instance.GetType());

                do
                {
                    for (int j = 0; j < cache.ReplicationOrderedProperties.Length; j++)
                    {
                        InstanceBridge.InstanceClassCacheProperty property = cache.ReplicationOrderedProperties[j];
                        object? value = NetworkMarshal.MarshalNetworkToClr(GameManager, br, property.PropertyType);

                        property.Property.SetValue(instance, value);
                    }

                    cache = cache.RetrieveParentCache();
                }
                while (cache != null);
            }

            int deltaBufferReparentedInstancesCount = br.ReadInt32();

            for (int i = 0; i < deltaBufferReparentedInstancesCount; i++)
            {
                ulong instanceId = br.ReadUInt64();
                ulong parentId = br.ReadUInt64();

                Instance? newparent = GameManager.GameRegistry.GetLocalInstanceById(parentId);
                Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(instanceId);
                instance?.Parent = newparent;
            }

            int deltaBufferRemovedInstancesCount = br.ReadInt32();

            for (int i = 0; i < deltaBufferRemovedInstancesCount; i++)
            {
                ulong instanceId = br.ReadUInt64();
                Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(instanceId);
                
                instance?.Destroy();
            }

            int deltaBufferPropertyChangesCount = br.ReadInt32();

            for (int i = 0; i < deltaBufferPropertyChangesCount; i++)
            {
                if (!br.ReadBoolean())
                    continue;

                ulong instanceId = br.ReadUInt64();
                Instance? instance = GameManager.GameRegistry.GetLocalInstanceById(instanceId);
                int propertyChangeCount = br.ReadInt32();

                InstanceBridge.InstanceClassCache cache = scriptContext.ResolveInstanceClassCacheForType(instance!.GetType());

                for (int j = 0; j < propertyChangeCount; j++)
                {
                    int networkOrdinal = br.ReadInt32();
                    InstanceBridge.InstanceClassCacheProperty property = cache.ReplicationOrderedProperties[networkOrdinal];
                    object? newValue = NetworkMarshal.MarshalNetworkToClr(GameManager, br, property.PropertyType);

                    property.Property.SetValue(instance, newValue);
                }
            }
        }

        return SchedulerTaskResult.NotCompleted;
    }
}