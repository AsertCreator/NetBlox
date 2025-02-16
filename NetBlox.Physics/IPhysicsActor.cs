using System.Numerics;

namespace NetBlox
{
	public interface IPhysicsActor
	{
		public Vector3 BodyPosition { get; set; }
		public Vector3 BodySize { get; set; }
		public Vector3 BodyRotation { get; set; }

		public void ReportContactBegin();
		public void ReportContactEnd();
	}
}
