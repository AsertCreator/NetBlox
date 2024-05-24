namespace NBRTTest
{
	using NetBlox.Runtime;
	internal class Program
	{
		internal static void Main(string[] args)
		{
			var m = new Machine();
			var f = new Function(m, [
				new Instruction() {
					Opcode = Opcode.Push2
				},
				new Instruction() {
					Opcode = Opcode.Push2
				},
				new Instruction() {
					Opcode = Opcode.Add
				},
				new Instruction() {
					Opcode = Opcode.Ret
				},
			]);
			var r = f.Invoke();
			Console.WriteLine(r);
		}
	}
}
