namespace ModSystem
{
    public interface IMod
    {
        string ModId();
        string ModVersion();
        string ModAuthor();
        bool IsMpCompatible();
    }
}