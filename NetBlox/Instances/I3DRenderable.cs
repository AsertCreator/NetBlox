using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NetBlox.Instances
{
	public interface I3DRenderable
	{
		public void Render(); // wow

		public static bool IsLookingTowards(Vector3 target, GameManager gameManager)
		{
			var looksat = gameManager.RenderManager.MainCamera.Target;
			var position = gameManager.RenderManager.MainCamera.Position;

			var dir = Vector3.Normalize(looksat - position);
			var dist = Vector3.Normalize(target - position);

			return Vector3.Dot(dir, dist) > 0;
		}
	}
}
