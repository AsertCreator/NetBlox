using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Xml;
using NetBlox.Instances;
using NetBlox.Structs;

namespace NetBlox;

public class PlaceParser
{
    public GameManager GameManager { get; init; }

    private XmlDocument? xmlDocument;
    private Dictionary<string, Instance> instances = [];
    private Dictionary<string, string> sharedStrings = [];
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
    private Instance? ParseItem(bool isRoot, XmlElement xmlElement)
    {
        string className = xmlElement.GetAttribute("class");
        Instance? myInstance = null;

        if (isRoot)
            myInstance = GameManager.RootModel.GetService(className);
        else
            myInstance = GameManager.GameRegistry.TryCreateNewDomesticInstanceOfClass(className);
        
        if (myInstance == null)
        {
            Trace.TraceWarning("Unknown Instance type while loading a place/model file: " + className);
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
                Instance? child = ParseItem(false, element);
                if (child != null)
                    child.Parent = myInstance;
            }
            else if (element.Name == "Properties")
            {
                for (int j = 0; j < element.ChildNodes.Count; j++)
                {
                    XmlElement? propertyElement = element.ChildNodes[j] as XmlElement;

                    if (propertyElement == null)
                        continue;

                    string propertyName = propertyElement.GetAttribute("name");
                    PropertyInfo? propertyInfo = type.GetProperty(propertyName);

                    if (propertyName == "Parent")
                        continue;
                    if (propertyInfo == null)
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
                            // TODO: do this
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
                Instance? result = ParseItem(false, rootChild);
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
        string allData = File.ReadAllText(path);
        xmlDocument = new XmlDocument();

        xmlDocument.LoadXml(allData);

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
                ParseItem(true, rootChild);
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
    private void LoadPlaceBinary(string path)
    {
        // TODO: load place binary
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

            if (marker == "<roblox>")
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