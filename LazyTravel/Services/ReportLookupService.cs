using LazyTravel.Models;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Services
{
    public class ReportLookupService : IReportLookupService
    {
        private readonly LazyTravelContext _context;

        // 三張對照表資料量小、幾乎不會變動,整個應用程式生命週期只查一次,用靜態欄位快取,
        // 避免每次顯示一筆檢舉單就打一次資料庫。加鎖是為了避免第一次同時有多個請求進來重複查詢。
        private static Dictionary<byte, string>? _typeCache;
        private static Dictionary<byte, string>? _categoryCache;
        private static Dictionary<byte, string>? _statusCache;
        private static readonly object _lock = new();

        public ReportLookupService(LazyTravelContext context)
        {
            _context = context;
        }

        private Dictionary<byte, string> TypeCache => EnsureCache(ref _typeCache, () => _context.ReportTargetTypeLookups.AsNoTracking().ToDictionary(x => x.TypeID, x => x.TypeName));

        private Dictionary<byte, string> CategoryCache => EnsureCache(ref _categoryCache, () => _context.ReportReasonCategoryLookups.AsNoTracking().ToDictionary(x => x.CategoryID, x => x.CategoryName));

        private Dictionary<byte, string> StatusCache => EnsureCache(ref _statusCache, () => _context.ReportStatusLookups.AsNoTracking().ToDictionary(x => x.StatusID, x => x.StatusName));

        private static Dictionary<byte, string> EnsureCache(ref Dictionary<byte, string>? field, Func<Dictionary<byte, string>> load)
        {
            if (field != null)
            {
                return field;
            }
            lock (_lock)
            {
                field ??= load();
            }
            return field;
        }

        public string GetTypeName(ReportTargetType type) =>
            TypeCache.TryGetValue((byte)type, out var name) ? name : type.ToString();

        public string GetCategoryName(ReportReasonCategory category) =>
            CategoryCache.TryGetValue((byte)category, out var name) ? name : category.ToString();

        public string GetStatusName(ReportStatus status) =>
            StatusCache.TryGetValue((byte)status, out var name) ? name : status.ToString();

        public IReadOnlyList<(ReportTargetType Value, string Name)> GetTypeOptions() =>
            TypeCache.OrderBy(kv => kv.Key).Select(kv => ((ReportTargetType)kv.Key, kv.Value)).ToList();

        public IReadOnlyList<(ReportReasonCategory Value, string Name)> GetCategoryOptions() =>
            CategoryCache.OrderBy(kv => kv.Key).Select(kv => ((ReportReasonCategory)kv.Key, kv.Value)).ToList();

        public IReadOnlyList<(ReportStatus Value, string Name)> GetStatusOptions() =>
            StatusCache.OrderBy(kv => kv.Key).Select(kv => ((ReportStatus)kv.Key, kv.Value)).ToList();
    }
}
