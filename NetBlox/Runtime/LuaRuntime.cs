using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using System.Reflection;
using System.Runtime;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime
{
    public static class LuaRuntime
	{
		public static Dictionary<ModuleScript, Table> LoadedModules;
		public static int ScriptExecutionTimeout = 7;

		public static void RunScript(string code, bool roblox, Instance? container, int security, bool wait)
		{
			try
			{
				CancellationTokenSource s = new();

				// maybe i shouldn't put this on a plain sight
				var t = Task.Run(() =>
				{
#pragma warning disable SYSLIB0046 // Type or member is obsolete
					ControlledExecution.Run(() =>
					{
						var scr = new Script(
							CoreModules.Basic | CoreModules.Metatables | CoreModules.Bit32 |
							CoreModules.TableIterators | CoreModules.String | CoreModules.ErrorHandling |
							CoreModules.Math | CoreModules.OS_Time | CoreModules.GlobalConsts);

						if (roblox)
						{
							var works = GameManager.GetService<Workspace>();

							scr.Globals["game"] = MakeInstanceTable(GameManager.CurrentRoot, scr);

							if (works != null)
								scr.Globals["workspace"] = MakeInstanceTable(works, scr);

							if (container != null)
								scr.Globals["script"] = MakeInstanceTable(container, scr);

							scr.Globals["wait"] = DynValue.NewCallback((x, y) =>
							{
								Thread.Sleep((int)(y[0].Number * 1000));
								return DynValue.Void;
							});
							scr.Globals["require"] = DynValue.NewCallback((x, y) =>
							{
								var table = y[0];
								var inst = SerializationManager.LuaDeserialize<Instance>(table, x.OwnerScript);

								throw new NotImplementedException();
							});
							scr.Globals["printidentity"] = DynValue.NewCallback((x, y) =>
							{
								PrintOut("Current identity is " + security);
								return DynValue.Void;
							});
							scr.Globals["print"] = DynValue.NewCallback((x, y) =>
							{
								PrintOut(y[0].ToString());
								return DynValue.Void;
							});
							scr.Globals["warn"] = DynValue.NewCallback((x, y) =>
							{
								PrintWarn(y[0].ToString());
								return DynValue.Void;
							});
							scr.Globals["error"] = DynValue.NewCallback((x, y) =>
							{
								PrintError(y[0].ToString());
								throw new Exception(y[0].ToString());
							});
							scr.Globals[""] = DynValue.NewCallback((x, y) =>
							{
								PrintError(y[0].ToString());
								throw new Exception(y[0].ToString());
							});
						}

						scr.DoString(code);
					}, s.Token);
#pragma warning restore SYSLIB0046 // Type or member is obsolete
				});
				if (wait)
				{
					t.Wait(ScriptExecutionTimeout * 1000);
					if (!t.IsCompleted) // drop the nuclear bomb
					{
						s.Cancel();
						LogManager.LogError("Exhausted maximum script execution time");
					}
				}
			}
			catch (Exception e)
			{
				PrintError(e.Message);
				LogManager.LogError("Script error: " + e.Message);
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

			var excs = AppManager.IsServer ? LuaSpace.ServerOnly : LuaSpace.ClientOnly;
			var type = inst.GetType();

			var tbl = new Table(scr);
			tbl.MetaTable = new Table(scr);

			var props = (IEnumerable<PropertyInfo?>)type.GetProperties();
			var meths = (IEnumerable<MethodInfo?>)type.GetMethods();

#pragma warning disable CS8602 // Dereference of a possibly null reference.

			props = from x in props where x.GetCustomAttribute<LuaAttribute>() != null ? (x.GetCustomAttribute<LuaAttribute>().Space == LuaSpace.Both || x.GetCustomAttribute<LuaAttribute>().Space == excs) : false select x;
			meths = from x in meths where x.GetCustomAttribute<LuaAttribute>() != null ? (x.GetCustomAttribute<LuaAttribute>().Space == LuaSpace.Both || x.GetCustomAttribute<LuaAttribute>().Space == excs) : false select x;

#pragma warning restore CS8602 // Dereference of a possibly null reference.

			tbl.MetaTable["__index"] = DynValue.NewCallback((x, y) =>
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

									if (!SerializationManager.LuaDeserializers.TryGetValue(prop.PropertyType.FullName, out var ld))
										return DynValue.Nil;

									if (b[i] == DynValue.Nil)
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
			tbl.MetaTable["__newindex"] = DynValue.NewCallback((x, y) =>
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
			tbl.MetaTable["__tostring"] = DynValue.NewCallback((x, y) => DynValue.NewString(inst.ClassName));
			tbl.MetaTable["__handle"] = inst.UniqueID.ToString();
			tbl.MetaTable["__handleType"] = 0;

			return tbl;
		}
	}
}
