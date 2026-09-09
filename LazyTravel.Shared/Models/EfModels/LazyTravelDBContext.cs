using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

// 已修復雙重命名空間的問題
namespace LazyTravel.Shared.Models.EfModels;

// 繼承 IdentityDbContext
public partial class LazyTravelDBContext : IdentityDbContext<Member, IdentityRole<int>, int>
{
	public LazyTravelDBContext()
	{
	}

	public LazyTravelDBContext(DbContextOptions<LazyTravelDBContext> options)
		: base(options)
	{
	}

	public virtual DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
	public virtual DbSet<Block> Blocks { get; set; }
	public virtual DbSet<Category> Categories { get; set; }
	public virtual DbSet<Employee> Employees { get; set; }
	public virtual DbSet<EmployeeRole> EmployeeRoles { get; set; }
	public virtual DbSet<Expense> Expenses { get; set; }
	public virtual DbSet<ExpenseSplit> ExpenseSplits { get; set; }
	public virtual DbSet<Follow> Follows { get; set; }
	public virtual DbSet<ForumComment> ForumComments { get; set; }
	public virtual DbSet<ForumImage> ForumImages { get; set; }
	public virtual DbSet<ForumInteract> ForumInteracts { get; set; }
	public virtual DbSet<ForumPost> ForumPosts { get; set; }
	public virtual DbSet<FriendRequest> FriendRequests { get; set; }
	public virtual DbSet<Friendship> Friendships { get; set; }
	public virtual DbSet<GroupMember> GroupMembers { get; set; }
	public virtual DbSet<ItineraryNode> ItineraryNodes { get; set; }
	public virtual DbSet<JoinRequest> JoinRequests { get; set; }
	public virtual DbSet<LoginHistory> LoginHistories { get; set; }
	public virtual DbSet<MemberSkill> MemberSkills { get; set; }
	public virtual DbSet<MemberSubscription> MemberSubscriptions { get; set; }
	public virtual DbSet<Notification> Notifications { get; set; }
	public virtual DbSet<Permission> Permissions { get; set; }
	public virtual DbSet<PostInteraction> PostInteractions { get; set; }
	public virtual DbSet<Report> Reports { get; set; }
	public virtual DbSet<ReportReasonCategoryLookup> ReportReasonCategoryLookups { get; set; }
	public virtual DbSet<ReportStatusLookup> ReportStatusLookups { get; set; }
	public virtual DbSet<ReportTargetTypeLookup> ReportTargetTypeLookups { get; set; }
	public virtual DbSet<Role> StaffRoles { get; set; }
	public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
	public virtual DbSet<TravelGroup> TravelGroups { get; set; }
	public virtual DbSet<TravelGroupBudget> TravelGroupBudgets { get; set; }
	public virtual DbSet<TravelGroupImage> TravelGroupImages { get; set; }
	public virtual DbSet<TravelGroupItineraryItem> TravelGroupItineraryItems { get; set; }
	public virtual DbSet<TravelGroupsLog> TravelGroupsLogs { get; set; }
	public virtual DbSet<TravelSkill> TravelSkills { get; set; }
	public virtual DbSet<VlogPost> VlogPosts { get; set; }
	public virtual DbSet<VlogPostImage> VlogPostImages { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// 呼叫底層 Identity 設定
		base.OnModelCreating(modelBuilder);

		// 強制將 Identity 表名對應回我們的乾淨名稱
		modelBuilder.Entity<Member>().ToTable("Members");
		modelBuilder.Entity<IdentityRole<int>>().ToTable("MemberRoles");
		modelBuilder.Entity<IdentityUserRole<int>>().ToTable("MemberUserRoles");
		modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("MemberClaims");
		modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("MemberLogins");
		modelBuilder.Entity<IdentityUserToken<int>>().ToTable("MemberTokens");
		modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("MemberRoleClaims");

		// ---------- 以下保留非 Identity 資料表的實體屬性設定 ----------
		modelBuilder.Entity<AdminAuditLog>(entity =>
		{
			entity.HasKey(e => e.LogId);
			entity.Property(e => e.LogId).HasColumnName("LogID");
			entity.Property(e => e.Action).IsRequired().HasMaxLength(100).IsUnicode(false);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.Description).HasMaxLength(500);
			entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
			entity.Property(e => e.Ipaddress).IsRequired().HasMaxLength(50).IsUnicode(false).HasColumnName("IPAddress");
			entity.Property(e => e.TargetId).HasMaxLength(50).IsUnicode(false).HasColumnName("TargetID");
			entity.Property(e => e.TargetResource).IsRequired().HasMaxLength(50).IsUnicode(false);

			entity.HasOne(d => d.Employee).WithMany(p => p.AdminAuditLogs)
				.HasForeignKey(d => d.EmployeeId)
				.HasConstraintName("FK_AdminAuditLogs_Employee");
		});

