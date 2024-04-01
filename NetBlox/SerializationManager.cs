using MoonSharp.Interpreter;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Globalization;
using System.Numerics;
using System.Reflection.Metadata;

namespace NetBlox
{
	public static class SerializationManager
	{
		public static Dictionary<string, Func<string, object>> Deserializers = new();
		public static Dictionary<string, Func<object, Script, DynValue>> LuaSerializers = new();
		public static Dictionary<string, Func<DynValue, Script, object>> LuaDeserializers = new();
		public static Dictionary<string, DataType> LuaDataTypes = new();

		public static T Deserialize<T>(string data)
		{
			var type = typeof(T);
			var name = type.FullName;

			return (T)Deserializers[name](data);
		}
		public static T LuaDeserialize<T>(DynValue dv, Script sc)
		{
			var type = typeof(T);
			var name = type.FullName;

			return (T)LuaDeserializers[name](dv, sc);
		}
		public static object LuaDeserialize(Type type, DynValue dv, Script sc)
		{
			var name = type.FullName;

			return LuaDeserializers[name](dv, sc);
		}
		public static object[] LuaDeserializeArray(Type et, DynValue dv, Script sc)
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
			Deserializers.Add("System.Numerics.Vector2", x => new Vector2(Deserialize<float>(x.Split()[0]), Deserialize<float>(x.Split()[1])));
			Deserializers.Add("System.Numerics.Vector3", x => new Vector3(Deserialize<float>(x.Split()[0]), Deserialize<float>(x.Split()[1]), Deserialize<float>(x.Split()[2])));
			Deserializers.Add("Raylib_cs.Color", x => new Color(Deserialize<byte>(x.Split()[0]), Deserialize<byte>(x.Split()[1]), Deserialize<byte>(x.Split()[2]), Deserialize<byte>(x.Split()[3])));
			Deserializers.Add("NetBlox.Structs.Shape", x => Enum.Parse(typeof(Shape), x));
			Deserializers.Add("NetBlox.Structs.SurfaceType", x => Enum.Parse(typeof(SurfaceType), x));

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
			(from z in GameManager.AllInstances 
			 where z.UniqueID.ToString() == (string)x.Table.MetaTable["__handle"] 
			 select z).FirstOrDefault() ?? throw new Exception($"Instance table with id {x.Table.MetaTable["__handle"]} is a zombie table!"));

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
		}
	}
}
