using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;
using NetBlox.Structs;

namespace NetBlox.Runtime.Bridges;

public static class Color3Bridge
{
    public class Color3Descriptor : IUserDataDescriptor
    {
        public string Name => nameof(Color3);
        public Type Type => typeof(Color3);
        public Color3 Value;

        public Color3Descriptor(Color3 value)
        {
            Value = value;
        }

        public string AsString(object obj) => "<" + Value.R + " " + Value.G + " " + Value.B + ">";
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "R":
                        return DynValue.NewNumber(Value.R / 255);
                    case "G":
                        return DynValue.NewNumber(Value.G / 255);
                    case "B":
                        return DynValue.NewNumber(Value.B / 255);
                }
            }

            throw new ScriptRuntimeException("No such property for Color3: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "R":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("R is supposed to be number");
                        Value.R = (byte)(value.Number * 255);
                        return true;
                    case "G":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("G is supposed to be number");
                        Value.G = (byte)(value.Number * 255);
                        return true;
                    case "B":
                        if (value.Type != DataType.Number)
                            throw new ScriptRuntimeException("B is supposed to be number");
                        Value.B = (byte)(value.Number * 255);
                        return true;
                }
            }

            throw new ScriptRuntimeException("No such property for Color3: " + index.ToString());
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                throw new ScriptRuntimeException("Cannot add Color3");
            }
            else if (metaname == "__sub")
            {
                throw new ScriptRuntimeException("Cannot subtract Color3");
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply Color3");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide Color3");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not Color3Descriptor other)
                        return DynValue.False;
                    if (other.Value == Value)
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
        Table Color3module = scriptContext.AllocateTableInRegistry(nameof(Color3Bridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table Color3meta = scriptContext.AllocateTableInRegistry(nameof(Color3Bridge) + "_meta", out _);
            Color3meta["__index"] = Color3meta;
            Color3meta["__metatable"] = "Locked";
            Color3meta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 3)
                {
                    byte r = (byte)(args[0].CheckType("Color3.new", DataType.Number).Number * 255);
                    byte g = (byte)(args[1].CheckType("Color3.new", DataType.Number).Number * 255);
                    byte b = (byte)(args[2].CheckType("Color3.new", DataType.Number).Number * 255);
                    return DynValue.NewUserData(PushUserData(gameManager, new Color3(r, g, b)));
                }
                throw new ScriptRuntimeException("Color3.new requires 3 number arguments");
            });
            Color3module.MetaTable = Color3meta;
        }

        script.Globals["Color3"] = Color3module;
    }
    public static UserData PushUserData(GameManager gameManager, Color3 value)
    {
        var obj = new Color3Descriptor(value);
        return UserData.Create(obj, obj).UserData;
    }
}