		modelBuilder.Entity<Block>(entity =>
		{
			entity.HasKey(e => new { e.BlockerId, e.BlockedId });
			entity.Property(e => e.BlockerId).HasColumnName("BlockerID");
			entity.Property(e => e.BlockedId).HasColumnName("BlockedID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Blocked).WithMany(p => p.BlockBlockeds)
				.HasForeignKey(d => d.BlockedId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Blocks_Blocked");

			entity.HasOne(d => d.Blocker).WithMany(p => p.BlockBlockers)
				.HasForeignKey(d => d.BlockerId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Blocks_Blocker");
		});

		modelBuilder.Entity<Category>(entity =>
		{
			entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
			entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(50);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.IsActive).HasDefaultValue(true);
			entity.Property(e => e.ModuleType).HasDefaultValue((byte)1);
		});

		modelBuilder.Entity<Employee>(entity =>
		{
			entity.HasIndex(e => e.Email, "UQ_Employees_Email").IsUnique().HasFilter("([IsDelete]=(0))");
			entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.Department).IsRequired().HasMaxLength(50);
			entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
			entity.Property(e => e.EmployeeNo).IsRequired().HasMaxLength(20).IsUnicode(false);
			entity.Property(e => e.LastLoginAt).HasColumnType("datetime");
			entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
			entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
			entity.Property(e => e.Status).HasDefaultValue((byte)1);
		});

		modelBuilder.Entity<EmployeeRole>(entity =>
		{
			entity.HasKey(e => new { e.EmployeeId, e.RoleId });
			entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
			entity.Property(e => e.RoleId).HasColumnName("RoleID");
			entity.Property(e => e.GrantedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Employee).WithMany(p => p.EmployeeRoles)
				.HasForeignKey(d => d.EmployeeId)
				.HasConstraintName("FK_EmployeeRoles_Employee");

			entity.HasOne(d => d.Role).WithMany(p => p.EmployeeRoles)
				.HasForeignKey(d => d.RoleId)
				.HasConstraintName("FK_EmployeeRoles_Role");
		});

		modelBuilder.Entity<Expense>(entity =>
		{
			entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
			entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.PayerId).HasColumnName("PayerID");
			entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
			entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

			entity.HasOne(d => d.Group).WithMany(p => p.Expenses)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_Expenses_Group");

			entity.HasOne(d => d.Payer).WithMany(p => p.Expenses)
				.HasForeignKey(d => d.PayerId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Expenses_Payer");
		});

		modelBuilder.Entity<ExpenseSplit>(entity =>
		{
			entity.HasKey(e => e.SplitId);
			entity.Property(e => e.SplitId).HasColumnName("SplitID");
			entity.Property(e => e.ExpenseId).HasColumnName("ExpenseID");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.OweAmount).HasColumnType("decimal(18, 2)");

			entity.HasOne(d => d.Expense).WithMany(p => p.ExpenseSplits)
				.HasForeignKey(d => d.ExpenseId)
				.HasConstraintName("FK_ExpenseSplits_Expense");

			entity.HasOne(d => d.Member).WithMany(p => p.ExpenseSplits)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_ExpenseSplits_Member");
		});

