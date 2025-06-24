using Raylib_cs;

namespace NetBlox
{
	public enum HumanoidControlType
	{
		Forward, Backward, WalkLeft, WalkRight, Jump
	}
	public class HumanoidControl
	{
		public KeyboardKey[] Keys;
		public string FriendlyName;
		public HumanoidControlType Type;

		public static List<HumanoidControl> AllControls = [];

		static HumanoidControl()
		{
			AllControls.Add(new()
			{
				Keys = [KeyboardKey.W, KeyboardKey.Up],
				FriendlyName = "Walk forward",
				Type = HumanoidControlType.Forward
			});
			AllControls.Add(new()
			{
				Keys = [KeyboardKey.S, KeyboardKey.Down],
				FriendlyName = "Walk backward",
				Type = HumanoidControlType.Backward
			});
			AllControls.Add(new()
			{
				Keys = [KeyboardKey.A],
				FriendlyName = "Walk to the left",
				Type = HumanoidControlType.WalkLeft
			});
			AllControls.Add(new()
			{
				Keys = [KeyboardKey.D],
				FriendlyName = "Walk to the right",
				Type = HumanoidControlType.WalkRight
			});
			AllControls.Add(new()
			{
				Keys = [KeyboardKey.Space],
				FriendlyName = "Jump or stand up",
				Type = HumanoidControlType.Jump
			});
		}

		public static HumanoidControl? GetFor(HumanoidControlType type) => AllControls.Find(x => x.Type == type);

		public bool IsPressed()
		{
			for (int i = 0; i < Keys.Length; i++)
			{
				bool isKeyPressed = Raylib.IsKeyDown(Keys[i]);
				if (isKeyPressed)
					return true;
			}
			return false;
		}
	}
}
