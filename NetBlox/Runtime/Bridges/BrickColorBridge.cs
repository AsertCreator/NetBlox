using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using NetBlox.Instances.Services;
using NetBlox.Structs;

namespace NetBlox.Runtime.Bridges;

public static class BrickColorBridge
{
    public class BrickColorDescriptor : IUserDataDescriptor
    {
        public string Name => nameof(BrickColor);
        public Type Type => typeof(BrickColor);
        public BrickColor Value;
        public GameManager GameManager;

        public BrickColorDescriptor(GameManager gm, BrickColor value)
        {
            Value = value;
            GameManager = gm;
        }

        public string AsString(object obj) => Value.Name;
        public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "Number":
                        return DynValue.NewNumber(Value.Index);
                    case "r":
                        return DynValue.NewNumber(Value.Color3.R / 255f);
                    case "g":
                        return DynValue.NewNumber(Value.Color3.G / 255f);
                    case "b":
                        return DynValue.NewNumber(Value.Color3.B / 255f);
                    case "Name":
                        return DynValue.NewString(Value.Name);
                    case "Color":
                        return DynValue.NewUserData(Color3Bridge.PushUserData(GameManager, Value.Color3));
                }
            }

            throw new ScriptRuntimeException("No such property for BrickColor: " + index.ToString());
        }
        public bool SetIndex(Script script, object obj, DynValue index, DynValue value, bool isDirectIndexing)
        {
            if (index.Type == DataType.String)
            {
                switch (index.String)
                {
                    case "Number":
                    case "r":
                    case "g":
                    case "b":
                    case "Name":
                    case "Color":
                        throw new ScriptRuntimeException(index.String + " is read-only");
                }
            }

            throw new ScriptRuntimeException("No such property for BrickColor: " + index.ToString());
        }
        public bool IsTypeCompatible(Type type, object obj) => false;
        public DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (metaname == "__metatable")
                return DynValue.NewString("Locked");
            else if (metaname == "__add")
            {
                throw new ScriptRuntimeException("Cannot add BrickColor");
            }
            else if (metaname == "__sub")
            {
                throw new ScriptRuntimeException("Cannot subtract BrickColor");
            }
            else if (metaname == "__mul")
            {
                throw new ScriptRuntimeException("Cannot multiply BrickColor");
            }
            else if (metaname == "__div")
            {
                throw new ScriptRuntimeException("Cannot divide BrickColor");
            }
            else if (metaname == "__eq")
            {
                return DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
                {
                    DynValue dynValue = args[1];
                    if (dynValue.UserData.Object is not BrickColorDescriptor other)
                        return DynValue.False;
                    if (other.Value.Index == Value.Index)
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
        Table bcolormodule = scriptContext.AllocateTableInRegistry(nameof(BrickColorBridge), out bool isNew);
        Script script = scriptContext.GetLuaStateFor(securityIdentity);

        if (isNew)
        {
            Table bcolormeta = scriptContext.AllocateTableInRegistry(nameof(BrickColorBridge) + "_meta", out _);
            bcolormeta["__index"] = bcolormeta;
            bcolormeta["__metatable"] = "Locked";
            bcolormeta["pallete"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 1)
                {
                    int palleteId = (int)args[0].CheckType("BrickColor.pallete", DataType.Number).Number;
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.GetBrickColorByPalleteIndex(palleteId)));
                }
                throw new ArgumentException("BrickColor.pallete requires 1 number argument");
            });
            bcolormeta["random"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.Random()));
                }
                throw new ArgumentException("BrickColor.random takes no arguments");
            });
            bcolormeta["White"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.White));
                }
                throw new ArgumentException("BrickColor.White takes no arguments");
            });
            bcolormeta["Gray"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.MediumStoneGrey));
                }
                throw new ArgumentException("BrickColor.Gray takes no arguments");
            });
            bcolormeta["DarkGray"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.DarkStoneGrey));
                }
                throw new ArgumentException("BrickColor.DarkGray takes no arguments");
            });
            bcolormeta["Black"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.Black));
                }
                throw new ArgumentException("BrickColor.Black takes no arguments");
            });
            bcolormeta["Red"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.Red));
                }
                throw new ArgumentException("BrickColor.Red takes no arguments");
            });
            bcolormeta["Yellow"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.Yellow));
                }
                throw new ArgumentException("BrickColor.Yellow takes no arguments");
            });
            bcolormeta["Green"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.Green));
                }
                throw new ArgumentException("BrickColor.Green takes no arguments");
            });
            bcolormeta["Blue"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 0)
                {
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.Blue));
                }
                throw new ArgumentException("BrickColor.Blue takes no arguments");
            });
            bcolormeta["new"] = DynValue.NewCallback(delegate (ScriptExecutionContext executionContext, CallbackArguments args)
            {
                if (args.Count == 1)
                {
                    if (args[0].Type == DataType.Number)
                    {
                        return DynValue.NewUserData(PushUserData(gameManager, BrickColor.GetBrickColorByIndex((int)args[0].Number)));
                    }
                    else if (args[0].Type == DataType.String)
                    {
                        return DynValue.NewUserData(PushUserData(gameManager, BrickColor.GetBrickColorByName(args[0].String)));
                    }
                    else if (args[0].Type == DataType.UserData && args[0].UserData.Descriptor is Color3Bridge.Color3Descriptor col3d)
                    {
                        return DynValue.NewUserData(PushUserData(gameManager, BrickColor.GetBrickColorByClosestColor3(col3d.Value)));
                    }
                }
                else if (args.Count == 3)
                {
                    byte r = (byte)((float)args[0].CheckType("BrickColor.new", DataType.Number).Number * 255);
                    byte g = (byte)((float)args[1].CheckType("BrickColor.new", DataType.Number).Number * 255);
                    byte b = (byte)((float)args[2].CheckType("BrickColor.new", DataType.Number).Number * 255);
                    Color3 color3 = new Color3(r, g, b);
                    return DynValue.NewUserData(PushUserData(gameManager, BrickColor.GetBrickColorByClosestColor3(color3)));
                }
                throw new ArgumentException("BrickColor.new requires 1 number argument or 1 string argument or 1 Color3 argument or 3 number arguments");
            });
            bcolormodule.MetaTable = bcolormeta;
        }

        script.Globals["BrickColor"] = bcolormodule;
    }
    public static UserData PushUserData(GameManager gameManager, BrickColor value)
    {
        var obj = new BrickColorDescriptor(gameManager, value);
        return UserData.Create(obj, obj).UserData;
    }
}