		modelBuilder.Entity<Follow>(entity =>
		{
			entity.HasIndex(e => new { e.FollowerId, e.FolloweeId }, "UQ_Follows_Follower_Followee").IsUnique();
			entity.Property(e => e.FollowId).HasColumnName("FollowID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.FolloweeId).HasColumnName("FolloweeID");
			entity.Property(e => e.FollowerId).HasColumnName("FollowerID");
			entity.Property(e => e.Status).HasDefaultValue((byte)1);
			entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Followee).WithMany(p => p.FollowFollowees)
				.HasForeignKey(d => d.FolloweeId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Follows_Followee");

			entity.HasOne(d => d.Follower).WithMany(p => p.FollowFollowers)
				.HasForeignKey(d => d.FollowerId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Follows_Follower");
		});

		modelBuilder.Entity<ForumComment>(entity =>
		{
			entity.HasKey(e => e.CommentId);
			entity.Property(e => e.CommentId).HasColumnName("CommentID");
			entity.Property(e => e.Content).IsRequired().HasMaxLength(500);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.ForumPostId).HasColumnName("ForumPostID");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

			entity.HasOne(d => d.ForumPost).WithMany(p => p.ForumComments)
				.HasForeignKey(d => d.ForumPostId)
				.HasConstraintName("FK_ForumComments_Post");

			entity.HasOne(d => d.Member).WithMany(p => p.ForumComments)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_ForumComments_Member");
		});

		modelBuilder.Entity<ForumImage>(entity =>
		{
			entity.HasKey(e => e.ImageId);
			entity.Property(e => e.ImageId).HasColumnName("ImageID");
			entity.Property(e => e.ForumPostId).HasColumnName("ForumPostID");
			entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);

			entity.HasOne(d => d.ForumPost).WithMany(p => p.ForumImages)
				.HasForeignKey(d => d.ForumPostId)
				.HasConstraintName("FK_ForumImages_Post");
		});

