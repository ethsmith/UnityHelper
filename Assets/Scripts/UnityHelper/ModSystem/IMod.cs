namespace UnityHelper.ModSystem
{
    public interface IMod
    {
        string ModId();
        string ModVersion();
        string ModAuthor();
        bool IsMpCompatible();

        void Start();
        
        void Stop();
        
        void Update();
    }
}