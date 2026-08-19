using LazyTravel.Models;
using LazyTravel.Models.EfModels;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Services;

public static class TGReviewStatusService
{
	public const string Normal = "正常";
	public const string PendingReview = "檢舉審核中";
	public const string Violation = "違規";

	public static async Task SyncAsync(LazyTravelDBContext context)
	{
		var groups = await context.TravelGroups.ToListAsync();
		if (groups.Count == 0)
		{
			return;
		}

		var groupIds = groups.Select(g => g.GroupId).ToList();
		var reportsByGroupId = await context.Reports
			.Where(r => r.ReportType == (byte)ReportTargetType.TravelGroup &&
						r.TargetId.HasValue &&
						groupIds.Contains(r.TargetId.Value))
			.GroupBy(r => r.TargetId!.Value)
			.Select(g => new
			{
				GroupId = g.Key,
				HasUpheld = g.Any(r => r.ReportStatus == (byte)ReportStatus.Upheld),
				HasPending = g.Any(r => r.ReportStatus == (byte)ReportStatus.Pending)
			})
			.ToDictionaryAsync(g => g.GroupId);

		var hasChanges = false;
		foreach (var group in groups)
		{
			var reviewStatus = Normal;

			if (reportsByGroupId.TryGetValue(group.GroupId, out var reportState))
			{
				reviewStatus = reportState.HasUpheld
					? Violation
					: reportState.HasPending
						? PendingReview
						: Normal;
			}

			if (group.ReviewStatus == reviewStatus)
			{
				continue;
			}

			group.ReviewStatus = reviewStatus;
			group.UpdatedAt = DateTime.Now;
			hasChanges = true;
		}

		if (hasChanges)
		{
			await context.SaveChangesAsync();
		}
	}
}
