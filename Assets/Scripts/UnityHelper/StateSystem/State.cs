namespace UnityHelper.StateSystem
{
    public abstract class State
    {
        protected State(string id)
        {
            Id = id;
        }

        public string Id { get; private set; }
        public bool IsActive { get; private set; }

        public void Enable()
        {
            if (!IsActive)
            {
                IsActive = true;
                RegisterListeners();
                OnEnter();
            }
        }

        public void Disable()
        {
            if (IsActive)
            {
                UnregisterListeners();
                OnExit();
                IsActive = false;
            }
        }

        public void Update()
        {
            if (IsActive) OnUpdate();
        }

        protected abstract void OnEnter();
        protected abstract void OnExit();
        protected abstract void OnUpdate();

        protected virtual void RegisterListeners()
        {
        }

        protected virtual void UnregisterListeners()
        {
        }
    }
}