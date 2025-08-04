using MoonSharp.Interpreter;
using MoonSharp.Interpreter.DataTypes;
using NetBlox.Instances;
using NetBlox.Runtime;
using NetBlox.Structs;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace NetBlox
{
	public enum SerializationType
	{
		String, Enum, Int32, Int64, Single, Double, True, False, Vector3, Color3, Unknown
	}
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
	public static class SerializationManager
	{
		public static Dictionary<string, Func<string, object>> Deserializers = [];
		public static Dictionary<string, Func<object, string>> Serializers = [];
		public static Dictionary<string, Func<byte[], GameManager, object>> NetworkDeserializers = [];
		public static Dictionary<string, Func<object, GameManager, byte[]>> NetworkSerializers = [];
		public static Dictionary<string, Func<object, GameManager, DynValue>> LuaSerializers = [];
		public static Dictionary<string, Func<DynValue, GameManager, object>> LuaDeserializers = [];
		public static Dictionary<string, DataType> LuaDataTypes = [];
		private static readonly JsonSerializerOptions DefaultJSON = new()
		{
			IncludeFields = true
		};
		private static readonly bool init = false;

		public static string SerializeJson<T>(T obj) => JsonSerializer.Serialize(obj, DefaultJSON);
		public static T? DeserializeJson<T>(string d) => JsonSerializer.Deserialize<T>(d, DefaultJSON);
		public static bool IsReadonly(object obj, string prop)
		{
			var type = obj.GetType();
			var fi = type.GetRuntimeProperty(prop);
			return !fi.CanWrite;
		}
		public static string[] GetAccessibleProperties(object obj)
		{
			var type = obj.GetType();
			var pi = type.GetProperties();
			return (from x in pi where x.GetMethod.IsPublic select x.Name).ToArray();
		}
		public static SerializationType GetSerializationType(object obj, string prop)
		{
			var type = obj.GetType();
			var pi = type.GetProperty(prop);
			return pi.PropertyType.IsEnum
				? SerializationType.Enum
				: pi.PropertyType.FullName switch
				{
					"System.String" => SerializationType.String,
					"System.Int32" => SerializationType.Int32,
					"System.Int64" => SerializationType.Int64,
					"System.Single" => SerializationType.Single,
					"System.Double" => SerializationType.Double,
					"System.Boolean" => (bool)pi.GetValue(obj)! ? SerializationType.True : SerializationType.False,
					"System.Numerics.Vector3" => SerializationType.Vector3,
					"Raylib_cs.Color" => SerializationType.Color3,
					_ => SerializationType.Unknown,
				};
		}
		public static object? GetProperty(object obj, string name)
		{
			var type = obj.GetType();
			var fi = type.GetRuntimeProperty(name);
			return fi.GetValue(obj);
		}
		public static void SetProperty(object obj, string name, object? data)
		{
			var type = obj.GetType();
			var fi = type.GetRuntimeProperty(name);
			fi?.SetValue(obj, data);
		}
		public static void SetProperty(Type type, object obj, string name, object data)
		{
			var fi = type.GetRuntimeProperty(name);
			fi?.SetValue(obj, data);
		}
		public static T Deserialize<T>(string data)
		{
			var type = typeof(T);
			var name = type.FullName;
			var val = Deserializers[name](data);
			return (T)val;
		}
		public static Type GetPropertyType(object data, string prop)
		{
			var type = data.GetType();
			var pro = type.GetProperty(prop);
			return pro.PropertyType;
		}
		public static T NetworkDeserialize<T>(byte[] data, GameManager gm)
		{
			var type = typeof(T);
			var name = type.FullName;
			var val = NetworkDeserializers[name](data, gm);
			return (T)val;
		}
		public static byte[] NetworkSerialize<T>(T data, GameManager gm)
		{
			var type = typeof(T);
			var name = type.FullName;

			return NetworkSerializers[name](data, gm);
		}
		public static string Serialize<T>(T data)
		{
			var type = typeof(T);
			var name = type.FullName;

			return Serializers[name](data);
		}
		public static byte[] SerializeLuaObject(DynValue dv, GameManager gm)
		{
			if (dv.Type == DataType.Function || dv.Type == DataType.ClrFunction)
				return []; // we dont do that

			List<byte> bytes = [];

			void DoObject(DynValue dval)
			{
				switch (dval.Type)
				{
					case DataType.Nil:
					case DataType.Void:
					case DataType.Function: // no
					case DataType.ClrFunction: // no
						break;
					case DataType.Boolean:
						bytes.Add((byte)(0x80 + (dval.Boolean ? 1 : 0)));
						break;
					case DataType.Number:
						bytes.Add(0x82);
						bytes.AddRange(BitConverter.GetBytes(dval.Number));
						break;
					case DataType.String:
						bytes.Add(0x83);
						byte[] byt = Encoding.UTF8.GetBytes(dval.String);
						bytes.AddRange(BitConverter.GetBytes((short)byt.Length));
						bytes.AddRange(byt);
						break;
					case DataType.Table:
						if (dval.Table.MetaTable != null && dval.Table.AssociatedObject != null)
						{
							Guid guid = ((Instance)dval.Table.AssociatedObject).UniqueID;
							bytes.Add(0x85);
							bytes.AddRange(guid.ToByteArray());
						}
						else
						{
							var length = dval.Table.Keys.Count(); // i hate you moonsharp :fuckingsob:
							bytes.Add(0x84);
							bytes.AddRange(BitConverter.GetBytes(length));
							for (int i = 0; i < length; i++)
							{
								DoObject(dval.Table.Keys.ElementAt(i));
								DoObject(dval.Table.Values.ElementAt(i));
							}
						}
						break;
				}
			}
			DoObject(dv);

			return [.. bytes];
		}
		public static DynValue DeserializeLuaObject(byte[] bytes, GameManager gm)
		{
			using MemoryStream ms = new(bytes);
			using BinaryReader br = new(ms);

			DynValue GetObject()
			{
				switch (br.ReadByte())
				{
					case 0x80:
						return DynValue.False;
					case 0x81:
						return DynValue.True;
					case 0x82:
						return DynValue.NewNumber(br.ReadDouble());
					case 0x83:
						return DynValue.NewString(Encoding.UTF8.GetString(br.ReadBytes(br.ReadInt16())));
					case 0x84:
						Table table = new(gm.MainEnvironment);
						DynValue dv = DynValue.NewTable(table);
						int len = br.ReadInt32();

						for (int i = 0; i < len; i++)
						{
							DynValue key = GetObject();
							DynValue val = GetObject();
							table[key] = val;
						}
						return dv;
					case 0x85:
						return LuaRuntime.PushInstance(gm.GetInstance(new Guid(br.ReadBytes(16))));
					default:
						return DynValue.Nil;
				}
			}

			return GetObject();
		}
		static SerializationManager()
		{
			if (init) return;
			init = true;

			NetworkDeserializers.Add("System.Byte", (x, y) => x[0]);
			NetworkDeserializers.Add("System.Int16", (x, y) => BitConverter.ToInt16(x));
			NetworkDeserializers.Add("System.Int32", (x, y) => BitConverter.ToInt32(x));
			NetworkDeserializers.Add("System.Int64", (x, y) => BitConverter.ToInt64(x));
			NetworkDeserializers.Add("System.SByte", (x, y) => unchecked((sbyte)x[0]));
			NetworkDeserializers.Add("System.UInt16", (x, y) => BitConverter.ToUInt16(x));
			NetworkDeserializers.Add("System.UInt32", (x, y) => BitConverter.ToUInt32(x));
			NetworkDeserializers.Add("System.UInt64", (x, y) => BitConverter.ToUInt64(x));
			NetworkDeserializers.Add("System.Boolean", (x, y) => x[0] != 0);
			NetworkDeserializers.Add("System.Single", (x, y) => BitConverter.ToSingle(x));
			NetworkDeserializers.Add("System.Double", (x, y) => BitConverter.ToDouble(x));
			NetworkDeserializers.Add("System.String", (x, y) => Encoding.UTF8.GetString(x));
			NetworkDeserializers.Add("System.Char", (x, y) => BitConverter.ToChar(x));
			NetworkDeserializers.Add("System.Guid", (x, y) => new Guid(x));
			NetworkDeserializers.Add("System.Numerics.Vector2", (x, y) => 
			new Vector2(
				BitConverter.ToSingle(x.AsSpan()[0..4]), 
				BitConverter.ToSingle(x.AsSpan()[4..8])));
			NetworkDeserializers.Add("System.Numerics.Vector3", (x, y) => 
			new Vector3(
				BitConverter.ToSingle(x.AsSpan()[0..4]), 
				BitConverter.ToSingle(x.AsSpan()[4..8]), 
				BitConverter.ToSingle(x.AsSpan()[8..12])));
			NetworkDeserializers.Add("Raylib_cs.Color", (x, y) => new Color(x[0], x[1], x[2], x[3]));
			NetworkDeserializers.Add("NetBlox.Structs.Shape", (x, y) => (Shape)BitConverter.ToInt32(x));
			NetworkDeserializers.Add("NetBlox.Structs.Faces", (x, y) => (Faces)BitConverter.ToInt32(x));
			NetworkDeserializers.Add("NetBlox.Structs.SurfaceType", (x, y) => (SurfaceType)BitConverter.ToInt32(x));
			NetworkDeserializers.Add("NetBlox.Instances.Instance", (x, y) => y.GetInstance(new Guid(x))!);

			NetworkSerializers.Add("System.Byte", (x, y) => [(byte)x]);
			NetworkSerializers.Add("System.Int16", (x, y) => BitConverter.GetBytes((short)x));
			NetworkSerializers.Add("System.Int32", (x, y) => BitConverter.GetBytes((int)x));
			NetworkSerializers.Add("System.Int64", (x, y) => BitConverter.GetBytes((long)x));
			NetworkSerializers.Add("System.SByte", (x, y) => [unchecked((byte)(sbyte)x)]);
			NetworkSerializers.Add("System.UInt16", (x, y) => BitConverter.GetBytes((ushort)x));
			NetworkSerializers.Add("System.UInt32", (x, y) => BitConverter.GetBytes((uint)x));
			NetworkSerializers.Add("System.UInt64", (x, y) => BitConverter.GetBytes((ulong)x));
			NetworkSerializers.Add("System.Boolean", (x, y) => [(bool)x ? (byte)1 : (byte)0]);
			NetworkSerializers.Add("System.Single", (x, y) => BitConverter.GetBytes((float)x));
			NetworkSerializers.Add("System.Double", (x, y) => BitConverter.GetBytes((double)x));
			NetworkSerializers.Add("System.String", (x, y) => Encoding.UTF8.GetBytes((string)x));
			NetworkSerializers.Add("System.Char", (x, y) => BitConverter.GetBytes((char)x));
			NetworkSerializers.Add("System.Guid", (x, y) => ((Guid)x).ToByteArray());
			NetworkSerializers.Add("System.Numerics.Vector2", (x, y) => // i wonder tf am i writing
			{
				List<byte> lb = [];
				Vector2 v2 = (Vector2)x;
				lb.AddRange(NetworkSerialize(v2.X, y));
				lb.AddRange(NetworkSerialize(v2.Y, y));
				return [.. lb];
			});
			NetworkSerializers.Add("System.Numerics.Vector3", (x, y) =>
			{
				List<byte> lb = [];
				Vector3 v3 = (Vector3)x;
				lb.AddRange(NetworkSerialize(v3.X, y));
				lb.AddRange(NetworkSerialize(v3.Y, y));
				lb.AddRange(NetworkSerialize(v3.Z, y));
				return [.. lb];
			});
			NetworkSerializers.Add("Raylib_cs.Color", (x, y) => [((Color)x).R, ((Color)x).G, ((Color)x).B, ((Color)x).A]);
			NetworkSerializers.Add("NetBlox.Structs.Shape", (x, y) => NetworkSerialize((int)(Shape)x, y));
			NetworkSerializers.Add("NetBlox.Structs.Faces", (x, y) => NetworkSerialize((int)(Faces)x, y));
			NetworkSerializers.Add("NetBlox.Structs.SurfaceType", (x, y) => NetworkSerialize((int)(SurfaceType)x, y));
			NetworkSerializers.Add("NetBlox.Instances.Instance", (x, y) => (x as Instance).UniqueID.ToByteArray());

			LuaSerializers.Add("System.Byte", (x, y) => DynValue.NewNumber((Byte)x));
			LuaSerializers.Add("System.Int16", (x, y) => DynValue.NewNumber((Int16)x));
			LuaSerializers.Add("System.Int32", (x, y) => DynValue.NewNumber((Int32)x));
			LuaSerializers.Add("System.Int64", (x, y) => DynValue.NewNumber((Int64)x));
			LuaSerializers.Add("System.SByte", (x, y) => DynValue.NewNumber((SByte)x));
			LuaSerializers.Add("System.UInt16", (x, y) => DynValue.NewNumber((UInt16)x));
			LuaSerializers.Add("System.UInt32", (x, y) => DynValue.NewNumber((UInt32)x));
			LuaSerializers.Add("System.UInt64", (x, y) => DynValue.NewNumber((UInt64)x));
			LuaSerializers.Add("System.Boolean", (x, y) => DynValue.NewBoolean((bool)x));
			LuaSerializers.Add("System.Single", (x, y) => DynValue.NewNumber((double)(float)x));
			LuaSerializers.Add("System.Double", (x, y) => DynValue.NewNumber((double)x));
			LuaSerializers.Add("System.String", (x, y) => DynValue.NewString((string)x));
			LuaSerializers.Add("System.Char", (x, y) => DynValue.NewString("" + (char)x));
			LuaSerializers.Add("NetBlox.Structs.Shape", (x, y) => DynValue.NewNumber((double)(Shape)x));
			LuaSerializers.Add("NetBlox.Structs.Faces", (x, y) => DynValue.NewNumber((double)(Faces)x));
			LuaSerializers.Add("NetBlox.Structs.SurfaceType", (x, y) => DynValue.NewNumber((double)(SurfaceType)x));
			LuaSerializers.Add("NetBlox.Instances.Instance", (x, y) => LuaRuntime.PushInstance((Instance)x));
			LuaSerializers.Add("NetBlox.Runtime.LuaSignal", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				["Connect"] = DynValue.NewCallback((_x, _y) =>
				{
					var s = (LuaSignal)x;
					var i = 0;
					lock (s)
						s.Connect(_y[1]);

					return DynValue.NewTable(new Table(y.MainEnvironment)
					{
						["Disconnect"] = DynValue.NewCallback((x2, y2) =>
						{
							lock (s) s.Attached.RemoveAt(i);
							return DynValue.Void;
						}),
						IsProtected = true
					});
				}),
				["Wait"] = DynValue.NewCallback((_x, _y) =>
				{
					var s = (LuaSignal)x;
					s.Wait();
					return DynValue.Void;
				}),
				IsProtected = true
			}));
			LuaSerializers.Add("System.Numerics.Vector2", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				["X"] = ((Vector2)x).X,
				["Y"] = ((Vector2)x).Y,
				IsProtected = true
			}));
			LuaSerializers.Add("System.Numerics.Vector3", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				["X"] = ((Vector3)x).X,
				["Y"] = ((Vector3)x).Y,
				["Z"] = ((Vector3)x).Z,
				IsProtected = true
			}));
			LuaSerializers.Add("Raylib_cs.Color", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				["R"] = ((Color)x).R / 255f,
				["G"] = ((Color)x).G / 255f,
				["B"] = ((Color)x).B / 255f,
				IsProtected = true
			}));
			LuaSerializers.Add("NetBlox.Structs.UDim2", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				["X"] = ((UDim2)x).X,
				["Y"] = ((UDim2)x).Y,
				["XOff"] = ((UDim2)x).XOff,
				["YOff"] = ((UDim2)x).YOff,
				IsProtected = true
			}));
			LuaSerializers.Add("NetBlox.Structs.BrickColor", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				// no changes from user expected
				AssociatedObject = (BrickColor)x,
				IsProtected = true
			}));
			LuaSerializers.Add("NetBlox.Structs.ByteArray", (x, y) => DynValue.NewTable(new Table(y.MainEnvironment)
			{
				// no changes from user expected
				AssociatedObject = (ByteArray)x,
				IsProtected = true
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
			LuaDeserializers.Add("System.Double", (x, y) => x.Number);
			LuaDeserializers.Add("System.String", (x, y) => x.String);
			LuaDeserializers.Add("System.Char", (x, y) => x.String[0]);
			LuaDeserializers.Add("NetBlox.Structs.Shape", (x, y) => (Shape)x.Number);
			LuaDeserializers.Add("NetBlox.Structs.Faces", (x, y) => (Faces)x.Number);
			LuaDeserializers.Add("NetBlox.Structs.SurfaceType", (x, y) => (SurfaceType)x.Number);
			LuaDeserializers.Add("NetBlox.Instances.Instance", (x, y) => (Instance)x.Table.AssociatedObject);
			LuaDeserializers.Add("System.Numerics.Vector2", (x, y) => new Vector2(
				(float)(double)x.Table["X"],
				(float)(double)x.Table["Y"]));
			LuaDeserializers.Add("System.Numerics.Vector3", (x, y) => new Vector3(
				(float)(double)x.Table["X"],
				(float)(double)x.Table["Y"],
				(float)(double)x.Table["Z"]));
			LuaDeserializers.Add("Raylib_cs.Color", (x, y) => new Color(
				(int)(Convert.ToSingle(x.Table["R"]) * 255),
				(int)(Convert.ToSingle(x.Table["G"]) * 255),
				(int)(Convert.ToSingle(x.Table["B"]) * 255),
				255));
			LuaDeserializers.Add("NetBlox.Structs.UDim2", (x, y) => new UDim2(
				Convert.ToSingle((double)x.Table["X"]),
				Convert.ToSingle((double)x.Table["XOff"]),
				Convert.ToSingle((double)x.Table["Y"]),
				Convert.ToSingle((double)x.Table["YOff"])));
			LuaDeserializers.Add("NetBlox.Structs.BrickColor", (x, y) => x.Table.AssociatedObject);
			LuaDeserializers.Add("NetBlox.Structs.ByteArray", (x, y) => x.Table.AssociatedObject);

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
			LuaDataTypes.Add("NetBlox.Structs.Faces", DataType.Number);
			LuaDataTypes.Add("NetBlox.Structs.SurfaceType", DataType.Number);
			LuaDataTypes.Add("NetBlox.Instances.Instance", DataType.Table);
			LuaDataTypes.Add("System.Numerics.Vector2", DataType.Table);
			LuaDataTypes.Add("System.Numerics.Vector3", DataType.Table);
			LuaDataTypes.Add("Raylib_cs.Color", DataType.Table);
			LuaDataTypes.Add("NetBlox.Structs.UDim2", DataType.Table);
			LuaDataTypes.Add("NetBlox.Structs.BrickColor", DataType.Table);
			LuaDataTypes.Add("NetBlox.Structs.ByteArray", DataType.Table);
		}
	}
}

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
