using System.Globalization;
using System.Text;

namespace AgendaLivre.Windows;

internal sealed class WhatsAppManualSendGuard
{
    internal static readonly TimeSpan DefaultDuplicateWindow = TimeSpan.FromSeconds(12);

    private readonly TimeSpan _duplicateWindow;
    private readonly Dictionary<string, RecentAttempt> _recentAttempts = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    internal WhatsAppManualSendGuard(TimeSpan? duplicateWindow = null)
    {
        _duplicateWindow = duplicateWindow ?? DefaultDuplicateWindow;
    }

    internal WhatsAppManualSendDecision Begin(
        string phone,
        string text,
        DateTimeOffset now,
        Func<string>? createAttemptId = null)
    {
        var key = BuildKey(phone, text);
        lock (_sync)
        {
            foreach (var expired in _recentAttempts
                         .Where(item => now - item.Value.StartedAt >= _duplicateWindow)
                         .Select(item => item.Key)
                         .ToArray())
            {
                _recentAttempts.Remove(expired);
            }

            if (_recentAttempts.TryGetValue(key, out var recent))
            {
                return new WhatsAppManualSendDecision(false, recent.AttemptId);
            }

            var attemptId = (createAttemptId ?? (() => Guid.NewGuid().ToString("N")))();
            _recentAttempts[key] = new RecentAttempt(attemptId, now);
            return new WhatsAppManualSendDecision(true, attemptId);
        }
    }

    private static string BuildKey(string phone, string text) =>
        $"{phone.Trim()}\n{text.Trim()}";

    private sealed record RecentAttempt(string AttemptId, DateTimeOffset StartedAt);
}

internal readonly record struct WhatsAppManualSendDecision(bool Accepted, string AttemptId);

internal static class WhatsAppManualSendPolicy
{
    internal const int MaxTextLength = 8_000;

    internal static bool IsTextWithinLimit(string text) => text.Length <= MaxTextLength;

    internal static bool AllowsLegacyFallback(int statusCode, string responseBody)
    {
        if (statusCode != 404 || string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        var marker = Fold(responseBody);
        if (marker.Contains("instance_not_found", StringComparison.Ordinal) ||
            marker.Contains("instance not found", StringComparison.Ordinal) ||
            marker.Contains("instancia nao encontrada", StringComparison.Ordinal) ||
            marker.Contains("invalid instance", StringComparison.Ordinal) ||
            marker.Contains("instancia invalida", StringComparison.Ordinal))
        {
            return false;
        }

        return marker.Contains("route_not_found", StringComparison.Ordinal) ||
               marker.Contains("endpoint_not_found", StringComparison.Ordinal) ||
               marker.Contains("route not found", StringComparison.Ordinal) ||
               marker.Contains("endpoint not found", StringComparison.Ordinal) ||
               marker.Contains("rota nao encontrada", StringComparison.Ordinal) ||
               marker.Contains("cannot post /api/agenda/send", StringComparison.Ordinal);
    }

    internal static bool IsAmbiguousHttpStatus(int statusCode) =>
        statusCode is 408 or 502 or 503 or 504;

    internal static bool IsExistingPending(bool accepted, string status)
    {
        var normalized = status.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');
        return !accepted && normalized is "sending" or "pending" or "queued" or "processing" or "pendente" or "enviando";
    }

    internal static bool CanTransitionDeliveryStatus(string currentStatus, string incomingStatus)
    {
        if (string.Equals(currentStatus, incomingStatus, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(currentStatus, "erro", StringComparison.OrdinalIgnoreCase))
        {
            return incomingStatus is "enviado" or "entregue" or "lido";
        }

        if (string.Equals(incomingStatus, "erro", StringComparison.OrdinalIgnoreCase))
        {
            return currentStatus is "pendente" or "incerto";
        }

        return DeliveryRank(incomingStatus) >= DeliveryRank(currentStatus);
    }

    private static int DeliveryRank(string status) => status switch
    {
        "pendente" or "incerto" => 1,
        "enviado" => 2,
        "entregue" => 3,
        "lido" => 4,
        _ => 0
    };

    private static string Fold(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
