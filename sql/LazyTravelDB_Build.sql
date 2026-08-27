/* ============================================================
   LazyTravel 旅遊平台 - 資料庫完整建置腳本 (2026/8/26 版)
   來源：lazytraveldb.sql（已移除本機專屬路徑與 sa5 帳號設定的乾淨版匯出），
         整理成可在任何人電腦上重複執行的重建腳本。
   用途：在 SSMS 對 LazyTravelDB 一次建立/重建全部 45 張資料表
         (含 ASP.NET Core Identity 會員骨架 + 企業 RBAC + 揪團預算/圖片/行程項目)，
         並灌入對應的示範/測試資料。

   本檔相對原始匯出檔修正的問題：
     - 原始檔用 CREATE DATABASE 沒有先判斷資料庫是否已存在，且完全沒有
       DROP TABLE 的步驟 —— 只要在「已經有這套資料表結構」的電腦上執行
       （例如各組員自己電腦上如果已經建過一次），會在第一張 CREATE TABLE
       就直接報錯「已經有名為 'XXX' 的物件」，整份腳本會中斷在那裡。
     - 本檔補上了：
         1. CREATE DATABASE 前先判斷 IF DB_ID(...) IS NULL，避免資料庫已存在時報錯。
         2. 建表前用動態 SQL 找出目前所有外鍵與資料表並卸除，讓腳本變成
            「可重複執行的重建腳本」，不用手動維護 45 張表的卸除順序。

   ⚠️ 注意：這是「重建腳本」，執行時會把現有的資料表整個清空重建，
      舊資料表裡的資料會全部消失！正式使用前請先確認：
        1. 目前 LazyTravelDB 裡沒有需要保留的資料，或已經備份
        2. 不會誤刪別人正在用的資料庫

   舊檔案關係：
     - 本檔取代舊版「LazyTravelDB_Build.sql」(32 張表、舊式 Members 表)。
       舊版已不符合現況，僅供歷史對照用途。
     - LazyTravelDB_Migration_v2.sql → 舊版的增量更新腳本，內容已完全
       包含在本檔中，之後新環境建置請直接用本檔，不用再疊加執行它。
     - 本檔內含示範/測試資料（管理員帳號 ADMIN01、10 筆示範會員、
       Permissions/Roles/RolePermissions 種子資料、1 筆示範 VlogPosts 文章）。
       跟既有的 RBAC_Seed.sql / Reports_TestData.sql / TravelGroups_TestData.sql
       可能有重疊的種子資料（例如 Permissions/Roles），如果那幾份腳本也要跑，
       請注意不要重複灌兩次造成主鍵衝突。
     - Employees.PasswordHash 是示範帳號的雜湊值，正式環境請勿沿用。

   跟這份腳本一起看：Lazy Travel 旅遊平台資料表_V2.docx（期末版規格書）。
   ============================================================ */

IF DB_ID(N'LazyTravelDB') IS NULL
BEGIN
    CREATE DATABASE LazyTravelDB;
END
GO

USE LazyTravelDB;
GO

-- ------------------------------------------------------------
-- 第一步：動態卸除所有外鍵約束，再卸除資料表本身。
-- 用動態 SQL 找出目前所有的外鍵/資料表來卸除，不用手動維護 45 張表的卸除順序，
-- 避免手動排列表順序時漏排、或因為外鍵相依關係卸不掉的問題。
-- ------------------------------------------------------------
DECLARE @dropFk NVARCHAR(MAX) = N'';
SELECT @dropFk += N'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
                 + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id;
EXEC sp_executesql @dropFk;
GO

DECLARE @dropTbl NVARCHAR(MAX) = N'';
SELECT @dropTbl += N'DROP TABLE IF EXISTS ' + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name) + N';' + CHAR(10)
FROM sys.tables
WHERE name IN (
    N'AdminAuditLogs', N'Blocks', N'Categories', N'EmployeeRoles', N'Employees',
    N'Expenses', N'ExpenseSplits', N'Follows', N'ForumComments', N'ForumImages',
    N'ForumInteracts', N'ForumPosts', N'FriendRequests', N'Friendships', N'GroupMembers',
    N'ItineraryNodes', N'JoinRequests', N'LoginHistories', N'MemberClaims', N'MemberLogins',
    N'MemberRoleClaims', N'MemberRoles', N'Members', N'MemberSkills', N'MemberSubscriptions',
    N'MemberTokens', N'MemberUserRoles', N'Notifications', N'Permissions', N'PostInteractions',
    N'ReportReasonCategories', N'Reports', N'ReportStatuses', N'ReportTargetTypes',
    N'RolePermissions', N'Roles', N'SubscriptionPlans', N'TravelGroupBudgets',
    N'TravelGroupImages', N'TravelGroupItineraryItems', N'TravelGroups', N'TravelGroupsLog',
    N'TravelSkills', N'VlogPostImages', N'VlogPosts'
);
EXEC sp_executesql @dropTbl;
GO

/****** 以下開始：建立資料表 → 灌入示範資料 → 建立索引/預設值 → 建立外鍵/CHECK 約束 ******/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[AdminAuditLogs](
	[LogID] [bigint] IDENTITY(1,1) NOT NULL,
	[EmployeeID] [int] NOT NULL,
	[Action] [varchar](100) NOT NULL,
	[TargetResource] [varchar](50) NOT NULL,
	[TargetID] [varchar](50) NULL,
	[IPAddress] [varchar](50) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Description] [nvarchar](500) NULL,
 CONSTRAINT [PK_AdminAuditLogs] PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Blocks](
	[BlockerID] [int] NOT NULL,
	[BlockedID] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Blocks] PRIMARY KEY CLUSTERED 
