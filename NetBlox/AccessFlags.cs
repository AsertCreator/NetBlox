namespace NetBlox;

[Flags]
public enum AccessFlags
{
    DisableUserScripts = 1, DisableCoreScripts = 2, DisableHttpAccess = 4, DisableNetworking = 8, DisableFilesystemAccess = 16, DisableCaching = 32,
    AllowCustomAssemblies = 64
}