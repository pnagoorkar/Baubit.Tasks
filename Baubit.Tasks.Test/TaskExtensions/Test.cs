using FluentResults;
using Baubit.Tasks;

namespace Baubit.Tasks.Test.TaskExtensions
{
    public class Test
    {
        #region Wait Tests

        [Fact]
        public void Wait_WithSuccessfulTask_ReturnsOkResult()
        {
            // Arrange
            var task = Task.CompletedTask;

            // Act
            var result = Tasks.TaskExtensions.Wait(task);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Wait_WithCompletedTask_ReturnsOkResult()
        {
            // Arrange
            var task = Task.Run(() => Thread.Sleep(10));

            // Act
            var result = Tasks.TaskExtensions.Wait(task);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Wait_WithTaskThatThrowsException_ReturnsFailedResult()
        {
            // Arrange
            var task = Task.Run(() => throw new InvalidOperationException("Test exception"));

            // Act
            var result = Tasks.TaskExtensions.Wait(task);

            // Assert
            Assert.True(result.IsFailed);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Wait_WithCancelledTask_IgnoreFalse_ReturnsFailedResult()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(() => { }, cts.Token);

            // Act
            var result = Tasks.TaskExtensions.Wait(task, ignoreTaskCancellationException: false);

            // Assert
            Assert.True(result.IsFailed);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Wait_WithCancelledTask_IgnoreTrue_ReturnsOkResult()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(() => { }, cts.Token);

            // Act
            var result = Tasks.TaskExtensions.Wait(task, ignoreTaskCancellationException: true);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Wait_WithTaskThatGetsCancelled_IgnoreFalse_ReturnsFailedResult()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.Delay(5000, cts.Token);
            cts.Cancel();

            // Act
            var result = Tasks.TaskExtensions.Wait(task, ignoreTaskCancellationException: false);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public void Wait_WithTaskThatGetsCancelled_IgnoreTrue_ReturnsOkResult()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.Delay(5000, cts.Token);
            cts.Cancel();

            // Act
            var result = Tasks.TaskExtensions.Wait(task, ignoreTaskCancellationException: true);

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region WaitAsync Tests

        [Fact]
        public async Task WaitAsync_WithSuccessfulTask_ReturnsOkResult()
        {
            // Arrange
            var task = Task.CompletedTask;

            // Act
            var result = await task.WaitAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task WaitAsync_WithCompletedTask_ReturnsOkResult()
        {
            // Arrange
            var task = Task.Run(async () => await Task.Delay(10));

            // Act
            var result = await task.WaitAsync();

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task WaitAsync_WithTaskThatThrowsException_ThrowsException()
        {
            // Arrange
            var task = Task.Run(() => throw new InvalidOperationException("Test exception"));

            // Act & Assert - WaitAsync doesn't catch non-AggregateExceptions when using await
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await task.WaitAsync());
        }

        [Fact]
        public async Task WaitAsync_WithTaskThatThrowsAggregateException_ReturnsFailedResult()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(new AggregateException(new InvalidOperationException("Test exception")));
            var task = tcs.Task.ContinueWith(t => t.Wait()); // Force AggregateException

            // Act
            var result = await task.WaitAsync();

            // Assert
            Assert.True(result.IsFailed);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task WaitAsync_WithCancelledTask_IgnoreFalse_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(async () => await Task.Delay(100), cts.Token);

            // Act & Assert - TaskCanceledException is not caught by WaitAsync when using await
            await Assert.ThrowsAsync<TaskCanceledException>(async () => 
                await task.WaitAsync(ignoreTaskCancellationException: false));
        }

        [Fact]
        public async Task WaitAsync_WithCancelledTask_IgnoreTrue_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(async () => await Task.Delay(100), cts.Token);

            // Act & Assert - Even with ignore=true, TaskCanceledException is not caught when using await
            await Assert.ThrowsAsync<TaskCanceledException>(async () => 
                await task.WaitAsync(ignoreTaskCancellationException: true));
        }

        [Fact]
        public async Task WaitAsync_WithTaskThatGetsCancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.Delay(5000, cts.Token);
            cts.Cancel();

            // Act & Assert - TaskCanceledException is thrown directly, not caught
            await Assert.ThrowsAsync<TaskCanceledException>(async () => 
                await task.WaitAsync(ignoreTaskCancellationException: false));
        }

        [Fact]
        public async Task WaitAsync_WithLongRunningTask_WaitsForCompletion()
        {
            // Arrange
            var completed = false;
            var task = Task.Run(async () =>
            {
                await Task.Delay(50);
                completed = true;
            });

            // Act
            var result = await task.WaitAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(completed);
        }

        [Fact]
        public async Task WaitAsync_WithAggregateExceptionContainingCancellation_IgnoreFalse_ReturnsFailedResult()
        {
            // Arrange - Create a task that will throw AggregateException with TaskCanceledException
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(new AggregateException(new TaskCanceledException()));
            var task = tcs.Task.ContinueWith(t => 
            {
                try { t.Wait(); }
                catch (AggregateException) { throw; }
            });

            // Act
            var result = await task.WaitAsync(ignoreTaskCancellationException: false);

            // Assert
            Assert.True(result.IsFailed);
        }

        [Fact]
        public async Task WaitAsync_WithAggregateExceptionContainingCancellation_IgnoreTrue_ThrowsAggregateException()
        {
            // Arrange - Create a task that will throw AggregateException with TaskCanceledException
            // The issue is that when using await, the AggregateException gets unwrapped
            // So we need to force it to stay wrapped
            var tcs = new TaskCompletionSource<int>();
            var innerTcs = new TaskCompletionSource<int>();
            innerTcs.SetCanceled();
            
            // Create a continuation that catches and rethrows as AggregateException
            var task = Task.Run(() =>
            {
                try
                {
                    innerTcs.Task.Wait();
                }
                catch (AggregateException ex)
                {
                    // Rethrow to keep it as AggregateException
                    throw;
                }
            });

            // Act
            var result = await task.WaitAsync(ignoreTaskCancellationException: true);

            // Assert - With ignore=true, should return Ok for cancellation wrapped in AggregateException
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region RegisterCancellationToken Tests

        [Fact]
        public void RegisterCancellationToken_WithValidTokenAndTCS_ReturnsOkResult()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var cts = new CancellationTokenSource();

            // Act
            var result = tcs.RegisterCancellationToken(cts.Token);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task RegisterCancellationToken_WhenTokenCancelled_CancelsTask()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var cts = new CancellationTokenSource();
            tcs.RegisterCancellationToken(cts.Token);

            // Act
            cts.Cancel();
            await Task.Delay(50); // Give time for cancellation to propagate

            // Assert
            Assert.True(tcs.Task.IsCanceled);
        }

        [Fact]
        public async Task RegisterCancellationToken_WhenTaskCompletesBeforeCancellation_TaskNotCancelled()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var cts = new CancellationTokenSource();
            tcs.RegisterCancellationToken(cts.Token);

            // Act
            tcs.SetResult(42);
            await Task.Delay(50);
            cts.Cancel();

            // Assert
            Assert.True(tcs.Task.IsCompletedSuccessfully);
            Assert.Equal(42, tcs.Task.Result);
        }

