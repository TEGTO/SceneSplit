using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using SceneSplit.Api.Hubs;
using SceneSplit.Configuration;
using System.Collections.Concurrent;

namespace SceneSplit.Api.HostedServices;

public class S3ObjectImageWatcher : BackgroundService
{
    private readonly IAmazonS3 s3;
    private readonly string bucketName;
    private readonly int pollSeconds;
    private readonly ILogger<S3ObjectImageWatcher> logger;
    private readonly IHubContext<SceneSplitHub, ISceneSplitHubClient> hubContext;
    private readonly IMediator mediator;

    private readonly HashSet<string> knownKeys = new(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> userRefreshTokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Task> userRefreshTasks = new(StringComparer.Ordinal);

    public S3ObjectImageWatcher(
        IAmazonS3 s3,
        IConfiguration configuration,
        ILogger<S3ObjectImageWatcher> logger,
        IHubContext<SceneSplitHub, ISceneSplitHubClient> hubContext,
        IMediator mediator)
    {
        this.s3 = s3;
        this.logger = logger;
        this.hubContext = hubContext;
        this.mediator = mediator;

        bucketName = configuration[ApiConfigurationKeys.OBJECT_IMAGE_BUCKET]
            ?? throw new InvalidOperationException($"{ApiConfigurationKeys.OBJECT_IMAGE_BUCKET} missing");

        pollSeconds = configuration.GetValue<int?>(ApiConfigurationKeys.OBJECT_IMAGE_POLL_INTERVAL_SECONDS) ?? 10;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.S3ObjectImageWatcherStarting(logger, bucketName, pollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var newUserIds = await ScanForNewUserIds(stoppingToken);

                if (newUserIds.Count > 0)
                {
                    Log.S3ObjectImageWatcherDetectedUsers(logger, newUserIds.Count);
                }

                foreach (var userId in newUserIds)
                {
                    TriggerUserRefresh(userId, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                Log.S3ObjectImageWatcherCancellation(logger);
                break;
            }
            catch (Exception ex)
            {
                Log.S3ObjectImageWatcherPollingError(logger, ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                Log.S3ObjectImageWatcherCancellation(logger);
                break;
            }
        }

        Log.S3ObjectImageWatcherStopped(logger);
    }

    private void TriggerUserRefresh(string userId, CancellationToken stoppingToken)
    {
        if (userRefreshTokens.TryRemove(userId, out var existingCts))
        {
            try
            {
                existingCts.Cancel();
            }
            finally
            {
                existingCts.Dispose();
                Log.S3ObjectImageWatcherRefreshCanceled(logger, userId);
            }
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        userRefreshTokens[userId] = cts;

        Log.S3ObjectImageWatcherRefreshStarted(logger, userId);

        var task = Task.Run(async () =>
        {
            try
            {
                await SceneSplitHub.UpdateUserImagesAsync(userId, hubContext.Clients, mediator, cts.Token);
                Log.S3ObjectImageWatcherPushedUser(logger, userId);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Log.S3ObjectImageWatcherCancellation(logger);
            }
            catch (Exception ex)
            {
                Log.S3ObjectImageWatcherRefreshError(logger, ex, userId);
            }
            finally
            {
                userRefreshTokens.TryGetValue(userId, out var currentCts);
                if (ReferenceEquals(currentCts, cts))
                {
                    userRefreshTokens.TryRemove(userId, out _);
                }
                cts.Dispose();
                userRefreshTasks.TryRemove(userId, out _);
            }
        }, CancellationToken.None);

        userRefreshTasks[userId] = task;
    }

    private async Task<HashSet<string>> ScanForNewUserIds(CancellationToken ct)
    {
        var affectedUserIds = new HashSet<string>(StringComparer.Ordinal);
        string? continuation = null;

        do
        {
            var listReq = new ListObjectsV2Request
            {
                BucketName = bucketName,
                ContinuationToken = continuation
            };

            var listResp = await s3.ListObjectsV2Async(listReq, ct);

            foreach (var key in listResp.S3Objects.Select(obj => obj.Key))
            {
                if (!knownKeys.Add(key))
                {
                    continue;
                }

                var tagging = await s3.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = key
                }, ct);

                var userTag = tagging.Tagging?.FirstOrDefault(t => t.Key == WorkflowTags.USER_ID_TAG);
                if (userTag?.Value is { Length: > 0 } userId)
                {
                    affectedUserIds.Add(userId);
                }
            }

            continuation = listResp.IsTruncated == true ? listResp.NextContinuationToken : null;

        } while (continuation != null && !ct.IsCancellationRequested);

        return affectedUserIds;
    }
}