using System.Runtime.InteropServices;

public static partial class NativeMethods
{
    [LibraryImport("libc", SetLastError = true)]
    public static partial int nanosleep(ref Timespec req, ref Timespec rem);
}

[StructLayout(LayoutKind.Sequential)]
public struct Timespec
{
    public nint tv_sec;
    public nint tv_nsec;
}