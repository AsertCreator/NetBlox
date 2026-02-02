using NetBlox.Instances;

namespace NetBlox.Runtime
{
	// having this as a struct was definitely an idea
	// not a great one though
	public class InstanceSiblingHandle<T> : IDisposable where T : Instance
	{
		public event EventHandler<T> OnSiblingReleased;
		public event EventHandler<T> OnSiblingTaken;

		public bool IsPresent => cachedInstance != null;
		public Instance? ReferencePointParent => referencePoint.Parent;
		public T? WantedSibling => cachedInstance;
		public bool Activated => activated;

		public static bool DebugLog = false;

		private Instance referencePoint;
		private T? cachedInstance;
		private string siblingName;
		private bool activated;

		public InstanceSiblingHandle(Instance relativeTo, string name)
		{
			referencePoint = relativeTo;
			siblingName = name;
			activated = false;
		}
		public void Reactivate()
		{
			activated = true;

			referencePoint.OnAdoptedBy += OnAdoptedBy;
			referencePoint.OnDisownedBy += OnDisownedBy;

			if (ReferencePointParent != null) // if currently parented
			{
				referencePoint.Parent.NativeChildAdded += OnSiblingAdded;
				referencePoint.Parent.NativeChildRemoved += OnSiblingRemoved;

				Rescan();
			}

			LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": activated...");
		}
		public void Deactivate()
		{
			if (referencePoint.Parent != null)
			{
				referencePoint.Parent.NativeChildAdded -= OnSiblingAdded;
				referencePoint.Parent.NativeChildRemoved -= OnSiblingRemoved;
			}
			referencePoint.OnAdoptedBy -= OnAdoptedBy;
			referencePoint.OnDisownedBy -= OnDisownedBy;

			LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": deactivated...");
		}
		private void Rescan()
		{
			if (DebugLog)
				LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": beginning sibling rescan...");
			for (int i = 0; i < referencePoint.Parent.Children.Count; i++)
			{
				var sibling = referencePoint.Parent.Children[i];

				if (sibling.Name == siblingName && sibling is T typedSibling)
				{
					cachedInstance = typedSibling;
					if (typedSibling != null)
					{
						if (DebugLog)
							LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": found named sibling...");
						OnSiblingTaken?.Invoke(referencePoint, typedSibling);
						return;
					}
				}
			}
		}

		private void OnSiblingAdded(object? _, Instance sibling)
		{
			if (cachedInstance != null)
				return;
			if (sibling.Name == siblingName && sibling is T typedSibling)
			{
				cachedInstance = typedSibling;
				if (typedSibling != null)
				{
					if (DebugLog)
						LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": found newly added named sibling...");
					OnSiblingTaken?.Invoke(referencePoint, typedSibling);
				}
			}
		}
		private void OnSiblingRemoved(object? _, Instance sibling)
		{
			if (sibling == cachedInstance)
			{
				if (cachedInstance != null)
					OnSiblingReleased?.Invoke(referencePoint, cachedInstance);
				cachedInstance = null;
				if (DebugLog)
					LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": released previously taken sibling...");

				Rescan();
			}
		}
		private void OnAdoptedBy(object? _, Instance newparent)
		{
			newparent.NativeChildAdded += OnSiblingAdded;
			newparent.NativeChildRemoved += OnSiblingRemoved;

			if (DebugLog)
				LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": the reference point got adopted; rescan on due...");

			Rescan();
		}
		private void OnDisownedBy(object? _, Instance oldparent)
		{
			oldparent.NativeChildAdded -= OnSiblingAdded;
			oldparent.NativeChildRemoved -= OnSiblingRemoved;

			if (cachedInstance != null)
				OnSiblingReleased?.Invoke(referencePoint, cachedInstance);
			cachedInstance = null;

			if (DebugLog)
				LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": the reference point got disowned, sibling released...");
		}
		public void Dispose()
		{
			if (referencePoint.Parent != null)
			{
				referencePoint.Parent.NativeChildAdded -= OnSiblingAdded;
				referencePoint.Parent.NativeChildRemoved -= OnSiblingRemoved;
			}
			referencePoint.OnAdoptedBy -= OnAdoptedBy;
			referencePoint.OnDisownedBy -= OnDisownedBy;

			if (DebugLog)
				LogManager.LogInfo("InstanceSiblingHandle-" + siblingName + ": disposed...");
		}
	}
}
