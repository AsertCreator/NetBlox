using NetBlox.Instances;

namespace NetBlox.Structs
{
	public class PhysicsAssembly
	{
		public List<BasePart> AllBaseParts;
		public BasePart RootPart;

		private PhysicsAssembly() { }

		public static PhysicsAssembly? Create(BasePart p0, BasePart p1)
		{
			if (p0.Anchored && p1.Anchored)
				return null;

			var assembly = new PhysicsAssembly();
			assembly.AllBaseParts = [p0, p1];
			assembly.ChooseNewRootPart();

			p0.Assembly = assembly;
			p1.Assembly = assembly;

			return assembly;
		}
		public bool TryToAdd(BasePart part)
		{
			if (RootPart.Anchored && part.Anchored)
				return false;
			if (part.Assembly != null)
				return false;

			AllBaseParts.Add(part);
			ChooseNewRootPart();
			part.Assembly = this;
			return true;
		}
		public bool TryToRemove(BasePart part)
		{
			if (part.Assembly != this)
				return false;

			AllBaseParts.Remove(part);
			ChooseNewRootPart();
			part.Assembly = null;

			return true;
		}
		public void ChooseNewRootPart() // this is copyrighted and trademarked algorithm
		{
			BasePart? foundAnchored = null;

			for (int i = 0; i < AllBaseParts.Count; i++)
			{
				var part = AllBaseParts[i];
				
			}
		}
	}
}
