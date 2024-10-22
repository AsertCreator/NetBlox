using NetBlox.Runtime;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances.Services
{
	[Service]
	public class Lighting : Instance
	{
		[Lua([Security.Capability.None])]
		public double ClockTime { get => GameManager.RenderManager.TimeOfDay; set => GameManager.RenderManager.TimeOfDay = value % 24; }
		[Lua([Security.Capability.None])]
		public string TimeOfDay 
		{ 
			get 
			{
				return ""; // no
			} 
			set 
			{ 
				// no
			} 
		}
		[Lua([Security.Capability.None])]
		public string CurrentTime => "no";
		public Vector3 SunPosition = new Vector3(2, 2, 0);
		public bool SunLocality = false;

		public Lighting(GameManager ins) : base(ins) { }

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(Lighting) == classname) return true;
			return base.IsA(classname);
		}
	}
}
