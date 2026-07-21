namespace Iridium.Runtime
{
    public interface IPatchDefinition
    {
        string Id { get; }
        RuntimeKind[] SupportedRuntimes { get; }
    }
}
