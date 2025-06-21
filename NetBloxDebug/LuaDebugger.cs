using MoonSharp.Interpreter.Debugging;
using MoonSharp.Interpreter;

namespace NetBloxDebug
{
	public class LuaDebugger : IDebugger
	{
		public Dictionary<string, DynValue> CurrentWatches = [];
		public List<SourceCode> Sources = [];
		public List<SourceRef> Breakpoints = [];
		public DebugService DebugService;
		public bool Paused;
		public static DebuggerAction NoAction = new DebuggerAction() { Action = DebuggerAction.ActionType.None };

		public DebuggerAction GetAction(int ip, SourceRef sourceref)
		{
			if (Breakpoints.Contains(sourceref))
				return new DebuggerAction() 
				{ 
					Action = DebuggerAction.ActionType.SetBreakpoint, 
					SourceLine = sourceref.FromLine,
					SourceCol = sourceref.FromChar,
					SourceID = sourceref.SourceIdx
				};

			return NoAction;
		}
		public DebuggerCaps GetDebuggerCaps()
		{
			return DebuggerCaps.HasLineBasedBreakpoints | DebuggerCaps.CanDebugSourceCode;
		}
		public List<DynamicExpression> GetWatchItems() => 
			CurrentWatches.Keys.Select(x => DebugService.OwnerScript.CreateDynamicExpression(x)).ToList();
		public bool IsPauseRequested() => Paused;
		public void SetDebugService(DebugService debugService) => DebugService = debugService;
		public void SetSourceCode(SourceCode sourceCode) => Sources.Add(sourceCode);
		public bool SignalRuntimeException(ScriptRuntimeException ex) => true;
		public void Update(WatchType watchType, IEnumerable<WatchItem> items)
		{
			if (watchType == WatchType.Watches)
			{
				var count = items.Count();

				for (int i = 0; i < count; i++)
				{
					var item = items.ElementAt(i);

					CurrentWatches[item.Name] = item.Value;
				}
			}
		}

		public void SetByteCode(string[] byteCode)
		{ }
		public void SignalExecutionEnded()
		{ }
		public void RefreshBreakpoints(IEnumerable<SourceRef> refs) 
		{ }
	}
}
