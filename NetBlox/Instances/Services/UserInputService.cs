using NetBlox.Runtime;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Text;

namespace NetBlox.Instances.Services
{
	public class UserInputService : Instance
	{
		public UserInputService(GameManager ins) : base(ins) { }
		[Lua([Security.Capability.None])]
		public bool TocuhEnabled => Profile.IsTouchDevice;

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(UserInputService) == classname) return true;
			return base.IsA(classname);
		}
	}
}
