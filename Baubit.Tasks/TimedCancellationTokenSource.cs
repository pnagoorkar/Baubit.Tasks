namespace Baubit.Tasks
{
    /// <summary>
    /// Represents a <see cref="CancellationTokenSource"/> that automatically cancels after a specified timeout period.
    /// The timer can be configured to start either when the token is first accessed or when cancellation is explicitly requested.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="CancellationTokenSource"/> to provide automatic timeout functionality.
    /// The timeout timer starts based on the <c>timerStartsAtTokenAccess</c> parameter:
    /// <list type="bullet">
    /// <item>When <c>true</c> (default): Timer starts on first access to the <see cref="Token"/> property</item>
    /// <item>When <c>false</c>: Timer starts on first access to the <see cref="IsCancellationRequested"/> property</item>
    /// </list>
    /// <para>
    /// Note: Due to the use of <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/>, calling <see cref="TryReset"/> 
    /// will return <c>false</c> after the timer has been triggered, as the base implementation does not support resetting 
    /// after <c>CancelAfter</c> has been called.
    /// </para>
    /// </remarks>
    public class TimedCancellationTokenSource : CancellationTokenSource
    {
        /// <summary>
        /// Gets the timeout period after which this <see cref="TimedCancellationTokenSource"/> will be automatically cancelled.
        /// </summary>
        /// <value>
        /// The timeout as a <see cref="TimeSpan"/>. If set to <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>, 
        /// the cancellation token will never timeout automatically.
        /// </value>
        public TimeSpan Timeout { get; init; }

        /// <summary>
        /// Gets a value indicating whether cancellation has been requested for this token source.
        /// </summary>
        /// <value>
        /// <c>true</c> if cancellation has been requested; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Accessing this property for the first time will start the cancellation timer if it hasn't already been started.
        /// Subsequent accesses will not restart the timer.
        /// </remarks>
        public new bool IsCancellationRequested
        {
            get
            {
                if (!cancellationTriggered)
                {
                    CancelAfter(Timeout);
                    cancellationTriggered = true;
                }
                return base.IsCancellationRequested;
            }
        }

        /// <summary>
        /// Gets the <see cref="CancellationToken"/> associated with this <see cref="TimedCancellationTokenSource"/>.
        /// </summary>
        /// <value>
        /// The <see cref="CancellationToken"/> associated with this instance.
        /// </value>
        /// <remarks>
        /// If <c>timerStartsAtTokenAccess</c> was set to <c>true</c> during construction (default), 
        /// accessing this property for the first time will start the cancellation timer.
        /// If <c>timerStartsAtTokenAccess</c> is <c>false</c>, the timer will not start until 
        /// <see cref="IsCancellationRequested"/> is accessed.
        /// </remarks>
        public new CancellationToken Token
        {
            get
            {
                if (timerStartsAtTokenAccess && !cancellationTriggered)
                {
                    CancelAfter(Timeout);
                    cancellationTriggered = true;
                }
                return base.Token;
            }
        }

        private bool timerStartsAtTokenAccess;
        private bool cancellationTriggered;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedCancellationTokenSource"/> class with a specified timeout in milliseconds.
        /// </summary>
        /// <param name="millisecondTimeout">The timeout period in milliseconds. Must be a non-negative value.</param>
        /// <param name="timerStartsAtTokenAccess">
        /// If <c>true</c> (default), the timer starts when the <see cref="Token"/> property is first accessed.
        /// If <c>false</c>, the timer starts when the <see cref="IsCancellationRequested"/> property is first accessed.
        /// </param>
        /// <remarks>
        /// This constructor converts the millisecond timeout to a <see cref="TimeSpan"/> and delegates to the primary constructor.
        /// </remarks>
        public TimedCancellationTokenSource(uint millisecondTimeout, bool timerStartsAtTokenAccess = true) 
            : this(new TimeSpan(0, 0, 0, 0, (int)millisecondTimeout), timerStartsAtTokenAccess) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimedCancellationTokenSource"/> class with a specified timeout.
        /// </summary>
        /// <param name="timeOut">
        /// The timeout period as a <see cref="TimeSpan"/>. If <c>null</c>, the timeout will be set to 
        /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>, meaning no automatic cancellation will occur.
        /// </param>
        /// <param name="timerStartsAtTokenAccess">
        /// If <c>true</c> (default), the timer starts when the <see cref="Token"/> property is first accessed.
        /// If <c>false</c>, the timer starts when the <see cref="IsCancellationRequested"/> property is first accessed.
        /// </param>
        /// <remarks>
        /// Use this constructor when you need more precise control over when the cancellation timer starts.
        /// Setting <paramref name="timerStartsAtTokenAccess"/> to <c>false</c> allows you to obtain the token 
        /// without immediately starting the countdown.
        /// </remarks>
        public TimedCancellationTokenSource(TimeSpan? timeOut, bool timerStartsAtTokenAccess = true)
        {
            Timeout = timeOut ?? System.Threading.Timeout.InfiniteTimeSpan;
            this.timerStartsAtTokenAccess = timerStartsAtTokenAccess;

        }

        /// <summary>
        /// Attempts to reset the <see cref="TimedCancellationTokenSource"/> to its initial state.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the token source was successfully reset; otherwise, <c>false</c>.
        /// Returns <c>false</c> if <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/> was previously called,
        /// as the base implementation does not support resetting in this scenario.
        /// </returns>
        /// <remarks>
        /// If the reset is successful, the internal <c>cancellationTriggered</c> flag is reset, 
        /// allowing the timer to be started again on the next access to <see cref="Token"/> or <see cref="IsCancellationRequested"/>.
        /// <para>
        /// Note: In .NET 9, <see cref="CancellationTokenSource.TryReset"/> returns <c>false</c> if the cancellation 
        /// was triggered by a timer set with <see cref="CancellationTokenSource.CancelAfter(TimeSpan)"/>.
        /// It typically returns <c>true</c> only if cancellation was triggered manually via <see cref="CancellationTokenSource.Cancel()"/>
        /// and the timer had not been started.
        /// </para>
        /// </remarks>
        public new bool TryReset()
        {
            var retVal = base.TryReset();
            if (retVal)
            {
                cancellationTriggered = false;
            }
            return retVal;
        }
    }
}