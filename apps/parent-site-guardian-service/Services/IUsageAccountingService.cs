using ParentSiteGuardian.Service.Api;

namespace ParentSiteGuardian.Service.Services;

public interface IUsageAccountingService
{
    Task<UsageTickResponse> RecordTickAsync(UsageTickRequest request, CancellationToken cancellationToken = default);
    Task<DecisionResponse?> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
    Task<TodayUsageResponse?> GetTodayAsync(string deviceId, CancellationToken cancellationToken = default);
    Task<UsageHistoryResponse?> GetHistoryAsync(string deviceId, int days, CancellationToken cancellationToken = default);
}
