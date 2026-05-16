using FluentResults;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Baubit.Tasks
{
    /// <summary>
    /// Provides extension methods for <see cref="Task"/> and <see cref="TaskCompletionSource{TResult}"/> 
    /// to enhance error handling and cancellation token integration.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Synchronously waits for the task to complete and returns a <see cref="Result"/> indicating success or failure.
        /// </summary>
        /// <param name="task">The task to wait for.</param>
        /// <param name="ignoreTaskCancellationException">
        /// If <c>true</c>, returns <see cref="Result.Ok()"/> when the task is cancelled; 
        /// if <c>false</c> (default), returns a failed <see cref="Result"/> with the cancellation exception.
        /// </param>
        /// <returns>
        /// A <see cref="Result"/> indicating success or failure:
        /// <list type="bullet">
        /// <item><see cref="Result.Ok()"/> if the task completes successfully</item>
        /// <item>A failed <see cref="Result"/> containing the exception if the task fails</item>
        /// <item>If <paramref name="ignoreTaskCancellationException"/> is <c>true</c> and the task is cancelled, returns <see cref="Result.Ok()"/></item>
        /// <item>If <paramref name="ignoreTaskCancellationException"/> is <c>false</c> and the task is cancelled, returns a failed <see cref="Result"/></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This method catches <see cref="AggregateException"/> and handles <see cref="TaskCanceledException"/> 
        /// specially based on the <paramref name="ignoreTaskCancellationException"/> parameter.
        /// <para>
        /// Note: This method blocks the calling thread until the task completes. Consider using 
        /// <see cref="WaitAsync(Task, bool)"/> for asynchronous scenarios.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var task = Task.Run(() => DoWork());
        /// var result = task.Wait();
        /// if (result.IsSuccess)
        /// {
        ///     // Task completed successfully
        /// }
        /// </code>
        /// </example>
        public static Result Wait(this Task task, bool ignoreTaskCancellationException = false)
        {
            try
            {
                task.Wait();
                return Result.Ok();
            }
            catch (AggregateException aExp)
            {
                if (aExp.InnerException is TaskCanceledException)
                {
                    return ignoreTaskCancellationException ? Result.Ok() : Result.Fail(new ExceptionalError(aExp.InnerException));
                }
                else
                {
                    return Result.Fail(new ExceptionalError(aExp));
                }
            }
        }

        /// <summary>
        /// Asynchronously waits for the task to complete and returns a <see cref="Result"/> indicating success or failure.
        /// </summary>
        /// <param name="task">The task to wait for.</param>
        /// <param name="ignoreTaskCancellationException">
        /// If <c>true</c>, returns <see cref="Result.Ok()"/> when an <see cref="AggregateException"/> 
        /// containing a <see cref="TaskCanceledException"/> is caught; 
        /// if <c>false</c> (default), returns a failed <see cref="Result"/> with the cancellation exception.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a <see cref="Result"/> indicating:
        /// <list type="bullet">
        /// <item><see cref="Result.Ok()"/> if the task completes successfully</item>
        /// <item>A failed <see cref="Result"/> if an <see cref="AggregateException"/> is caught</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> This method only catches <see cref="AggregateException"/>. 
        /// When using <c>await</c>, exceptions are typically unwrapped, so <see cref="TaskCanceledException"/> 
        /// and other exceptions will be thrown directly rather than being caught and converted to a failed <see cref="Result"/>.
        /// </para>
        /// <para>
        /// To catch <see cref="AggregateException"/> with this method, the exception must remain wrapped, 
        /// which typically occurs in continuation scenarios or when explicitly creating wrapped exceptions.
        /// </para>
        /// <para>
        /// The <paramref name="ignoreTaskCancellationException"/> parameter only affects behavior when 
        /// <see cref="TaskCanceledException"/> is wrapped inside an <see cref="AggregateException"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var task = Task.Run(() => DoWork());
        /// var result = await task.WaitAsync();
        /// if (result.IsSuccess)
        /// {
        ///     // Task completed successfully
        /// }
        /// </code>
        /// </example>
        public static async Task<Result> WaitAsync(this Task task, bool ignoreTaskCancellationException = false)
        {
            try
            {
                await task;
                return Result.Ok();
            }
            catch (AggregateException aExp)
            {
                if (aExp.InnerException is TaskCanceledException)
                {
                    return ignoreTaskCancellationException ? Result.Ok() : Result.Fail(new ExceptionalError(aExp.InnerException));
                }
                else
                {
                    return Result.Fail(new ExceptionalError(aExp));
                }
            }
        }

        /// <summary>
        /// Asynchronously waits for the task to complete or be cancelled via the provided <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="task">The task to wait for.</param>
        /// <param name="cancellationToken">The cancellation token to observe while waiting.</param>
        /// <returns>A task that completes when the original task completes or the cancellation token is triggered.</returns>
        /// <exception cref="TaskCanceledException">Thrown when the <paramref name="cancellationToken"/> is cancelled before the task completes.</exception>
        /// <remarks>
        /// <para>
        /// This method provides functionality similar to .NET 6+'s built-in <c>Task.WaitAsync(CancellationToken)</c> 
        /// for use in .NET Standard 2.0 environments.
        /// </para>
        /// <para>
        /// If the cancellation token is cancelled before the task completes, a <see cref="TaskCanceledException"/> is thrown.
        /// If the original task completes first (successfully, faulted, or cancelled), the result is propagated.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        /// try
        /// {
        ///     await longRunningTask.WaitAsync(cts.Token);
        /// }
        /// catch (TaskCanceledException)
        /// {
        ///     // Timeout occurred
        /// }
        /// </code>
        /// </example>
        public static async Task WaitAsync(this Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
            {
                var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completedTask == tcs.Task)
                {
                    throw new TaskCanceledException(tcs.Task);
                }
                await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously waits for the task to complete or be cancelled via the provided <see cref="CancellationToken"/>,
        /// and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The type of the task result.</typeparam>
        /// <param name="task">The task to wait for.</param>
        /// <param name="cancellationToken">The cancellation token to observe while waiting.</param>
        /// <returns>A task that completes with the result when the original task completes or throws when the cancellation token is triggered.</returns>
        /// <exception cref="TaskCanceledException">Thrown when the <paramref name="cancellationToken"/> is cancelled before the task completes.</exception>
        /// <remarks>
        /// <para>
        /// This method provides functionality similar to .NET 6+'s built-in <c>Task&lt;TResult&gt;.WaitAsync(CancellationToken)</c> 
        /// for use in .NET Standard 2.0 environments.
        /// </para>
        /// <para>
        /// If the cancellation token is cancelled before the task completes, a <see cref="TaskCanceledException"/> is thrown.
        /// If the original task completes first (successfully, faulted, or cancelled), the result is propagated.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        /// try
        /// {
        ///     var result = await longRunningTask.WaitAsync(cts.Token);
        /// }
        /// catch (TaskCanceledException)
        /// {
        ///     // Timeout occurred
        /// }
        /// </code>
        /// </example>
        public static async Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
            {
                var completedTask = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completedTask == tcs.Task)
                {
                    throw new TaskCanceledException(tcs.Task);
                }
                return await task.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Registers a <see cref="CancellationToken"/> with a <see cref="TaskCompletionSource{TResult}"/> 
        /// so that the task is automatically cancelled when the token is cancelled.
        /// </summary>
        /// <typeparam name="T">The type of the result value associated with the <see cref="TaskCompletionSource{TResult}"/>.</typeparam>
        /// <param name="taskCompletionSource">The <see cref="TaskCompletionSource{TResult}"/> to register the token with.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to register.</param>
        /// <returns>
        /// A <see cref="Result"/> indicating whether the registration was successful:
        /// <list type="bullet">
        /// <item><see cref="Result.Ok()"/> if registration succeeded</item>
        /// <item>A failed <see cref="Result"/> if an exception occurred during registration</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method sets up a callback that will call <see cref="TaskCompletionSource{TResult}.TrySetCanceled(CancellationToken)"/> 
        /// when the cancellation token is triggered. The registration is performed without synchronization context 
        /// to avoid potential deadlocks.
        /// </para>
        /// <para>
        /// The method also sets up a continuation on the task to dispose of the registration when the task completes, 
        /// ensuring proper resource cleanup.
        /// </para>
        /// <para>
        /// If the task completes (successfully or with an exception) before the cancellation token is triggered, 
        /// the task will not be cancelled. Cancellation only affects tasks that are still pending when the token is cancelled.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var tcs = new TaskCompletionSource&lt;int&gt;();
        /// var cts = new CancellationTokenSource();
        /// 
        /// var result = tcs.RegisterCancellationToken(cts.Token);
        /// if (result.IsSuccess)
        /// {
        ///     // Registration successful
        ///     // If cts.Cancel() is called, tcs.Task will be cancelled
        /// }
        /// </code>
        /// </example>
        public static Result RegisterCancellationToken<T>(this TaskCompletionSource<T> taskCompletionSource, CancellationToken cancellationToken)
        {
            return Result.Try(() => cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
                         .Bind(registration => Result.Try(() => { taskCompletionSource.Task.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default); }));
        }

        /// <summary>
        /// Returns a task that completes when the <paramref name="cancellationToken"/> is cancelled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of <see cref="bool"/> that resolves to <c>true</c> when the token is cancelled.
        /// </returns>
        /// <remarks>
        /// This overload waits indefinitely until the token is cancelled. For a timeout-bounded variant, 
        /// use <see cref="GetCancellationAwaiterAsync(CancellationToken, TimeSpan)"/>.
        /// </remarks>
        public static Task<bool> GetCancellationAwaiterAsync(this CancellationToken cancellationToken)
        {
            return cancellationToken.GetCancellationAwaiterAsync(additionalTokens: []);
        }

        /// <summary>
        /// Returns a task that completes when the <paramref name="cancellationToken"/> is cancelled or the specified timeout elapses.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        /// <param name="maxAwait">The maximum duration to wait before the returned task completes regardless of cancellation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of <see cref="bool"/> that resolves to <c>true</c> when the token is cancelled or the timeout elapses.
        /// </returns>
        /// <remarks>
        /// Internally creates a <see cref="CancellationTokenSource"/> that cancels after <paramref name="maxAwait"/> and 
        /// links it as an additional token so the awaiter completes on whichever fires first.
        /// </remarks>
        public static Task<bool> GetCancellationAwaiterAsync(this CancellationToken cancellationToken, TimeSpan maxAwait)
        {
            return cancellationToken.GetCancellationAwaiterAsync(maxAwait: maxAwait, additionalTokens: []);
        }

        /// <summary>
        /// Returns a task that completes when the <paramref name="cancellationToken"/> is cancelled, the specified timeout elapses,
        /// or any of the <paramref name="additionalTokens"/> are cancelled.
        /// </summary>
        /// <param name="cancellationToken">The primary cancellation token to observe.</param>
        /// <param name="maxAwait">The maximum duration to wait before the returned task completes.</param>
        /// <param name="additionalTokens">Zero or more additional cancellation tokens to observe alongside the primary token and the timeout.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of <see cref="bool"/> that resolves to <c>true</c> when any of the observed tokens is cancelled or the timeout elapses.
        /// </returns>
        /// <remarks>
        /// A timed <see cref="CancellationTokenSource"/> is created for the duration of the call and disposed automatically 
        /// when the awaiter completes.
        /// </remarks>
        public static async Task<bool> GetCancellationAwaiterAsync(this CancellationToken cancellationToken, TimeSpan maxAwait, params CancellationToken[] additionalTokens)
        {
            using var timedCTS = new CancellationTokenSource(maxAwait);
            return await cancellationToken.GetCancellationAwaiterAsync(additionalTokens: [timedCTS.Token, .. additionalTokens]).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a task that completes when the <paramref name="cancellationToken"/> or any of the <paramref name="additionalTokens"/> are cancelled.
        /// </summary>
        /// <param name="cancellationToken">The primary cancellation token to observe.</param>
        /// <param name="additionalTokens">Zero or more additional cancellation tokens to observe alongside the primary token.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> of <see cref="bool"/> that resolves to <c>true</c> when any of the observed tokens is cancelled.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Short-circuit checks are performed before and immediately after registering each token to avoid unnecessary waits 
        /// when a token is already cancelled or becomes cancelled during registration.
        /// </para>
        /// <para>
        /// Any <see cref="TaskCanceledException"/> thrown by the internal <see cref="TaskCompletionSource{TResult}"/> is swallowed; 
        /// the method always returns <c>true</c>.
        /// </para>
        /// </remarks>
        public static async Task<bool> GetCancellationAwaiterAsync(this CancellationToken cancellationToken, params CancellationToken[] additionalTokens)
        {
            var allTokens = (IEnumerable<CancellationToken>)[cancellationToken, .. additionalTokens];
            var taskCompletionSource = new TaskCompletionSource<bool>();
            foreach (var ct in allTokens)
            {
                if (ct.IsCancellationRequested) return true; // short circuit - avoid wasting time if the ct is already cancelled.
                taskCompletionSource.RegisterCancellationToken(ct);
                if (ct.IsCancellationRequested) return true; // in case the ct was cancelled betwee the prior check and registration
            }
            try
            {
                await taskCompletionSource.Task.ConfigureAwait(false);
            }
            catch
            {
                // the awaited task will complete only when the linked CTS is cancelled. It will throw a task cancelled exception. Swallow it.
            }
            return true;
        }

        /// <summary>
        /// Creates a <see cref="TimedCancellationTokenSource"/> that automatically cancels after the specified <paramref name="timeSpan"/>.
        /// </summary>
        /// <param name="timeSpan">The duration after which the <see cref="TimedCancellationTokenSource"/> will be cancelled.</param>
        /// <param name="timerStartsAtTokenAccess">
        /// When <c>true</c> (default), the countdown timer starts the first time the <see cref="CancellationTokenSource.Token"/> property is accessed.
        /// When <c>false</c>, the timer starts the first time <see cref="CancellationTokenSource.IsCancellationRequested"/> is read.
        /// </param>
        /// <returns>A new <see cref="TimedCancellationTokenSource"/> configured with the given timeout and start strategy.</returns>
        public static TimedCancellationTokenSource CreateTimedCancellationTokenSource(this TimeSpan timeSpan, bool timerStartsAtTokenAccess = true)
        {
            return new TimedCancellationTokenSource(timeSpan, timerStartsAtTokenAccess);
        }
    }
}