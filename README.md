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

## License

MIT
