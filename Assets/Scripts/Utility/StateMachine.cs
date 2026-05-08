using System;

namespace BC.Utility
{
    // StateMachineを扱うためのクラス
    public class StateMachine<T>
    {
        private T _currentState;

        public T CurrentState => _currentState;
        private int _stateChangeVersion = 0;
        private Action<T> _onStateChange;

        public void ChangeState(T newState)
        {
            _currentState = newState;
            _stateChangeVersion++;
            _onStateChange?.Invoke(newState);
        }

        public void Subscribe(Action<T> callback)
        {
            _onStateChange += callback;
        }
        public void Unsubscribe(Action<T> callback)
        {
            _onStateChange -= callback;
        }

    }
}