namespace AgendaLivre.Windows;

internal readonly record struct WhatsAppMessageConsolidationResult(
    WhatsAppMessage? Keeper,
    int RemovedCount,
    bool Changed);

internal readonly record struct WhatsAppMessageConsolidationSummary(
    int RemovedCount,
    bool Changed);

internal static class WhatsAppMessageIdentityReconciler
{
    internal static WhatsAppMessageConsolidationResult ConsolidateExactMatches(
        List<WhatsAppMessage> localMessages,
        WhatsAppMessage authoritative,
        Func<WhatsAppMessage, WhatsAppMessage, bool>? mergeDuplicateState = null)
    {
        ArgumentNullException.ThrowIfNull(localMessages);
        ArgumentNullException.ThrowIfNull(authoritative);

        var matches = localMessages
            .Where(candidate => IsExactIdentityMatch(candidate, authoritative))
            .ToList();
        if (matches.Count == 0)
        {
            return new WhatsAppMessageConsolidationResult(null, 0, false);
        }

        var keeper = SelectCanonical(matches, authoritative);
        var changed = false;
        var removedCount = 0;
        foreach (var duplicate in matches.Where(candidate => !ReferenceEquals(candidate, keeper)).ToList())
        {
            changed |= CopyMissingStableIdentity(keeper, duplicate);
            if (mergeDuplicateState is not null)
            {
                changed |= mergeDuplicateState(keeper, duplicate);
            }

            if (localMessages.Remove(duplicate))
            {
                removedCount++;
                changed = true;
            }
        }

        changed |= ApplyAuthoritativeStableIdentity(keeper, authoritative);
        return new WhatsAppMessageConsolidationResult(keeper, removedCount, changed);
    }

    internal static WhatsAppMessageConsolidationSummary ConsolidateAuthoritativeExactDuplicates(
        List<WhatsAppMessage> localMessages,
        IEnumerable<WhatsAppMessage> authoritativeMessages,
        Func<WhatsAppMessage, WhatsAppMessage, bool>? mergeDuplicateState = null)
    {
        ArgumentNullException.ThrowIfNull(localMessages);
        ArgumentNullException.ThrowIfNull(authoritativeMessages);

        var changed = false;
        var removedCount = 0;
        foreach (var authoritative in authoritativeMessages)
        {
            var result = ConsolidateExactMatches(
                localMessages,
                authoritative,
                mergeDuplicateState);
            changed |= result.Changed;
            removedCount += result.RemovedCount;
        }

        return new WhatsAppMessageConsolidationSummary(removedCount, changed);
    }

    internal static bool IsExactIdentityMatch(
        WhatsAppMessage candidate,
        WhatsAppMessage authoritative)
    {
        if (!InstancesAreCompatible(candidate.Instance, authoritative.Instance))
        {
            return false;
        }

        var requestId = Clean(authoritative.ClientRequestId);
        if (requestId.Length > 0 &&
            (Same(candidate.ClientRequestId, requestId) || Same(candidate.Id, requestId)))
        {
            return true;
        }

        var providerId = Clean(authoritative.ProviderMessageId);
        if (providerId.Length > 0 &&
            (Same(candidate.ProviderMessageId, providerId) || Same(candidate.Id, providerId)))
        {
            return true;
        }

        var authoritativeId = Clean(authoritative.Id);
        return authoritativeId.Length > 0 && Same(candidate.Id, authoritativeId);
    }

    private static WhatsAppMessage SelectCanonical(
        IReadOnlyCollection<WhatsAppMessage> matches,
        WhatsAppMessage authoritative)
    {
        var requestId = Clean(authoritative.ClientRequestId);
        var providerId = Clean(authoritative.ProviderMessageId);
        var authoritativeId = Clean(authoritative.Id);
        return matches
            .OrderByDescending(candidate => requestId.Length > 0 &&
                (Same(candidate.ClientRequestId, requestId) || Same(candidate.Id, requestId)))
            .ThenByDescending(candidate => providerId.Length > 0 && Same(candidate.Id, providerId))
            .ThenByDescending(candidate => authoritativeId.Length > 0 && Same(candidate.Id, authoritativeId))
            .ThenByDescending(candidate => !string.IsNullOrWhiteSpace(candidate.ClientRequestId))
            .ThenBy(candidate => candidate.CreatedAt)
            .First();
    }

    private static bool CopyMissingStableIdentity(
        WhatsAppMessage keeper,
        WhatsAppMessage duplicate)
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(keeper.ClientRequestId) &&
            !string.IsNullOrWhiteSpace(duplicate.ClientRequestId))
        {
            keeper.ClientRequestId = duplicate.ClientRequestId.Trim();
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(keeper.ProviderMessageId) &&
            !string.IsNullOrWhiteSpace(duplicate.ProviderMessageId))
        {
            keeper.ProviderMessageId = duplicate.ProviderMessageId.Trim();
            changed = true;
        }

        return changed;
    }

    private static bool ApplyAuthoritativeStableIdentity(
        WhatsAppMessage keeper,
        WhatsAppMessage authoritative)
    {
        var changed = false;
        changed |= SetIfPresent(
            keeper.ClientRequestId,
            authoritative.ClientRequestId,
            value => keeper.ClientRequestId = value);
        changed |= SetIfPresent(
            keeper.ProviderMessageId,
            authoritative.ProviderMessageId,
            value => keeper.ProviderMessageId = value);
        return changed;
    }

    private static bool SetIfPresent(string current, string incoming, Action<string> setter)
    {
        var value = Clean(incoming);
        if (value.Length == 0 || Same(current, value))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private static bool InstancesAreCompatible(string candidate, string authoritative) =>
        string.IsNullOrWhiteSpace(candidate) ||
        string.IsNullOrWhiteSpace(authoritative) ||
        Same(candidate, authoritative);

    private static bool Same(string? first, string? second) =>
        string.Equals(Clean(first), Clean(second), StringComparison.OrdinalIgnoreCase);

    private static string Clean(string? value) => value?.Trim() ?? "";
}
