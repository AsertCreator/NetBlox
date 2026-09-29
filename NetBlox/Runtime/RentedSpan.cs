using System.Buffers;

namespace NetBlox.Runtime;

public ref struct RentedSpan<T>
{
    private T[] array;
    public Span<T> Values;

    public RentedSpan(int i)
    {
        array = ArrayPool<T>.Shared.Rent(i);
        Values = array.AsSpan(0, i);
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(array);
    }
}