using System.Diagnostics;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime;
using NetBlox.Runtime.Bridges;

namespace NetBlox.Network;

public class ReplicationAgent
{
    public readonly GameManager GameManager;

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
    }
    public void ApplyDeltaReplicationFrame(byte[] frame)
    {
        
    }
}