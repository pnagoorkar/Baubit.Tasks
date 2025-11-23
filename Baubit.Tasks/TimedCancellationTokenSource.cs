namespace Baubit.Tasks
{
    public class TimedCancellationTokenSource : CancellationTokenSource
    {
        public TimeSpan Timeout { get; init; }

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
        public TimedCancellationTokenSource(uint millisecondTimeout, bool timerStartsAtTokenAccess = true) : this(new TimeSpan(0, 0, 0, 0, (int)millisecondTimeout), timerStartsAtTokenAccess) { }
        public TimedCancellationTokenSource(TimeSpan? timeOut, bool timerStartsAtTokenAccess = true)
        {
            Timeout = timeOut ?? System.Threading.Timeout.InfiniteTimeSpan;
            this.timerStartsAtTokenAccess = timerStartsAtTokenAccess;

        }

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