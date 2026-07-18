using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace AgendaLivre.Windows;

public enum AgendaSyncConflictResolution
{
    UseThisComputer,
    UseCloud
}

internal interface IAgendaSyncRetryableException
{
}

internal readonly record struct AgendaConflictResolutionTransition(
    long BaseRevision,
    bool Pending,
    bool ApplyRemote,
    bool QueueLocal);

internal static class AgendaSyncConflictPolicy
{
    public static AgendaConflictResolutionTransition CreateTransition(
        AgendaSyncConflictResolution resolution,
        long remoteRevision) => resolution switch
        {
            AgendaSyncConflictResolution.UseThisComputer =>
                new AgendaConflictResolutionTransition(remoteRevision, Pending: true, ApplyRemote: false, QueueLocal: true),
            AgendaSyncConflictResolution.UseCloud =>
                new AgendaConflictResolutionTransition(remoteRevision, Pending: false, ApplyRemote: true, QueueLocal: false),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Resolução de conflito inválida.")
        };
}

internal static class AgendaSyncRetryPolicy
{
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(30);

    public static TimeSpan DelayAfterFailure(int consecutiveFailures)
    {
        var exponent = Math.Clamp(consecutiveFailures - 1, 0, 4);
        var seconds = Math.Pow(2, exponent + 1);
        return TimeSpan.FromSeconds(Math.Min(MaximumDelay.TotalSeconds, seconds));
    }

    public static bool IsRetryable(Exception exception) =>
        exception is HttpRequestException or TaskCanceledException or JsonException or IAgendaSyncRetryableException;
}

internal static class AgendaOfflineSessionPolicy
{
    public static bool HasUsableCachedIdentity(string? userId, string? email, string? refreshToken) =>
        !string.IsNullOrWhiteSpace(userId) &&
        !string.IsNullOrWhiteSpace(email) &&
        !string.IsNullOrWhiteSpace(refreshToken);

    public static bool InvalidatesCachedSession(HttpStatusCode? statusCode) =>
        statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}
