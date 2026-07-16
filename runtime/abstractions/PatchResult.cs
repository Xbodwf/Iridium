namespace Iridium.Runtime
{
    public enum PatchResultState
    {
        Applied,
        Removed,
        NotFound,
        Failed
    }

    public sealed class PatchResult
    {
        public PatchResultState State { get; }
        public string Message { get; }
        public bool Succeeded => State == PatchResultState.Applied || State == PatchResultState.Removed;

        private PatchResult(PatchResultState state, string message)
        {
            State = state;
            Message = message;
        }

        public static PatchResult Applied(string message) => new(PatchResultState.Applied, message);
        public static PatchResult Removed(string message) => new(PatchResultState.Removed, message);
        public static PatchResult NotFound(string message) => new(PatchResultState.NotFound, message);
        public static PatchResult Failed(string message) => new(PatchResultState.Failed, message);
    }
}
