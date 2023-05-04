using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions.Documents.Subscriptions;
using Sentry;

namespace Engi.Substrate;

public abstract class SubscriptionProcessingBase<T> : BackgroundService
    where T : class
{
    protected SubscriptionProcessingBase(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IHub sentry,
        IOptions<EngiOptions> engiOptions,
        ILoggerFactory loggerFactory)
    {
        Name = $"SubscriptionProcessor<{GetType().Name}>";

        Store = store;
        ServiceProvider = serviceProvider;
        Logger = loggerFactory.CreateLogger(GetType());
        Sentry = sentry;
        ProcessConcurrently = engiOptions.Value.ProcessRavenSubscriptionsConcurrently;
    }

    protected string Name { get; init; }

    protected IDocumentStore Store { get; init; }

    protected IServiceProvider ServiceProvider { get; init; }

    protected ILogger Logger { get; init; }

    protected IHub Sentry { get; init; }

    protected bool ProcessConcurrently { get; set; }

    protected int? MaxDocumentsPerBatch { get; set; }

    protected abstract string CreateQuery();

    protected virtual Task InitializeAsync() => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workerOptions = new SubscriptionWorkerOptions(Name)
        {
            Strategy = ProcessConcurrently 
                ? SubscriptionOpeningStrategy.Concurrent 
                : SubscriptionOpeningStrategy.WaitForFree,
            MaxErroneousPeriod = TimeSpan.FromHours(1),
            TimeToWaitBeforeConnectionRetry = TimeSpan.FromMinutes(1),
        };

        if(MaxDocumentsPerBatch.HasValue)
        {
            workerOptions.MaxDocsPerBatch = MaxDocumentsPerBatch.Value;
        }

        string query = CreateQuery();

        await CreateOrUpdateAsync(Name, query);

        await InitializeAsync();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            SubscriptionWorker<T>? worker = null;

            try
            {
                worker = Store.Subscriptions.GetSubscriptionWorker<T>(workerOptions);

                worker.OnSubscriptionConnectionRetry +=
                    exception => LogEvent(LogLevel.Information,
                        nameof(worker.OnSubscriptionConnectionRetry), exception);
                worker.OnUnexpectedSubscriptionError +=
                    exception => LogEvent(LogLevel.Warning,
                        nameof(worker.OnUnexpectedSubscriptionError), exception);
                worker.OnDisposed +=
                    _ => LogEvent(LogLevel.Information,
                        nameof(worker.OnDisposed), null);

                await worker.Run(ProcessBatchAsync, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // this signals a shutdown

                break;
            }
            catch (Exception ex)
            {
                if (IsTransientException(ex))
                {
                    LogEvent(LogLevel.Warning, "SubscriberError", ex);

                    continue;
                }

                // the subscription seems to be terminated after MaxErroneousPeriod
                // has elapsed but can often be because of transient exceptions like timeouts
                // manifest inside SubscriptionMessageTypeException and thrown as AggregateException
                // which also includes SubscriptionInvalidStateException

                if (ex is AggregateException aggregate
                    && aggregate.InnerExceptions
                        .All(innerEx => IsTransientException(innerEx) || innerEx is SubscriptionInvalidStateException))
                {
                    LogEvent(LogLevel.Warning,
                        $"Ignoring {nameof(SubscriptionInvalidStateException)} with only transient inner exceptions.",
                        ex);

                    continue;
                }

                // we don't allow item errors to bubble up here
                // so it can only mean that the subscription is failing for a worse reason

                ThrowSubscriptionProcessingTerminatedException(ex);

                break;
            }
            finally
            {
                if (worker != null)
                {
                    await worker.DisposeAsync();
                }
            }
        }
    }

    private async Task CreateOrUpdateAsync(string name, string query)
    {
        try
        {
            var state = await Store.Subscriptions.GetSubscriptionStateAsync(name);

            if (state.Query != query)
            {
                var options = new SubscriptionUpdateOptions
                {
                    Id = state.SubscriptionId,
                    Name = state.SubscriptionName,
                    Query = query
                };

                await Store.Subscriptions.UpdateAsync(options);
            }
        }
        catch (SubscriptionDoesNotExistException)
        {
            var options = new SubscriptionCreationOptions
            {
                Name = name,
                Query = query
            };
            
            await Store.Subscriptions.CreateAsync(options);
        }
    }

    private bool IsTransientException(Exception ex)
    {
        if (ex is SubscriptionMessageTypeException
            && (ex.Message.Contains("System.OperationCanceledException")
                || ex.Message.Contains("System.TimeoutException")))
        {
            return true;
        }

        if (ex is InvalidOperationException
            && (ex.Message.Contains("System.OperationCanceledException")
                || ex.Message.Contains("System.TimeoutException")
                || ex.Message.Contains("Raven.Client.Exceptions.Database.DatabaseDisabledException")))
        {
            return true;
        }

        if (ex is OperationCanceledException
            or IOException
            or EndOfStreamException
            or TimeoutException
            or SubscriptionDoesNotBelongToNodeException)
        {
            return true;
        }

        return false;
    }

    private void ThrowSubscriptionProcessingTerminatedException(Exception inner)
    {
        try
        {
            throw new SubscriptionProcessingTerminatedException(Name, inner);
        }
        catch (Exception ex)
        {
            CaptureException(LogLevel.Critical, ex);

            throw;
        }
    }

    private async Task ProcessBatchAsync(SubscriptionBatch<T> batch)
    {
        Sentry.AddBreadcrumb("Processing batch",
            level: BreadcrumbLevel.Info,
            data: new Dictionary<string, string>
            {
                ["batch"] = string.Join(", ",
                    batch.Items.Select(x => x.Id))
            });

        try
        {
            await using var scope = ServiceProvider.CreateAsyncScope();

            await ProcessBatchAsync(batch, scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            CaptureBatchException(ex, batch);
        }
    }

    protected abstract Task ProcessBatchAsync(SubscriptionBatch<T> batch, IServiceProvider serviceProvider);
    
    private void CaptureException(
        LogLevel logLevel,
        Exception exception)
    {
        Sentry.CaptureException(exception, scope =>
        {
            scope.Level = MapToSentry(logLevel);
        });

        Logger.Log(logLevel, exception,
            "Error while processing subscription.");
    }

    private void LogEvent(
        LogLevel logLevel,
        string eventName,
        Exception? exception)
    {
        if (exception == null)
        {
            Logger.Log(logLevel, $"{eventName} emitted");
        }
        else
        {
            Logger.Log(logLevel, exception, $"{eventName}: Error while processing subscription");
        }
    }

    private void CaptureBatchException(Exception exception, SubscriptionBatch<T> batch)
    {
        Logger.LogError(exception,
            "Failed to process items: {0}",
            string.Join(", ", batch.Items.Select(x => x.Id)));

        Sentry.CaptureException(exception);
    }

    private SentryLevel MapToSentry(LogLevel level)
    {
        return level switch
        {
            LogLevel.Critical => SentryLevel.Fatal,
            LogLevel.Debug => SentryLevel.Debug,
            LogLevel.Error => SentryLevel.Error,
            LogLevel.Information => SentryLevel.Info,
            LogLevel.Trace => SentryLevel.Info,
            LogLevel.Warning => SentryLevel.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }
}