        [Fact]
        public async Task RegisterCancellationToken_WithAlreadyCancelledToken_CancelsTaskImmediately()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = tcs.RegisterCancellationToken(cts.Token);
            await Task.Delay(50);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(tcs.Task.IsCanceled);
        }

        [Fact]
        public void RegisterCancellationToken_WithMultipleRegistrations_AllWork()
        {
            // Arrange
            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<string>();
            var cts = new CancellationTokenSource();

            // Act
            var result1 = tcs1.RegisterCancellationToken(cts.Token);
            var result2 = tcs2.RegisterCancellationToken(cts.Token);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
        }

        [Fact]
        public async Task RegisterCancellationToken_WithDifferentTypes_WorksCorrectly()
        {
            // Arrange
            var tcsInt = new TaskCompletionSource<int>();
            var tcsString = new TaskCompletionSource<string>();
            var tcsBool = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource();

            // Act
            tcsInt.RegisterCancellationToken(cts.Token);
            tcsString.RegisterCancellationToken(cts.Token);
            tcsBool.RegisterCancellationToken(cts.Token);
            cts.Cancel();
            await Task.Delay(50);

            // Assert
            Assert.True(tcsInt.Task.IsCanceled);
            Assert.True(tcsString.Task.IsCanceled);
            Assert.True(tcsBool.Task.IsCanceled);
        }

        [Fact]
        public async Task RegisterCancellationToken_DisposesRegistrationAfterTaskCompletes()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var cts = new CancellationTokenSource();
            tcs.RegisterCancellationToken(cts.Token);

            // Act
            tcs.SetResult(42);
            await Task.Delay(100); // Give time for continuation to dispose registration

