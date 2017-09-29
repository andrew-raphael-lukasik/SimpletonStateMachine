using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// State machine for simpletons.
/// </summary>
class SimpletonStateMachine : IEnumerator<SimpletonStateMachine.State>
{
    #region FIELDS

    State _initial;
    State _current;

    #endregion
    #region CONSTRUCTORS

    public SimpletonStateMachine ( State initial )
    {
        this._initial = initial;
        this._current = initial;
    }

    #endregion
    #region IEnumerator implementation

    public bool MoveNext ()
    {
        if( _current!=null )
        {
            //make sure OnEnter was called:
            //NOTE: this could be put inside SimpletonStateMachine's constructor but
            //      it would prohibit OnEnter from containing useful main thread code there
            if( _current.entered==false )
            {
                ( (State.IInternalStateCalls)_current ).CallOnEnter();
            }

            //update current state:
            _current.OnUpdate();

            //test transitions:
            for( int i = 0 ; i<_current.transitions.Count ; i++ )
            {
                var next = _current.transitions[ i ];
                if( next.condition() )
                {
                    //previous state exit:
                    ( (State.IInternalStateCalls)_current ).CallOnExit();

                    //swat states:
                    _current = next.state;

                    //new state enter:
                    ( (State.IInternalStateCalls)_current ).CallOnEnter();
                }
            }

            //return:
            return true;
        } else
        {
            //return:
            return false;
        }
    }

    public void Reset ()
    {
        ( (State.IInternalStateCalls)_current ).CallOnExit();
        _current = _initial;
    }

    object IEnumerator.Current { get { return _current; } }

    #endregion
    #region IDisposable implementation

    bool _disposed = false;

    public void Dispose ()
    {
        if( _disposed==false )
        {
            //Dispose current:
            _current.Dispose();
            _current = null;

            //Dispose initial:
            _initial.Dispose();
            _initial = null;
        } else
        {
            Debug.LogWarning( "Dispose() was called earlier already!" );
        }
    }

    #endregion
    #region IEnumerator implementation

    public State Current { get { return _current; } }

    #endregion
    #region NESTED_CLASSES

    /// <summary>
    /// State for SimpletonStateMachine.
    /// </summary>
    public abstract class State : State.IInternalStateCalls, System.IDisposable
    {
        public bool entered { get; private set; }
        public bool exited{ get; private set; }
        public abstract void OnEnter ();
        public abstract void OnExit ();
        public abstract void OnUpdate ();
        public List<ConditionState> transitions = new List<ConditionState>( 5 );

        #region constructors

        public State ( params ConditionState[] transitions )
        {
            this.transitions.AddRange( transitions );
        }

        #endregion
        #region IInternalStateCalls implementation

        bool _enter_called;
        bool _exit_called;

        void State.IInternalStateCalls.CallOnEnter ()
        {
            if( _enter_called==false )
            {
                //execute OnEnter:
                OnEnter();

                //set enter flag:
                _enter_called = true;

                //reset exit flag:
                _exit_called = false;
            }
        }

        void State.IInternalStateCalls.CallOnExit ()
        {
            if( _exit_called==false )
            {
                //execute OnExit:
                OnExit();

                //set exit flag:
                _exit_called = true;

                //reset enter flag:
                _enter_called = false;
            }
        }

        #endregion
        #region IDisposable implementation

        bool _disposed = false;

        public virtual void Dispose ()
        {
            if( _disposed==false )
            {
                //forward Dispose:
                foreach( var transition in transitions )
                {
                    transition.state.Dispose();
                }

                //clear list of transitions:
                transitions.Clear();
            }
        }

        public interface IInternalStateCalls
        {
            void CallOnEnter ();
            void CallOnExit ();
        }

        #endregion
        #region nested_classes

        public class ConditionState
        {
            public System.Func<bool> condition;
            public State state;
            public ConditionState ( System.Func<bool> condition , State state )
            {
                this.condition = condition;
                this.state = state;
            }
        }

        #endregion
    }

    #endregion
}