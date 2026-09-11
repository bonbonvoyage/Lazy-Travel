using Microsoft.AspNetCore.Identity;

 
namespace LazyTravel.Shared.Models.EfModels;
 
public partial class Member : IdentityUser<int>
{
	public string? City { get; set; }
	public string Name { get; set; } = null!;
	public string? LineId { get; set; }
	public string? InstagramUrl { get; set; }
	public string? FacebookUrl { get; set; }
	public byte ContactBookVisibility { get; set; }
	public bool IsPrivateAccount { get; set; }
	public string? AvatarUrl { get; set; }
	public DateOnly? BirthDate { get; set; }
	public byte Gender { get; set; }
	public string? Occupation { get; set; }
	public string? Mbti { get; set; }
	public string? Bio { get; set; }
	public byte Status { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? LastLoginAt { get; set; }
	public string? LastLoginIp { get; set; }
	public bool IsDelete { get; set; }
 
	public virtual ICollection<Block> BlockBlockeds { get; set; } = new List<Block>();
	public virtual ICollection<Block> BlockBlockers { get; set; } = new List<Block>();
	public virtual ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
	public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
	public virtual ICollection<Follow> FollowFollowees { get; set; } = new List<Follow>();
	public virtual ICollection<Follow> FollowFollowers { get; set; } = new List<Follow>();
	public virtual ICollection<ForumComment> ForumComments { get; set; } = new List<ForumComment>();
	public virtual ICollection<ForumInteract> ForumInteracts { get; set; } = new List<ForumInteract>();
	public virtual ICollection<ForumPost> ForumPosts { get; set; } = new List<ForumPost>();
	public virtual ICollection<FriendRequest> FriendRequestReceivers { get; set; } = new List<FriendRequest>();
	public virtual ICollection<FriendRequest> FriendRequestRequesters { get; set; } = new List<FriendRequest>();
	public virtual ICollection<Friendship> FriendshipMemberId1Navigations { get; set; } = new List<Friendship>();
	public virtual ICollection<Friendship> FriendshipMemberId2Navigations { get; set; } = new List<Friendship>();
	public virtual ICollection<GroupMember> GroupMemberMembers { get; set; } = new List<GroupMember>();
	public virtual ICollection<GroupMember> GroupMemberRemovedByMembers { get; set; } = new List<GroupMember>();
	public virtual ICollection<JoinRequest> JoinRequestMembers { get; set; } = new List<JoinRequest>();
	public virtual ICollection<JoinRequest> JoinRequestReviewedByMembers { get; set; } = new List<JoinRequest>();
	public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
	public virtual ICollection<MemberSkill> MemberSkills { get; set; } = new List<MemberSkill>();
	public virtual ICollection<MemberSubscription> MemberSubscriptions { get; set; } = new List<MemberSubscription>();
	public virtual ICollection<MemberTravelDNA> MemberTravelDnas { get; set; } = new List<MemberTravelDNA>();
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
	public virtual ICollection<PostInteraction> PostInteractions { get; set; } = new List<PostInteraction>();
	public virtual ICollection<Report> ReportReportedMembers { get; set; } = new List<Report>();
	public virtual ICollection<Report> ReportReporters { get; set; } = new List<Report>();
	public virtual ICollection<TravelGroupImage> TravelGroupImages { get; set; } = new List<TravelGroupImage>();
	public virtual ICollection<TravelGroupInteraction> TravelGroupInteractions { get; set; } = new List<TravelGroupInteraction>();
	public virtual ICollection<TravelGroup> TravelGroups { get; set; } = new List<TravelGroup>();
	// 🌟 TravelGroupsLog.ChangeByMemberID 的外鍵已改指向 Employees，這個集合搬到 Employee.cs 去了。
	public virtual ICollection<VlogPostImage> VlogPostImages { get; set; } = new List<VlogPostImage>();
	public virtual ICollection<VlogPost> VlogPosts { get; set; } = new List<VlogPost>();
}