(
	[BlockerID] ASC,
	[BlockedID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Categories](
	[CategoryID] [int] IDENTITY(1,1) NOT NULL,
	[CategoryName] [nvarchar](50) NOT NULL,
	[ModuleType] [tinyint] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[CategoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[EmployeeRoles](
	[EmployeeID] [int] NOT NULL,
	[RoleID] [int] NOT NULL,
	[GrantedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_EmployeeRoles] PRIMARY KEY CLUSTERED 
(
	[EmployeeID] ASC,
	[RoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Employees](
	[EmployeeID] [int] IDENTITY(1,1) NOT NULL,
	[EmployeeNo] [varchar](20) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Department] [nvarchar](50) NOT NULL,
	[Status] [tinyint] NOT NULL,
	[LastLoginAt] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL,
	[IsDelete] [bit] NOT NULL,
 CONSTRAINT [PK_Employees] PRIMARY KEY CLUSTERED 
(
	[EmployeeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Expenses](
	[ExpenseID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[PayerID] [int] NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[ExpenseDate] [date] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[IsDelete] [bit] NOT NULL,
 CONSTRAINT [PK_Expenses] PRIMARY KEY CLUSTERED 
(
	[ExpenseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ExpenseSplits](
	[SplitID] [int] IDENTITY(1,1) NOT NULL,
	[ExpenseID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[OweAmount] [decimal](18, 2) NOT NULL,
	[IsPaid] [bit] NOT NULL,
 CONSTRAINT [PK_ExpenseSplits] PRIMARY KEY CLUSTERED 
(
	[SplitID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Follows](
	[FollowID] [int] IDENTITY(1,1) NOT NULL,
	[FollowerID] [int] NOT NULL,
	[FolloweeID] [int] NOT NULL,
	[Status] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Follows] PRIMARY KEY CLUSTERED 
(
	[FollowID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ForumComments](
	[CommentID] [int] IDENTITY(1,1) NOT NULL,
	[ForumPostID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[Content] [nvarchar](500) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[IsDelete] [bit] NOT NULL,
 CONSTRAINT [PK_ForumComments] PRIMARY KEY CLUSTERED 
(
	[CommentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ForumImages](
	[ImageID] [int] IDENTITY(1,1) NOT NULL,
	[ForumPostID] [int] NOT NULL,
	[ImageUrl] [nvarchar](500) NOT NULL,
	[SortOrder] [int] NOT NULL,
 CONSTRAINT [PK_ForumImages] PRIMARY KEY CLUSTERED 
(
	[ImageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ForumInteracts](
	[ForumPostID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[ActionType] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_ForumInteracts] PRIMARY KEY CLUSTERED 
(
	[ForumPostID] ASC,
	[MemberID] ASC,
	[ActionType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ForumPosts](
	[ForumPostID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[CategoryID] [int] NOT NULL,
	[Title] [nvarchar](150) NOT NULL,
	[Content] [nvarchar](max) NOT NULL,
	[IsPinned] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[IsDelete] [bit] NOT NULL,
 CONSTRAINT [PK_ForumPosts] PRIMARY KEY CLUSTERED 
(
	[ForumPostID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[FriendRequests](
	[RequestID] [int] IDENTITY(1,1) NOT NULL,
	[RequesterID] [int] NOT NULL,
	[ReceiverID] [int] NOT NULL,
	[Message] [nvarchar](300) NULL,
	[RequestStatus] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ReviewedAt] [datetime] NULL,
 CONSTRAINT [PK_FriendRequests] PRIMARY KEY CLUSTERED 
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Friendships](
	[FriendshipID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID1] [int] NOT NULL,
	[MemberID2] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Friendships] PRIMARY KEY CLUSTERED 
(
	[FriendshipID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[GroupMembers](
	[GroupMemberID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[MemberRole] [tinyint] NOT NULL,
	[JoinedAt] [datetime] NOT NULL,
	[LeftAt] [datetime] NULL,
	[IsRemoved] [bit] NOT NULL,
	[RemovedByMemberID] [int] NULL,
	[RemovedAt] [datetime] NULL,
	[RemoveReason] [nvarchar](300) NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_GroupMembers] PRIMARY KEY CLUSTERED 
(
	[GroupMemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ItineraryNodes](
	[NodeID] [int] IDENTITY(1,1) NOT NULL,
	[PostID] [int] NOT NULL,
	[DayNumber] [int] NOT NULL,
	[LocationName] [nvarchar](100) NOT NULL,
	[ArrivalTime] [time](7) NULL,
	[StayTime] [int] NULL,
	[DepartureTime] [time](7) NULL,
	[MediaUrl] [nvarchar](500) NULL,
	[MediaType] [tinyint] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Remarks] [nvarchar](max) NULL,
 CONSTRAINT [PK_ItineraryNodes] PRIMARY KEY CLUSTERED 
(
	[NodeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[JoinRequests](
	[RequestID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[Message] [nvarchar](500) NULL,
	[RequestStatus] [tinyint] NOT NULL,
	[ReviewedByMemberID] [int] NULL,
	[ReviewedAt] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_JoinRequests] PRIMARY KEY CLUSTERED 
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[LoginHistories](
	[HistoryID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[LoginIP] [varchar](50) NOT NULL,
	[IsSuccess] [bit] NOT NULL,
	[UserAgent] [nvarchar](255) NULL,
	[AttemptedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_LoginHistories] PRIMARY KEY CLUSTERED 
(
	[HistoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_MemberClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberLogins](
	[LoginProvider] [nvarchar](128) NOT NULL,
	[ProviderKey] [nvarchar](128) NOT NULL,
	[ProviderDisplayName] [nvarchar](max) NULL,
	[UserId] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_MemberLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberRoleClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[RoleId] [int] NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_MemberRoleClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberRoles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](256) NULL,
	[NormalizedName] [nvarchar](256) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
 CONSTRAINT [PK_MemberRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Members](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](256) NULL,
	[NormalizedUserName] [nvarchar](256) NULL,
	[Email] [nvarchar](256) NULL,
	[NormalizedEmail] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[ConcurrencyStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEnd] [datetimeoffset](7) NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[LineId] [nvarchar](50) NULL,
	[InstagramUrl] [nvarchar](255) NULL,
	[FacebookUrl] [nvarchar](255) NULL,
	[ContactBookVisibility] [tinyint] NOT NULL,
	[IsPrivateAccount] [bit] NOT NULL,
	[AvatarUrl] [nvarchar](500) NULL,
	[BirthDate] [date] NULL,
	[Gender] [tinyint] NOT NULL,
	[Occupation] [nvarchar](50) NULL,
	[MBTI] [varchar](4) NULL,
	[Bio] [nvarchar](500) NULL,
	[Status] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[LastLoginAt] [datetime] NULL,
	[LastLoginIp] [varchar](50) NULL,
	[IsDelete] [bit] NOT NULL,
 CONSTRAINT [PK_Members] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberSkills](
	[MemberID] [int] NOT NULL,
	[SkillID] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_MemberSkills] PRIMARY KEY CLUSTERED 
(
	[MemberID] ASC,
	[SkillID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberSubscriptions](
	[SubscriptionID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[PlanID] [int] NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[Status] [int] NOT NULL,
 CONSTRAINT [PK_MemberSubscriptions] PRIMARY KEY CLUSTERED 
(
	[SubscriptionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberTokens](
	[UserId] [int] NOT NULL,
	[LoginProvider] [nvarchar](128) NOT NULL,
	[Name] [nvarchar](128) NOT NULL,
	[Value] [nvarchar](max) NULL,
	[ExpiresAt] [datetime] NOT NULL,
 CONSTRAINT [PK_MemberTokens] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[LoginProvider] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberUserRoles](
	[UserId] [int] NOT NULL,
	[RoleId] [int] NOT NULL,
 CONSTRAINT [PK_MemberUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Notifications](
	[NotificationID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[NotificationCategory] [tinyint] NOT NULL,
	[TargetType] [tinyint] NULL,
	[RelatedID] [int] NULL,
	[Content] [nvarchar](255) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED 
(
	[NotificationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Permissions](
	[PermissionID] [int] IDENTITY(1,1) NOT NULL,
	[PermissionCode] [varchar](100) NOT NULL,
	[ModuleName] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](200) NULL,
 CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED 
(
	[PermissionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PostInteractions](
	[PostID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[ActionType] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_PostInteractions] PRIMARY KEY CLUSTERED 
(
	[PostID] ASC,
	[MemberID] ASC,
	[ActionType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ReportReasonCategories](
	[CategoryID] [tinyint] NOT NULL,
	[CategoryName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ReportReasonCategories] PRIMARY KEY CLUSTERED 
(
	[CategoryID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Reports](
	[ReportID] [int] IDENTITY(1,1) NOT NULL,
	[ReporterID] [int] NOT NULL,
	[ReportedMemberID] [int] NULL,
	[ReportType] [tinyint] NOT NULL,
	[TargetID] [int] NULL,
	[Reason] [nvarchar](500) NOT NULL,
	[ReportStatus] [tinyint] NOT NULL,
	[AdminNotes] [nvarchar](500) NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ReasonCategory] [tinyint] NOT NULL,
	[IsMalicious] [bit] NOT NULL,
	[TargetTitle] [nvarchar](200) NULL,
	[Description] [nvarchar](500) NULL,
	[EvidenceUrl] [nvarchar](300) NULL,
	[TargetSnapshot] [nvarchar](500) NULL,
 CONSTRAINT [PK_Reports] PRIMARY KEY CLUSTERED 
(
	[ReportID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ReportStatuses](
	[StatusID] [tinyint] NOT NULL,
	[StatusName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ReportStatuses] PRIMARY KEY CLUSTERED 
(
	[StatusID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ReportTargetTypes](
	[TypeID] [tinyint] NOT NULL,
	[TypeName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ReportTargetTypes] PRIMARY KEY CLUSTERED 
(
	[TypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[RolePermissions](
	[RoleID] [int] NOT NULL,
	[PermissionID] [int] NOT NULL,
 CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED 
(
	[RoleID] ASC,
	[PermissionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Roles](
	[RoleID] [int] IDENTITY(1,1) NOT NULL,
	[RoleCode] [varchar](50) NOT NULL,
	[RoleName] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](200) NULL,
 CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED 
(
	[RoleID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[SubscriptionPlans](
	[PlanID] [int] IDENTITY(1,1) NOT NULL,
	[PlanName] [nvarchar](50) NOT NULL,
	[Price] [decimal](10, 2) NOT NULL,
	[DurationDays] [int] NOT NULL,
	[Description] [nvarchar](300) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY CLUSTERED 
(
	[PlanID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TravelGroupBudgets](
	[BudgetID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[BudgetCategory] [tinyint] NOT NULL,
	[BudgetName] [nvarchar](100) NOT NULL,
	[Amount] [decimal](10, 2) NULL,
	[CurrencyCode] [char](3) NOT NULL,
	[IsRequired] [bit] NOT NULL,
	[SortOrder] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_TravelGroupBudgets] PRIMARY KEY CLUSTERED 
(
	[BudgetID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TravelGroupImages](
	[ImageID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[ImageUrl] [nvarchar](600) NOT NULL,
	[ImageType] [tinyint] NOT NULL,
	[AltText] [nvarchar](150) NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsCover] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[UploadedByMemberID] [int] NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_TravelGroupImages] PRIMARY KEY CLUSTERED 
(
	[ImageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TravelGroupItineraryItems](
	[ItineraryItemID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[DayNumber] [int] NOT NULL,
	[StartTime] [time](7) NULL,
	[EndTime] [time](7) NULL,
	[Title] [nvarchar](150) NOT NULL,
	[LocationName] [nvarchar](150) NOT NULL,
	[Description] [nvarchar](1000) NOT NULL,
	[SortOrder] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_TravelGroupItineraryItems] PRIMARY KEY CLUSTERED 
(
	[ItineraryItemID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TravelGroups](
	[GroupID] [int] IDENTITY(1,1) NOT NULL,
	[OwnerMemberID] [int] NOT NULL,
	[GroupTitle] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](1000) NULL,
	[StartDate] [date] NULL,
	[EndDate] [date] NULL,
	[MinPeople] [int] NOT NULL,
	[MaxPeople] [int] NOT NULL,
	[CurrentPeople] [int] NOT NULL,
	[JoinRule] [tinyint] NOT NULL,
	[GroupStatus] [tinyint] NOT NULL,
	[IsPublic] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
	[IsDelete] [bit] NOT NULL,
	[ReviewStatus] [tinyint] NOT NULL,
	[Country] [nvarchar](50) NOT NULL,
	[Region] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_TravelGroups] PRIMARY KEY CLUSTERED 
(
	[GroupID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TravelGroupsLog](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[GroupID] [int] NOT NULL,
	[ChangeByMemberID] [int] NOT NULL,
	[FieldName] [nvarchar](50) NOT NULL,
	[OldValue] [nvarchar](300) NOT NULL,
	[NewValue] [nvarchar](300) NOT NULL,
	[ChangeType] [nvarchar](30) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Remark] [nvarchar](300) NULL,
 CONSTRAINT [PK_TravelGroupsLog] PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TravelSkills](
	[SkillID] [int] IDENTITY(1,1) NOT NULL,
	[SkillName] [nvarchar](50) NOT NULL,
	[SkillCategory] [tinyint] NOT NULL,
	[IconCode] [varchar](50) NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_TravelSkills] PRIMARY KEY CLUSTERED 
(
	[SkillID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[VlogPostImages](
	[ImageID] [int] IDENTITY(1,1) NOT NULL,
	[VlogPostID] [int] NOT NULL,
	[ImageUrl] [nvarchar](600) NOT NULL,
	[ImageType] [tinyint] NOT NULL,
	[AltText] [nvarchar](150) NOT NULL,
	[SortOrder] [int] NOT NULL,
	[IsCover] [bit] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[UploadedByMemberID] [int] NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_VlogPostImages] PRIMARY KEY CLUSTERED 
(
	[ImageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[VlogPosts](
	[PostID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[Title] [nvarchar](150) NOT NULL,
	[MediaUrl] [nvarchar](500) NULL,
	[MediaType] [tinyint] NOT NULL,
	[Content] [nvarchar](max) NULL,
	[Destination] [nvarchar](100) NOT NULL,
	[TravelDays] [int] NOT NULL,
	[Status] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[TravelDate] [datetime] NULL,
	[TravelPeople] [nvarchar](50) NOT NULL,
	[IsDelete] [bit] NOT NULL,
 CONSTRAINT [PK_VlogPosts] PRIMARY KEY CLUSTERED 
(
	[PostID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

/****** 插入初始資料 (Data Seed) ******/

SET IDENTITY_INSERT [dbo].[AdminAuditLogs] ON 
INSERT [dbo].[AdminAuditLogs] ([LogID], [EmployeeID], [Action], [TargetResource], [TargetID], [IPAddress], [CreatedAt], [Description]) VALUES (1, 1, N'member:account:block', N'Members', N'13|測試', N'127.0.0.1', CAST(N'2026-08-26T15:17:55.043' AS DateTime), NULL)
INSERT [dbo].[AdminAuditLogs] ([LogID], [EmployeeID], [Action], [TargetResource], [TargetID], [IPAddress], [CreatedAt], [Description]) VALUES (2, 1, N'member:pii:unmask', N'Members', N'1', N'::1', CAST(N'2026-08-26T15:19:56.633' AS DateTime), N'解除遮蔽並調閱會員完整個資')
SET IDENTITY_INSERT [dbo].[AdminAuditLogs] OFF
GO

INSERT [dbo].[EmployeeRoles] ([EmployeeID], [RoleID], [GrantedAt]) VALUES (1, 1, CAST(N'2026-08-26T15:13:35.880' AS DateTime))
GO

SET IDENTITY_INSERT [dbo].[Employees] ON 
INSERT [dbo].[Employees] ([EmployeeID], [EmployeeNo], [Email], [PasswordHash], [Name], [Department], [Status], [LastLoginAt], [CreatedAt], [IsDelete]) VALUES (1, N'ADMIN01', N'LazyTravel01@gmail.com', N'$2a$11$soAl3SCy0y6kTxijTdB.ceNw1No6fP80SqC2oIQgGgsrSJyiSXC7G', N'系統管理員', N'系統管理部', 1, CAST(N'2026-08-26T15:17:05.170' AS DateTime), CAST(N'2026-08-26T15:13:35.880' AS DateTime), 0)
SET IDENTITY_INSERT [dbo].[Employees] OFF
GO

SET IDENTITY_INSERT [dbo].[Members] ON 
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (1, NULL, NULL, N'demo-member-1@lazytravel.local', NULL, 0, NULL, NULL, N'30f9d54f-c5d7-4885-8b54-a91914930695', NULL, 0, 0, NULL, 0, 0, N'阿慢', NULL, NULL, NULL, 0, 0, NULL, NULL, 0, NULL, NULL, NULL, 0, CAST(N'2026-08-26T14:47:11.297' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (2, NULL, NULL, N'demo-member-2@lazytravel.local', NULL, 0, NULL, NULL, N'610f645d-3f96-4fe0-ba96-845ce190becf', NULL, 0, 0, NULL, 0, 0, N'小海', NULL, NULL, NULL, 0, 0, NULL, NULL, 0, NULL, NULL, NULL, 0, CAST(N'2026-08-26T14:47:11.443' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (3, NULL, NULL, N'demo-member-3@lazytravel.local', NULL, 0, NULL, NULL, N'07b4c65d-b05e-4408-b00d-ec601cca7d7e', NULL, 0, 0, NULL, 0, 0, N'阿凱', NULL, NULL, NULL, 0, 0, NULL, NULL, 0, NULL, NULL, NULL, 0, CAST(N'2026-08-26T14:47:11.443' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (4, NULL, NULL, N'official@lazytravel.local', NULL, 0, NULL, NULL, N'e0d07b0f-157b-4a7f-89c7-23a370b88738', NULL, 0, 0, NULL, 0, 0, N'LazyTravel 官方', NULL, NULL, NULL, 0, 0, NULL, NULL, 0, NULL, NULL, NULL, 0, CAST(N'2026-08-26T14:47:11.443' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (5, NULL, NULL, N'member01@gmail.com', NULL, 0, NULL, N'D39C5559-470A-4822-81A3-0911F2366531', N'6129C166-3975-4400-9F36-B5B15DAC4295', NULL, 0, 0, NULL, 0, 0, N'王小明', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1995-03-12' AS Date), 1, N'軟體工程師', N'INTJ', N'喜歡到處旅行拍照。', 1, CAST(N'2026-08-16T15:13:35.883' AS DateTime), CAST(N'2026-08-25T15:13:35.883' AS DateTime), N'127.0.0.1', 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (6, NULL, NULL, N'member02@gmail.com', NULL, 0, NULL, N'A14FAC0D-E5B2-40D0-AB75-B15E6A68E1F6', N'0477E42D-1173-43A7-B500-7A84B6D47CB0', NULL, 0, 0, NULL, 0, 0, N'陳雅婷', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1998-07-25' AS Date), 2, N'行銷企劃', N'ENFP', NULL, 1, CAST(N'2026-08-17T15:13:35.883' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (7, NULL, NULL, N'member03@gmail.com', NULL, 0, NULL, N'0AAB6D78-5A33-4578-A585-AD31F88391B0', N'5D057F5B-A4C6-4330-8801-1600EFEFB9D5', NULL, 0, 0, NULL, 0, 0, N'林俊傑', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1990-11-02' AS Date), 1, NULL, NULL, N'熱愛登山露營。', 1, CAST(N'2026-08-18T15:13:35.883' AS DateTime), CAST(N'2026-08-24T15:13:35.883' AS DateTime), N'127.0.0.1', 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (8, NULL, NULL, N'member04@gmail.com', NULL, 0, NULL, N'5BD059EA-4421-42A2-A62A-E903A13EAE36', N'6670DD37-F856-4ADA-8A3A-0A2F3759845E', NULL, 0, 0, NULL, 0, 0, N'黃詩涵', NULL, NULL, NULL, 0, 0, NULL, CAST(N'2000-01-18' AS Date), 2, N'學生', N'ISFJ', N'第一次揪團出國旅遊。', 1, CAST(N'2026-08-19T15:13:35.883' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (9, NULL, NULL, N'member05@gmail.com', NULL, 0, NULL, N'82062981-5B69-4FA1-B57F-79673A0483EE', N'33CC24A6-DE0B-46FF-B9BA-A4F9BF1AADAF', NULL, 0, 0, NULL, 0, 0, N'張家豪', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1993-05-30' AS Date), 1, N'業務', N'ESTP', NULL, 1, CAST(N'2026-08-20T15:13:35.883' AS DateTime), CAST(N'2026-08-23T15:13:35.883' AS DateTime), N'127.0.0.1', 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (10, NULL, NULL, N'member06@gmail.com', NULL, 0, NULL, N'DA5AC65C-1C23-4049-A70C-D4632A39D4A9', N'ED076D1B-E25F-42FC-AEF8-8A576C936E66', NULL, 0, 0, NULL, 0, 0, N'李佳穎', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1996-09-09' AS Date), 2, N'設計師', N'INFP', N'喜歡拍網美照。', 1, CAST(N'2026-08-21T15:13:35.883' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (11, NULL, NULL, N'member07@gmail.com', NULL, 0, NULL, N'47AF0889-8C33-4FA3-A112-397DEABF1634', N'FBD7E8C5-79B4-425D-A145-86B344BBDF54', NULL, 0, 0, NULL, 0, 0, N'吳承翰', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1992-02-14' AS Date), 1, NULL, NULL, NULL, 2, CAST(N'2026-08-22T15:13:35.883' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (12, NULL, NULL, N'member08@gmail.com', NULL, 0, NULL, N'FE0DFAF1-29E6-4CBF-A515-10B2689E61D9', N'8DE584A2-2ADB-4E49-ABA6-46941775489F', NULL, 0, 0, NULL, 0, 0, N'許雅筑', NULL, NULL, NULL, 0, 1, NULL, CAST(N'1999-12-05' AS Date), 2, N'研究生', N'INTP', N'私人帳號測試用。', 1, CAST(N'2026-08-23T15:13:35.883' AS DateTime), NULL, NULL, 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (13, NULL, NULL, N'member09@gmail.com', NULL, 0, NULL, N'CFAC80D9-A0E3-4F1D-9DD1-322A40D32775', N'86453016-3463-4297-B797-1F6FC5924DA9', NULL, 0, 0, NULL, 0, 0, N'趙柏翔', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1988-06-20' AS Date), 1, N'廚師', N'ESFJ', N'喜歡帶團吃美食。', 2, CAST(N'2026-08-24T15:13:35.883' AS DateTime), CAST(N'2026-08-25T15:13:35.883' AS DateTime), N'127.0.0.1', 0)
INSERT [dbo].[Members] ([Id], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount], [Name], [LineId], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [IsPrivateAccount], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [CreatedAt], [LastLoginAt], [LastLoginIp], [IsDelete]) VALUES (14, NULL, NULL, N'member10@gmail.com', NULL, 0, NULL, N'7E94D77D-1041-4C68-944B-E6EDF013D077', N'4E28D278-906E-431A-AC34-C63CE0CB9E4B', NULL, 0, 0, NULL, 0, 0, N'周映璇', NULL, NULL, NULL, 0, 0, NULL, CAST(N'1997-04-08' AS Date), 2, N'空服員', N'ENTJ', N'環遊世界中。', 1, CAST(N'2026-08-25T15:13:35.883' AS DateTime), NULL, NULL, 0)
SET IDENTITY_INSERT [dbo].[Members] OFF
GO

SET IDENTITY_INSERT [dbo].[Permissions] ON 
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (1, N'system:dashboard:read', N'總覽', N'檢視後台總覽儀表板')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (2, N'member:account:read', N'會員管理', N'檢視會員列表與詳細資料')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (3, N'member:account:block', N'會員管理', N'停權 / 解除停權會員帳號')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (4, N'member:pii:unmask', N'會員管理', N'解除遮蔽並調閱會員完整個資')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (5, N'content:report:read', N'檢舉管理', N'檢視檢舉列表')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (6, N'content:report:audit', N'檢舉管理', N'審核檢舉案件')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (7, N'content:vlog:read', N'文章管理', N'檢視 Vlog 文章列表')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (8, N'content:vlog:create', N'文章管理', N'新增 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (9, N'content:vlog:update', N'文章管理', N'編輯 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (10, N'content:vlog:delete', N'文章管理', N'刪除 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (11, N'content:vlog:restore', N'文章管理', N'還原已刪除的 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (12, N'content:vlog:submit', N'文章管理', N'送審 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (13, N'content:vlog:publish', N'文章管理', N'發布 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (14, N'content:vlog:return', N'文章管理', N'退回 Vlog 文章至草稿')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (15, N'content:vlog:audit', N'文章管理', N'審核 Vlog 文章')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (16, N'content:forum:read', N'論壇管理', N'檢視論壇貼文')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (17, N'social:travelgroup:read', N'揪團管理', N'檢視揪團列表與詳細資料')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (18, N'finance:plan:read', N'財務管理', N'檢視訂閱方案')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (19, N'finance:plan:manage', N'財務管理', N'管理訂閱方案')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (20, N'finance:split:read', N'財務管理', N'檢視分帳紀錄')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (21, N'system:analytics:read', N'系統管理', N'檢視數據分析')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (22, N'system:notification:read', N'系統管理', N'檢視系統通知')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (23, N'system:employee:read', N'員工管理', N'檢視員工列表')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (24, N'system:employee:manage', N'員工管理', N'管理員工帳號（含最高權限）')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (25, N'system:role:manage', N'角色管理', N'管理角色與權限')
INSERT [dbo].[Permissions] ([PermissionID], [PermissionCode], [ModuleName], [Description]) VALUES (26, N'ROLE_SUPER_ADMIN', N'系統管理', N'超級管理員萬用權限（VlogPostPermissions 用來繞過文章模組個別權限檢查）')
SET IDENTITY_INSERT [dbo].[Permissions] OFF
GO

INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 1)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 2)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 3)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 4)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 5)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 6)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 7)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 8)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 9)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 10)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 11)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 12)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 13)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 14)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 15)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 16)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 17)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 18)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 19)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 20)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 21)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 22)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 23)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 24)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 25)
INSERT [dbo].[RolePermissions] ([RoleID], [PermissionID]) VALUES (1, 26)
GO

SET IDENTITY_INSERT [dbo].[Roles] ON 
INSERT [dbo].[Roles] ([RoleID], [RoleCode], [RoleName], [Description]) VALUES (1, N'SUPER_ADMIN', N'系統管理員', N'擁有系統全部功能權限的最高管理角色（開發/測試用）')
SET IDENTITY_INSERT [dbo].[Roles] OFF
GO

SET IDENTITY_INSERT [dbo].[VlogPosts] ON 
INSERT [dbo].[VlogPosts] ([PostID], [MemberID], [Title], [MediaUrl], [MediaType], [Content], [Destination], [TravelDays], [Status], [CreatedAt], [UpdatedAt], [TravelDate], [TravelPeople], [IsDelete]) VALUES (1, 1, N'花蓮三天兩夜，慢慢晃海岸線', N'https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=600&q=80', 0, N'沿著台11線一路往南，行程排得很鬆，只想好好曬太陽看海。', N'花蓮', 3, 2, CAST(N'2026-03-02T21:19:00.000' AS DateTime), NULL, CAST(N'2026-05-02T00:00:00.000' AS DateTime), N'Small', 0)
SET IDENTITY_INSERT [dbo].[VlogPosts] OFF
GO


/****** 建立索引與預設值設定 ******/

SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Employees_Email] ON [dbo].[Employees]
(
	[Email] ASC
)
WHERE ([IsDelete]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Follows] ADD  CONSTRAINT [UQ_Follows_Follower_Followee] UNIQUE NONCLUSTERED 
(
	[FollowerID] ASC,
	[FolloweeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_ForumPosts_IsDelete] ON [dbo].[ForumPosts]
(
	[IsDelete] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Friendships] ADD  CONSTRAINT [UQ_Friendships_Pair] UNIQUE NONCLUSTERED 
(
	[MemberID1] ASC,
	[MemberID2] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_GroupMembers_MemberID] ON [dbo].[GroupMembers]
(
	[MemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_LoginHistories_MemberID] ON [dbo].[LoginHistories]
(
	[MemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_MemberRoles_NormalizedName] ON [dbo].[MemberRoles]
(
	[NormalizedName] ASC
)
WHERE ([NormalizedName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Members_NormalizedEmail] ON [dbo].[Members]
(
	[NormalizedEmail] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Members_NormalizedUserName] ON [dbo].[Members]
(
	[NormalizedUserName] ASC
)
WHERE ([NormalizedUserName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Members_Status] ON [dbo].[Members]
(
	[Status] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Notifications_Member_Read_Time] ON [dbo].[Notifications]
(
	[MemberID] ASC,
	[IsRead] ASC,
	[CreatedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Reports_Status] ON [dbo].[Reports]
(
	[ReportStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_TravelGroups_IsDelete] ON [dbo].[TravelGroups]
(
	[IsDelete] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_TravelGroups_StartDate] ON [dbo].[TravelGroups]
(
	[StartDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_TravelGroups_Status] ON [dbo].[TravelGroups]
(
	[GroupStatus] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_VlogPosts_IsDelete] ON [dbo].[VlogPosts]
(
	[IsDelete] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


/****** 設定欄位預設值 ******/
ALTER TABLE [dbo].[AdminAuditLogs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Blocks] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Categories] ADD  DEFAULT ((1)) FOR [ModuleType]
GO
ALTER TABLE [dbo].[Categories] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Categories] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[EmployeeRoles] ADD  DEFAULT (getdate()) FOR [GrantedAt]
GO
ALTER TABLE [dbo].[Employees] ADD  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Employees] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Employees] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[Expenses] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Expenses] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[ExpenseSplits] ADD  DEFAULT ((0)) FOR [IsPaid]
GO
ALTER TABLE [dbo].[Follows] ADD  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Follows] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Follows] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[ForumComments] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ForumComments] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[ForumImages] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[ForumInteracts] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ForumPosts] ADD  DEFAULT ((0)) FOR [IsPinned]
GO
ALTER TABLE [dbo].[ForumPosts] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ForumPosts] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[FriendRequests] ADD  DEFAULT ((0)) FOR [RequestStatus]
GO
ALTER TABLE [dbo].[FriendRequests] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Friendships] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[GroupMembers] ADD  DEFAULT ((0)) FOR [MemberRole]
GO
ALTER TABLE [dbo].[GroupMembers] ADD  DEFAULT (getdate()) FOR [JoinedAt]
GO
ALTER TABLE [dbo].[GroupMembers] ADD  DEFAULT ((0)) FOR [IsRemoved]
GO
ALTER TABLE [dbo].[GroupMembers] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[ItineraryNodes] ADD  DEFAULT ((0)) FOR [MediaType]
GO
ALTER TABLE [dbo].[JoinRequests] ADD  DEFAULT ((0)) FOR [RequestStatus]
GO
ALTER TABLE [dbo].[JoinRequests] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[LoginHistories] ADD  DEFAULT (getdate()) FOR [AttemptedAt]
GO
ALTER TABLE [dbo].[MemberLogins] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [EmailConfirmed]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [PhoneNumberConfirmed]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [TwoFactorEnabled]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((1)) FOR [LockoutEnabled]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [AccessFailedCount]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [ContactBookVisibility]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [IsPrivateAccount]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [Gender]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[MemberSkills] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[MemberSubscriptions] ADD  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[MemberTokens] ADD  DEFAULT (dateadd(hour,(24),getdate())) FOR [ExpiresAt]
GO
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT ((0)) FOR [IsRead]
GO
ALTER TABLE [dbo].[Notifications] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[PostInteractions] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Reports] ADD  DEFAULT ((0)) FOR [ReportStatus]
GO
ALTER TABLE [dbo].[Reports] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Reports] ADD  DEFAULT ((5)) FOR [ReasonCategory]
GO
ALTER TABLE [dbo].[Reports] ADD  DEFAULT ((0)) FOR [IsMalicious]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  DEFAULT ((0)) FOR [Price]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  DEFAULT ((0)) FOR [DurationDays]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[TravelGroupBudgets] ADD  DEFAULT ('TWD') FOR [CurrencyCode]
GO
ALTER TABLE [dbo].[TravelGroupBudgets] ADD  DEFAULT ((1)) FOR [IsRequired]
GO
ALTER TABLE [dbo].[TravelGroupBudgets] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[TravelGroupBudgets] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[TravelGroupBudgets] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[TravelGroupImages] ADD  DEFAULT ((0)) FOR [ImageType]
GO
ALTER TABLE [dbo].[TravelGroupImages] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[TravelGroupImages] ADD  DEFAULT ((0)) FOR [IsCover]
GO
ALTER TABLE [dbo].[TravelGroupImages] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[TravelGroupImages] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[TravelGroupImages] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[TravelGroupItineraryItems] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[TravelGroupItineraryItems] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[TravelGroupItineraryItems] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((2)) FOR [MinPeople]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((10)) FOR [MaxPeople]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((1)) FOR [CurrentPeople]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((1)) FOR [JoinRule]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((0)) FOR [GroupStatus]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((1)) FOR [IsPublic]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[TravelGroups] ADD  DEFAULT ((0)) FOR [ReviewStatus]
GO
ALTER TABLE [dbo].[TravelGroupsLog] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[TravelSkills] ADD  DEFAULT ((1)) FOR [SkillCategory]
GO
ALTER TABLE [dbo].[TravelSkills] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[VlogPostImages] ADD  DEFAULT ((0)) FOR [ImageType]
GO
ALTER TABLE [dbo].[VlogPostImages] ADD  DEFAULT ((0)) FOR [SortOrder]
GO
ALTER TABLE [dbo].[VlogPostImages] ADD  DEFAULT ((0)) FOR [IsCover]
GO
ALTER TABLE [dbo].[VlogPostImages] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[VlogPostImages] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[VlogPostImages] ADD  DEFAULT (getdate()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[VlogPosts] ADD  DEFAULT ((0)) FOR [MediaType]
GO
ALTER TABLE [dbo].[VlogPosts] ADD  DEFAULT ((1)) FOR [TravelDays]
GO
ALTER TABLE [dbo].[VlogPosts] ADD  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[VlogPosts] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[VlogPosts] ADD  DEFAULT ((0)) FOR [IsDelete]
GO

/****** 設定 Foreign Key (外來鍵) 與約束條件 ******/

ALTER TABLE [dbo].[AdminAuditLogs]  WITH CHECK ADD  CONSTRAINT [FK_AdminAuditLogs_Employee] FOREIGN KEY([EmployeeID]) REFERENCES [dbo].[Employees] ([EmployeeID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Blocks]  WITH CHECK ADD  CONSTRAINT [FK_Blocks_Blocked] FOREIGN KEY([BlockedID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Blocks]  WITH CHECK ADD  CONSTRAINT [FK_Blocks_Blocker] FOREIGN KEY([BlockerID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[EmployeeRoles]  WITH CHECK ADD  CONSTRAINT [FK_EmployeeRoles_Employee] FOREIGN KEY([EmployeeID]) REFERENCES [dbo].[Employees] ([EmployeeID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EmployeeRoles]  WITH CHECK ADD  CONSTRAINT [FK_EmployeeRoles_Role] FOREIGN KEY([RoleID]) REFERENCES [dbo].[Roles] ([RoleID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [FK_Expenses_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD  CONSTRAINT [FK_Expenses_Payer] FOREIGN KEY([PayerID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[ExpenseSplits]  WITH CHECK ADD  CONSTRAINT [FK_ExpenseSplits_Expense] FOREIGN KEY([ExpenseID]) REFERENCES [dbo].[Expenses] ([ExpenseID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ExpenseSplits]  WITH CHECK ADD  CONSTRAINT [FK_ExpenseSplits_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Follows]  WITH CHECK ADD  CONSTRAINT [FK_Follows_Followee] FOREIGN KEY([FolloweeID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Follows]  WITH CHECK ADD  CONSTRAINT [FK_Follows_Follower] FOREIGN KEY([FollowerID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[ForumComments]  WITH CHECK ADD  CONSTRAINT [FK_ForumComments_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[ForumComments]  WITH CHECK ADD  CONSTRAINT [FK_ForumComments_Post] FOREIGN KEY([ForumPostID]) REFERENCES [dbo].[ForumPosts] ([ForumPostID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ForumImages]  WITH CHECK ADD  CONSTRAINT [FK_ForumImages_Post] FOREIGN KEY([ForumPostID]) REFERENCES [dbo].[ForumPosts] ([ForumPostID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ForumInteracts]  WITH CHECK ADD  CONSTRAINT [FK_ForumInteracts_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[ForumInteracts]  WITH CHECK ADD  CONSTRAINT [FK_ForumInteracts_Post] FOREIGN KEY([ForumPostID]) REFERENCES [dbo].[ForumPosts] ([ForumPostID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ForumPosts]  WITH CHECK ADD  CONSTRAINT [FK_ForumPosts_Category] FOREIGN KEY([CategoryID]) REFERENCES [dbo].[Categories] ([CategoryID])
GO
ALTER TABLE [dbo].[ForumPosts]  WITH CHECK ADD  CONSTRAINT [FK_ForumPosts_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[FriendRequests]  WITH CHECK ADD  CONSTRAINT [FK_FriendReq_Receiver] FOREIGN KEY([ReceiverID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[FriendRequests]  WITH CHECK ADD  CONSTRAINT [FK_FriendReq_Requester] FOREIGN KEY([RequesterID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Friendships]  WITH CHECK ADD  CONSTRAINT [FK_Friendships_M1] FOREIGN KEY([MemberID1]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Friendships]  WITH CHECK ADD  CONSTRAINT [FK_Friendships_M2] FOREIGN KEY([MemberID2]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[GroupMembers]  WITH CHECK ADD  CONSTRAINT [FK_GroupMembers_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[GroupMembers]  WITH CHECK ADD  CONSTRAINT [FK_GroupMembers_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[GroupMembers]  WITH CHECK ADD  CONSTRAINT [FK_GroupMembers_RemovedBy] FOREIGN KEY([RemovedByMemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[ItineraryNodes]  WITH CHECK ADD  CONSTRAINT [FK_ItineraryNodes_Post] FOREIGN KEY([PostID]) REFERENCES [dbo].[VlogPosts] ([PostID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JoinRequests]  WITH CHECK ADD  CONSTRAINT [FK_JoinRequests_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[JoinRequests]  WITH CHECK ADD  CONSTRAINT [FK_JoinRequests_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[JoinRequests]  WITH CHECK ADD  CONSTRAINT [FK_JoinRequests_Reviewer] FOREIGN KEY([ReviewedByMemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[LoginHistories]  WITH CHECK ADD  CONSTRAINT [FK_LoginHistories_Members] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberClaims]  WITH CHECK ADD  CONSTRAINT [FK_MemberClaims_Members] FOREIGN KEY([UserId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberLogins]  WITH CHECK ADD  CONSTRAINT [FK_MemberLogins_Members] FOREIGN KEY([UserId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberRoleClaims]  WITH CHECK ADD  CONSTRAINT [FK_MemberRoleClaims_MemberRoles] FOREIGN KEY([RoleId]) REFERENCES [dbo].[MemberRoles] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberSkills]  WITH CHECK ADD  CONSTRAINT [FK_MemberSkills_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberSkills]  WITH CHECK ADD  CONSTRAINT [FK_MemberSkills_Skill] FOREIGN KEY([SkillID]) REFERENCES [dbo].[TravelSkills] ([SkillID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberSubscriptions]  WITH CHECK ADD  CONSTRAINT [FK_MemberSubscriptions_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberSubscriptions]  WITH CHECK ADD  CONSTRAINT [FK_MemberSubscriptions_Plan] FOREIGN KEY([PlanID]) REFERENCES [dbo].[SubscriptionPlans] ([PlanID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberTokens]  WITH CHECK ADD  CONSTRAINT [FK_MemberTokens_Members] FOREIGN KEY([UserId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_MemberUserRoles_MemberRoles] FOREIGN KEY([RoleId]) REFERENCES [dbo].[MemberRoles] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MemberUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_MemberUserRoles_Members] FOREIGN KEY([UserId]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_Members] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PostInteractions]  WITH CHECK ADD  CONSTRAINT [FK_PostInteractions_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[PostInteractions]  WITH CHECK ADD  CONSTRAINT [FK_PostInteractions_Post] FOREIGN KEY([PostID]) REFERENCES [dbo].[VlogPosts] ([PostID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Category] FOREIGN KEY([ReasonCategory]) REFERENCES [dbo].[ReportReasonCategories] ([CategoryID])
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_ReportedMember] FOREIGN KEY([ReportedMemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Reporter] FOREIGN KEY([ReporterID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Status] FOREIGN KEY([ReportStatus]) REFERENCES [dbo].[ReportStatuses] ([StatusID])
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD  CONSTRAINT [FK_Reports_Type] FOREIGN KEY([ReportType]) REFERENCES [dbo].[ReportTargetTypes] ([TypeID])
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Permission] FOREIGN KEY([PermissionID]) REFERENCES [dbo].[Permissions] ([PermissionID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[RolePermissions]  WITH CHECK ADD  CONSTRAINT [FK_RolePermissions_Role] FOREIGN KEY([RoleID]) REFERENCES [dbo].[Roles] ([RoleID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TravelGroupBudgets]  WITH CHECK ADD  CONSTRAINT [FK_TravelGroupBudgets_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TravelGroupImages]  WITH CHECK ADD  CONSTRAINT [FK_GroupImages_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TravelGroupImages]  WITH CHECK ADD  CONSTRAINT [FK_GroupImages_Member] FOREIGN KEY([UploadedByMemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[TravelGroupItineraryItems]  WITH CHECK ADD  CONSTRAINT [FK_ItineraryItems_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TravelGroups]  WITH CHECK ADD  CONSTRAINT [FK_TravelGroups_Owner] FOREIGN KEY([OwnerMemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[TravelGroupsLog]  WITH CHECK ADD  CONSTRAINT [FK_TravelGroupsLog_Employee] FOREIGN KEY([ChangeByMemberID]) REFERENCES [dbo].[Employees] ([EmployeeID])
GO
ALTER TABLE [dbo].[TravelGroupsLog]  WITH CHECK ADD  CONSTRAINT [FK_TravelGroupsLog_Group] FOREIGN KEY([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[VlogPostImages]  WITH CHECK ADD  CONSTRAINT [FK_VlogPostImages_Member] FOREIGN KEY([UploadedByMemberID]) REFERENCES [dbo].[Members] ([Id])
GO
ALTER TABLE [dbo].[VlogPostImages]  WITH CHECK ADD  CONSTRAINT [FK_VlogPostImages_Post] FOREIGN KEY([VlogPostID]) REFERENCES [dbo].[VlogPosts] ([PostID]) ON DELETE CASCADE
GO
ALTER TABLE [dbo].[VlogPosts]  WITH CHECK ADD  CONSTRAINT [FK_VlogPosts_Member] FOREIGN KEY([MemberID]) REFERENCES [dbo].[Members] ([Id])
GO

ALTER TABLE [dbo].[Follows]  WITH CHECK ADD  CONSTRAINT [CHK_Follow_NotSelf] CHECK  (([FollowerID]<>[FolloweeID]))
GO
ALTER TABLE [dbo].[Friendships]  WITH CHECK ADD  CONSTRAINT [CHK_Friendship_Order] CHECK  (([MemberID1]<[MemberID2]))
GO
ALTER TABLE [dbo].[TravelGroups]  WITH CHECK ADD  CONSTRAINT [CHK_TravelGroups_People] CHECK  (([CurrentPeople]<=[MaxPeople]))
GO

PRINT N'LazyTravelDB 建置完成：共 45 張資料表（含 Identity 會員骨架 + 企業 RBAC + 揪團預算/圖片/行程項目），已灌入示範資料。';
GO
