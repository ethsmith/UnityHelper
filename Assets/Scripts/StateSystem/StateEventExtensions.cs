using EventSystem;

namespace StateSystem
{
    public static class StateEventExtensions
    {
        public static void Listen<T>(this State state, System.Action<T> callback, int priority = 0) where T : Event
        {
            EventBus.ListenTo(state, callback, priority);
        }

        public static void StopListening<T>(this State state, System.Action<T> callback) where T : Event
        {
            EventBus.StopListening(callback);
        }

        public static void StopListeningToAll(this State state)
        {
            EventBus.StopListeningToAll(state);
        }
    }
}