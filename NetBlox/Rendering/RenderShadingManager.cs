using NetBlox.Instances;
using Raylib_cs;
using System.Linq;
using System.Numerics;

namespace NetBlox.Rendering
{
	public enum RenderShadingMode
	{
		NoShading, FlatShading, SmoothShading
	}
	public class RenderShadingManager
	{
		public RenderManager RenderManager;
		public GameManager GameManager;
		public Shader? FlatLightingShader;
		public Shader? SmoothLightingShader;
		public RenderShadingMode Mode => Mode;
		public DataModel Root => GameManager.CurrentRoot;

		private Color sunColor;
		private Vector3 sunLookAt;
		private RenderShadingMode mode = RenderShadingMode.NoShading;

		public RenderShadingManager(RenderManager rm)
		{
			RenderManager = rm;
			GameManager = rm.GameManager;
			sunColor = Color.White;
		}

		public void SwitchToMode(RenderShadingMode targetmode)
		{
			switch (targetmode)
			{
				case RenderShadingMode.NoShading:
					mode = RenderShadingMode.NoShading;
					break;
				case RenderShadingMode.FlatShading:
					if (!FlatLightingShader.HasValue)
					{
						RenderManager.LoadShader("rbxasset://shaders/flatlighting.shad", x => FlatLightingShader = x);
					}
					mode = RenderShadingMode.FlatShading;
					break;
				case RenderShadingMode.SmoothShading:
					if (!SmoothLightingShader.HasValue)
					{
						RenderManager.LoadShader("rbxasset://shaders/smoothlighting.shad", x => SmoothLightingShader = x);
					}
					mode = RenderShadingMode.SmoothShading;
					break;
			}
		}
		public void SetCameraLookAt(Vector3 lookat)
		{
			Shader? shader = SupplyShader();
			if (!shader.HasValue)
				return;
			int loc = Raylib.GetShaderLocation(shader.Value, "cameraLookat");
			Raylib.SetShaderValue(shader.Value, loc, lookat, ShaderUniformDataType.Vec3);
		}
		public void SetSunLookAt(Vector3 lookat)
		{
			Shader? shader = SupplyShader();
			sunLookAt = lookat;
			if (!shader.HasValue)
				return;
			int loc = Raylib.GetShaderLocation(shader.Value, "sunLookat");
			Raylib.SetShaderValue(shader.Value, loc, lookat, ShaderUniformDataType.Vec3);
		}
		public void SetSunColor(Color tint)
		{
			Shader? shader = SupplyShader();
			sunColor = tint;
			if (!shader.HasValue)
				return;
			int loc = Raylib.GetShaderLocation(shader.Value, "sunColor");
			Raylib.SetShaderValue(shader.Value, loc, new Vector4(tint.R / 255f, tint.G / 255f, tint.B / 255f, 1), ShaderUniformDataType.Vec4);
		}
		public Shader? SupplyShader()
		{
			switch (mode)
			{
				case RenderShadingMode.NoShading:
					return null;
				case RenderShadingMode.FlatShading:
					if (!FlatLightingShader.HasValue)
						return null;
					return FlatLightingShader.Value;
				case RenderShadingMode.SmoothShading:
					if (!SmoothLightingShader.HasValue)
						return null;
					return SmoothLightingShader.Value;
			}
			return null;
		}
	}
}
