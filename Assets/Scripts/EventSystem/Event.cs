namespace EventSystem
{
    public abstract class Event
    {
        protected Event(object sender)
        {
            Sender = sender;
        }

        public object Sender { get; }
    }
}