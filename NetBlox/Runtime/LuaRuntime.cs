using MoonSharp.Interpreter;
using MoonSharp.Interpreter.DataTypes;
using NetBlox.Common;
using NetBlox.Instances;
using NetBlox.Instances.Scripts;
using NetBlox.Instances.Services;
using NetBlox.Structs;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;
using Script = MoonSharp.Interpreter.Script;

namespace NetBlox.Runtime
{
	public static class LuaRuntime
	{
		public static Exception? LastException;
		public static int ScriptExecutionTimeout = 700;
		private readonly static Type DynValueType = typeof(DynValue);

		public static void Setup(GameManager gm)
		{
			Script tenv;
			tenv = new Script(
				CoreModules.Basic | CoreModules.Metatables | CoreModules.Bit32 | CoreModules.Coroutine |
				CoreModules.TableIterators | CoreModules.Table | CoreModules.String | CoreModules.ErrorHandling |
				CoreModules.Math | CoreModules.OS_Time | CoreModules.GlobalConsts);
			tenv.Globals.RegisterModuleType(typeof(TaskModule));
			gm.MainEnvironment = tenv;

			// heavy sandbox lol.
			// it based on scriptcontext leaked source code
			// dont tell anybody ok... shhhh...
			{
				tenv.Globals["_G"] = DynValue.NewTable(tenv);
				tenv.Globals["shared"] = DynValue.NewTable(tenv);
				tenv.Globals["_VERSION"] = gm.CurrentRoot.GetService<PlatformService>().FormatVersion();

				tenv.Globals["game"] = PushInstance(gm.CurrentRoot, gm);
				tenv.Globals["Game"] = tenv.Globals["game"];

				tenv.Globals["load"] = DynValue.Nil;
				tenv.Globals["loadfile"] = DynValue.Nil;
				tenv.Globals["dofile"] = DynValue.Nil;

				tenv.Globals["elapsedTime"] = DynValue.NewCallback((x, y) =>
				{
					return DynValue.NewNumber(new TimeSpan(AppManager.WhenStartedRunning.Ticks).TotalSeconds);
				});
				tenv.Globals["time"] = DynValue.NewCallback((x, y) => 
				{
					var rs = gm.CurrentRoot.GetService<RunService>();
					if (!gm.IsRunning) return DynValue.NewNumber(0);
					return DynValue.NewNumber((DateTime.UtcNow - rs.LastTimeStartedRunning).TotalSeconds);
				});
				tenv.Globals["tick"] = DynValue.NewCallback((x, y) => 
					DynValue.NewNumber(new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds));

				tenv.Globals["wait"] = DynValue.NewCallback((x, y) =>
				{
					var wa = y.Count == 0 ? DateTime.UtcNow : DateTime.UtcNow.AddSeconds(y[0].Number);
					TaskScheduler.CurrentJob.JobTimingContext.JoinedUntil = wa;
					return DynValue.NewYieldReq([]); // here we go to the next, bc thread is paused
				});
				tenv.Globals["delay"] = tenv.Globals["wait"];
				tenv.Globals["Wait"] = tenv.Globals["wait"];
				tenv.Globals["Delay"] = tenv.Globals["wait"];

				if (Debugger.IsAttached)
				{
					tenv.Globals["crash__"] = DynValue.NewCallback((x, y) =>
					{
						// that was way too... weird
						LogManager.LogError("NetBlox's crash__ function was called");
						Environment.FailFast("NetBlox's crash__ function was called");
						throw new ScriptRuntimeException("HAHAHA");
					});
					tenv.Globals["hang__"] = DynValue.NewCallback((x, y) =>
					{
						LogManager.LogError("NetBlox's hang__ function was called");
						while (true) ;
					});
				}

				tenv.Globals["version"] = DynValue.NewCallback((x, y) =>
					DynValue.NewString($"{Common.Version.VersionMajor}.{Common.Version.VersionMinor}.{Common.Version.VersionPatch}"));
				tenv.Globals["Version"] = tenv.Globals["version"];

				tenv.Globals["require"] = DynValue.NewCallback((x, y) =>
				{
					var input = y[0];

					if (input.Type == DataType.Table)
					{
						var module = input.Table.AssociatedObject as ModuleScript
							?? throw new ScriptRuntimeException("expected a ModuleScript");

						if (gm.LoadedModules.TryGetValue(module, out var dv)) return dv;

						var returnjob = TaskScheduler.CurrentJob;

						returnjob.JobTimingContext.JoinedTo = TaskScheduler.ScheduleScript(gm, module.Source, Security.Level, module, msjob =>
						{
							DynValue dv = msjob.ScriptJobContext.YieldAnswer;
							Debug.Assert(dv != null);

							gm.LoadedModules[module] = dv;

							returnjob.ScriptJobContext.YieldReturn = dv.Type == DataType.Tuple ? dv.Tuple : [dv];
							return JobResult.CompletedSuccess;
						});

						return DynValue.NewYieldReq([]);
					}
					else if (input.Type == DataType.Number)
					{
						throw new ScriptRuntimeException("cannot load assets yet");
					}

					throw new ScriptRuntimeException("expected asset id or ModuleScript to be passed to require");
				});
				tenv.Globals["loadstring"] = DynValue.NewCallback((x, y) =>
				{
					if (!gm.CurrentRoot.GetService<ScriptContext>().LoadStringEnabled)
						throw new ScriptRuntimeException("loadstring is not accessible");
					return tenv.LoadString(y[0].CastToString());
				});
				tenv.Globals["printidentity"] = DynValue.NewCallback((x, y) =>
				{
					if (y.Count != 0)
					{
						string prefix = y.AsStringUsingMeta(x, 0, "printidentity");
						PrintOut(prefix + " " + Security.Level);
					}
					else
						PrintOut("Current identity is " + Security.Level);
					return DynValue.Void;
				});
				tenv.Globals["print"] = DynValue.NewCallback((x, y) =>
				{
					List<string> strs = [];
					for (int i = 0; i < y.Count; i++)
						strs.Add(y.AsStringUsingMeta(x, i, "print"));
					PrintOut(string.Join(' ', strs));
					return DynValue.Void;
				});
				tenv.Globals["warn"] = DynValue.NewCallback((x, y) =>
				{
					List<string> strs = [];
					for (int i = 0; i < y.Count; i++)
						strs.Add(y.AsStringUsingMeta(x, i, "warn"));
					PrintWarn(string.Join(' ', strs));
					return DynValue.Void;
				});
				tenv.Globals["error"] = DynValue.NewCallback((x, y) =>
				{
					List<string> strs = [];
					for (int i = 0; i < y.Count; i++)
						strs.Add(y.AsStringUsingMeta(x, i, "error"));
					PrintError(string.Join(' ', strs));
					throw new ScriptRuntimeException(y[0].ToString());
				});
				tenv.Globals["spawn"] = DynValue.NewCallback((x, y) =>
				{
					TaskScheduler.ScheduleScript(gm, y[0], Security.Level, null);
					return DynValue.Void;
				});
			}

			MakeDataType(gm, "Instance", (x, y) =>
			{
				try
				{
					var key = y[0].CastToString();
					var inst = InstanceCreator.CreateAccessibleInstanceIfExists(key, gm)
						?? throw new ScriptRuntimeException("Unable to create Instance of type " + key);
					if (y.Count > 1)
					{
						var part = y[1];
						var parent = part.Table.AssociatedObject;
						part.Table.RequireType(MoonSharp.Interpreter.DataTypes.AssociatedObjectType.Instance, 1, "Instance.new");
						inst.Parent = (Instance)parent;
					}
					return PushInstance(inst, gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "UDim2", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["NetBlox.Structs.UDim2"]
						(new UDim2(
							Convert.ToSingle(y[0].Number), 
							Convert.ToSingle(y[1].Number), 
							Convert.ToSingle(y[2].Number), 
							Convert.ToSingle(y[3].Number)), gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "Color3", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["Raylib_cs.Color"]
						(new Color(
							(int)((double)(y[0].Number * 255)),
							(int)((double)(y[1].Number * 255)),
							(int)((double)(y[2].Number * 255)),
							255), gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "Vector2", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["System.Numerics.Vector2"]
						(new Vector2(
							(float)((double)y[0].Number),
							(float)((double)y[1].Number)), gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "Vector3", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["System.Numerics.Vector3"]
						(new Vector3(
							(float)((double)y[0].Number),
							(float)((double)y[1].Number),
							(float)((double)y[2].Number)), gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			MakeDataType(gm, "BrickColor", (x, y) =>
			{
				try
				{
					return SerializationManager.LuaSerializers["NetBlox.Structs.BrickColor"](BrickColor.ByIndex((int)y[0].Number)!, gm);
				}
				catch
				{
					return DynValue.Void;
				}
			});
			LuaTypes.ImportAll(tenv.Globals);
		}
		public static void MakeDataType(GameManager gm, string name, Func<ScriptExecutionContext, CallbackArguments, DynValue> func)
		{
			Table it = new(gm.MainEnvironment);
			it["new"] = DynValue.NewCallback(func);
			gm.MainEnvironment.Globals[name] = DynValue.NewTable(it);
		}
		public static Table ShallowCloneTable(Table t)
		{
			Table ta = new(t.OwnerScript);
			t.Pairs.ToList().ForEach(x => ta.Set(x.Key, x.Value));
			return ta;
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
		public static DynValue PushInstance(Instance? targetInstanceIWantToForget, GameManager gm)
		{
			var scr = gm.MainEnvironment;

			if (targetInstanceIWantToForget == null)
				return DynValue.Nil;

			// i want to bulge out my eyes
			if (targetInstanceIWantToForget.Table != null) return DynValue.NewTable(targetInstanceIWantToForget.Table);

			var type = targetInstanceIWantToForget.GetType();
			var chash = targetInstanceIWantToForget.ClassName.GetHashCode();

			if (Instance.MetaTables.TryGetValue(chash, out Table meta))
			{
				var table = new Table(scr)
				{
					MetaTable = meta,
					AssociatedObject = targetInstanceIWantToForget,
					IsProtected = true
				};
				targetInstanceIWantToForget.Table = table;
				return DynValue.NewTable(table);
			}
			else
			{
				meta = new Table(scr);

				var props = type.GetProperties().Where(x => x.GetCustomAttribute<LuaAttribute>() != null).ToList();
				var meths = type.GetMethods().Where(x => x.GetCustomAttribute<LuaAttribute>() != null).ToList();

				meta["__index"] = DynValue.NewCallback((x, y) =>
				{
					var key = y[1].String;
					var prop = props.Find(x => x.Name == key);
					var meth = meths.Find(x => x.Name == key);
					var inst = (y[0].Table.AssociatedObject as Instance)!;

					if (prop != null)
					{
						var val = prop.GetValue(inst);

						if (val != null && SerializationManager.LuaSerializers.TryGetValue(prop.PropertyType.FullName!, out var ls))
							return ls(val, gm);
						else
							return DynValue.Nil;
					}
					else if (meth != null)
					{
						var sec = meth.GetCustomAttribute<LuaAttribute>()!;
						var parms = meth.GetParameters();

						Security.Require(meth.Name, sec.Capabilities);

						return DynValue.NewCallback((a, b) =>
						{
							try
							{
								var args = new List<object?>();

								if (b[0].Type != DataType.Table || (b[0].Type == DataType.Table && !b[0].Table.IsProtected))
									throw new ScriptRuntimeException("Instance functions must be called using semicolon operator");

								for (int i = 0; i < parms.Length; i++)
								{
									var parinfo = parms[i];
									var partype = parinfo.ParameterType;

									if (partype != DynValueType)
									{
										if (!SerializationManager.LuaDeserializers.TryGetValue(partype.FullName ?? "", out var ld))
											return DynValue.Nil;

										if (b[i + 1].IsNil())
											args.Add(null);
										else
											args.Add(ld(b[i + 1], gm));
									}
									else
										args.Add(b[i + 1]);
								}

								var ret = meth!.Invoke(inst, [.. args]);
								var rett = meth.ReturnType;

								if (ret is LuaYield)
									return DynValue.NewYieldReq([]); // do it immediately
								if (ret is DynValue value)
									return value;

								if (!rett.IsArray)
								{
									if (ret != null && SerializationManager.LuaSerializers.TryGetValue(rett.FullName ?? "", out var ls))
										return ls(ret, gm);
									else
										return DynValue.Nil;
								}
								else
								{
									Table res = new(scr);
									Array arr = (ret as Array)!;
									Type? elt = rett.GetElementType();

									if (elt == null) return DynValue.Nil;

									if (SerializationManager.LuaSerializers.TryGetValue(elt.FullName ?? "", out var ls))
									{
										for (int i = 0; i < arr.Length; i++)
										{
											var val = ls(arr.GetValue(i)!, gm); // i hate you vs debugger for fuck sake help
											res[i + 1] = val;
										}
									}

									return DynValue.NewTable(res);
								}
							}
							catch (TargetInvocationException ex)
							{
								if (ex.InnerException != null)
									throw ex.InnerException;
								throw new ScriptRuntimeException($"\"{meth.Name}\" doesn't accept one or more of parameters provided to it");
							}
						});
					}
					else
					{
						Instance? child = inst.FindFirstChild(key);

						return child == null
							? throw new ScriptRuntimeException($"\"{inst.GetType().Name}\" doesn't have a property, method or a child named \"{key}\"")
							: PushInstance(child, gm);
					}
				});
				meta["__newindex"] = DynValue.NewCallback((x, y) =>
				{
					var inst = (y[0].Table.AssociatedObject as Instance)!;
					var key = y[1].String;
					var val = y[2];
					var prop = props.Find(x => x.Name == key)
						?? throw new ScriptRuntimeException($"\"{type.Name}\" doesn't have a property named \"{key}\"");

					if (!prop.CanWrite)
						throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" is read-only");

					if (val.IsNil())
						prop.SetValue(inst!, null);
					else
					{
						if (SerializationManager.LuaDeserializers.TryGetValue(prop.PropertyType.FullName ?? "", out var ld))
						{
							var ret = ld(val, gm);
							var exc = SerializationManager.LuaDataTypes[prop.PropertyType.FullName ?? ""];

							if (val.Type != exc)
								throw new ScriptRuntimeException($"Property \"{key}\" of \"{type.Name}\" only accepts {exc}");

							prop.SetValue(inst!, ret);

							inst.Changed.Fire(DynValue.NewString(key));

							if (inst.ChangedSignals.TryGetValue(key, out LuaSignal? value))
								value.Fire(val);

							if (gm.NetworkManager.IsServer || inst.IsDomestic)
								gm.NetworkManager.AddReplication(inst, 
									NetworkManager.Replication.REPM_TOALL, 
									NetworkManager.Replication.REPW_PROPCHG, false);
						}
					}

					return DynValue.Nil;
				});
				meta["__tostring"] = DynValue.NewCallback((x, y) => DynValue.NewString((y[0].Table.AssociatedObject as Instance)!.Name));
				meta.IsProtected = true;

				Instance.MetaTables[chash] = meta;
				var table = new Table(scr)
				{
					MetaTable = meta,
					AssociatedObject = targetInstanceIWantToForget,
					ObjectType = MoonSharp.Interpreter.DataTypes.AssociatedObjectType.Instance,
					IsProtected = true
				};
				targetInstanceIWantToForget.Table = table;
				return DynValue.NewTable(table);
			}
		}
	}
}
