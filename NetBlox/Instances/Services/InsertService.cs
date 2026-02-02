using NetBlox.Runtime;

namespace NetBlox.Instances.Services
{
	[Service]
	public class InsertService : Instance
	{
		[Lua([Security.Capability.None])]
		public bool AllowClientInsertModels { get; set; } = true;

		public InsertService(GameManager ins) : base(ins)
		{
			GameManager.RegisterService(this, ServiceType.InsertService);
		}

		[Lua([Security.Capability.None])]
		public override bool IsA(string classname)
		{
			if (nameof(InsertService) == classname) return true;
			return base.IsA(classname);
		}

		[Lua([Security.Capability.None])]
		public Instance LoadAsset(long id)
		{
			// TODO: the whole InsertService
			return null;
		}
	}
}
