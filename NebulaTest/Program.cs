using MoonSharp.Interpreter;

namespace NebulaTest
{
	internal static class Program
	{
		internal static void Main(string[] args)
		{
			bool running = true;
			Script script = new Script(CoreModules.Preset_Complete);
			script.Options.DebugPrint = Console.WriteLine;
			script.Options.DebugInput = x => { Console.Write(x + " "); return Console.ReadLine(); };
			script.Globals["exit"] = () =>
			{
				running = false;
			};

			Console.WriteLine("Type Nebula/Lua code to run:");
			while (running)
			{
				try
				{
					Console.Write(">> ");
					string code = Console.ReadLine();
					script.DoString(code);
				}
				catch (SyntaxErrorException ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Script compile error: " + ex.Message);
					Console.ResetColor();
				}
				catch (ScriptRuntimeException ex)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Script runtime error: " + ex.DecoratedMessage);
					Console.ResetColor();
				}
			}
		}
	}
}
