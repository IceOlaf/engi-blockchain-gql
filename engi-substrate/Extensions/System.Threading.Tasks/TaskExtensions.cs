namespace System.Threading.Tasks;

public static class TaskExtensions
{
    public static Task TimeoutAfter(this Task task, TimeSpan timeout)
    {
        // Short-circuit #1: infinite timeout or task already completed
        if (task.IsCompleted || timeout == Timeout.InfiniteTimeSpan)
        {
            // Either the task has already completed or timeout will never occur.
            // No proxy necessary.
            return task;
        }

        // tcs.Task will be returned as a proxy to the caller
        TaskCompletionSource<object> tcs = new();

        // Short-circuit #2: zero timeout
        if (timeout == TimeSpan.Zero)
        {
            // We've already timed out.
            tcs.SetException(new TimeoutException());
            return tcs.Task;
        }

        // Set up a timer to complete after the specified timeout period
        Timer timer = new Timer(state =>
        {
            // Recover your state information
            var myTcs = (TaskCompletionSource<object>)state!;

            // Fault our proxy with a TimeoutException
            myTcs.TrySetException(new TimeoutException());
        }, tcs, timeout, Timeout.InfiniteTimeSpan);

        // Wire up the logic for what happens when source task completes
        task.ContinueWith((antecedent, state) =>
            {
                // Recover our state data
                var tuple = (Tuple<Timer, TaskCompletionSource<object>>)state!;

                // Cancel the Timer
                tuple.Item1.Dispose();

                // Marshal results to proxy
                MarshalTaskResults(antecedent, tuple.Item2!);
            },
            Tuple.Create(timer, tcs),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return tcs.Task;
    }

    public static Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
    {
        // Short-circuit #1: infinite timeout or task already completed
        if (task.IsCompleted || timeout == Timeout.InfiniteTimeSpan)
        {
            // Either the task has already completed or timeout will never occur.
            // No proxy necessary.
            return task;
        }

        // tcs.Task will be returned as a proxy to the caller
        TaskCompletionSource<T> tcs = new();

        // Short-circuit #2: zero timeout
        if (timeout == TimeSpan.Zero)
        {
            // We've already timed out.
            tcs.SetException(new TimeoutException());
            return tcs.Task;
        }

        // Set up a timer to complete after the specified timeout period
        Timer timer = new Timer(state =>
        {
            // Recover your state information
            var myTcs = (TaskCompletionSource<T>)state!;

            // Fault our proxy with a TimeoutException
            myTcs.TrySetException(new TimeoutException());
        }, tcs, timeout, Timeout.InfiniteTimeSpan);

        // Wire up the logic for what happens when source task completes
        task.ContinueWith((antecedent, state) =>
            {
                // Recover our state data
                var tuple = (Tuple<Timer, TaskCompletionSource<T>>)state!;

                // Cancel the Timer
                tuple.Item1.Dispose();

                // Marshal results to proxy
                MarshalTaskResults(antecedent, tuple.Item2!);
            },
            Tuple.Create(timer, tcs),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return tcs.Task;
    }

    internal static void MarshalTaskResults<TResult>(
        Task source, TaskCompletionSource<TResult?> proxy)
    {
        switch (source.Status)
        {
            case TaskStatus.Faulted:
                proxy.TrySetException(source.Exception!);
                break;
            case TaskStatus.Canceled:
                proxy.TrySetCanceled();
                break;
            case TaskStatus.RanToCompletion:
                var castedSource = source as Task<TResult?>;
                proxy.TrySetResult(
                    castedSource == null
                        ? default // source is a Task
                        : castedSource.Result); // source is a Task<TResult>
                break;
        }
    }
}
