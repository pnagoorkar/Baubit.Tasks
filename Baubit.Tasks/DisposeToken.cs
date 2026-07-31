using System;

namespace Baubit.Tasks
{
    /// <summary>
    /// A disposable token that invokes a caller-supplied <see cref="Action"/> exactly once
    /// when disposed. Useful for exposing a lightweight, revocable "handle" whose lifetime
    /// controls the execution of cleanup logic (e.g. unregistering a callback or releasing a resource).
    /// </summary>
    public class DisposeToken : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance has already been disposed.
        /// </summary>
        public bool Disposed { get => disposedValue; }
        private bool disposedValue;
        private Action onDispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisposeToken"/> class.
        /// </summary>
        /// <param name="onDispose">The action to invoke the first time <see cref="Dispose"/> is called. May be <see langword="null"/>.</param>
        public DisposeToken(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        /// <summary>
        /// Performs the actual dispose logic, invoking the <c>onDispose</c> callback exactly once.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> if called from <see cref="Dispose"/>; <see langword="false"/> if called from a finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    onDispose?.Invoke();
                    onDispose = null;
                }
                disposedValue = true;
            }
        }

        /// <summary>
        /// Disposes this token, invoking the registered callback if it has not already been invoked.
        /// Safe to call multiple times; subsequent calls are no-ops.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
