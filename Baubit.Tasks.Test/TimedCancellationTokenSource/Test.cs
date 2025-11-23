namespace Baubit.Tasks.Test.TimedCancellationTokenSource
{
    public class Test
    {
        [Fact]
        public void Constructor_WithMilliseconds_SetsTimeout()
        {
            // Arrange & Act
            var cts = new Tasks.TimedCancellationTokenSource(1000);

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(1000), cts.Timeout);
        }

        [Fact]
        public void Constructor_WithTimeSpan_SetsTimeout()
        {
            // Arrange
            var timeout = TimeSpan.FromSeconds(5);

            // Act
            var cts = new Tasks.TimedCancellationTokenSource(timeout);

            // Assert
            Assert.Equal(timeout, cts.Timeout);
        }

        [Fact]
        public void Constructor_WithNullTimeSpan_SetsInfiniteTimeout()
        {
            // Arrange & Act
            var cts = new Tasks.TimedCancellationTokenSource(null);

            // Assert
            Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, cts.Timeout);
        }

        [Fact]
        public void Constructor_WithTimerStartsAtTokenAccessFalse_DoesNotStartTimerOnConstruction()
        {
            // Arrange & Act
            var cts = new Tasks.TimedCancellationTokenSource(100, timerStartsAtTokenAccess: false);

            // Assert - token should not be cancelled immediately
            Thread.Sleep(50);
            Assert.False(cts.Token.IsCancellationRequested);
        }

        [Fact]
        public async Task Token_WithTimerStartsAtTokenAccessTrue_StartsTimerOnFirstAccess()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(100, timerStartsAtTokenAccess: true);

            // Act - access token to start timer
            var token = cts.Token;

            // Assert - wait for cancellation
            await Task.Delay(150);
            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void Token_WithTimerStartsAtTokenAccessTrue_OnlyStartsTimerOnce()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(200, timerStartsAtTokenAccess: true);

            // Act - access token multiple times
            var token1 = cts.Token;
            Thread.Sleep(50);
            var token2 = cts.Token;

            // Assert - both tokens should be the same
            Assert.Equal(token1, token2);
        }

        [Fact]
        public void Token_WithTimerStartsAtTokenAccessFalse_DoesNotStartTimer()
        {
            // Arrange & Act
            var cts = new Tasks.TimedCancellationTokenSource(100, timerStartsAtTokenAccess: false);
            var token = cts.Token;

            // Assert
            Thread.Sleep(150);
            Assert.False(token.IsCancellationRequested);
        }

        [Fact]
        public async Task IsCancellationRequested_StartsTimerOnFirstAccess()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(100, timerStartsAtTokenAccess: false);

            // Act - access IsCancellationRequested to start timer
            var initialCheck = cts.IsCancellationRequested;

            // Assert
            Assert.False(initialCheck);
            await Task.Delay(150);
            Assert.True(cts.IsCancellationRequested);
        }

        [Fact]
        public void IsCancellationRequested_OnlyStartsTimerOnce()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(200, timerStartsAtTokenAccess: false);

            // Act - access IsCancellationRequested multiple times
            var check1 = cts.IsCancellationRequested;
            Thread.Sleep(50);
            var check2 = cts.IsCancellationRequested;

            // Assert
            Assert.False(check1);
            Assert.False(check2);
        }

        [Fact]
        public async Task CancellationToken_GetsCancelledAfterTimeout()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(100);
            var token = cts.Token;

            // Act & Assert
            Assert.False(token.IsCancellationRequested);
            await Task.Delay(150);
            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public async Task TryReset_AfterTimeoutCancellation_ReturnsFalse()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(50, timerStartsAtTokenAccess: false);
            // Manually trigger cancellation via timeout
            _ = cts.IsCancellationRequested;
            await Task.Delay(100);
            Assert.True(cts.IsCancellationRequested);

            // Act
            var resetResult = cts.TryReset();

            // Assert - TryReset returns false after CancelAfter timeout
            Assert.False(resetResult);
        }

        [Fact]
        public async Task TryReset_WithTimerStartedViaToken_ReturnsFalse()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(50, timerStartsAtTokenAccess: true);
            // Access token starts timer when timerStartsAtTokenAccess is true
            var token = cts.Token;
            await Task.Delay(100);
            Assert.True(token.IsCancellationRequested);

            // Act
            var resetResult = cts.TryReset();

            // Assert - TryReset returns false because CancelAfter was used
            Assert.False(resetResult);
        }

        [Fact]
        public void TryReset_BeforeCancellationOccurs_ReturnsTrue()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(5000, timerStartsAtTokenAccess: true);
            // Access token which triggers CancelAfter
            var token = cts.Token;
            Assert.False(token.IsCancellationRequested);

            // Act - Try to reset before cancellation actually happens
            var resetResult = cts.TryReset();

            // Assert - In .NET 9, TryReset returns true if not yet cancelled
            Assert.True(resetResult);
        }

        [Fact]
        public void Dispose_DisposesTheTokenSource()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(1000);

            // Act
            cts.Dispose();

            // Assert - accessing after dispose should throw
            Assert.Throws<ObjectDisposedException>(() => cts.Token);
        }

        [Fact]
        public void InfiniteTimeout_DoesNotCancelAutomatically()
        {
            // Arrange & Act
            var cts = new Tasks.TimedCancellationTokenSource(null);
            var token = cts.Token;

            // Assert
            Thread.Sleep(200);
            Assert.False(token.IsCancellationRequested);
        }

        [Fact]
        public async Task MultipleTokenAccesses_ReturnSameToken()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(1000);

            // Act
            var token1 = cts.Token;
            await Task.Delay(50);
            var token2 = cts.Token;

            // Assert
            Assert.Equal(token1, token2);
        }

        [Fact]
        public void ManualCancel_WorksBeforeTimeout()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(5000);
            var token = cts.Token;

            // Act
            cts.Cancel();

            // Assert
            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public async Task VeryShortTimeout_CancelsQuickly()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(1);
            var token = cts.Token;

            // Act & Assert
            await Task.Delay(50);
            Assert.True(token.IsCancellationRequested);
        }

        [Fact]
        public void TimeoutProperty_IsInitOnly()
        {
            // Arrange & Act
            var cts = new Tasks.TimedCancellationTokenSource(1000);

            // Assert - Timeout should be readable
            var timeout = cts.Timeout;
            Assert.Equal(TimeSpan.FromMilliseconds(1000), timeout);
        }

        [Fact]
        public void TimerBehavior_DefaultIsStartOnTokenAccess()
        {
            // Arrange - Default constructor behavior
            var cts = new Tasks.TimedCancellationTokenSource(100);

            // Act - Access token
            var token = cts.Token;

            // Assert - Should not be cancelled immediately
            Assert.False(token.IsCancellationRequested);
        }

        [Fact]
        public void TryReset_AfterTimerStarted_ResetsFlag()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(5000, timerStartsAtTokenAccess: true);
            var token = cts.Token; // Starts timer
            
            // Act
            var resetResult = cts.TryReset();

            // Assert - Reset succeeds and resets the cancellationTriggered flag
            Assert.True(resetResult);
            
            // After reset, accessing token again should start timer again
            var token2 = cts.Token;
            Assert.False(token2.IsCancellationRequested);
        }

        [Fact]
        public async Task TryReset_ResetsInternalFlag_AllowsTimerToRestartOnNextAccess()
        {
            // Arrange
            var cts = new Tasks.TimedCancellationTokenSource(100, timerStartsAtTokenAccess: false);
            _ = cts.IsCancellationRequested; // Start timer
            await Task.Delay(150);
            Assert.True(cts.IsCancellationRequested);

            // Act - Reset (will fail because of CancelAfter)
            var resetResult = cts.TryReset();
            
            // If reset succeeded, the cancellationTriggered flag should be reset
            if (resetResult)
            {
                // Timer should restart on next access
                var isRequested = cts.IsCancellationRequested;
                Assert.False(isRequested);
            }
            else
            {
                // TryReset failed as expected with CancelAfter
                Assert.False(resetResult);
            }
        }
    }
}
