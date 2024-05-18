using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using Raylib_cs;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;

namespace NetBlox
{
	public enum SerializationType
	{
		String, Enum, Int32, Int64, Single, Double, True, False, Vector3, Color3, Unknown
	}
	public static class SerializationManager
	{
		public static Dictionary<string, Func<string, object>> Deserializers = new();
		public static Dictionary<string, Func<object, string>> Serializers = new();
		public static Dictionary<string, Func<object, GameManager, DynValue>> LuaSerializers = new();
		public static Dictionary<string, Func<DynValue, GameManager, object>> LuaDeserializers = new();
		public static Dictionary<string, DataType> LuaDataTypes = new();
		
		public static SerializationType GetSerializationType(object obj, string prop)
		{
			var type = obj.GetType();
			var pi = type.GetProperty(prop);
			if (pi.PropertyType.IsEnum)
				return SerializationType.Enum;
			switch (pi.PropertyType.FullName)
			{
				case "System.String": return SerializationType.String;
				case "System.Int32": return SerializationType.Int32;
				case "System.Int64": return SerializationType.Int64;
				case "System.Single": return SerializationType.Single;
				case "System.Double": return SerializationType.Double;
				case "System.Boolean": return (bool)pi.GetValue(obj)! ? SerializationType.True : SerializationType.False;
				case "System.Numerics.Vector3": return SerializationType.Vector3;
				case "Raylib_cs.Color": return SerializationType.Color3;
				default: return SerializationType.Unknown;
			}
		}
		public static string[] GetAccessibleProperties(object obj)
		{
			var type = obj.GetType();
			var pi = type.GetProperties();
			return (from x in pi where x.GetMethod.IsPublic select x.Name).ToArray();
		}
		public static object GetProperty(object obj, string name)
		{
			var type = obj.GetType();
			var fi = type.GetRuntimeProperty(name);
			return fi.GetValue(obj);
		}
		public static void SetProperty(object obj, string name, object data)
		{
			var type = obj.GetType();
			var fi = type.GetRuntimeProperty(name);
			if (fi != null)
				fi.SetValue(obj, data);
		}
		public static void SetProperty(Type type, object obj, string name, object data)
		{
			var fi = type.GetRuntimeProperty(name);
			if (fi != null)
				fi.SetValue(obj, data);
		}
		public static void SetField(object obj, string field, object data)
		{
			var type = obj.GetType();
			var fi = type.GetRuntimeField(field);
			if (fi != null)
				fi.SetValue(obj, data);
		}
		public static void SetField(Type type, object obj, string field, object data)
		{
			var fi = type.GetRuntimeField(field);
			if (fi != null)
				fi.SetValue(obj, data);
		}
		public static object Deserialize(Type type, string data)
		{
			var name = type.FullName;

			return Deserializers[name](data);
		}
		public static T Deserialize<T>(string data)
		{
			var type = typeof(T);
			var name = type.FullName;
			var val = Deserializers[name](data);
			return (T)val;
		}
		public static string Serialize(object data)
		{
			var type = data.GetType();
			var name = type.FullName;

			return Serializers[name](data);
		}
		public static string Serialize<T>(T data)
		{
			var type = typeof(T);
			var name = type.FullName;

			return Serializers[name](data);
		}
		public static T LuaDeserialize<T>(DynValue dv, GameManager sc)
		{
			var type = typeof(T);
			var name = type.FullName;

			return (T)LuaDeserializers[name](dv, sc);
		}
		public static object LuaDeserialize(Type type, DynValue dv, GameManager sc)
		{
			var name = type.FullName;

