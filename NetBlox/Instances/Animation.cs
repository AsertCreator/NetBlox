using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances
{
	[Creatable]
	public class Animation(GameManager ins) : Instance(ins)
	{
		[Lua([Security.Capability.None])]
		public Instance[] Subjects { get; set; } = [];
		[Lua([Security.Capability.None])]
		public string AnimationUrl 
		{
			get => animationUrl;
			set
			{
				animationUrl = value;
				AppManager.ResolveUrlAsync(animationUrl, true, true).ContinueWith(x =>
				{
					AnimationRawData = File.ReadAllText(x.Result);
				});
			}
		}
		[Lua([Security.Capability.CoreSecurity])]
		public string AnimationRawData { get; set; }
		[Lua([Security.Capability.None])]
		public LuaSignal AnimationStarted { get; set; } = new();
		[Lua([Security.Capability.None])]
		public LuaSignal AnimationEnded { get; set; } = new();
		[Lua([Security.Capability.None])]
		public bool IsPlaying => isPlaying;
		private string animationUrl = "";
		private bool isPlaying = false;

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Animation) == classname) return true;
			return base.IsA(classname);
		}
		[Lua([Security.Capability.None])]
		public void Start()
		{
			AnimationStarted.Fire();
			isPlaying = true;
		}
		[Lua([Security.Capability.None])]
		public void Stop()
		{
			// time = 0;
			isPlaying = false;
			AnimationEnded.Fire();
		}
		public override void Process()
		{
			base.Process();
		}
	}
}
