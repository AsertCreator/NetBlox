using MoonSharp.Interpreter;
using NetBlox.Network;
using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	public struct ChatMessage
	{
		public Player? Sender;
		public string Message;
	}

	[Service]
	public class Chat : Instance
	{
		[Lua([Security.Capability.None])]
		public LuaSignal Chatted { get; init; }
		public DateTime LastTimeChatted;
		public List<ChatMessage> Conversation = [];

		public Chat(GameManager ins) : base(ins) 
		{
			Chatted = new LuaSignal(ins);
		}

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
		public void ProcessMessage(ChatMessage message)
		{
			message.Message = Profanity.Filter(message.Message);

			Chatted.Fire(LuaRuntime.PushInstance(message.Sender), DynValue.NewString(message.Message));

			if (GameManager.NetworkManager.IsClient)
			{
				Conversation.Add(message);
			}
			if (GameManager.NetworkManager.IsServer)
			{
				for (int i = 0; i < GameManager.NetworkManager.Clients.Count; i++)
				{
					var rc = GameManager.NetworkManager.Clients[i];
					rc.SendPacket(NPChat.Create(message));
				}
			}
		}
		[Lua([Security.Capability.None])]
		public void SendMessage(string msg) // cHaT iS iNvAlId NaMe
		{
			var plrs = Root.GetService<Players>(true);
			if (plrs == null)
			{
				LogManager.LogWarn("Tried to chat, while Players service wasn't loaded!");
				return;
			}
			var lp = plrs.LocalPlayer;
			if (plrs == null)
			{
				LogManager.LogWarn("Tried to chat, while the LocalPlayer wasn't loaded!");
				return;
			}

			var chatMessage = new ChatMessage();
			chatMessage.Sender = GameManager.CurrentRoot.GetService<Players>().LocalPlayer as Player;
			chatMessage.Message = msg;

			GameManager.NetworkManager.SendServerboundPacket(NPChat.Create(chatMessage));
		}
	}
}
