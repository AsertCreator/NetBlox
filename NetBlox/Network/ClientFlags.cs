namespace NetBlox.Network;

[Flags]
public enum ClientFlags : short
{
    IsDesktop = 1 << 0, IsMobile = 1 << 1, Authenticated = 1 << 2
}

public static class ClientFlagsExtension
{
    public static ClientFlags Evaluate(this GameManager gm)
    {
        ClientFlags flags = 0;
        if (gm.GameRenderer!.IsDesktop)
            flags |= ClientFlags.IsDesktop;
        else
            flags |= ClientFlags.IsMobile;
        if (gm.CloudConfiguration.UserId > 0)
            flags |= ClientFlags.Authenticated;
        return flags;
    }
}