			return LuaDeserializers[name](dv, sc);
		}
		public static object[] LuaDeserializeArray(Type et, DynValue dv, GameManager sc)
		{
			List<object> l = new();
			for (int i = 0; i < dv.Table.Length; i++)
			{
				l.Add(LuaDeserialize(et, dv.Table.Get(i), sc));
			}
			return l.ToArray();
		}
		public static void Initialize()
		{
			Deserializers.Add("System.Byte", x => Byte.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.Int16", x => Int16.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.Int32", x => Int32.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.Int64", x => Int64.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.SByte", x => SByte.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.UInt16", x => UInt16.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.UInt32", x => UInt32.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.UInt64", x => UInt64.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.Boolean", x => Boolean.Parse(x));
			Deserializers.Add("System.Single", x => Single.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.Double", x => Double.Parse(x, CultureInfo.InvariantCulture));
			Deserializers.Add("System.String", x => x);
			Deserializers.Add("System.Char", x => x[0]);
			Deserializers.Add("System.Guid", x => Guid.Parse(x));
			Deserializers.Add("System.Numerics.Vector2", x => new Vector2(Deserialize<float>(x.Split(' ')[0]), Deserialize<float>(x.Split(' ')[1])));
			Deserializers.Add("System.Numerics.Vector3", x => new Vector3(Deserialize<float>(x.Split(' ')[0]), Deserialize<float>(x.Split(' ')[1]), Deserialize<float>(x.Split(' ')[2])));
			Deserializers.Add("Raylib_cs.Color", x => new Color(Deserialize<byte>(x.Split(' ')[0]), Deserialize<byte>(x.Split(' ')[1]), Deserialize<byte>(x.Split(' ')[2]), Deserialize<byte>(x.Split(' ')[3])));
			Deserializers.Add("NetBlox.Structs.Shape", x => (Shape)Deserialize<int>(x));
			Deserializers.Add("NetBlox.Structs.SurfaceType", x => (SurfaceType)Deserialize<int>(x));

			Serializers.Add("System.Byte", x => ((byte)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.Int16", x => ((Int16)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.Int32", x => ((Int32)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.Int64", x => ((Int64)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.SByte", x => ((SByte)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.UInt16", x => ((UInt16)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.UInt32", x => ((UInt32)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.UInt64", x => ((UInt64)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.Boolean", x => ((Boolean)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.Single", x => ((Single)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.Double", x => ((Double)x).ToString(CultureInfo.InvariantCulture));
			Serializers.Add("System.String", x => (string)x);
			Serializers.Add("System.Char", x => ((char)x).ToString());
			Serializers.Add("System.Guid", x => x.ToString());
			Serializers.Add("System.Numerics.Vector2", x => $"{Serialize(((Vector2)x).X)} {Serialize(((Vector2)x).Y)}");
			Serializers.Add("System.Numerics.Vector3", x => $"{Serialize(((Vector3)x).X)} {Serialize(((Vector3)x).Y)} {Serialize(((Vector3)x).Z)}");
			Serializers.Add("Raylib_cs.Color", x => $"{Serialize(((Color)x).R)} {Serialize(((Color)x).G)} {Serialize(((Color)x).B)} {Serialize(((Color)x).A)}");
			Serializers.Add("NetBlox.Structs.Shape", x => (int)(Shape)x + "");
			Serializers.Add("NetBlox.Structs.SurfaceType", x => (int)(SurfaceType)x + "");

			LuaSerializers.Add("System.Byte", (x, y) => DynValue.NewNumber((double)(Byte)x));
			LuaSerializers.Add("System.Int16", (x, y) => DynValue.NewNumber((double)(Int16)x));
			LuaSerializers.Add("System.Int32", (x, y) => DynValue.NewNumber((double)(Int32)x));
			LuaSerializers.Add("System.Int64", (x, y) => DynValue.NewNumber((double)(Int64)x));
			LuaSerializers.Add("System.SByte", (x, y) => DynValue.NewNumber((double)(SByte)x));
			LuaSerializers.Add("System.UInt16", (x, y) => DynValue.NewNumber((double)(UInt16)x));
			LuaSerializers.Add("System.UInt32", (x, y) => DynValue.NewNumber((double)(UInt32)x));
			LuaSerializers.Add("System.UInt64", (x, y) => DynValue.NewNumber((double)(UInt64)x));
			LuaSerializers.Add("System.Boolean", (x, y) => DynValue.NewBoolean((bool)x));
			LuaSerializers.Add("System.Single", (x, y) => DynValue.NewNumber((double)(float)x));
			LuaSerializers.Add("System.Double", (x, y) => DynValue.NewNumber((double)x));
			LuaSerializers.Add("System.String", (x, y) => DynValue.NewString((string)x));
			LuaSerializers.Add("System.Char", (x, y) => DynValue.NewString("" + (char)x));
			LuaSerializers.Add("NetBlox.Structs.Shape", (x, y) => DynValue.NewNumber((double)(Shape)x));
			LuaSerializers.Add("NetBlox.Structs.SurfaceType", (x, y) => DynValue.NewNumber((double)(SurfaceType)x));
			LuaSerializers.Add("NetBlox.Instances.Instance", (x, y) => DynValue.NewTable(LuaRuntime.MakeInstanceTable((Instance)x, y)));
			LuaSerializers.Add("System.Numerics.Vector2", (x, y) => DynValue.NewTable(new Table(y.CurrentRoot.MainEnv) {
				["X"] = ((Vector2)x).X,
				["Y"] = ((Vector2)x).Y
			}));
			LuaSerializers.Add("NetBlox.Runtime.LuaSignal", (x, y) => 
				DynValue.NewTable(new Table(y.CurrentRoot.MainEnv)
			{
				["Connect"] = DynValue.NewCallback((_x, _y) =>
				{
					var s = (LuaSignal)x;
					var i = 0;
					lock (s) 
						s.Connect(_y[1]);

					return DynValue.NewTable(new Table(y.CurrentRoot.MainEnv)
					{
						["Disconnect"] = DynValue.NewCallback((x2, y2) =>
						{
							lock (s) s.Attached.RemoveAt(i);
							return DynValue.Void;
						})
					});
				}),
				["Wait"] = DynValue.NewCallback((_x, _y) =>
				{
					var s = (LuaSignal)x;
					s.Wait();
					return DynValue.Void;
				})
			}));
			LuaSerializers.Add("System.Numerics.Vector3", (x, y) => DynValue.NewTable(new Table(y.CurrentRoot.MainEnv)
			{
				["X"] = ((Vector3)x).X,
				["Y"] = ((Vector3)x).Y,
				["Z"] = ((Vector3)x).Z
			}));
			LuaSerializers.Add("Raylib_cs.Color", (x, y) => DynValue.NewTable(new Table(y.CurrentRoot.MainEnv)
			{
				["R"] = ((Color)x).R / 255f,
				["G"] = ((Color)x).G / 255f,
				["B"] = ((Color)x).B / 255f
			}));
			LuaSerializers.Add("NetBlox.Structs.UDim2", (x, y) => DynValue.NewTable(new Table(y.CurrentRoot.MainEnv)
			{
				["X"] = ((UDim2)x).X,
				["Y"] = ((UDim2)x).Y,
				["XOff"] = ((UDim2)x).XOff,
				["YOff"] = ((UDim2)x).YOff
			}));

			LuaDeserializers.Add("System.Byte", (x, y) => (Byte)x.Number);
			LuaDeserializers.Add("System.Int16", (x, y) => (Int16)x.Number);
			LuaDeserializers.Add("System.Int32", (x, y) => (Int32)x.Number);
			LuaDeserializers.Add("System.Int64", (x, y) => (Int64)x.Number);
			LuaDeserializers.Add("System.SByte", (x, y) => (SByte)x.Number);
			LuaDeserializers.Add("System.UInt16", (x, y) => (UInt16)x.Number);
			LuaDeserializers.Add("System.UInt32", (x, y) => (UInt32)x.Number);
			LuaDeserializers.Add("System.UInt64", (x, y) => (UInt64)x.Number);
			LuaDeserializers.Add("System.Boolean", (x, y) => x.Boolean);
			LuaDeserializers.Add("System.Single", (x, y) => (Single)x.Number);
			LuaDeserializers.Add("System.Double", (x, y) => (Double)x.Number);
			LuaDeserializers.Add("System.String", (x, y) => x.String);
			LuaDeserializers.Add("System.Char", (x, y) => x.String[0]);
			LuaDeserializers.Add("NetBlox.Structs.Shape", (x, y) => (Shape)x.Number);
			LuaDeserializers.Add("NetBlox.Structs.SurfaceType", (x, y) => (SurfaceType)x.Number);
			LuaDeserializers.Add("NetBlox.Instances.Instance", (x, y) => 
			(from z in y.AllInstances 
			 where z.UniqueID.ToString() == (string)x.Table.MetaTable["__handle"] 
			 select z).FirstOrDefault() ?? throw new Exception($"Instance table with id {x.Table.MetaTable["__handle"]} is a zombie table!"));
			LuaDeserializers.Add("System.Numerics.Vector2", (x, y) => new Vector2(
				(float)x.Table["X"], 
				(float)x.Table["Y"]));
			LuaDeserializers.Add("System.Numerics.Vector3", (x, y) => new Vector3(
				(float)x.Table["X"], 
				(float)x.Table["Y"], 
				(float)x.Table["Z"]));
			LuaDeserializers.Add("Raylib_cs.Color", (x, y) => new Color(
				(int)(Convert.ToSingle(x.Table["R"]) * 255), 
				(int)(Convert.ToSingle(x.Table["G"]) * 255), 
				(int)(Convert.ToSingle(x.Table["B"]) * 255), 
				255));
			LuaDeserializers.Add("NetBlox.Structs.UDim2", (x, y) => new UDim2(
				Convert.ToSingle(x.Table["X"]),
				Convert.ToSingle(x.Table["XOff"]),
				Convert.ToSingle(x.Table["Y"]),
				Convert.ToSingle(x.Table["YOff"])));

			LuaDataTypes.Add("System.Byte", DataType.Number);
			LuaDataTypes.Add("System.Int16", DataType.Number);
			LuaDataTypes.Add("System.Int32", DataType.Number);
			LuaDataTypes.Add("System.Int64", DataType.Number);
			LuaDataTypes.Add("System.SByte", DataType.Number);
			LuaDataTypes.Add("System.UInt16", DataType.Number);
			LuaDataTypes.Add("System.UInt32", DataType.Number);
			LuaDataTypes.Add("System.UInt64", DataType.Number);
			LuaDataTypes.Add("System.Boolean", DataType.Boolean);
			LuaDataTypes.Add("System.Single", DataType.Number);
			LuaDataTypes.Add("System.Double", DataType.Number);
			LuaDataTypes.Add("System.String", DataType.String);
			LuaDataTypes.Add("System.Char", DataType.String);
			LuaDataTypes.Add("NetBlox.Structs.Shape", DataType.Number);
			LuaDataTypes.Add("NetBlox.Structs.SurfaceType", DataType.Number);
			LuaDataTypes.Add("NetBlox.Instances.Instance", DataType.Table);
			LuaDataTypes.Add("System.Numerics.Vector2", DataType.Table);
			LuaDataTypes.Add("System.Numerics.Vector3", DataType.Table);
			LuaDataTypes.Add("Raylib_cs.Color", DataType.Table);
			LuaDataTypes.Add("NetBlox.Structs.UDim2", DataType.Table);
		}
	}
}
