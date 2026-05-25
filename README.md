# Baubit.Tasks


[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Tasks/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Tasks)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.Tasks.svg)](https://www.nuget.org/packages/Baubit.Tasks/)
[![NuGet](https://img.shields.io/nuget/dt/Baubit.Tasks.svg)](https://www.nuget.org/packages/Baubit.Tasks) <br/>
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)<br/>
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.Tasks/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.Tasks)

Task utilities with FluentResults integration.

## Install

```bash
dotnet add package Baubit.Tasks
```

## Features

### WaitingRoom\<TValue\>

Coordinates concurrent awaiters waiting for a single result value. All guests that call `Join` are held until `TrySetResult`, `TrySetCanceled`, or `Dispose` is called.

```csharp
using var room = new WaitingRoom<string>();

// Multiple callers can join concurrently
var task1 = room.Join(cancellationToken);
var task2 = room.Join(cancellationToken);

Console.WriteLine(room.HasGuests); // true

// Completes all waiting tasks at once
room.TrySetResult("hello");

var result1 = await task1; // "hello"
var result2 = await task2; // "hello"

// Cancel all waiting tasks
room.TrySetCanceled();

// Disposing also cancels any remaining guests
room.Dispose();
```

| Member | Description |
|--------|-------------|
| `HasGuests` | `true` when at least one caller is currently awaiting a result. |
| `Join(CancellationToken)` | Joins the waiting room and returns a task that completes when a result is set or cancellation is requested. |
| `TrySetResult(TValue)` | Supplies the result to all waiting guests. Returns `true` if the result was set successfully. |
| `TrySetCanceled(CancellationToken)` | Cancels all waiting guests. Returns `true` if cancellation was applied. |
| `Dispose()` | Cancels any remaining guests and releases resources. |

### TimedCancellationTokenSource

Auto-cancelling `CancellationTokenSource` with configurable timeout.

```csharp
// Timer starts when token is accessed (default)
using var cts = new TimedCancellationTokenSource(TimeSpan.FromSeconds(30));
var token = cts.Token; // Timer starts now

// Timer starts on explicit check
using var cts = new TimedCancellationTokenSource(1000, timerStartsAtTokenAccess: false);
if (cts.IsCancellationRequested) // Timer starts now
{
    // Handle cancellation
}
```

### Task Extensions

#### Wait / WaitAsync

Convert task exceptions to `Result` objects.

```csharp
var result = task.Wait();
if (result.IsSuccess) { /* ... */ }

var result = await task.WaitAsync();
```

#### WaitAsync with CancellationToken

Asynchronously wait for a task with cancellation support. Provides .NET 6+ `WaitAsync(CancellationToken)` functionality for .NET Standard 2.0.

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await longRunningTask.WaitAsync(cts.Token);
}
catch (TaskCanceledException)
{
    // Timeout occurred
}

// With result
var result = await task.WaitAsync<int>(cts.Token);
```

#### RegisterCancellationToken

Link `CancellationToken` to `TaskCompletionSource`.

```csharp
var tcs = new TaskCompletionSource<int>();
var cts = new CancellationTokenSource();

tcs.RegisterCancellationToken(cts.Token);
cts.Cancel(); // Automatically cancels tcs.Task
```

#### GetCancellationAwaiterAsync

Asynchronously wait until one or more `CancellationToken`s are cancelled. Always returns `true` when any observed token is cancelled. Short-circuit checks are performed before and after each registration to avoid unnecessary waits.

```csharp
// Wait for a single token to be cancelled
await myCancellationToken.GetCancellationAwaiterAsync();

// Wait for any of several tokens to be cancelled
await myCancellationToken.GetCancellationAwaiterAsync(token2, token3);

// Wait with a timeout — completes when the token is cancelled or the timeout elapses
await myCancellationToken.GetCancellationAwaiterAsync(TimeSpan.FromSeconds(5));

// Wait with a timeout and additional tokens
await myCancellationToken.GetCancellationAwaiterAsync(TimeSpan.FromSeconds(5), token2, token3);
```

#### CreateTimedCancellationTokenSource

Extension method on `TimeSpan` for fluent creation of a `TimedCancellationTokenSource`.

```csharp
// Timer starts when Token is first accessed (default)
using var cts = TimeSpan.FromSeconds(30).CreateTimedCancellationTokenSource();

// Timer starts when IsCancellationRequested is first read
using var cts = TimeSpan.FromSeconds(30).CreateTimedCancellationTokenSource(timerStartsAtTokenAccess: false);
```

## License

MIT
