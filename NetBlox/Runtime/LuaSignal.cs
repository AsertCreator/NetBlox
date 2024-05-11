using MoonSharp.Interpreter;

namespace NetBlox.Runtime
{
	public class LuaSignal
	{
		public List<DynValue> Attached = new();
		public bool HasFired = false;

		[Lua([Security.Capability.None])]
		public void Connect(DynValue dv)
		{
			Attached.Add(dv);
		}
		[Lua([Security.Capability.None])]
		public void Wait()
		{
			while (!HasFired) ;
		}
		public void Fire()
		{
			Task.Run(() =>
			{
				HasFired = true;
				for (int i = 0; i < Attached.Count; i++)
				{
					Attached[i].Function.Call();
				}
			});
		}
	}
}
