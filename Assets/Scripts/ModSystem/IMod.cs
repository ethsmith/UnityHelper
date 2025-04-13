namespace ModSystem
{
    public interface IMod
    {
        string ModId { get; }
        string ModVersion { get; }
        string ModAuthor { get; }
        string IsMpCompatible { get; }
    }
}