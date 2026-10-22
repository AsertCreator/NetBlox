namespace NetBlox;

public static class Version
{
    public const int VersionMajor = 19;
    public const int VersionMinor = 1;
    public const int VersionPatch = 0;

    public static string VersionString => VersionMajor + "." + VersionMinor + "." + VersionPatch;
}