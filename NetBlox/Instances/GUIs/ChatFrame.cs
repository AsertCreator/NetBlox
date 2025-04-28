using NetBlox.Instances.Services;
using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances.GUIs
{
	public class ChatFrame : Frame
	{
		public ChatFrame(GameManager ins) : base(ins) { }

		public override void RenderGUI(Vector2 cp, Vector2 cs)
		{
			base.RenderGUI(cp, cs);
			var p = Position.Calculate(cp, cs);
			var s = Size.Calculate(cp, cs);
			var chat = Root.GetService<Chat>(true);

			if (chat == null)
				return;
			var bs = GameManager.RenderManager.MainFont.BaseSize / 2;

			//for (int i = 0; i < s.Y / bs && i < chat.Conversation.Count; i++)
			//{
			//	var lastmsg = (chat.Conversation.Count >= s.Y / bs) ? chat.Conversation[^((int)(s.Y / bs) - i + 1)] : chat.Conversation[i];
			//	var playercol = lastmsg.Player.GetPlayerColor().Color;
			//	var textcol = Color.White;

			//	Raylib.DrawTextEx(GameManager.RenderManager.MainFont, lastmsg.Player.Name + ": ", p + new Vector2(0, bs * i), 16, 1.6f, playercol);
			//	var size = Raylib.MeasureTextEx(GameManager.RenderManager.MainFont, lastmsg.Player.Name + ": ", 16, 1.6f);
			//	Raylib.DrawTextEx(GameManager.RenderManager.MainFont, lastmsg.Message, p + new Vector2(size.X, bs * i), 16, 1.6f, textcol);
			//}
		}
		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(ChatFrame) == classname) return true;
			return base.IsA(classname);
		}
	}
}
