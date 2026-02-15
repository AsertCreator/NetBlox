using MoonSharp.Interpreter;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances.Callables
{
    [Creatable]
    public class BindableEvent : Instance
    {
		[Lua([Security.Capability.None])]
		public LuaSignal Event { get; private set; }

        public BindableEvent(GameManager ins) : base(ins) 
		{
			Event = new LuaSignal(ins);
		}

        [Lua([Security.Capability.None])]
        public void Fire([TupleArgument] DynValue tuple)
        {
            Event.Fire(tuple);
        }
        [Lua([Security.Capability.None])]
        public override bool IsA(string classname)
        {
            if (nameof(BindableEvent) == classname) return true;
            return base.IsA(classname);
        }
    }
}