		modelBuilder.Entity<ForumInteract>(entity =>
		{
			entity.HasKey(e => new { e.ForumPostId, e.MemberId, e.ActionType });
			entity.Property(e => e.ForumPostId).HasColumnName("ForumPostID");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.ForumPost).WithMany(p => p.ForumInteracts)
				.HasForeignKey(d => d.ForumPostId)
				.HasConstraintName("FK_ForumInteracts_Post");

			entity.HasOne(d => d.Member).WithMany(p => p.ForumInteracts)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_ForumInteracts_Member");
		});

		modelBuilder.Entity<ForumPost>(entity =>
		{
			entity.HasIndex(e => e.IsDelete, "IX_ForumPosts_IsDelete");
			entity.Property(e => e.ForumPostId).HasColumnName("ForumPostID");
			entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
			entity.Property(e => e.Content).IsRequired();
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
			entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

			entity.HasOne(d => d.Category).WithMany(p => p.ForumPosts)
				.HasForeignKey(d => d.CategoryId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_ForumPosts_Category");

			entity.HasOne(d => d.Member).WithMany(p => p.ForumPosts)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_ForumPosts_Member");
		});

		modelBuilder.Entity<FriendRequest>(entity =>
		{
			entity.HasKey(e => e.RequestId);
			entity.Property(e => e.RequestId).HasColumnName("RequestID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.Message).HasMaxLength(300);
			entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
			entity.Property(e => e.RequesterId).HasColumnName("RequesterID");
			entity.Property(e => e.ReviewedAt).HasColumnType("datetime");

			entity.HasOne(d => d.Receiver).WithMany(p => p.FriendRequestReceivers)
				.HasForeignKey(d => d.ReceiverId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_FriendReq_Receiver");

			entity.HasOne(d => d.Requester).WithMany(p => p.FriendRequestRequesters)
				.HasForeignKey(d => d.RequesterId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_FriendReq_Requester");
		});

		modelBuilder.Entity<Friendship>(entity =>
		{
			entity.HasIndex(e => new { e.MemberId1, e.MemberId2 }, "UQ_Friendships_Pair").IsUnique();
			entity.Property(e => e.FriendshipId).HasColumnName("FriendshipID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.MemberId1).HasColumnName("MemberID1");
			entity.Property(e => e.MemberId2).HasColumnName("MemberID2");

			entity.HasOne(d => d.MemberId1Navigation).WithMany(p => p.FriendshipMemberId1Navigations)
				.HasForeignKey(d => d.MemberId1)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Friendships_M1");

			entity.HasOne(d => d.MemberId2Navigation).WithMany(p => p.FriendshipMemberId2Navigations)
				.HasForeignKey(d => d.MemberId2)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Friendships_M2");
		});

		modelBuilder.Entity<GroupMember>(entity =>
		{
			entity.HasIndex(e => e.MemberId, "IX_GroupMembers_MemberID");
			entity.Property(e => e.GroupMemberId).HasColumnName("GroupMemberID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.JoinedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.LeftAt).HasColumnType("datetime");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.RemoveReason).HasMaxLength(300);
			entity.Property(e => e.RemovedAt).HasColumnType("datetime");
			entity.Property(e => e.RemovedByMemberId).HasColumnName("RemovedByMemberID");

			entity.HasOne(d => d.Group).WithMany(p => p.GroupMembers)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_GroupMembers_Group");

			entity.HasOne(d => d.Member).WithMany(p => p.GroupMemberMembers)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_GroupMembers_Member");

			entity.HasOne(d => d.RemovedByMember).WithMany(p => p.GroupMemberRemovedByMembers)
				.HasForeignKey(d => d.RemovedByMemberId)
				.HasConstraintName("FK_GroupMembers_RemovedBy");
		});

		modelBuilder.Entity<ItineraryNode>(entity =>
		{
			entity.HasKey(e => e.NodeId);
			entity.Property(e => e.NodeId).HasColumnName("NodeID");
			entity.Property(e => e.LocationName).IsRequired().HasMaxLength(100);
			entity.Property(e => e.MediaType).HasConversion<byte>();
			entity.Property(e => e.MediaUrl).HasMaxLength(500);
			entity.Property(e => e.PostId).HasColumnName("PostID");

			entity.HasOne(d => d.Post).WithMany(p => p.ItineraryNodes)
				.HasForeignKey(d => d.PostId)
				.HasConstraintName("FK_ItineraryNodes_Post");
		});

		modelBuilder.Entity<JoinRequest>(entity =>
		{
			entity.HasKey(e => e.RequestId);
			entity.Property(e => e.RequestId).HasColumnName("RequestID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.Message).HasMaxLength(500);
			entity.Property(e => e.ReviewedAt).HasColumnType("datetime");
			entity.Property(e => e.ReviewedByMemberId).HasColumnName("ReviewedByMemberID");

			entity.HasOne(d => d.Group).WithMany(p => p.JoinRequests)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_JoinRequests_Group");

			entity.HasOne(d => d.Member).WithMany(p => p.JoinRequestMembers)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_JoinRequests_Member");

			entity.HasOne(d => d.ReviewedByMember).WithMany(p => p.JoinRequestReviewedByMembers)
				.HasForeignKey(d => d.ReviewedByMemberId)
				.HasConstraintName("FK_JoinRequests_Reviewer");
		});

		modelBuilder.Entity<LoginHistory>(entity =>
		{
			entity.HasKey(e => e.HistoryId);
			entity.HasIndex(e => e.MemberId, "IX_LoginHistories_MemberID");
			entity.Property(e => e.HistoryId).HasColumnName("HistoryID");
			entity.Property(e => e.AttemptedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.LoginIp).IsRequired().HasMaxLength(50).IsUnicode(false).HasColumnName("LoginIP");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.UserAgent).HasMaxLength(255);

			entity.HasOne(d => d.Member).WithMany(p => p.LoginHistories)
				.HasForeignKey(d => d.MemberId)
				.HasConstraintName("FK_LoginHistories_Members");
		});

		modelBuilder.Entity<MemberSkill>(entity =>
		{
			entity.HasKey(e => new { e.MemberId, e.SkillId });
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.SkillId).HasColumnName("SkillID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Member).WithMany(p => p.MemberSkills)
				.HasForeignKey(d => d.MemberId)
				.HasConstraintName("FK_MemberSkills_Member");

			entity.HasOne(d => d.Skill).WithMany(p => p.MemberSkills)
				.HasForeignKey(d => d.SkillId)
				.HasConstraintName("FK_MemberSkills_Skill");
		});

		modelBuilder.Entity<MemberSubscription>(entity =>
		{
			entity.HasKey(e => e.SubscriptionId);
			entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionID");
			entity.Property(e => e.EndDate).HasColumnType("datetime");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.PlanId).HasColumnName("PlanID");
			entity.Property(e => e.StartDate).HasColumnType("datetime");

			entity.HasOne(d => d.Member).WithMany(p => p.MemberSubscriptions)
				.HasForeignKey(d => d.MemberId)
				.HasConstraintName("FK_MemberSubscriptions_Member");

			entity.HasOne(d => d.Plan).WithMany(p => p.MemberSubscriptions)
				.HasForeignKey(d => d.PlanId)
				.HasConstraintName("FK_MemberSubscriptions_Plan");
		});

		modelBuilder.Entity<Notification>(entity =>
		{
			entity.HasIndex(e => new { e.MemberId, e.IsRead, e.CreatedAt }, "IX_Notifications_Member_Read_Time");
			entity.Property(e => e.NotificationId).HasColumnName("NotificationID");
			entity.Property(e => e.Content).IsRequired().HasMaxLength(255);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.RelatedId).HasColumnName("RelatedID");

			entity.HasOne(d => d.Member).WithMany(p => p.Notifications)
				.HasForeignKey(d => d.MemberId)
				.HasConstraintName("FK_Notifications_Members");
		});

		modelBuilder.Entity<Permission>(entity =>
		{
			entity.Property(e => e.PermissionId).HasColumnName("PermissionID");
			entity.Property(e => e.Description).HasMaxLength(200);
			entity.Property(e => e.ModuleName).IsRequired().HasMaxLength(50);
			entity.Property(e => e.PermissionCode).IsRequired().HasMaxLength(100).IsUnicode(false);
		});

		modelBuilder.Entity<PostInteraction>(entity =>
		{
			entity.HasKey(e => new { e.PostId, e.MemberId, e.ActionType });
			entity.Property(e => e.PostId).HasColumnName("PostID");
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.ActionType).HasConversion<byte>();
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Member).WithMany(p => p.PostInteractions)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_PostInteractions_Member");

			entity.HasOne(d => d.Post).WithMany(p => p.PostInteractions)
				.HasForeignKey(d => d.PostId)
				.HasConstraintName("FK_PostInteractions_Post");
		});

		modelBuilder.Entity<Report>(entity =>
		{
			entity.HasIndex(e => e.ReportStatus, "IX_Reports_Status");
			entity.Property(e => e.ReportId).HasColumnName("ReportID");
			entity.Property(e => e.AdminNotes).HasMaxLength(500);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.Description).HasMaxLength(500);
			entity.Property(e => e.EvidenceUrl).HasMaxLength(300);
			entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
			entity.Property(e => e.ReasonCategory).HasDefaultValue((byte)5);
			entity.Property(e => e.ReportedMemberId).HasColumnName("ReportedMemberID");
			entity.Property(e => e.ReporterId).HasColumnName("ReporterID");
			entity.Property(e => e.TargetId).HasColumnName("TargetID");
			entity.Property(e => e.TargetSnapshot).HasMaxLength(500);
			entity.Property(e => e.TargetTitle).HasMaxLength(200);

			entity.HasOne(d => d.ReasonCategoryNavigation).WithMany(p => p.Reports)
				.HasForeignKey(d => d.ReasonCategory)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Reports_Category");

			entity.HasOne(d => d.ReportStatusNavigation).WithMany(p => p.Reports)
				.HasForeignKey(d => d.ReportStatus)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Reports_Status");

			entity.HasOne(d => d.ReportTypeNavigation).WithMany(p => p.Reports)
				.HasForeignKey(d => d.ReportType)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Reports_Type");

			entity.HasOne(d => d.ReportedMember).WithMany(p => p.ReportReportedMembers)
				.HasForeignKey(d => d.ReportedMemberId)
				.HasConstraintName("FK_Reports_ReportedMember");

			entity.HasOne(d => d.Reporter).WithMany(p => p.ReportReporters)
				.HasForeignKey(d => d.ReporterId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_Reports_Reporter");
		});

		modelBuilder.Entity<ReportReasonCategoryLookup>(entity =>
		{
			// 🌟 類別名稱從 ReportReasonCategory 改成 ReportReasonCategoryLookup（避免撞名），
			// DbSet 屬性名稱也改了，所以要用 .ToTable(...) 把資料表名稱釘住，不然 EF 會照 DbSet
			// 屬性名稱去找一張叫「ReportReasonCategoryLookups」的表，但實際資料表還是叫 ReportReasonCategories。
			entity.ToTable("ReportReasonCategories");
			entity.HasKey(e => e.CategoryId);
			entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
			entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(50);
		});

		modelBuilder.Entity<ReportStatusLookup>(entity =>
		{
			entity.ToTable("ReportStatuses");
			entity.HasKey(e => e.StatusId);
			entity.Property(e => e.StatusId).HasColumnName("StatusID");
			entity.Property(e => e.StatusName).IsRequired().HasMaxLength(50);
		});

		modelBuilder.Entity<ReportTargetTypeLookup>(entity =>
		{
			entity.ToTable("ReportTargetTypes");
			entity.HasKey(e => e.TypeId);
			entity.Property(e => e.TypeId).HasColumnName("TypeID");
			entity.Property(e => e.TypeName).IsRequired().HasMaxLength(50);
		});

		modelBuilder.Entity<Role>(entity =>
		{
			entity.ToTable("Roles");
			entity.Property(e => e.RoleId).HasColumnName("RoleID");
			entity.Property(e => e.Description).HasMaxLength(200);
			entity.Property(e => e.RoleCode).IsRequired().HasMaxLength(50).IsUnicode(false);
			entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);

			entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
				.UsingEntity<Dictionary<string, object>>(
					"RolePermission",
					r => r.HasOne<Permission>().WithMany()
						.HasForeignKey("PermissionId")
						.HasConstraintName("FK_RolePermissions_Permission"),
					l => l.HasOne<Role>().WithMany()
						.HasForeignKey("RoleId")
						.HasConstraintName("FK_RolePermissions_Role"),
					j =>
					{
						j.HasKey("RoleId", "PermissionId");
						j.ToTable("RolePermissions");
						j.IndexerProperty<int>("RoleId").HasColumnName("RoleID");
						j.IndexerProperty<int>("PermissionId").HasColumnName("PermissionID");
					});
		});

		modelBuilder.Entity<SubscriptionPlan>(entity =>
		{
			entity.HasKey(e => e.PlanId);
			entity.Property(e => e.PlanId).HasColumnName("PlanID");
			entity.Property(e => e.Description).HasMaxLength(300);
			entity.Property(e => e.IsActive).HasDefaultValue(true);
			entity.Property(e => e.PlanName).IsRequired().HasMaxLength(50);
			entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
		});

		modelBuilder.Entity<TravelGroup>(entity =>
		{
			entity.HasKey(e => e.GroupId);
			entity.HasIndex(e => e.IsDelete, "IX_TravelGroups_IsDelete");
			entity.HasIndex(e => e.StartDate, "IX_TravelGroups_StartDate");
			entity.HasIndex(e => e.GroupStatus, "IX_TravelGroups_Status");

			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.Country).IsRequired().HasMaxLength(50);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.CurrentPeople).HasDefaultValue(1);
			entity.Property(e => e.Description).HasMaxLength(1000);
			entity.Property(e => e.GroupTitle).IsRequired().HasMaxLength(100);
			entity.Property(e => e.IsPublic).HasDefaultValue(true).ValueGeneratedNever();
			entity.Property(e => e.JoinRule).HasDefaultValue((byte)1);
			entity.Property(e => e.MaxPeople).HasDefaultValue(10);
			entity.Property(e => e.MinPeople).HasDefaultValue(2);
			entity.Property(e => e.OwnerMemberId).HasColumnName("OwnerMemberID");
			entity.Property(e => e.Region).IsRequired().HasMaxLength(100);
			entity.Property(e => e.ReviewStatus).HasConversion<byte>();
			entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.OwnerMember).WithMany(p => p.TravelGroups)
				.HasForeignKey(d => d.OwnerMemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_TravelGroups_Owner");
		});

		modelBuilder.Entity<TravelGroupBudget>(entity =>
		{
			entity.HasKey(e => e.BudgetId);
			entity.Property(e => e.BudgetId).HasColumnName("BudgetID");
			entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
			entity.Property(e => e.BudgetName).IsRequired().HasMaxLength(100);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.CurrencyCode).IsRequired().HasMaxLength(3).IsUnicode(false).HasDefaultValue("TWD").IsFixedLength();
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.IsRequired).HasDefaultValue(true).ValueGeneratedNever();
			entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Group).WithMany(p => p.TravelGroupBudgets)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_TravelGroupBudgets_Group");
		});

		modelBuilder.Entity<TravelGroupImage>(entity =>
		{
			entity.HasKey(e => e.ImageId);
			entity.Property(e => e.ImageId).HasColumnName("ImageID");
			entity.Property(e => e.AltText).IsRequired().HasMaxLength(150);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(600);
			entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.UploadedByMemberId).HasColumnName("UploadedByMemberID");

			entity.HasOne(d => d.Group).WithMany(p => p.TravelGroupImages)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_GroupImages_Group");

			entity.HasOne(d => d.UploadedByMember).WithMany(p => p.TravelGroupImages)
				.HasForeignKey(d => d.UploadedByMemberId)
				.HasConstraintName("FK_GroupImages_Member");
		});

		modelBuilder.Entity<TravelGroupItineraryItem>(entity =>
		{
			entity.HasKey(e => e.ItineraryItemId);
			entity.Property(e => e.ItineraryItemId).HasColumnName("ItineraryItemID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.LocationName).IsRequired().HasMaxLength(150);
			entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
			entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

			entity.HasOne(d => d.Group).WithMany(p => p.TravelGroupItineraryItems)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_ItineraryItems_Group");
		});

		modelBuilder.Entity<TravelGroupsLog>(entity =>
		{
			entity.HasKey(e => e.LogId);
			entity.ToTable("TravelGroupsLog");
			entity.Property(e => e.LogId).HasColumnName("LogID");
			entity.Property(e => e.ChangeByMemberId).HasColumnName("ChangeByMemberID");
			entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(30);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.FieldName).IsRequired().HasMaxLength(50);
			entity.Property(e => e.GroupId).HasColumnName("GroupID");
			entity.Property(e => e.NewValue).IsRequired().HasMaxLength(300);
			entity.Property(e => e.OldValue).IsRequired().HasMaxLength(300);
			entity.Property(e => e.Remark).HasMaxLength(300);

			// 🌟 外鍵已請使用者從 Members 改指向 Employees（詳見 TravelGroupsLog.cs 的說明），
			// 約束名稱也同步改成 FK_TravelGroupsLog_Employee。
			entity.HasOne(d => d.ChangeByEmployee).WithMany(p => p.TravelGroupsLogs)
				.HasForeignKey(d => d.ChangeByMemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_TravelGroupsLog_Employee");

			entity.HasOne(d => d.Group).WithMany(p => p.TravelGroupsLogs)
				.HasForeignKey(d => d.GroupId)
				.HasConstraintName("FK_TravelGroupsLog_Group");
		});

		modelBuilder.Entity<TravelSkill>(entity =>
		{
			entity.HasKey(e => e.SkillId);
			entity.Property(e => e.SkillId).HasColumnName("SkillID");
			entity.Property(e => e.IconCode).HasMaxLength(50).IsUnicode(false);
			entity.Property(e => e.IsActive).HasDefaultValue(true);
			entity.Property(e => e.SkillCategory).HasDefaultValue((byte)1);
			entity.Property(e => e.SkillName).IsRequired().HasMaxLength(50);
		});

		modelBuilder.Entity<VlogPost>(entity =>
		{
			entity.HasKey(e => e.PostId);
			entity.HasIndex(e => e.IsDelete, "IX_VlogPosts_IsDelete");
			entity.Property(e => e.PostId).HasColumnName("PostID");
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.Destination).IsRequired().HasMaxLength(100);
			entity.Property(e => e.MediaType).HasConversion<byte>();
			entity.Property(e => e.MediaUrl).HasMaxLength(500);
			entity.Property(e => e.MemberId).HasColumnName("MemberID");
			entity.Property(e => e.Status).HasConversion<byte>();
			entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
			entity.Property(e => e.TravelDate).HasColumnType("datetime");
			entity.Property(e => e.TravelDays).HasDefaultValue(1);
			// 🌟 TravelPeople 現在資料庫是 nvarchar(50)，用 HasConversion<string>() 把
			// TravelGroupSize enum 存成 "Solo"/"Small"/"Large" 這樣的字串（依使用者指示保留 nvarchar(50)）。
			entity.Property(e => e.TravelPeople).IsRequired().HasMaxLength(50).HasConversion<string>();
			entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

			entity.HasOne(d => d.Member).WithMany(p => p.VlogPosts)
				.HasForeignKey(d => d.MemberId)
				.OnDelete(DeleteBehavior.ClientSetNull)
				.HasConstraintName("FK_VlogPosts_Member");
		});

		modelBuilder.Entity<VlogPostImage>(entity =>
		{
			entity.HasKey(e => e.ImageId);
			entity.Property(e => e.ImageId).HasColumnName("ImageID");
			entity.Property(e => e.AltText).IsRequired().HasMaxLength(150);
			entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(600);
			entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
			entity.Property(e => e.UploadedByMemberId).HasColumnName("UploadedByMemberID");
			entity.Property(e => e.VlogPostId).HasColumnName("VlogPostID");

			entity.HasOne(d => d.UploadedByMember).WithMany(p => p.VlogPostImages)
				.HasForeignKey(d => d.UploadedByMemberId)
				.HasConstraintName("FK_VlogPostImages_Member");

			entity.HasOne(d => d.VlogPost).WithMany(p => p.VlogPostImages)
				.HasForeignKey(d => d.VlogPostId)
				.HasConstraintName("FK_VlogPostImages_Post");
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
