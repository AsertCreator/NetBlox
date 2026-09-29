using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;
using NetBlox.Structs;

namespace NetBlox.Runtime.Bridges;

public static class EnumValueBridge
{
    public class EnumValueDescriptor : IUserDataDescriptor
    {
        public string Name => nameof(EnumValue);
        public Type Type => typeof(EnumValue);
        public EnumValue Value;

        public EnumValueDescriptor(EnumValue value)
        {
            Value = value;
        }

        public string AsString(object obj) => Value.Name;
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            throw new ScriptRuntimeException("No such property for EnumValue: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            throw new ScriptRuntimeException("No such property for EnumValue: " + index.ToString());
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                throw new ScriptRuntimeException("Cannot add EnumValue");
            }
            else if (metaname == "__sub")
            {
                throw new ScriptRuntimeException("Cannot subtract EnumValue");
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply EnumValue");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide v");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not EnumValueDescriptor other)
                        return DynValue.False;
                    if (other.Value.Value == Value.Value && other.Value.EnumType == Value.EnumType)
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
        Table Enummodule = scriptContext.AllocateTableInRegistry(nameof(EnumValueBridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table Enummeta = scriptContext.AllocateTableInRegistry(nameof(EnumValueBridge) + "_meta", out _);
            Enummeta["__index"] = Enummeta;
            Enummeta["__metatable"] = "Locked";
            
            void AddEnum<T>() where T : Enum
            {
                Type enumtype = typeof(T);
                Table enumtable = scriptContext.AllocateTableInRegistry(nameof(EnumValueBridge) + "_" + enumtype.Name, out bool isNewSubTable);

                int[] enumvalues = (int[])enumtype.GetEnumValuesAsUnderlyingType();
                string[] enumnames = enumtype.GetEnumNames();

                for (int i = 0; i < enumnames.Length; i++)
                {
                    EnumValue enumValue = new EnumValue()
                    {
                        EnumType = enumtype,
                        Name = enumnames[i],
                        Value = enumvalues[i]
                    };
                    DynValue dv = DynValue.NewUserData(PushUserData(gameManager, enumValue));
                    enumtable[enumnames[i]] = dv;
                }

                Enummeta[enumtype.Name] = enumtable;
            }

            AddEnum<SurfaceType>();
            AddEnum<Faces>();
            AddEnum<TextXAlignment>();
            AddEnum<TextYAlignment>();

            Enummodule.MetaTable = Enummeta;
        }

        script.Globals["Enum"] = Enummodule;
    }
    public static UserData PushUserData(GameManager gameManager, EnumValue value)
    {
        var obj = new EnumValueDescriptor(value);
        return UserData.Create(obj, obj).UserData;
    }
}