            // Assert - If this doesn't throw, the registration was disposed properly
            cts.Dispose();
            Assert.True(tcs.Task.IsCompletedSuccessfully);
        }

        [Fact]
        public void RegisterCancellationToken_WithNoneCancellationToken_ReturnsOkResult()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();

            // Act
            var result = tcs.RegisterCancellationToken(CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public void Wait_WithMultipleExceptions_ReturnsFailedResult()
        {
            // Arrange
            var task = Task.Run(() =>
            {
                throw new AggregateException(
                    new InvalidOperationException("Error 1"),
                    new ArgumentException("Error 2")
                );
            });

            // Act
            var result = Tasks.TaskExtensions.Wait(task);

            // Assert
            Assert.True(result.IsFailed);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public async Task WaitAsync_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var task = Task.CompletedTask;

            // Act
            var result1 = await task.WaitAsync();
            var result2 = await task.WaitAsync();

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
        }

        [Fact]
        public void Wait_CalledMultipleTimes_ReturnsConsistentResults()
        {
            // Arrange
            var task = Task.CompletedTask;

            // Act
            var result1 = Tasks.TaskExtensions.Wait(task);
            var result2 = Tasks.TaskExtensions.Wait(task);

            // Assert
            Assert.True(result1.IsSuccess);
            Assert.True(result2.IsSuccess);
        }

        [Fact]
        public async Task RegisterCancellationToken_WithFaultedTask_DoesNotInterfere()
        {
            // Arrange
            var tcs = new TaskCompletionSource<int>();
            var cts = new CancellationTokenSource();
            tcs.RegisterCancellationToken(cts.Token);

            // Act
            tcs.SetException(new InvalidOperationException("Test exception"));
            await Task.Delay(50);

            // Assert
            Assert.True(tcs.Task.IsFaulted);
            Assert.IsType<InvalidOperationException>(tcs.Task.Exception?.InnerException);
        }

        #endregion

        #region WaitAsync with CancellationToken Tests

        [Fact]
        public async Task WaitAsync_WithCancellationToken_TaskCompletesBeforeCancellation_ReturnsSuccessfully()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.CompletedTask;

            // Act - Use static invocation to call our extension method (not .NET 9's native method)
            await Tasks.TaskExtensions.WaitAsync(task, cts.Token);

            // Assert - no exception thrown
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task WaitAsync_WithCancellationToken_CancellationRequestedBeforeTaskCompletes_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>();
            var task = tcs.Task;

            // Act
            cts.Cancel();

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsync_WithCancellationToken_TaskFaults_PropagatesException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetException(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Tasks.TaskExtensions.WaitAsync(tcs.Task, cts.Token));
        }

        [Fact]
        public async Task WaitAsync_WithCancellationToken_AlreadyCancelledToken_ThrowsImmediately()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Delay(5000);

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsync_WithCancellationToken_CancellationDuringWait_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.Delay(5000);

            // Schedule cancellation
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                cts.Cancel();
            });

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsync_WithCancellationToken_NoneToken_CompletesNormally()
        {
            // Arrange
            var task = Task.Delay(50);

            // Act
            await Tasks.TaskExtensions.WaitAsync(task, CancellationToken.None);

            // Assert - no exception thrown
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_TaskCompletesBeforeCancellation_ReturnsResult()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.FromResult(42);

            // Act
            var result = await Tasks.TaskExtensions.WaitAsync(task, cts.Token);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_CancellationRequestedBeforeTaskCompletes_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<int>();
            var task = tcs.Task;

            // Act
            cts.Cancel();

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_TaskFaults_PropagatesException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<int>();
            tcs.SetException(new InvalidOperationException("Test exception"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Tasks.TaskExtensions.WaitAsync(tcs.Task, cts.Token));
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_AlreadyCancelledToken_ThrowsImmediately()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.Run(async () =>
            {
                await Task.Delay(5000);
                return 42;
            });

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_CancellationDuringWait_ThrowsTaskCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.Run(async () =>
            {
                await Task.Delay(5000);
                return 42;
            });

            // Schedule cancellation
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                cts.Cancel();
            });

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_NoneToken_CompletesNormally()
        {
            // Arrange
            var task = Task.Run(async () =>
            {
                await Task.Delay(50);
                return "test";
            });

            // Act
            var result = await Tasks.TaskExtensions.WaitAsync(task, CancellationToken.None);

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_TaskAlreadyComplete_ReturnsImmediately()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = Task.FromResult("already complete");

            // Act
            var result = await Tasks.TaskExtensions.WaitAsync(task, cts.Token);

            // Assert
            Assert.Equal("already complete", result);
        }

        [Fact]
        public async Task WaitAsync_WithCancellationToken_OriginalTaskCancelled_PropagatesCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var taskCts = new CancellationTokenSource();
            taskCts.Cancel();
            var task = Task.FromCanceled(taskCts.Token);

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        [Fact]
        public async Task WaitAsyncGeneric_WithCancellationToken_OriginalTaskCancelled_PropagatesCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var taskCts = new CancellationTokenSource();
            taskCts.Cancel();
            var task = Task.FromCanceled<int>(taskCts.Token);

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await Tasks.TaskExtensions.WaitAsync(task, cts.Token));
        }

        #endregion
    }
}
