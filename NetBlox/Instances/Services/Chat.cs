using MoonSharp.Interpreter;
using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	[Service]
	public class Chat : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal Chatted { get; init; } = new();
		[Lua([Security.Capability.None])]
		public LuaSignal MessageRecieved { get; init; } = new();
		public DateTime LastTimeChatted;
		// public List<NetworkManager.ChatMessageData> Conversation = [];

		public Chat(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Chat) == classname) return true;
			return base.IsA(classname);
		}
		public override void Process()
		{
			base.Process();
		}
		public void ProcessMessage(Player plr, string message)
		{
			//if (GameManager.NetworkManager.IsClientGame)
			//{
			//	Conversation.Add(new NetworkManager.ChatMessageData()
			//	{
			//		Player = plr,
			//		Message = message
			//	});
			//}
			//if (GameManager.NetworkManager.IsServerGame)
			//{
			//	message = Profanity.Filter(message);
			//	GameManager.NetworkManager.ChatMessages.Enqueue(new NetworkManager.ChatMessageData()
			//	{
			//		Player = plr,
			//		Message = message
			//	});
			//}
		}
		[Lua([Security.Capability.None])]
		public void SendMessage(string msg) // cHaT iS iNvAlId NaMe
		{
			//var plrs = Root.GetService<Players>(true);
			//if (plrs == null)
			//{
			//	LogManager.LogWarn("Tried to chat, while Players service wasn't loaded!");
			//	return;
			//}
			//var lp = plrs.LocalPlayer;
			//if (plrs == null)
			//{
			//	LogManager.LogWarn("Tried to chat, while the LocalPlayer wasn't loaded!");
			//	return;
			//}
			//GameManager.NetworkManager.ChatMessage = msg;
		}
	}
}
