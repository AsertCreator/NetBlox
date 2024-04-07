using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using System.ComponentModel;
using System.Reflection;
using System.Runtime;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime
{
	public class LuaThread
	{
		public int Level;
		public DynValue? MsThread;
		public Coroutine? Coroutine;
		public Table? Global;
		public Script? Script;
		public BaseScript? ScrInst;
		public DateTime WaitUntil;
		public string Name = string.Empty;
	}
	public static class LuaRuntime
	{
		public static LinkedListNode<LuaThread>? CurrentThread;
		public static LinkedList<LuaThread> Threads = new();
		public static Exception? LastException;
		public static int ScriptExecutionTimeout = 7;

		public static void Setup(DataModel dm)
		{
			var works = GameManager.GetService<Workspace>();

			dm.MainEnv = new Script(
				CoreModules.Basic | CoreModules.Metatables | CoreModules.Bit32 | CoreModules.Coroutine |
				CoreModules.TableIterators | CoreModules.String | CoreModules.ErrorHandling |
				CoreModules.Math | CoreModules.OS_Time | CoreModules.GlobalConsts);
			dm.MainEnv.Globals["game"] = MakeInstanceTable(GameManager.CurrentRoot, dm.MainEnv);

			if (works != null)
				dm.MainEnv.Globals["workspace"] = MakeInstanceTable(works, dm.MainEnv);

			dm.MainEnv.Globals["wait"] = DynValue.NewCallback((x, y) =>
			{
				var wa = y.Count == 0 ? DateTime.Now : DateTime.Now.AddSeconds(y[0].Number);
				GetThreadFor(x.GetCallingCoroutine()).WaitUntil = wa;
				return DynValue.NewYieldReq(y.GetArray());
			});
			dm.MainEnv.Globals["require"] = DynValue.NewCallback((x, y) =>
			{
				var table = y[0];
				var inst = SerializationManager.LuaDeserialize<Instance>(table, x.OwnerScript);

				throw new NotImplementedException();
			});
			dm.MainEnv.Globals["printidentity"] = DynValue.NewCallback((x, y) =>
			{
				PrintOut("Current identity is " + GetThreadFor(x.GetCallingCoroutine()).Level);
				return DynValue.Void;
			});
			dm.MainEnv.Globals["print"] = DynValue.NewCallback((x, y) =>
			{
				PrintOut(y.AsStringUsingMeta(x, 0, "print"));
				return DynValue.Void;
			});
			dm.MainEnv.Globals["warn"] = DynValue.NewCallback((x, y) =>
			{
				PrintWarn(y.AsStringUsingMeta(x, 0, "warn"));
				return DynValue.Void;
			});
			dm.MainEnv.Globals["error"] = DynValue.NewCallback((x, y) =>
			{
				PrintError(y.AsStringUsingMeta(x, 0, "error"));
				throw new Exception(y[0].ToString());
			});
		}
		public static LuaThread GetThreadFor(Coroutine c)
		{
			return (from x in Threads where x.Coroutine == c select x).First();
		}
		public static void Execute(string code, int sl, BaseScript? bs, DataModel dm)
		{
			if (dm == null)
				throw new Exception("DataModel must be present in order to execute scripts!");

			try
			{
				var d = dm.MainEnv.CreateCoroutine(dm.MainEnv.DoString("return function() " + code + " end")); // ummm thats weird
				var lt = new LuaThread
				{
					Script = dm.MainEnv,
					ScrInst = bs,
					WaitUntil = default,
					Coroutine = d.Coroutine,
					Level = sl,
					MsThread = d
				};

				if (bs != null)
					lt.Name = bs.GetFullName();

				Threads.AddLast(lt);
			}
			catch (Exception ex)
			{
				LastException = ex; // for the sake of overcomplification
				throw;
			}
		}
		public static void PrintOut(string msg)
		{
			LogManager.LogInfo(msg);
		}
		public static void PrintWarn(string msg)
		{
			LogManager.LogWarn(msg);
		}
		public static void PrintError(string msg)
		{
			LogManager.LogError(msg);
		}
		public static Table MakeInstanceTable(Instance inst, Script scr)
		{
			// i want to bulge out my eyes
			if (inst.LuaTable != null) return inst.LuaTable;

			var excs = NetworkManager.IsServer ? LuaSpace.ServerOnly : LuaSpace.ClientOnly;
			var type = inst.GetType();

			inst.LuaTable = new Table(scr)
			{
				MetaTable = new Table(scr)
			};

			var props = (IEnumerable<PropertyInfo?>)type.GetProperties();
			var meths = (IEnumerable<MethodInfo?>)type.GetMethods();

#pragma warning disable CS8602 // Dereference of a possibly null reference.

			props = from x in props where x.GetCustomAttribute<LuaAttribute>() != null ? (x.GetCustomAttribute<LuaAttribute>().Space == LuaSpace.Both || x.GetCustomAttribute<LuaAttribute>().Space == excs) : false select x;
			meths = from x in meths where x.GetCustomAttribute<LuaAttribute>() != null ? (x.GetCustomAttribute<LuaAttribute>().Space == LuaSpace.Both || x.GetCustomAttribute<LuaAttribute>().Space == excs) : false select x;

#pragma warning restore CS8602 // Dereference of a possibly null reference.

			inst.LuaTable.MetaTable["__index"] = DynValue.NewCallback((x, y) =>
			{
				var key = y[1].String;
				var prop = (from z in props where z.Name == key select z).FirstOrDefault();
				var meth = (from z in meths where z.Name == key select z).FirstOrDefault();

				if (prop != null)
				{
					if (prop.GetValue(inst) == null)
						return DynValue.Nil;

					if (!SerializationManager.LuaSerializers.TryGetValue(prop.PropertyType.FullName!, out var ls))
						return DynValue.Nil;

					var val = prop.GetValue(inst);

					if (val != null)
						return ls(val, scr);
					else
						return DynValue.Nil;
				}
				else
				{
					if (meth != null)
					{
						return DynValue.NewCallback((a, b) =>
						{
							try
							{
								var args = new List<object?>();
								var parms = meth.GetParameters();

								for (int i = 0; i < parms.Length; i++)
								{
									var info = parms[i];
									var t = info.ParameterType;

									if (!SerializationManager.LuaDeserializers.TryGetValue(t.FullName, out var ld))
										return DynValue.Nil;

									if (b[i + 1] == DynValue.Nil)
										args.Add(null);
									else
										args.Add(ld(b[i], scr));
								}

								var ret = meth!.Invoke(inst, args.ToArray());

								if (!SerializationManager.LuaSerializers.TryGetValue(meth.ReturnType.FullName, out var ls))
									return DynValue.Nil;

								if (ret != null)
									return ls(ret, scr);
								else
									return DynValue.Nil;
							}
							catch (TargetInvocationException)
							{
								throw new ScriptRuntimeException($"\"{meth.Name}\" doesn't accept one or more of parameters provided to it");
							}
						});
					}
					else
					{
						Instance[] children = inst.GetChildren();
						Instance? child = (from h in children where h.Name == key select h).FirstOrDefault();

						if (prop == null && meth == null && child == null)
							throw new ScriptRuntimeException($"\"{inst.GetType().Name}\" doesn't have a property, method or a child named \"{key}\"");

						return DynValue.NewTable(MakeInstanceTable(child, scr));
					}
				}
			});
			inst.LuaTable.MetaTable["__newindex"] = DynValue.NewCallback((x, y) =>
			{
				var key = y[1].String;
				var val = y[2];
				var prop = (from z in props where z.Name == key select z).FirstOrDefault();

				if (prop == null)
					throw new ScriptRuntimeException($"\"{type.Name}\" doesn't have a property named \"{key}\"");
				else
				{
					if (prop.CanWrite)
					{
						if (val == DynValue.Nil)
							prop.SetValue(inst!, null);
						else
						{
							if (!SerializationManager.LuaDeserializers.TryGetValue(prop.PropertyType.FullName, out var ld))
								return DynValue.Nil;
							else
							{
								var ret = ld(val, scr);
								var exc = SerializationManager.LuaDataTypes[prop.PropertyType.FullName];

								if (val.Type != exc)
									throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" only accepts {exc}");

								prop.SetValue(inst!, ret);
							}
						}
					}
					else
						throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" is read-only");
				}
				return DynValue.Nil;
			});
			inst.LuaTable.MetaTable["__tostring"] = DynValue.NewCallback((x, y) => DynValue.NewString(inst.ClassName));
			inst.LuaTable.MetaTable["__handle"] = inst.UniqueID.ToString();
			inst.LuaTable.MetaTable["__handleType"] = 0;

			return inst.LuaTable;
		}
	}
}
