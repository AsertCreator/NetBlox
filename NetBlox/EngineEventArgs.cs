namespace NetBlox;

public delegate void EngineEventHandler(EngineEventArgs eventArgs);

public class EngineEventArgs
{
    public required GameEvent GameEvent;
    public required object? EventData;
}