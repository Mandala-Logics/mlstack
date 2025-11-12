using System;

namespace DDLib
{
    /// <summary>
    /// Represents the state of the timeout.
    /// </summary>
    public enum TimeoutState { Initialized, Normal, TimedOut }

    /// <summary>
    /// Provides an abstract base class for managing timeouts.
    /// </summary>
    public abstract class TimeoutBase
    {
        //PUBLIC PROPERTIES
        /// <summary>
        /// Gets the current state of the timeout.
        /// </summary>
        public TimeoutState State { get; private set; }

        /// <summary>
        /// Gets the duration of the timeout.
        /// </summary>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Gets the last time the timeout was touched.
        /// </summary>
        public DateTime LastTouched { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current time has passed the timeout period since the last touch.
        /// </summary>
        public bool IsPastTime => State == TimeoutState.Normal && DateTime.Now >= LastTouched + Timeout;

        //CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutBase"/> class with the specified timeout duration and state.
        /// </summary>
        /// <param name="timeout">The duration of the timeout.</param>
        /// <param name="state">The initial state of the timeout.</param>
        /// <exception cref="ArgumentException">Thrown if the timeout is not between 10 seconds and 10 minutes.</exception>
        public TimeoutBase(TimeSpan timeout, TimeoutState state) 
        {
            if (timeout.TotalMinutes > 10) throw new ArgumentException("Timeout greater than ten minutes.");
            if (timeout.TotalSeconds < 10) throw new ArgumentException("Timeout less than ten seconds.");

            Timeout = timeout;
            State = state;
            LastTouched = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutBase"/> class with the specified timeout duration.
        /// </summary>
        /// <param name="timeout">The duration of the timeout.</param>
        /// <exception cref="ArgumentException">Thrown if the timeout is not between 10 seconds and 10 minutes.</exception>
        public TimeoutBase(TimeSpan timeout) : this(timeout, TimeoutState.Initialized) { }

        //PUBLIC METHODS
        /// <summary>
        /// Checks if the timeout period has been exceeded and updates the state if necessary. Returns true if timout has occoured.
        /// </summary>
        public bool TimeoutIf()
        {
            bool ret;

            if (ret = IsPastTime) 
            {
                OnStateChanged(State = TimeoutState.TimedOut);
            }

            return ret;
        }

        /// <summary>
        /// Forces the timeout state to <see cref="TimeoutState.TimedOut"/> regardless of the current state.
        /// </summary>
        public void ForceTimeout()
        {
            if (State == TimeoutState.Normal) 
            {
                OnStateChanged(State = TimeoutState.TimedOut);
            }
        }

        //PROTECTED METHODS
        /// <summary>
        /// When overridden in a derived class, handles the state change.
        /// </summary>
        /// <param name="state">The new state of the timeout.</param>
        protected abstract void OnStateChanged(TimeoutState state);

        /// <summary>
        /// Updates the <see cref="LastTouched"/> property to the current time and changes the state to <see cref="TimeoutState.Normal"/> if necessary.
        /// </summary>
        protected virtual void OnTouched()
        {
            LastTouched = DateTime.Now;

            if (State != TimeoutState.Normal)
            {
                OnStateChanged(State = TimeoutState.Normal);
            }
        }
    }
}