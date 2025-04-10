namespace EventSystem
{
    public interface ICancellable
    {
        bool IsCancelled { get; }
        void Cancel();
    }
}