using System.Collections.Generic;
using UnityEngine;

namespace StateSystem
{
    public class StateManager : MonoBehaviour
    {
        private Dictionary<string, State> _states = new();

        public void RegisterState(State state)
        {
            _states[state.Id] = state;
        }

        public void EnableState(string id)
        {
            if (_states.TryGetValue(id, out var state))
                state.Enable();
        }

        public void DisableState(string id)
        {
            if (_states.TryGetValue(id, out var state))
                state.Disable();
        }

        private void Update()
        {
            foreach (var state in _states.Values)
            {
                state.Update();
            }
        }
    }
}