using NetBlox.Instances;

namespace NetBlox.Runtime
{
	public struct InstanceSiblingHandle<T> : IDisposable where T : Instance
	{
		public event EventHandler<T> OnSiblingReleased;
		public event EventHandler<T> OnSiblingTaken;

		public readonly bool IsPresent => cachedInstance != null;
		public readonly Instance? ReferencePointParent => referencePoint.Parent;
		public readonly T? WantedSibling => cachedInstance;

		private Instance referencePoint;
		private T? cachedInstance;
		private string siblingName;

		public InstanceSiblingHandle(Instance relativeTo, string name)
		{
			referencePoint = relativeTo;
			siblingName = name;

			relativeTo.OnAdoptedBy += OnAdoptedBy;
			relativeTo.OnDisownedBy += OnDisownedBy;

			if (relativeTo.Parent != null) // if currently parented
			{
				referencePoint.Parent.NativeChildAdded += OnSiblingAdded;
				referencePoint.Parent.NativeChildRemoved += OnSiblingRemoved;

				Rescan();
			}
		}

		private void Rescan()
		{
			for (int i = 0; i < referencePoint.Parent.Children.Count; i++)
			{
				var sibling = referencePoint.Parent.Children[i];

				if (sibling.Name == siblingName && sibling is T typedSibling)
				{
					cachedInstance = typedSibling;
					if (typedSibling != null)
					{
						OnSiblingTaken?.Invoke(referencePoint, typedSibling);
						return;
					}
				}
			}
		}

		private void OnSiblingAdded(object? _, Instance sibling)
		{
			if (sibling.Name == siblingName && sibling is T typedSibling)
			{
				cachedInstance = typedSibling;
				if (typedSibling != null)
					OnSiblingTaken?.Invoke(referencePoint, typedSibling);
			}
		}
		private void OnSiblingRemoved(object? _, Instance sibling)
		{
			if (sibling == cachedInstance)
			{
				if (cachedInstance != null)
					OnSiblingReleased?.Invoke(referencePoint, cachedInstance);
				cachedInstance = null;
			}
		}
		private void OnAdoptedBy(object? _, Instance newparent)
		{
			newparent.NativeChildAdded += OnSiblingAdded;
			newparent.NativeChildRemoved += OnSiblingRemoved;

			Rescan();
		}
		private void OnDisownedBy(object? _, Instance oldparent)
		{
			oldparent.NativeChildAdded -= OnSiblingAdded;
			oldparent.NativeChildRemoved -= OnSiblingRemoved;

			if (cachedInstance != null)
				OnSiblingReleased?.Invoke(referencePoint, cachedInstance);
			cachedInstance = null;
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
		}
	}
}
