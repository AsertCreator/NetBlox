using System.Reflection;
using System.Text;

namespace NBApiDumper;

public static class Program
{
    static Assembly? assembly;

    enum SimpleSecurityCapabilityLevel
    {
        Default, Inaccessible, LocalUser, RobloxScript, RobloxEngine, Plugin, ElevatedPlugin, WritePlayer, DestroyServices
    }
    record class CachedMethodInfo
    {
        public required string Name;
        public required MethodInfo Method;
        public required SimpleSecurityCapabilityLevel RequiredLevel;
        public required (string, string)[] Parameters;
        public required string ReturnType;
        public required bool Deprecated;
    }

    static string DescribeType(Type type)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# " + type.Name);

        MethodInfo[] methodinfos = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        PropertyInfo[] propertyinfos = type.GetProperties();

        List<CachedMethodInfo> cachedMethodInfos = [];
        List<CachedMethodInfo> privateMethods = [];
        List<CachedMethodInfo> publicMethods = [];

        Type scriptCallableAttribute = assembly!.GetType("NetBlox.Runtime.ScriptCallableAttribute")!;
        PropertyInfo scriptCallableLevelValueProperty = scriptCallableAttribute.GetProperty("RequiredLevel")!;

        foreach (MethodInfo methodinfo in methodinfos)
        {
            Attribute? attribute = methodinfo.GetCustomAttribute(scriptCallableAttribute);
            if (attribute == null)
                continue;
            
            SimpleSecurityCapabilityLevel level = (SimpleSecurityCapabilityLevel)Enum.ToObject(typeof(SimpleSecurityCapabilityLevel),
                Convert.ToInt32(scriptCallableLevelValueProperty.GetValue(attribute)));

            CachedMethodInfo cachedMethodInfo = new CachedMethodInfo()
            {
                Name = methodinfo.Name,
                Method = methodinfo,
                ReturnType = methodinfo.ReturnType.Name,
                Parameters = methodinfo.GetParameters().Select(x => (x.ParameterType.Name, x.Name)).ToArray()!,
                Deprecated = methodinfo.GetCustomAttributes<ObsoleteAttribute>() != null,
                RequiredLevel = level
            };

            cachedMethodInfos.Add(cachedMethodInfo);
            if (cachedMethodInfo.RequiredLevel <= SimpleSecurityCapabilityLevel.LocalUser)
                publicMethods.Add(cachedMethodInfo);
            else
                privateMethods.Add(cachedMethodInfo);
        }

        sb.AppendLine("## Methods");
        sb.AppendLine($"There are {cachedMethodInfos.Count} scriptable methods defined in type {type.Name}. {publicMethods.Count} of them are public" +
            $", and {privateMethods.Count} of them are private.");

        foreach (CachedMethodInfo cachedMethod in publicMethods)
        {
            sb.AppendLine("<details open>");
            sb.AppendLine("<summary><b>- " + cachedMethod.Name + "</b>: <i>" + cachedMethod.ReturnType + " (" + cachedMethod.RequiredLevel + ")</i></summary>\n");
            sb.AppendLine("|Parameter type|Parameter name|\n|-----|----|");
            foreach ((string propertyType, string propertyName) in cachedMethod.Parameters)
            {
                sb.AppendLine("| " + propertyType + " | " + propertyName + "|");
            }
            sb.AppendLine("</details>");
        }

        sb.AppendLine("<details>");
        sb.AppendLine("<summary><i>Private methods</i></summary>\n");

        foreach (CachedMethodInfo cachedMethod in privateMethods)
        {
            sb.AppendLine("<details open>");
            sb.AppendLine("<summary><b>- " + cachedMethod.Name + "</b>: <i>" + cachedMethod.ReturnType + " (" + cachedMethod.RequiredLevel + ")</i></summary>\n");
            sb.AppendLine("|Parameter type|Parameter name|\n|-----|----|");
            foreach ((string propertyType, string propertyName) in cachedMethod.Parameters)
            {
                sb.AppendLine("| " + propertyType + " | " + propertyName + "|");
            }
            sb.AppendLine("</details>");
        }

        sb.AppendLine("</details>");

        return sb.ToString();
    }
    static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("must be provided two arguments: <NetBlox.dll path> <output directory>");
            return 1;
        }

        DirectoryInfo directoryInfo = new DirectoryInfo(args[1]);
        if (!directoryInfo.Exists)
            directoryInfo.Create();

        string fullNetBloxDllPath = Path.GetFullPath(args[0]);
        string moonsharpDllPath = Path.Combine(Path.GetDirectoryName(fullNetBloxDllPath)!, "MoonSharp.Interpreter.dll");
        string raylibCsDllPath = Path.Combine(Path.GetDirectoryName(fullNetBloxDllPath)!, "Raylib-cs.dll");

        if (File.Exists(moonsharpDllPath))
            Assembly.LoadFrom(moonsharpDllPath);
        if (File.Exists(raylibCsDllPath))
            Assembly.LoadFrom(raylibCsDllPath);

        assembly = Assembly.LoadFile(fullNetBloxDllPath);
        Type? type = assembly.GetType("NetBlox.Instances.Instance");

        if (type == null)
        {
            Console.Error.WriteLine("NetBlox.dll doesn't contain the Instance type");
            return 1;
        }

        try
        {
            Type[] potentialInstanceTypes = assembly.GetTypes();
            List<Type> instanceTypes = [];

            for (int i = 0; i < potentialInstanceTypes.Length; i++)
            {
                Type instanceType = potentialInstanceTypes[i];
                if (!instanceType.IsAssignableTo(type))
                    continue;

                instanceTypes.Add(instanceType);

                string stringData = DescribeType(instanceType);
                File.WriteAllText(Path.Combine(directoryInfo.FullName, instanceType.Name + ".md"), stringData);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            Console.Error.WriteLine(ex.GetType() + ", msg: " + ex.Message);
            foreach (Exception? sex in ex.LoaderExceptions)
            {
                if (sex == null)
                    continue;
                Console.Error.WriteLine("Subexception: " + sex.GetType() + ", msg: " + sex.Message.TrimEnd());
            }
        }
        

        return 0;
    }
}