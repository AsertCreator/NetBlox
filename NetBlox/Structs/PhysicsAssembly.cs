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
		private bool TryToAdd(BasePart part)
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
		private bool TryToRemove(BasePart part)
		{
			if (part.Assembly != this)
				return false;

			AllBaseParts.Remove(part);
			ChooseNewRootPart();
			part.Assembly = null;

			return true;
		}
		private void ChooseNewRootPart() // this is copyrighted and trademarked algorithm
		{
			BasePart? foundAnchored = null;

			for (int i = 0; i < AllBaseParts.Count; i++)
			{
				var part = AllBaseParts[i];
				if (part.Anchored)
					foundAnchored = part;
			}

			if (foundAnchored != null)
			{
				RootPart = foundAnchored;
				return;
			}

			if (AllBaseParts.Count > 0)
				RootPart = AllBaseParts[0];
			else
				RootPart = null;
		}

		public static void RemovePartFromAssembly(BasePart target)
		{
			// this atuomatically separates assemblies when needed
			// is this actually efficient?

			if (target.Assembly == null)
				return;

			PhysicsAssembly rootAssembly = target.Assembly;

			rootAssembly.TryToRemove(target);

			if (target.Assembly.AllBaseParts.Count < 2)
				return;

			List<List<BasePart>> newassemblies = new();
			HashSet<BasePart> unvisitedparts = [.. rootAssembly.AllBaseParts];
			HashSet<BasePart> rootAssemblyCutoffParts = [];

			while (unvisitedparts.Count > 0)
			{
				BasePart part = unvisitedparts.ElementAt(0);
				HashSet<BasePart> visitedparts = [];

				void Visit(BasePart node)
				{
					if (!unvisitedparts.Contains(node))
					{
						visitedparts.Add(node);

						for	(int i = 0; i < part.ActiveConstraints.Count; i++)
						{
							Constraint constraint = node.ActiveConstraints[i];
							BasePart other = constraint.Part0 == node ? constraint.Part1 : constraint.Part0;

							if (other != target)
								Visit(other);
						}
					}
				}

				Visit(part);

				// whatever assembly has the root part becomes the new old assembly
				bool isRootAssembly = visitedparts.Contains(target.Assembly.RootPart);

				if (isRootAssembly)
				{
					newassemblies.Insert(0, visitedparts.ToList());
				}
				else
				{
					newassemblies.Add(visitedparts.ToList());
					rootAssemblyCutoffParts.UnionWith(visitedparts);
				}
			}

			for (int i = 0; i < newassemblies.Count; i++)
			{
				if (i == 0)
				{
					while (rootAssemblyCutoffParts.Any())
					{
						BasePart part = rootAssemblyCutoffParts.First();

						target.Assembly.TryToRemove(part);

						rootAssemblyCutoffParts.Remove(part);
					}
				}
				else
				{
					PhysicsAssembly assembly = new();
					List<BasePart> assemblyParts = newassemblies[i];

					foreach (BasePart assemblyPart in assemblyParts)
					{
						assemblyPart.Assembly?.TryToRemove(assemblyPart);
						assembly.TryToAdd(assemblyPart);
					}
				}
			}


		}
		public static void AddTwoPartsToAssembly(BasePart part0, BasePart part1)
		{
			if (part0.Assembly == null && part1.Assembly == null)
			{
				PhysicsAssembly assembly = new();

				assembly.TryToAdd(part0);
				assembly.TryToAdd(part1);
			}
			else if (part0.Assembly == null)
			{
				part1.Assembly.TryToAdd(part0);
			}
			else if (part1.Assembly == null)
			{
				part0.Assembly.TryToAdd(part0);
			}
			else if (part0.Assembly != part1.Assembly)
			{
				MergeAssemblies(part0.Assembly, part1.Assembly);
			}
		}
		public static void MergeAssemblies(PhysicsAssembly assembly0, PhysicsAssembly assembly1)
		{
			
		}
	}
}
