using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Xml;
using NetBlox.Instances;
using NetBlox.Instances.Services;
using NetBlox.Runtime.Bridges;
using NetBlox.Structs;

namespace NetBlox;

public class PlaceParser
{
    public GameManager GameManager { get; init; }

    private XmlDocument? xmlDocument;
    private Dictionary<string, Instance> instances = [];
    private Dictionary<string, string> sharedStrings = [];
    private HashSet<string> metClasses = [];
    private List<(PropertyInfo Property, Instance Instance, string Reference)> refFixups = [];

    public PlaceParser(GameManager gameManager)
    {
        GameManager = gameManager;
    }

    [DoesNotReturn]
    void ThrowMalformedException()
    {
        throw new InvalidDataException("Cannot load this place/model file - data malformed");
    }
    [DoesNotReturn]
    void ThrowNotSupportedException()
    {
        throw new InvalidDataException("Cannot load this place/model file - place unsupported");
    }
    private Instance? ParseItemXml(bool isRoot, XmlElement xmlElement)
    {
        string className = xmlElement.GetAttribute("class");
        Instance? myInstance = null;

        if (isRoot)
            myInstance = GameManager.RootModel.GetService(className);
        else
            myInstance = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(className);
        
        if (myInstance == null)
        {
            if (!metClasses.Contains(className))
            {
                Trace.TraceWarning("Unknown Instance type while loading a place/model file: " + className);
                metClasses.Add(className);
            }
            return null;
        }
        
        string referent = xmlElement.GetAttribute("referent");
        if (!string.IsNullOrWhiteSpace(referent))
            instances[referent] = myInstance;
        
        Type type = myInstance.GetType();
        
        for (int i = 0; i < xmlElement.ChildNodes.Count; i++)
        {
            XmlElement? element = xmlElement.ChildNodes[i] as XmlElement;
            if (element == null)
                continue;
            
            if (element.Name == "Item")
            {
                Instance? child = ParseItemXml(false, element);
                if (child != null)
                    child.Parent = myInstance;
            }
            else if (element.Name == "Properties")
            {
                ScriptContext scriptContext = GameManager.RootModel.GetService<ScriptContext>();
                InstanceBridge.InstanceClassCache cache = scriptContext.ResolveInstanceClassCacheForType(type);

                for (int j = 0; j < element.ChildNodes.Count; j++)
                {
                    XmlElement? propertyElement = element.ChildNodes[j] as XmlElement;

                    if (propertyElement == null)
                        continue;

                    string propertyName = propertyElement.GetAttribute("name");
                    InstanceBridge.InstanceClassCacheProperty? property = cache.TryGetProperty(propertyName);
                    if (property == null)
                        continue;

                    PropertyInfo propertyInfo = property.Property;

                    if (propertyName == "Parent")
                        continue;
                    if (!propertyInfo.CanWrite)
                        continue;

                    switch (propertyElement.Name)
                    {
                        case "BinaryString":
                            propertyInfo.SetValue(myInstance, Encoding.UTF8.GetString(Convert.FromBase64String(propertyElement.InnerText)));
                            break;
                        case "BrickColor":
                            propertyInfo.SetValue(myInstance, BrickColor.GetBrickColorByIndex(int.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture)));
                            break;
                        case "Color3":
                            propertyInfo.SetValue(myInstance, Color3.FromBGRA(uint.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture)));
                            break;
                        case "Color3uint8":
                            propertyInfo.SetValue(myInstance, Color3.FromARGBuint8(uint.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture)));
                            break;
                        case "CoordinateFrame":
                        {
                            CoordinateFrame cframe = default;

                            XmlElement? xElement = propertyElement.GetElementsByTagName("X")[0] as XmlElement;
                            XmlElement? yElement = propertyElement.GetElementsByTagName("Y")[0] as XmlElement;
                            XmlElement? zElement = propertyElement.GetElementsByTagName("Z")[0] as XmlElement;

                            XmlElement? r00Element = propertyElement.GetElementsByTagName("R00")[0] as XmlElement;
                            XmlElement? r01Element = propertyElement.GetElementsByTagName("R01")[0] as XmlElement;
                            XmlElement? r02Element = propertyElement.GetElementsByTagName("R02")[0] as XmlElement;
                            XmlElement? r10Element = propertyElement.GetElementsByTagName("R10")[0] as XmlElement;
                            XmlElement? r11Element = propertyElement.GetElementsByTagName("R11")[0] as XmlElement;
                            XmlElement? r12Element = propertyElement.GetElementsByTagName("R12")[0] as XmlElement;
                            XmlElement? r20Element = propertyElement.GetElementsByTagName("R20")[0] as XmlElement;
                            XmlElement? r21Element = propertyElement.GetElementsByTagName("R21")[0] as XmlElement;
                            XmlElement? r22Element = propertyElement.GetElementsByTagName("R22")[0] as XmlElement;

                            if (xElement != null) cframe.Position.X = float.Parse(xElement.InnerText, CultureInfo.InvariantCulture);
                            if (yElement != null) cframe.Position.Y = float.Parse(yElement.InnerText, CultureInfo.InvariantCulture);
                            if (zElement != null) cframe.Position.Z = float.Parse(zElement.InnerText, CultureInfo.InvariantCulture);

                            if (r00Element != null) cframe.Orientation.M11 = float.Parse(r00Element.InnerText, CultureInfo.InvariantCulture);
                            if (r01Element != null) cframe.Orientation.M12 = float.Parse(r01Element.InnerText, CultureInfo.InvariantCulture);
                            if (r02Element != null) cframe.Orientation.M13 = float.Parse(r02Element.InnerText, CultureInfo.InvariantCulture);
                            if (r10Element != null) cframe.Orientation.M21 = float.Parse(r10Element.InnerText, CultureInfo.InvariantCulture);
                            if (r11Element != null) cframe.Orientation.M22 = float.Parse(r11Element.InnerText, CultureInfo.InvariantCulture);
                            if (r12Element != null) cframe.Orientation.M23 = float.Parse(r12Element.InnerText, CultureInfo.InvariantCulture);
                            if (r20Element != null) cframe.Orientation.M31 = float.Parse(r20Element.InnerText, CultureInfo.InvariantCulture);
                            if (r21Element != null) cframe.Orientation.M32 = float.Parse(r21Element.InnerText, CultureInfo.InvariantCulture);
                            if (r22Element != null) cframe.Orientation.M33 = float.Parse(r22Element.InnerText, CultureInfo.InvariantCulture);

                            cframe.Orientation.M44 = 1;
                            
                            propertyInfo.SetValue(myInstance, cframe);
                            break;
                        }
                        case "Content":
                            if (propertyElement.FirstChild == null)
                                ThrowMalformedException();
                            if (propertyElement.FirstChild.Name == "null")
                                propertyInfo.SetValue(myInstance, "");
                            else if (propertyElement.FirstChild.Name == "url")
                                propertyInfo.SetValue(myInstance, propertyElement.FirstChild.InnerText);
                            else
                                ThrowMalformedException();
                            break;
                        case "Faces":
                            if (propertyElement.FirstChild == null)
                                ThrowMalformedException();
                            if (propertyElement.FirstChild.Name == "faces")
                                propertyInfo.SetValue(myInstance, (Faces)uint.Parse(propertyElement.FirstChild.InnerText, CultureInfo.InvariantCulture));
                            else
                                ThrowMalformedException();
                            break;
                        case "Ref":
                            if (instances.TryGetValue(propertyElement.InnerText, out Instance? value))
                            {
                                if (propertyInfo.PropertyType.IsAssignableTo(typeof(Instance)))
                                    propertyInfo.SetValue(myInstance, value);
                                else if (propertyInfo.PropertyType == typeof(ulong))
                                    propertyInfo.SetValue(myInstance, value.InstanceID);
                            }
                            else
                            {
                                refFixups.Add((propertyInfo, myInstance, propertyElement.InnerText));
                            }
                            break;
                        case "ProtectedString":
                            propertyInfo.SetValue(myInstance, propertyElement.InnerText);
                            break;
                        case "SharedString":
                            propertyInfo.SetValue(myInstance, sharedStrings[propertyElement.InnerText]);
                            break;
                        case "UDim":
                        {
                            UDim uDim = default;

                            XmlElement? sElement = propertyElement.GetElementsByTagName("S")[0] as XmlElement;
                            XmlElement? oElement = propertyElement.GetElementsByTagName("O")[0] as XmlElement;

                            if (sElement != null)
                                uDim.Scale = float.Parse(sElement.InnerText, CultureInfo.InvariantCulture);
                            if (oElement != null)
                                uDim.Offset = float.Parse(oElement.InnerText, CultureInfo.InvariantCulture);

                            propertyInfo.SetValue(myInstance, uDim);
                            break;
                        }
                        case "UDim2":
                        {
                            UDim2 uDim2 = default;

                            XmlElement? xsElement = propertyElement.GetElementsByTagName("XS")[0] as XmlElement;
                            XmlElement? xoElement = propertyElement.GetElementsByTagName("XO")[0] as XmlElement;
                            XmlElement? ysElement = propertyElement.GetElementsByTagName("YS")[0] as XmlElement;
                            XmlElement? yoElement = propertyElement.GetElementsByTagName("YO")[0] as XmlElement;

                            if (xsElement != null)
                                uDim2.X.Scale = float.Parse(xsElement.InnerText, CultureInfo.InvariantCulture);
                            if (xoElement != null)
                                uDim2.X.Offset = float.Parse(xoElement.InnerText, CultureInfo.InvariantCulture);
                            if (ysElement != null)
                                uDim2.Y.Scale = float.Parse(ysElement.InnerText, CultureInfo.InvariantCulture);
                            if (yoElement != null)
                                uDim2.Y.Offset = float.Parse(yoElement.InnerText, CultureInfo.InvariantCulture);
                            
                            propertyInfo.SetValue(myInstance, uDim2);
                            break;
                        }
                        case "Vector2":
                        {
                            Vector2 vector2 = default;

                            XmlElement? xElement = propertyElement.GetElementsByTagName("X")[0] as XmlElement;
                            XmlElement? yElement = propertyElement.GetElementsByTagName("Y")[0] as XmlElement;

                            if (xElement != null)
                                vector2.X = float.Parse(xElement.InnerText, CultureInfo.InvariantCulture);
                            if (yElement != null)
                                vector2.Y = float.Parse(yElement.InnerText, CultureInfo.InvariantCulture);
                            
                            propertyInfo.SetValue(myInstance, vector2);
                            break;
                        }
                        case "Vector3":
                        {
                            Vector3 vector3 = default;

                            XmlElement? xElement = propertyElement.GetElementsByTagName("X")[0] as XmlElement;
                            XmlElement? yElement = propertyElement.GetElementsByTagName("Y")[0] as XmlElement;
                            XmlElement? zElement = propertyElement.GetElementsByTagName("Z")[0] as XmlElement;

                            if (xElement != null)
                                vector3.X = float.Parse(xElement.InnerText, CultureInfo.InvariantCulture);
                            if (yElement != null)
                                vector3.Y = float.Parse(yElement.InnerText, CultureInfo.InvariantCulture);
                            if (zElement != null)
                                vector3.Z = float.Parse(zElement.InnerText, CultureInfo.InvariantCulture);
                            
                            propertyInfo.SetValue(myInstance, vector3);
                            break;
                        }
                        case "bool":
                            propertyInfo.SetValue(myInstance, bool.Parse(propertyElement.InnerText));
                            break;
                        case "double":
                            propertyInfo.SetValue(myInstance, 
                                Convert.ChangeType(double.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture), propertyInfo.PropertyType));
                            break;
                        case "float":
                            propertyInfo.SetValue(myInstance, 
                                Convert.ChangeType(float.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture), propertyInfo.PropertyType));
                            break;
                        case "int":
                            if (propertyInfo.PropertyType == typeof(BrickColor))
                                propertyInfo.SetValue(myInstance, 
                                    BrickColor.GetBrickColorByIndex(int.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture)));
                            propertyInfo.SetValue(myInstance, 
                                Convert.ChangeType(int.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture), propertyInfo.PropertyType));
                            break;
                        case "int64":
                            propertyInfo.SetValue(myInstance, 
                                Convert.ChangeType(long.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture), propertyInfo.PropertyType));
                            break;
                        case "string":
                            propertyInfo.SetValue(myInstance, propertyElement.InnerText);
                            break;
                        case "token":
                            object enumvalue = Enum.ToObject(propertyInfo.PropertyType, int.Parse(propertyElement.InnerText, CultureInfo.InvariantCulture));
                            propertyInfo.SetValue(myInstance, enumvalue);
                            break;
                    }
                }
            }
        }

        return myInstance;
    }
    public Instance[] LoadModelXml(string path)
    {
        List<Instance> allInstances = [];
        string allData = File.ReadAllText(path);
        xmlDocument = new XmlDocument();

        xmlDocument.LoadXml(allData);

        XmlElement? rootElement = xmlDocument.FirstChild as XmlElement;
        if (rootElement == null || rootElement.Name != "roblox")
            ThrowMalformedException();
        if (rootElement.GetAttribute("version") != "4")
            ThrowMalformedException();
        
        Trace.TraceInformation("Loading model from local file at path: " + path);

        for (int i = 0; i < rootElement.ChildNodes.Count; i++)
        {
            XmlElement? rootChild = rootElement.ChildNodes[i] as XmlElement;
            if (rootChild == null)
                continue;

            if (rootChild.Name == "SharedStrings")
            {
                for (int j = 0; j < rootChild.ChildNodes.Count; j++)
                {
                    XmlElement? sharedStringElement = rootChild.ChildNodes[j] as XmlElement;
                    if (sharedStringElement == null)
                        continue;
                    if (sharedStringElement.Name != "SharedString")
                        continue;
                    sharedStrings[sharedStringElement.GetAttribute("md5")] = sharedStringElement.InnerText;
                }
            }
        }

        for (int i = 0; i < rootElement.ChildNodes.Count; i++)
        {
            XmlElement? rootChild = rootElement.ChildNodes[i] as XmlElement;
            if (rootChild == null)
                continue;

            if (rootChild.Name == "Item")
            {
                Instance? result = ParseItemXml(false, rootChild);
                if (result != null)
                    allInstances.Add(result);
            }
        }

        for (int i = 0; i < refFixups.Count; i++)
        {
            var tuple = refFixups[i];
            if (instances.TryGetValue(tuple.Reference, out Instance? targetInstance))
            {
                if (tuple.Property.PropertyType.IsAssignableTo(typeof(Instance)))
                    tuple.Property.SetValue(tuple.Instance, targetInstance);
                else if (tuple.Property.PropertyType == typeof(ulong))
                    tuple.Property.SetValue(tuple.Instance, targetInstance.InstanceID);
            }
        }

        return allInstances.ToArray();
    }
    private void LoadPlaceXml(string path)
    {
        using Stream stream = File.OpenRead(path);
        xmlDocument = new XmlDocument();

        xmlDocument.Load(stream);

        XmlElement? rootElement = xmlDocument.FirstChild as XmlElement;
        if (rootElement == null || rootElement.Name != "roblox")
            ThrowMalformedException();
        if (rootElement.GetAttribute("version") != "4")
            ThrowMalformedException();
        
        Trace.TraceInformation("Loading place from local file at path: " + path);

        for (int i = 0; i < rootElement.ChildNodes.Count; i++)
        {
            XmlElement? rootChild = rootElement.ChildNodes[i] as XmlElement;
            if (rootChild == null)
                continue;

            if (rootChild.Name == "SharedStrings")
            {
                for (int j = 0; j < rootChild.ChildNodes.Count; j++)
                {
                    XmlElement? sharedStringElement = rootChild.ChildNodes[j] as XmlElement;
                    if (sharedStringElement == null)
                        continue;
                    if (sharedStringElement.Name != "SharedString")
                        continue;
                    sharedStrings[sharedStringElement.GetAttribute("md5")] = sharedStringElement.InnerText;
                }
            }
        }

        for (int i = 0; i < rootElement.ChildNodes.Count; i++)
        {
            XmlElement? rootChild = rootElement.ChildNodes[i] as XmlElement;
            if (rootChild == null)
                continue;

            if (rootChild.Name == "Item")
                ParseItemXml(true, rootChild);
        }

        for (int i = 0; i < refFixups.Count; i++)
        {
            var tuple = refFixups[i];
            if (instances.TryGetValue(tuple.Reference, out Instance? targetInstance))
            {
                if (tuple.Property.PropertyType.IsAssignableTo(typeof(Instance)))
                    tuple.Property.SetValue(tuple.Instance, targetInstance);
                else if (tuple.Property.PropertyType == typeof(ulong))
                    tuple.Property.SetValue(tuple.Instance, targetInstance.InstanceID);
            }
        }
    }
    // made with https://github.com/RobloxAPI/spec/blob/master/formats/rbxl.md
    private void LoadPlaceBinary(string path)
    {
        using Stream stream = File.OpenRead(path);
        using BinaryReader superReader = new BinaryReader(stream);

        // check header signature, it's supposedly the same everywhere

        if (superReader.ReadUInt64() != 0x21786F6C626F723C)
            ThrowMalformedException();
        if (superReader.ReadUInt32() != 0x0A0DFF89)
            ThrowMalformedException();
        if (superReader.ReadUInt16() != 0x0A1A)
            ThrowMalformedException();
        
        ushort version = superReader.ReadUInt16();

        if (version != 0)
            ThrowNotSupportedException();
        
        int classCount = superReader.ReadInt32();
        int instanceCount = superReader.ReadInt32();
        stream.Position += 8;

        Dictionary<string, string> metadata = [];
        Dictionary<int, string> classesIdToName = [];
        Dictionary<int, Dictionary<int, Instance>> binstance = [];
        Dictionary<int, Dictionary<string, List<object>>> bpropval = [];

        bool endReached = false;

        int[] ReadReferences(BinaryReader br, int count)
        {
            byte[] buffer = br.ReadBytes(count * 4);
            Span<byte> work = stackalloc byte[4];
            var values = new int[count];

            for (int i = 0; i < count; i++)
            {
                work[0] = buffer[(3 * count) + i];
                work[1] = buffer[(2 * count) + i];
                work[2] = buffer[(1 * count) + i];
                work[3] = buffer[(0 * count) + i];

                int value = BitConverter.ToInt32(work);
                values[i] = (int)((uint)value >> 1) ^ (-(value & 1));
                if (i > 1)
                    values[i] += values[i - 1];
            }

            return values;
        }
        string ReadRobloxString(BinaryReader binaryReader)
        {
            return Encoding.UTF8.GetString(binaryReader.ReadBytes(binaryReader.ReadInt32()));
        }
        void ProcessChunk(uint chunkName, byte[] chunkData)
        {
            using MemoryStream ms = new MemoryStream(chunkData);
            using BinaryReader br = new BinaryReader(ms);

            if (chunkName == 0x4154454D) // META
            {
                uint length = br.ReadUInt32();
                for (uint i = 0; i < length; i++)
                {
                    string key = ReadRobloxString(br);
                    string val = ReadRobloxString(br);
                    metadata[key] = val;
                }
            }
            else if (chunkName == 0x52545353) // SSTR
            {
                int version = br.ReadInt32();
                if (version != 0)
                    ThrowNotSupportedException();
                
                uint length = br.ReadUInt32();

                for (uint i = 0; i < length; i++)
                {
                    Guid guid = new Guid(br.ReadBytes(16));
                    string str = ReadRobloxString(br);

                    sharedStrings[guid.ToString()] = str;
                }
            }
            else if (chunkName == 0x54534E49) // INST
            {
                int classId = br.ReadInt32();
                string className = ReadRobloxString(br);
                bool hasService = br.ReadBoolean();
                int length = br.ReadInt32();

                classesIdToName[classId] = className;

                if (!GameManager.GameRegistry.IsTypeDefined(className))
                {
                    Trace.TraceWarning("Unknown Instance type while loading a place/model file: " + className);
                    return;
                }

                int[] references = ReadReferences(br, length);

                binstance[classId] = new Dictionary<int, Instance>();

                for (int i = 0; i < length; i++)
                    binstance[classId][references[i]] = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(className)!;

                // actually we'll not read the rest fuck that
            }
            else if (chunkName == 0x504F5250) // PROP
            {
                int classId = br.ReadInt32();
                string propertyName = ReadRobloxString(br);

                if (!bpropval.ContainsKey(classId))
                    bpropval[classId] = new Dictionary<string, List<object>>();
                if (!bpropval[classId].ContainsKey(propertyName))
                    bpropval[classId][propertyName] = new List<object>();

                
            }
        }

        while (!endReached)
        {
            uint chunkName = superReader.ReadUInt32();
            uint compressedLength = superReader.ReadUInt32();
            uint uncompressedLength = superReader.ReadUInt32();

            stream.Position += 4;

            if (compressedLength == 0 && uncompressedLength != 0)
                ProcessChunk(chunkName, superReader.ReadBytes((int)uncompressedLength));
            else
            {
                
            }
        }
    }
    public void LoadPlaceMultiplexed(string path)
    {
        if (!File.Exists(path))
            throw new InvalidDataException("Cannot load this place file - no such file");

        bool isXml = false;
        {
            using FileStream fileStream = File.OpenRead(path);
            byte[] newbuffer = new byte[8];
            fileStream.ReadExactly(newbuffer);

            string marker = Encoding.ASCII.GetString(newbuffer);

            if (marker == "<roblox ")
                isXml = true;
            else if (marker == "<roblox!")
                isXml = false;
            else
                throw new InvalidDataException("Cannot load this place file - not a place");
        }

        if (isXml)
            LoadPlaceXml(path);
        else
            LoadPlaceBinary(path);
    }
}