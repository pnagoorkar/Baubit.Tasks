# Baubit.Tasks


[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Tasks/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Tasks)
[![NuGet](https://img.shields.io/nuget/v/Baubit.Tasks.svg)](https://www.nuget.org/packages/Baubit.Tasks/)

Task utilities for .NET 9 with FluentResults integration.

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

#### RegisterCancellationToken

Link `CancellationToken` to `TaskCompletionSource`.

```csharp
var tcs = new TaskCompletionSource<int>();
var cts = new CancellationTokenSource();

tcs.RegisterCancellationToken(cts.Token);
cts.Cancel(); // Automatically cancels tcs.Task
```

## Requirements

- .NET 9.0
- [FluentResults](https://github.com/altmann/FluentResults) (via Baubit.Traceability)

## License

MIT
