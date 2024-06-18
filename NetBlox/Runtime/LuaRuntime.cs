using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime
{
	public class LuaThread
	{
		public int Level;
		public GameManager GameManager;
		public DynValue MsThread;
		public Coroutine Coroutine;
		public DynValue[] StartArgs;
		public Script Script;
		public BaseScript? ScrInst;
		public DateTime WaitUntil;
		public Action<DynValue>? FinishCallback;
		public string Name = string.Empty;

		public LuaThread(GameManager gm, DataModel dm, BaseScript? bs, DynValue d, int sl, DynValue[]? dv = null, Action<DynValue>? fc = null)
		{
			GameManager = gm;
			Script = dm.MainEnv;
			ScrInst = bs;
			WaitUntil = default;
			Coroutine = d.Coroutine;
			Level = sl;
			MsThread = d;
			FinishCallback = fc;
			StartArgs = dv ?? [];
		}
	}
	public static class LuaRuntime
	{
		public static LinkedListNode<LuaThread>? CurrentThread;
		public static LinkedList<LuaThread> Threads = new();
		public static Exception? LastException;
		public static int ScriptExecutionTimeout = 700;
		private static Type dvt = typeof(DynValue);
		private static bool init = false;

		public static void Setup(GameManager gm, DataModel dm)
		{
			var works = dm.GetService<Workspace>(true);

			dm.MainEnv = new Script(
				CoreModules.Basic | CoreModules.Metatables | CoreModules.Bit32 | CoreModules.Coroutine |
				CoreModules.TableIterators | CoreModules.Table | CoreModules.String | CoreModules.ErrorHandling |
				CoreModules.Math | CoreModules.OS_Time | CoreModules.GlobalConsts);

			dm.MainEnv.Globals["game"] = MakeInstanceTable(dm, gm);
			if (works != null)
				dm.MainEnv.Globals["workspace"] = MakeInstanceTable(works, gm);

			dm.MainEnv.Globals["wait"] = DynValue.NewCallback((x, y) =>
			{
				var wa = y.Count == 0 ? DateTime.Now : DateTime.Now.AddSeconds(y[0].Number);
				GetThreadFor(x.GetCallingCoroutine()).WaitUntil = wa;
				return DynValue.NewYieldReq(y.GetArray()); // here we go to the next, bc thread is paused
			});
			dm.MainEnv.Globals["require"] = DynValue.NewCallback((x, y) =>
			{
				var table = y[0];
				var inst = SerializationManager.LuaDeserialize<Instance>(table, gm);

				if (inst is not ModuleScript)
					throw new Exception("Expected a ModuleScript");

				var ms = inst as ModuleScript;
				if (gm.CurrentRoot.LoadedModules.TryGetValue(ms, out var dv)) return dv;

				var on = CurrentThread.Value.Name;
				CurrentThread.Value.Name = ms.GetFullName();
				var cl = ImmediateExecute(ms.Source, gm, ms);
				gm.CurrentRoot.LoadedModules[ms] = cl;
				ms.DoneModulating = true;
				ms.DynValue = cl;
				CurrentThread.Value.Name = on;
				return cl;
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

			MakeDataType(dm, "Instance", (x, y) =>
			{
				try
				{
					var inst = InstanceCreator.CreateAccessibleInstance(y[0].String, gm);
					return DynValue.NewTable(MakeInstanceTable(inst, gm));
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(dm, "UDim2", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["NetBlox.Structs.UDim2"]
						(new UDim2(
							Convert.ToSingle(y[0].Number), 
							Convert.ToSingle(y[1].Number), 
							Convert.ToSingle(y[2].Number), 
							Convert.ToSingle(y[3].Number)), dm.GameManager);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(dm, "Color3", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["Raylib_cs.Color"]
						(new Color(
							Convert.ToInt32(y[0].Number * 255),
							Convert.ToInt32(y[1].Number * 255),
							Convert.ToInt32(y[2].Number * 255),
							255), dm.GameManager);
				}
				catch
				{
					return DynValue.Void;
				}
			});

			Execute(string.Empty, 0, dm.GameManager, null); // we will run nothing to initialize lua
		}
		public static void MakeDataType(DataModel dm, string name, Func<ScriptExecutionContext, CallbackArguments, DynValue> func)
		{
			var it = new Table(dm.MainEnv);
			it["new"] = DynValue.NewCallback(func);
			dm.MainEnv.Globals[name] = DynValue.NewTable(it);
		}
		public static LuaThread GetThreadFor(Coroutine c)
		{
			return (from x in Threads where x.Coroutine == c select x).First();
		}
		public static void Execute(string code, int sl, GameManager gm, BaseScript? bs, Action<DynValue>? fc = null)
		{
			try
			{
				var d = gm.CurrentRoot.MainEnv.CreateCoroutine(gm.CurrentRoot.MainEnv.LoadString(code));
				var lt = new LuaThread(gm, gm.CurrentRoot, bs, d, sl, [], fc);

				d.Coroutine.AutoYieldCounter = 100000;

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
		public static void Execute(DynValue fun, int sl, GameManager gm, BaseScript? bs, DynValue[]? dva, Action<DynValue>? fc = null)
		{
			try
			{
				var d = gm.CurrentRoot.MainEnv.CreateCoroutine(fun);
				var lt = new LuaThread(gm, gm.CurrentRoot, bs, d, sl, dva, fc);

				d.Coroutine.AutoYieldCounter = 100000;

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
		public static DynValue ImmediateExecute(string code, GameManager gm, BaseScript? bs)
		{
			try
			{
				var g = ShallowCloneTable(gm.CurrentRoot.MainEnv.Globals);
				g["script"] = MakeInstanceTable(bs, bs.GameManager);
				var v = gm.CurrentRoot.MainEnv.DoString(code, g);
				return v;
			}
			catch (Exception ex)
			{
				LastException = ex; // for the sake of overcomplification
				throw;
			}
		}
		public static Table ShallowCloneTable(Table t)
		{
			Table ta = new Table(t.OwnerScript);
			t.Pairs.ToList().ForEach(x => ta.Set(x.Key, x.Value));
			return ta;
		}
		public static void ReportedExecute(Action ac, string name, bool remthread)
		{
			try
			{
				ac();
			}
			catch (ScriptRuntimeException ex)
			{
				var ct = CurrentThread;
				{
					LogManager.LogError(ex.Message);
					for (int i = 0; i < ex.CallStack.Count; i++)
						LogManager.LogError($"    at {ct.Value.Name}:{((ex.CallStack[i].Location != null) ? ex.CallStack[i].Location.FromLine.ToString() : "(unknown)")}");
				}

				if (remthread)
					if (Threads.Contains(CurrentThread.Value))
						Threads.Remove(CurrentThread);
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
		public static Table MakeInstanceTable(Instance inst, GameManager gm)
		{
			var scr = gm.CurrentRoot.MainEnv;

			// i want to bulge out my eyes
			if (inst.Tables.TryGetValue(scr, out Table t)) return t;

			var excs = gm.NetworkManager.IsServer ? LuaSpace.ServerOnly : LuaSpace.ClientOnly;
			var type = inst.GetType();

			var table = new Table(scr)
			{
				MetaTable = new Table(scr)
			};

			var props = (IEnumerable<PropertyInfo?>)type.GetProperties();
			var meths = (IEnumerable<MethodInfo?>)type.GetMethods();

#pragma warning disable CS8602 // Dereference of a possibly null reference.

			props = from x in props where x.GetCustomAttribute<LuaAttribute>() != null ? (x.GetCustomAttribute<LuaAttribute>().Space == LuaSpace.Both || x.GetCustomAttribute<LuaAttribute>().Space == excs) : false select x;
			meths = from x in meths where x.GetCustomAttribute<LuaAttribute>() != null ? (x.GetCustomAttribute<LuaAttribute>().Space == LuaSpace.Both || x.GetCustomAttribute<LuaAttribute>().Space == excs) : false select x;

#pragma warning restore CS8602 // Dereference of a possibly null reference.

			table.MetaTable["__index"] = DynValue.NewCallback((x, y) =>
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
						return ls(val, gm);
					else
						return DynValue.Nil;
				}
				else
				{
					if (meth != null)
					{
						var sec = meth.GetCustomAttribute<LuaAttribute>();

						if (Security.IsCompatible(CurrentThread.Value.Level, sec.Capabilities))
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

										if (t != dvt)
										{
											if (!SerializationManager.LuaDeserializers.TryGetValue(t.FullName, out var ld))
												return DynValue.Nil;

											if (b[i + 1] == DynValue.Nil)
												args.Add(null);
											else
												args.Add(ld(b[i + 1], gm));
										}
										else
											args.Add(b[i + 1]);
									}

									var ret = meth!.Invoke(inst, args.ToArray());
									var rett = meth.ReturnType;

									if (meth.ReturnType.Name == "LuaYield")
									{
										var hasr = (bool)meth.ReturnType.GetField("HasResult")!.GetValue(ret)!; // idc lol
										if (!hasr)
											return DynValue.NewYieldReq(y.GetArray());
										else
											rett = meth.ReturnType.GetGenericArguments()[0];
									}
									if (!rett.IsArray)
									{
										if (!SerializationManager.LuaSerializers.TryGetValue(rett.FullName, out var ls))
											return DynValue.Nil;

										if (ret != null)
											return ls(ret, gm);
										else
											return DynValue.Nil;
									}
									else
									{
										Table res = new(scr);
										Array arr = (ret as Array)!;

										if (SerializationManager.LuaSerializers.TryGetValue(rett.GetElementType().FullName, out var ls))
										{
											for (int i = 0; i < arr.Length; i++)
											{
												var val = ls(arr.GetValue(i)!, gm); // i hate you vs debugger for fuck sake help
												res[i] = val;
											}
										}

										return DynValue.NewTable(res);
									}
								}
								catch (TargetInvocationException ex)
								{
									throw new ScriptRuntimeException($"\"{meth.Name}\" doesn't accept one or more of parameters provided to it");
								}
							});
						else
							throw new Exception($"\"{meth.Name}\" is not accessible");
					}
					else
					{
						Instance[] children = inst.GetChildren();
						Instance? child = (from h in children where h.Name == key select h).FirstOrDefault();

						if (prop == null && meth == null && child == null)
							throw new ScriptRuntimeException($"\"{inst.GetType().Name}\" doesn't have a property, method or a child named \"{key}\"");

						return DynValue.NewTable(MakeInstanceTable(child, gm));
					}
				}
			});
			table.MetaTable["__newindex"] = DynValue.NewCallback((x, y) =>
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
							if (prop.Name == "Parent")
							{
								if (val.Type != DataType.Table)
									throw new ScriptRuntimeException($"Property \"Parent\" of \"{type.Name}\" only accepts Instance");
								if (val.Table.MetaTable == null)
									throw new ScriptRuntimeException($"Property \"Parent\" of \"{type.Name}\" only accepts Instance");
								if (val.Table.MetaTable["__handle"] == null)
									throw new ScriptRuntimeException($"Property \"Parent\" of \"{type.Name}\" only accepts Instance");

								if (Guid.TryParse(val.Table.MetaTable["__handle"].ToString(), out Guid uid))
								{
									prop.SetValue(inst!, gm.AllInstances.Find(x => x.UniqueID == uid));
									if (gm.NetworkManager.IsServer)
										gm.NetworkManager.AddReplication(inst, NetworkManager.Replication.REPM_BUTOWNER, NetworkManager.Replication.REPW_REPARNT, false);
								}
								else
									throw new ScriptRuntimeException($"Attempted to assign Instance's parent to foreign Instance");
							}
							else
							{
								if (!SerializationManager.LuaDeserializers.TryGetValue(prop.PropertyType.FullName, out var ld))
									return DynValue.Nil;
								else
								{
									var ret = ld(val, gm);
									var exc = SerializationManager.LuaDataTypes[prop.PropertyType.FullName];

									if (val.Type != exc)
										throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" only accepts {exc}");

									prop.SetValue(inst!, ret);
									if (gm.NetworkManager.IsServer)
										gm.NetworkManager.AddReplication(inst, NetworkManager.Replication.REPM_TOALL, NetworkManager.Replication.REPW_PROPCHG, false);
								}
							}
						}
					}
					else
						throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" is read-only");
				}
				return DynValue.Nil;
			});
			table.MetaTable["__tostring"] = DynValue.NewCallback((x, y) => DynValue.NewString(inst.Name));
			table.MetaTable["__handle"] = inst.UniqueID.ToString();
			table.MetaTable["__handleType"] = 0;

			inst.Tables[scr] = table;

			return table;
		}
	}
}
