namespace EventSystem
{
    public abstract class Event
    {
        public object Sender { get; }

        protected Event(object sender)
        {
            Sender = sender;
        }
    }
}