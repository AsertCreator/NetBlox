using MoonSharp.Interpreter;
using NetBlox.Instances.Scripts;
using System.Diagnostics;
using System.Xml.Linq;

namespace NetBlox.Runtime
{
	public class LuaSignal
	{
		public GameManager GameManager;
		public List<LuaConnection> Attached = [];
		public List<Action<DynValue[]>> NativeAttached = [];
		public ulong FireCount = 0;

		public LuaSignal(GameManager gameManager)
		{
			GameManager = gameManager;
		}

		[Lua([Security.Capability.None])]
		public LuaConnection? Connect(DynValue dv)
		{
			if (TaskScheduler.CurrentJob == null)
				return null;

			if (dv.Type != DataType.Function && dv.Type != DataType.ClrFunction)
			{
				throw new Exception("Cannot connect a non-function to a LuaSignal");
			}

			lock (this) 
			{
				if (TaskScheduler.CurrentJob.ScriptJobContext.BaseScript == null)
					return null;

				var connection = new LuaConnection(this, TaskScheduler.CurrentJob.ScriptJobContext.BaseScript)
				{
					Function = dv,
					Level = TaskScheduler.CurrentJob.SecurityLevel
				};

				Attached.Add(connection);

				return connection;
			}
		}
		[Lua([Security.Capability.None])]
		public LuaYield Wait()
		{
			var c = FireCount;
			var job = TaskScheduler.CurrentJob;

			job.JobTimingContext.TaskJoinedTo = Task.Run(async() =>
			{
				while (!GameManager.ShuttingDown)
				{
					if (c == FireCount)
						await Task.Yield();
					else
					{
						job.ScriptJobContext.YieldReturn = [DynValue.Void];
						return;
					}
				}
			});

			return new();
		}
		public void Fire(params DynValue[] dvs)
		{
			Action<DynValue[]>[]? nativeconnections = null;
			LuaConnection[]? luaconnections = null;

			lock (this)
			{
				nativeconnections = new Action<DynValue[]>[NativeAttached.Count];
				NativeAttached.CopyTo(nativeconnections, 0);

				luaconnections = new LuaConnection[Attached.Count];
				Attached.CopyTo(luaconnections, 0);

				FireCount++;
			}

			for (int i = 0; i < nativeconnections.Length; i++)
				nativeconnections[i](dvs);

			for (int i = 0; i < luaconnections.Length; i++)
			{
				if (Attached[i].Manager == null) continue;
				if (Attached[i].Function == null) continue;

				TaskScheduler.ScheduleScript(Attached[i].Manager, Attached[i].Function, Attached[i].Level, Attached[i].Script)
					.ScriptJobContext.YieldReturn = dvs;
			}
		}
		public void Disconnect(LuaConnection luaConnection)
		{
			Attached.Remove(luaConnection);
		}
	}
	public class LuaConnection
	{
		public LuaSignal Origin;
		public GameManager Manager;
		public BaseScript Script;
		public DynValue? Function;
		public int Level;

		public LuaConnection(LuaSignal signal, BaseScript bs)
		{
			Script = bs;
			Manager = bs.GameManager;

			Script.Destroying.NativeAttached.Add(_ =>
			{
				Origin.Disconnect(this);
			});
		}
	}
}
