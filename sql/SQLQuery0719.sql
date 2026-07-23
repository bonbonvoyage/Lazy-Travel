USE [master]
GO
/****** 物件:  Database [LazyTravelDB]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
CREATE DATABASE [LazyTravelDB]
GO
ALTER DATABASE [LazyTravelDB] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [LazyTravelDB].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [LazyTravelDB] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [LazyTravelDB] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [LazyTravelDB] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [LazyTravelDB] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [LazyTravelDB] SET ARITHABORT OFF 
GO
ALTER DATABASE [LazyTravelDB] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [LazyTravelDB] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [LazyTravelDB] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [LazyTravelDB] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [LazyTravelDB] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [LazyTravelDB] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [LazyTravelDB] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [LazyTravelDB] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [LazyTravelDB] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [LazyTravelDB] SET  ENABLE_BROKER 
GO
ALTER DATABASE [LazyTravelDB] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [LazyTravelDB] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [LazyTravelDB] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [LazyTravelDB] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [LazyTravelDB] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [LazyTravelDB] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [LazyTravelDB] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [LazyTravelDB] SET RECOVERY FULL 
GO
ALTER DATABASE [LazyTravelDB] SET  MULTI_USER 
GO
ALTER DATABASE [LazyTravelDB] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [LazyTravelDB] SET DB_CHAINING OFF 
GO
ALTER DATABASE [LazyTravelDB] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [LazyTravelDB] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [LazyTravelDB] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [LazyTravelDB] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [LazyTravelDB] SET QUERY_STORE = ON
GO
ALTER DATABASE [LazyTravelDB] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [LazyTravelDB]
GO
/****** 物件:  Table [dbo].[AdminLogs]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AdminLogs](
	[LogID] [int] IDENTITY(1,1) NOT NULL,
	[AdminID] [int] NOT NULL,
	[Action] [nvarchar](100) NOT NULL,
	[TargetTable] [nvarchar](50) NOT NULL,
	[TargetID] [int] NULL,
	[Description] [nvarchar](300) NULL,
	[IPAddress] [varchar](50) NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LogID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[AdminPermissions]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AdminPermissions](
	[AdminID] [int] NOT NULL,
	[PermissionCode] [varchar](50) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
 CONSTRAINT [PK_AdminPermissions] PRIMARY KEY CLUSTERED 
(
	[AdminID] ASC,
	[PermissionCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Blocks]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Blocks](
	[BlockerID] [int] NOT NULL,
	[BlockedID] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[BlockerID] ASC,
	[BlockedID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Expenses]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
PRIMARY KEY CLUSTERED 
(
	[ExpenseID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ExpenseSplits]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExpenseSplits](
	[SplitID] [int] IDENTITY(1,1) NOT NULL,
	[ExpenseID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[OweAmount] [decimal](18, 2) NOT NULL,
	[IsPaid] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SplitID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ExternalLogins]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ExternalLogins](
	[LoginID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[LoginProvider] [nvarchar](50) NOT NULL,
	[ProviderKey] [nvarchar](255) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LoginID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Follows]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Follows](
	[FollowID] [int] IDENTITY(1,1) NOT NULL,
	[FollowerID] [int] NOT NULL,
	[FolloweeID] [int] NOT NULL,
	[Status] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FollowID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ForumComments]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ForumComments](
	[CommentID] [int] IDENTITY(1,1) NOT NULL,
	[ForumPostID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[Content] [nvarchar](500) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[IsDelete] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CommentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ForumImages]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ForumImages](
	[ImageID] [int] IDENTITY(1,1) NOT NULL,
	[ForumPostID] [int] NOT NULL,
	[ImageUrl] [nvarchar](500) NOT NULL,
	[SortOrder] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ImageID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ForumInteracts]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ForumInteracts](
	[ForumPostID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[ActionType] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ForumPostID] ASC,
	[MemberID] ASC,
	[ActionType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ForumPosts]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ForumPosts](
	[ForumPostID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[Category] [int] NOT NULL,
	[Title] [nvarchar](150) NOT NULL,
	[Content] [nvarchar](max) NOT NULL,
	[IsPinned] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
	[IsDelete] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[ForumPostID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[FriendRequests]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FriendRequests](
	[RequestID] [int] IDENTITY(1,1) NOT NULL,
	[RequesterID] [int] NOT NULL,
	[ReceiverID] [int] NOT NULL,
	[Message] [nvarchar](300) NULL,
	[RequestStatus] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ReviewedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Friendships]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Friendships](
	[FriendshipID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID1] [int] NOT NULL,
	[MemberID2] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FriendshipID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[GroupMembers]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
PRIMARY KEY CLUSTERED 
(
	[GroupMemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[ItineraryNodes]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItineraryNodes](
	[NodeID] [int] IDENTITY(1,1) NOT NULL,
	[PostID] [int] NOT NULL,
	[DayNumber] [int] NOT NULL,
	[LocationName] [nvarchar](100) NOT NULL,
	[ArrivalTime] [time](7) NULL,
	[StayTime] [nvarchar](50) NULL,
	[DepartureTime] [time](7) NULL,
	[MediaUrl] [nvarchar](500) NULL,
	[MediaType] [tinyint] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Remarks] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[NodeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[JoinRequests]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
PRIMARY KEY CLUSTERED 
(
	[RequestID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[LoginHistories]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Members]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Members](
	[MemberID] [int] IDENTITY(1,1) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[PasswordHash] [nvarchar](255) NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Phone] [varchar](20) NULL,
	[LineId] [nvarchar](50) NULL,
	[IsPrivateAccount] [bit] NOT NULL,
	[IsEmailConfirmed] [bit] NOT NULL,
	[AvatarUrl] [nvarchar](max) NULL,
	[BirthDate] [date] NULL,
	[Gender] [tinyint] NOT NULL,
	[Occupation] [nvarchar](50) NULL,
	[MBTI] [varchar](4) NULL,
	[Bio] [nvarchar](500) NULL,
	[Status] [tinyint] NOT NULL,
	[Role] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[InstagramUrl] [nvarchar](255) NULL,
	[FacebookUrl] [nvarchar](255) NULL,
	[ContactBookVisibility] [tinyint] NOT NULL,
	[LastLoginAt] [datetime] NULL,
	[LastLoginIp] [varchar](50) NULL,
	[FailedLoginCount] [int] NOT NULL,
	[LockoutEndDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[MemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[MemberSubscriptions]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MemberSubscriptions](
	[SubscriptionID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[PlanID] [int] NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[Status] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[SubscriptionID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[MemberTokens]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MemberTokens](
	[TokenID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[LoginProvider] [nvarchar](50) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
	[ExpiryTime] [datetime] NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[TokenID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Notifications]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Notifications](
	[NotificationID] [int] IDENTITY(1,1) NOT NULL,
	[MemberID] [int] NOT NULL,
	[Type] [tinyint] NOT NULL,
	[RelatedID] [int] NULL,
	[Content] [nvarchar](255) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[NotificationID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[PostInteractions]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PostInteractions](
	[PostID] [int] NOT NULL,
	[MemberID] [int] NOT NULL,
	[ActionType] [tinyint] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PostID] ASC,
	[MemberID] ASC,
	[ActionType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[Reports]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
PRIMARY KEY CLUSTERED 
(
	[ReportID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[SubscriptionPlans]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SubscriptionPlans](
	[PlanID] [int] IDENTITY(1,1) NOT NULL,
	[PlanName] [nvarchar](50) NOT NULL,
	[Price] [decimal](10, 2) NOT NULL,
	[DurationDays] [int] NOT NULL,
	[Description] [nvarchar](300) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PlanID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[TravelGroups]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
PRIMARY KEY CLUSTERED 
(
	[GroupID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** 物件:  Table [dbo].[VlogPosts]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
	[IsDelete] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[PostID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_Audit', CAST(N'2026-07-18T15:14:32.710' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_Content', CAST(N'2026-07-18T15:14:32.710' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_Data', CAST(N'2026-07-18T15:14:32.710' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_Finance', CAST(N'2026-07-18T15:14:32.710' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_Member', CAST(N'2026-07-18T15:14:32.710' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_Security', CAST(N'2026-07-19T20:23:32.030' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (1, N'Mod_System', CAST(N'2026-07-18T15:14:32.710' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (2, N'Mod_Audit', CAST(N'2026-07-18T15:14:32.717' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (2, N'Mod_Content', CAST(N'2026-07-18T15:14:32.717' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (3, N'Mod_Data', CAST(N'2026-07-18T15:14:32.720' AS DateTime))
GO
INSERT [dbo].[AdminPermissions] ([AdminID], [PermissionCode], [CreatedAt]) VALUES (3, N'Mod_Finance', CAST(N'2026-07-18T15:14:32.720' AS DateTime))
GO
SET IDENTITY_INSERT [dbo].[Members] ON 
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (1, N'user1@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李怡君', N'0994218842', NULL, 0, 1, NULL, CAST(N'1998-05-10' AS Date), 2, N'自由工作者', N'ENTJ', NULL, 1, 1, CAST(N'2026-01-29T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (2, N'user2@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李冠宇', N'0928715597', NULL, 0, 1, NULL, CAST(N'1993-08-15' AS Date), 1, N'教師', N'ISTJ', NULL, 1, 1, CAST(N'2025-12-22T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (3, N'user3@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡詩涵', N'0910617710', NULL, 0, 1, NULL, CAST(N'1988-09-23' AS Date), 1, N'學生', N'ENFP', NULL, 1, 1, CAST(N'2026-06-16T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (4, N'user4@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張俊傑', N'0955524262', NULL, 0, 1, NULL, CAST(N'1991-11-19' AS Date), 2, N'設計師', N'ENFP', NULL, 1, 0, CAST(N'2026-01-10T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (5, N'user5@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林柏翰', N'0920924964', NULL, 0, 1, NULL, CAST(N'2003-11-18' AS Date), 1, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2026-02-19T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (6, N'user6@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳家豪', N'0914670265', NULL, 0, 1, NULL, CAST(N'1987-04-09' AS Date), 1, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2025-10-21T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (7, N'user7@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李佩蓉', N'0977802703', NULL, 0, 1, NULL, CAST(N'1996-09-19' AS Date), 2, N'護理師', N'ISTJ', NULL, 1, 0, CAST(N'2025-10-07T13:51:50.873' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (8, N'user8@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李雅婷', N'0943831747', NULL, 0, 1, NULL, CAST(N'1979-01-06' AS Date), 1, N'軟體工程師', N'INTJ', NULL, 1, 0, CAST(N'2026-07-03T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (9, N'user9@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊欣儀', N'0960493509', NULL, 0, 1, NULL, CAST(N'2005-12-09' AS Date), 1, N'護理師', N'ISTJ', NULL, 1, 0, CAST(N'2025-12-26T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (10, N'user10@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李冠宇', N'0937231289', NULL, 0, 1, NULL, CAST(N'1977-02-20' AS Date), 1, N'業務經理', N'ENFP', NULL, 1, 0, CAST(N'2026-06-29T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (11, N'user11@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉冠宇', N'0951175147', NULL, 0, 1, NULL, CAST(N'1992-05-06' AS Date), 2, N'軟體工程師', N'INFP', NULL, 1, 0, CAST(N'2026-05-23T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (12, N'user12@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林欣儀', N'0970609168', NULL, 0, 1, NULL, CAST(N'1980-03-21' AS Date), 1, N'業務經理', N'ESTP', NULL, 1, 0, CAST(N'2026-07-14T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (13, N'user13@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊冠宇', N'0969748853', NULL, 0, 1, NULL, CAST(N'2001-08-12' AS Date), 1, N'業務經理', N'ISFJ', NULL, 1, 0, CAST(N'2026-02-07T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (14, N'user14@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊冠宇', N'0947663404', NULL, 0, 1, NULL, CAST(N'2005-07-16' AS Date), 2, N'行銷企劃', N'ESFJ', NULL, 1, 0, CAST(N'2025-11-17T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (15, N'user15@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡柏翰', N'0972815344', NULL, 0, 1, NULL, CAST(N'1994-06-16' AS Date), 1, N'自由工作者', N'ESTP', NULL, 1, 0, CAST(N'2025-10-01T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (16, N'user16@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃怡君', N'0980396745', NULL, 0, 1, NULL, CAST(N'1991-09-25' AS Date), 1, N'行銷企劃', N'ENFP', NULL, 1, 0, CAST(N'2026-05-31T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (17, N'user17@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳柏翰', N'0978489875', NULL, 0, 1, NULL, CAST(N'2001-02-15' AS Date), 1, N'教師', N'INTJ', NULL, 1, 0, CAST(N'2025-09-26T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (18, N'user18@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張柏翰', N'0917129537', NULL, 0, 1, NULL, CAST(N'2002-07-16' AS Date), 2, N'護理師', N'ESTP', NULL, 1, 0, CAST(N'2026-06-06T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (19, N'user19@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡家豪', N'0948434168', NULL, 0, 1, NULL, CAST(N'1981-06-02' AS Date), 2, N'設計師', N'ESFJ', NULL, 1, 0, CAST(N'2025-09-12T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (20, N'user20@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳佩蓉', N'0982498695', NULL, 0, 1, NULL, CAST(N'1984-01-19' AS Date), 1, N'行銷企劃', N'ISFJ', NULL, 2, 0, CAST(N'2025-11-15T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (21, N'user21@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉宇翔', N'0952354839', NULL, 0, 1, NULL, CAST(N'1996-12-28' AS Date), 1, N'自由工作者', N'ISFJ', NULL, 1, 0, CAST(N'2025-08-22T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (22, N'user22@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳宇翔', N'0991569751', NULL, 0, 1, NULL, CAST(N'2004-11-14' AS Date), 1, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2026-04-06T13:51:50.877' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (23, N'user23@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李宇翔', N'0914636273', NULL, 0, 1, NULL, CAST(N'1995-12-13' AS Date), 1, N'業務經理', N'ENFP', NULL, 1, 0, CAST(N'2025-07-28T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (24, N'user24@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林柏翰', N'0925437451', NULL, 0, 1, NULL, CAST(N'1987-04-11' AS Date), 2, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2026-01-26T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (25, N'user25@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張雅婷', N'0992086897', NULL, 0, 1, NULL, CAST(N'1994-03-11' AS Date), 1, N'業務經理', N'ISTJ', NULL, 1, 0, CAST(N'2026-07-08T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (26, N'user26@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉柏翰', N'0974247695', NULL, 0, 1, NULL, CAST(N'2005-02-18' AS Date), 2, N'教師', N'INTJ', NULL, 1, 0, CAST(N'2026-02-18T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (27, N'user27@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡怡君', N'0926403199', NULL, 0, 1, NULL, CAST(N'1993-08-12' AS Date), 1, N'學生', N'ISTJ', NULL, 2, 0, CAST(N'2025-10-03T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (28, N'user28@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉宇翔', N'0997892733', NULL, 0, 1, NULL, CAST(N'1989-07-06' AS Date), 2, N'護理師', N'INTJ', NULL, 1, 0, CAST(N'2026-05-04T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (29, N'user29@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃詩涵', N'0965509445', NULL, 0, 1, NULL, CAST(N'1982-09-06' AS Date), 2, N'業務經理', N'INTJ', NULL, 1, 0, CAST(N'2026-07-09T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (30, N'user30@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊家豪', N'0931308777', NULL, 0, 1, NULL, CAST(N'1981-02-10' AS Date), 1, N'教師', N'ENTJ', NULL, 1, 0, CAST(N'2025-07-26T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (31, N'user31@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林冠宇', N'0954352405', NULL, 0, 1, NULL, CAST(N'2005-10-27' AS Date), 2, N'設計師', N'ENTJ', NULL, 1, 0, CAST(N'2026-05-20T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (32, N'user32@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊冠宇', N'0995516630', NULL, 0, 1, NULL, CAST(N'1995-12-02' AS Date), 2, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2025-10-18T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (33, N'user33@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡佩蓉', N'0997742372', NULL, 0, 1, NULL, CAST(N'1994-12-28' AS Date), 1, N'護理師', N'INFP', NULL, 1, 0, CAST(N'2025-10-30T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (34, N'user34@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳家豪', N'0990084242', NULL, 0, 1, NULL, CAST(N'1989-04-16' AS Date), 1, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2026-06-20T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (35, N'user35@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡柏翰', N'0995879173', NULL, 0, 1, NULL, CAST(N'1999-06-24' AS Date), 1, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2025-07-28T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (36, N'user36@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊欣儀', N'0911411233', NULL, 0, 1, NULL, CAST(N'2001-02-01' AS Date), 2, N'行銷企劃', N'INFP', NULL, 1, 0, CAST(N'2025-07-26T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (37, N'user37@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳冠宇', N'0995371263', NULL, 0, 1, NULL, CAST(N'1984-12-03' AS Date), 2, N'設計師', N'INFP', NULL, 1, 0, CAST(N'2026-03-20T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (38, N'user38@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林詩涵', N'0955338126', NULL, 0, 1, NULL, CAST(N'2005-10-16' AS Date), 1, N'學生', N'ENTJ', NULL, 1, 0, CAST(N'2026-05-20T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (39, N'user39@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉雅婷', N'0949315210', NULL, 0, 1, NULL, CAST(N'1991-10-28' AS Date), 2, N'行銷企劃', N'INTJ', NULL, 1, 0, CAST(N'2026-03-30T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (40, N'user40@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃欣儀', N'0929674415', NULL, 0, 1, NULL, CAST(N'2002-03-12' AS Date), 1, N'學生', N'ISFJ', NULL, 1, 0, CAST(N'2025-10-17T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (41, N'user41@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡佩蓉', N'0968196999', NULL, 0, 1, NULL, CAST(N'2002-02-03' AS Date), 1, N'業務經理', N'ESTP', NULL, 1, 0, CAST(N'2025-10-26T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (42, N'user42@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊雅婷', N'0999157362', NULL, 0, 1, NULL, CAST(N'2005-05-27' AS Date), 1, N'業務經理', N'ESTP', NULL, 1, 0, CAST(N'2026-05-04T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (43, N'user43@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉欣儀', N'0943717292', NULL, 0, 1, NULL, CAST(N'1991-02-18' AS Date), 1, N'業務經理', N'INFP', NULL, 1, 0, CAST(N'2026-06-05T13:51:50.880' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (44, N'user44@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林家豪', N'0976598902', NULL, 0, 1, NULL, CAST(N'2000-08-21' AS Date), 1, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2026-04-20T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (45, N'user45@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉欣儀', N'0958063888', NULL, 0, 1, NULL, CAST(N'2002-01-16' AS Date), 1, N'軟體工程師', N'ESTP', NULL, 1, 0, CAST(N'2026-04-29T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (46, N'user46@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡欣儀', N'0992632152', NULL, 0, 1, NULL, CAST(N'2003-12-12' AS Date), 1, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2026-07-01T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (47, N'user47@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張宇翔', N'0942403789', NULL, 0, 1, NULL, CAST(N'2005-05-01' AS Date), 1, N'自由工作者', N'ENFP', NULL, 1, 0, CAST(N'2025-07-19T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (48, N'user48@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳俊傑', N'0951422291', NULL, 0, 1, NULL, CAST(N'1981-03-25' AS Date), 1, N'行銷企劃', N'INTJ', NULL, 1, 0, CAST(N'2026-04-06T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (49, N'user49@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張佩蓉', N'0982583051', NULL, 0, 1, NULL, CAST(N'1977-05-21' AS Date), 2, N'自由工作者', N'ESTP', NULL, 1, 0, CAST(N'2026-04-11T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (50, N'user50@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊怡君', N'0948085537', NULL, 0, 1, NULL, CAST(N'1994-02-08' AS Date), 2, N'學生', N'ENFP', NULL, 1, 0, CAST(N'2025-08-13T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (51, N'user51@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李欣儀', N'0942809358', NULL, 0, 1, NULL, CAST(N'1984-01-09' AS Date), 1, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2026-02-15T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (52, N'user52@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳宇翔', N'0939779779', NULL, 0, 1, NULL, CAST(N'1975-08-20' AS Date), 2, N'護理師', N'ESTP', NULL, 1, 0, CAST(N'2026-01-22T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (53, N'user53@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳欣儀', N'0971569519', NULL, 0, 1, NULL, CAST(N'2004-07-04' AS Date), 2, N'軟體工程師', N'ISTJ', NULL, 1, 0, CAST(N'2025-12-02T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (54, N'user54@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉欣儀', N'0931846070', NULL, 0, 1, NULL, CAST(N'2000-02-11' AS Date), 1, N'業務經理', N'ENFP', NULL, 1, 0, CAST(N'2025-12-19T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (55, N'user55@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張欣儀', N'0938377422', NULL, 0, 1, NULL, CAST(N'1992-08-28' AS Date), 1, N'軟體工程師', N'INTJ', NULL, 1, 0, CAST(N'2025-10-12T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (56, N'user56@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉怡君', N'0999737435', NULL, 0, 1, NULL, CAST(N'1998-05-28' AS Date), 2, N'設計師', N'ENTJ', NULL, 1, 0, CAST(N'2026-01-21T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (57, N'user57@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊詩涵', N'0946620782', NULL, 0, 1, NULL, CAST(N'1977-08-11' AS Date), 2, N'軟體工程師', N'ENFP', NULL, 1, 0, CAST(N'2025-12-14T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (58, N'user58@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃冠宇', N'0930182225', NULL, 0, 1, NULL, CAST(N'1999-04-07' AS Date), 2, N'業務經理', N'INTJ', NULL, 1, 0, CAST(N'2026-04-11T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (59, N'user59@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳宇翔', N'0940652553', NULL, 0, 1, NULL, CAST(N'1981-11-14' AS Date), 2, N'設計師', N'ISFJ', NULL, 1, 0, CAST(N'2026-01-22T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (60, N'user60@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李佩蓉', N'0921998067', NULL, 0, 1, NULL, CAST(N'1982-10-06' AS Date), 2, N'行銷企劃', N'ENTJ', NULL, 1, 0, CAST(N'2025-08-25T13:51:50.883' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (61, N'user61@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳宇翔', N'0970752883', NULL, 0, 1, NULL, CAST(N'1987-03-08' AS Date), 2, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2026-02-23T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (62, N'user62@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊冠宇', N'0928779816', NULL, 0, 1, NULL, CAST(N'1996-05-20' AS Date), 2, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2025-12-03T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (63, N'user63@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳詩涵', N'0958002247', NULL, 0, 1, NULL, CAST(N'1993-01-27' AS Date), 1, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2025-11-24T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (64, N'user64@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李冠宇', N'0971952259', NULL, 0, 1, NULL, CAST(N'1987-05-10' AS Date), 1, N'自由工作者', N'ESTP', NULL, 2, 0, CAST(N'2026-05-31T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (65, N'user65@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡家豪', N'0910353110', NULL, 0, 1, NULL, CAST(N'1987-11-18' AS Date), 2, N'護理師', N'ENTJ', NULL, 1, 0, CAST(N'2025-07-20T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (66, N'user66@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡雅婷', N'0944207226', NULL, 0, 1, NULL, CAST(N'1984-03-09' AS Date), 1, N'軟體工程師', N'ISFJ', NULL, 1, 0, CAST(N'2025-08-18T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (67, N'user67@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳冠宇', N'0927444786', NULL, 0, 1, NULL, CAST(N'1975-12-23' AS Date), 1, N'教師', N'ISFJ', NULL, 1, 0, CAST(N'2026-01-08T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (68, N'user68@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡詩涵', N'0933091842', NULL, 0, 1, NULL, CAST(N'1995-07-20' AS Date), 1, N'教師', N'ESTP', NULL, 1, 0, CAST(N'2025-09-25T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (69, N'user69@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張雅婷', N'0955528219', NULL, 0, 1, NULL, CAST(N'1991-02-16' AS Date), 1, N'學生', N'ENTJ', NULL, 1, 0, CAST(N'2025-10-27T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (70, N'user70@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊佩蓉', N'0931307750', NULL, 0, 1, NULL, CAST(N'1977-08-14' AS Date), 1, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2026-06-16T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (71, N'user71@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張家豪', N'0915267114', NULL, 0, 1, NULL, CAST(N'1986-07-27' AS Date), 1, N'設計師', N'ISTJ', NULL, 1, 0, CAST(N'2026-01-28T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (72, N'user72@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳家豪', N'0910735377', NULL, 0, 1, NULL, CAST(N'1995-01-10' AS Date), 1, N'教師', N'ISFJ', NULL, 1, 0, CAST(N'2026-06-29T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (73, N'user73@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡家豪', N'0957717677', NULL, 0, 1, NULL, CAST(N'1998-01-10' AS Date), 1, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2025-10-22T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (74, N'user74@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉佩蓉', N'0929250729', NULL, 0, 1, NULL, CAST(N'1987-03-03' AS Date), 1, N'設計師', N'ESFJ', NULL, 1, 0, CAST(N'2026-05-29T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (75, N'user75@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡詩涵', N'0964511224', NULL, 0, 1, NULL, CAST(N'2001-06-14' AS Date), 2, N'軟體工程師', N'ESFJ', NULL, 1, 0, CAST(N'2026-06-07T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (76, N'user76@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳怡君', N'0935640009', NULL, 0, 1, NULL, CAST(N'1988-01-25' AS Date), 1, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2026-02-03T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (77, N'user77@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳詩涵', N'0948266154', NULL, 0, 1, NULL, CAST(N'2004-10-09' AS Date), 2, N'學生', N'ISFJ', NULL, 1, 0, CAST(N'2025-11-06T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (78, N'user78@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林怡君', N'0918330045', NULL, 0, 1, NULL, CAST(N'1977-04-20' AS Date), 2, N'設計師', N'ISFJ', NULL, 1, 0, CAST(N'2025-10-11T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (79, N'user79@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊宇翔', N'0941846227', NULL, 0, 1, NULL, CAST(N'1990-10-24' AS Date), 1, N'行銷企劃', N'ENFP', NULL, 1, 0, CAST(N'2025-10-20T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (80, N'user80@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李柏翰', N'0945750325', NULL, 0, 1, NULL, CAST(N'1982-11-27' AS Date), 1, N'行銷企劃', N'ISFJ', NULL, 1, 0, CAST(N'2025-08-23T13:51:50.887' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (81, N'user81@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳詩涵', N'0995373564', NULL, 0, 1, NULL, CAST(N'2005-03-07' AS Date), 2, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2025-10-11T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (82, N'user82@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林家豪', N'0951273253', NULL, 0, 1, NULL, CAST(N'2003-04-02' AS Date), 2, N'護理師', N'ESFJ', NULL, 1, 0, CAST(N'2025-11-23T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (83, N'user83@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳怡君', N'0917494721', NULL, 0, 1, NULL, CAST(N'1975-10-03' AS Date), 1, N'行銷企劃', N'INFP', NULL, 1, 0, CAST(N'2025-08-04T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (84, N'user84@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡怡君', N'0962687449', NULL, 0, 1, NULL, CAST(N'1988-05-12' AS Date), 2, N'業務經理', N'INTJ', NULL, 1, 0, CAST(N'2026-03-17T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (85, N'user85@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊怡君', N'0957881563', NULL, 0, 1, NULL, CAST(N'1976-12-08' AS Date), 2, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2026-06-07T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (86, N'user86@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊佩蓉', N'0963103580', NULL, 0, 1, NULL, CAST(N'2001-07-18' AS Date), 2, N'業務經理', N'INFP', NULL, 1, 0, CAST(N'2025-08-01T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (87, N'user87@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳俊傑', N'0935911762', NULL, 0, 1, NULL, CAST(N'2000-04-18' AS Date), 1, N'業務經理', N'INFP', NULL, 1, 0, CAST(N'2025-08-15T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (88, N'user88@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張雅婷', N'0996715901', NULL, 0, 1, NULL, CAST(N'1980-11-12' AS Date), 2, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2026-02-27T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (89, N'user89@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳欣儀', N'0989413357', NULL, 0, 1, NULL, CAST(N'1999-10-18' AS Date), 2, N'軟體工程師', N'ESFJ', NULL, 1, 0, CAST(N'2026-03-14T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (90, N'user90@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳雅婷', N'0919396783', NULL, 0, 1, NULL, CAST(N'1984-05-09' AS Date), 2, N'業務經理', N'ESTP', NULL, 1, 0, CAST(N'2025-12-29T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (91, N'user91@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林詩涵', N'0992511421', NULL, 0, 1, NULL, CAST(N'1985-10-16' AS Date), 2, N'行銷企劃', N'INTJ', NULL, 1, 0, CAST(N'2025-07-19T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (92, N'user92@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李詩涵', N'0961005587', NULL, 0, 1, NULL, CAST(N'1997-10-27' AS Date), 2, N'行銷企劃', N'ISTJ', NULL, 1, 0, CAST(N'2026-07-04T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (93, N'user93@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李俊傑', N'0924534042', NULL, 0, 1, NULL, CAST(N'1987-10-20' AS Date), 1, N'學生', N'ISTJ', NULL, 1, 0, CAST(N'2026-06-20T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (94, N'user94@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳欣儀', N'0914770738', NULL, 0, 1, NULL, CAST(N'2000-11-14' AS Date), 1, N'學生', N'INFP', NULL, 1, 0, CAST(N'2026-04-30T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (95, N'user95@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉家豪', N'0997061263', NULL, 0, 1, NULL, CAST(N'1976-11-12' AS Date), 2, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2025-11-22T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (96, N'user96@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊欣儀', N'0914854054', NULL, 0, 1, NULL, CAST(N'1978-08-22' AS Date), 1, N'行銷企劃', N'ENFP', NULL, 1, 0, CAST(N'2025-08-26T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (97, N'user97@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳欣儀', N'0976009315', NULL, 0, 1, NULL, CAST(N'2003-05-17' AS Date), 1, N'護理師', N'ISFJ', NULL, 1, 0, CAST(N'2026-06-13T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (98, N'user98@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳冠宇', N'0991605720', NULL, 0, 1, NULL, CAST(N'1988-08-24' AS Date), 2, N'自由工作者', N'ISTJ', NULL, 1, 0, CAST(N'2026-07-13T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (99, N'user99@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃雅婷', N'0991690089', NULL, 0, 1, NULL, CAST(N'2002-04-01' AS Date), 1, N'軟體工程師', N'INTJ', NULL, 1, 0, CAST(N'2026-04-27T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (100, N'user100@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李冠宇', N'0949930011', NULL, 0, 1, NULL, CAST(N'1980-03-15' AS Date), 1, N'護理師', N'ENFP', NULL, 1, 0, CAST(N'2025-08-08T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (101, N'user101@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊雅婷', N'0965604609', NULL, 0, 1, NULL, CAST(N'1995-03-05' AS Date), 1, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2026-02-05T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (102, N'user102@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳詩涵', N'0931729695', NULL, 0, 1, NULL, CAST(N'1991-06-25' AS Date), 2, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2025-08-05T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (103, N'user103@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李怡君', N'0976799594', NULL, 0, 1, NULL, CAST(N'2004-03-07' AS Date), 2, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2026-07-05T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (104, N'user104@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊宇翔', N'0927358316', NULL, 0, 1, NULL, CAST(N'1991-02-21' AS Date), 1, N'軟體工程師', N'INTJ', NULL, 1, 0, CAST(N'2026-06-28T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (105, N'user105@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡家豪', N'0915367294', NULL, 0, 1, NULL, CAST(N'1977-02-03' AS Date), 2, N'教師', N'ISFJ', NULL, 1, 0, CAST(N'2025-11-08T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (106, N'user106@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊柏翰', N'0926283737', NULL, 0, 1, NULL, CAST(N'1996-06-01' AS Date), 1, N'學生', N'ISTJ', NULL, 1, 0, CAST(N'2025-09-02T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (107, N'user107@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉俊傑', N'0950270145', NULL, 0, 1, NULL, CAST(N'1988-05-16' AS Date), 1, N'軟體工程師', N'ESTP', NULL, 1, 0, CAST(N'2025-07-21T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (108, N'user108@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林俊傑', N'0911655660', NULL, 0, 1, NULL, CAST(N'1985-07-18' AS Date), 2, N'教師', N'INTJ', NULL, 1, 0, CAST(N'2025-07-25T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (109, N'user109@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳冠宇', N'0941169545', NULL, 0, 1, NULL, CAST(N'2005-05-26' AS Date), 1, N'軟體工程師', N'INTJ', NULL, 1, 0, CAST(N'2025-12-09T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (110, N'user110@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊柏翰', N'0958633476', NULL, 0, 1, NULL, CAST(N'1986-09-11' AS Date), 2, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2026-07-06T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (111, N'user111@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡俊傑', N'0949165735', NULL, 0, 1, NULL, CAST(N'1983-04-18' AS Date), 2, N'行銷企劃', N'ESFJ', NULL, 1, 0, CAST(N'2026-03-09T13:51:50.890' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (112, N'user112@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊宇翔', N'0984305433', NULL, 0, 1, NULL, CAST(N'2001-01-10' AS Date), 2, N'業務經理', N'ISTJ', NULL, 1, 0, CAST(N'2026-04-23T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (113, N'user113@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王宇翔', N'0945866970', NULL, 0, 1, NULL, CAST(N'1994-11-16' AS Date), 2, N'學生', N'ISFJ', NULL, 1, 0, CAST(N'2026-01-21T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (114, N'user114@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王俊傑', N'0922103473', NULL, 0, 1, NULL, CAST(N'1987-05-16' AS Date), 2, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2025-11-20T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (115, N'user115@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林佩蓉', N'0983117725', NULL, 0, 1, NULL, CAST(N'1996-06-16' AS Date), 2, N'教師', N'INFP', NULL, 1, 0, CAST(N'2026-07-10T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (116, N'user116@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林雅婷', N'0983383531', NULL, 0, 1, NULL, CAST(N'1984-08-26' AS Date), 2, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2025-11-25T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (117, N'user117@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃佩蓉', N'0962412328', NULL, 0, 1, NULL, CAST(N'1988-05-24' AS Date), 1, N'自由工作者', N'ISTJ', NULL, 1, 0, CAST(N'2025-11-23T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (118, N'user118@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃宇翔', N'0946067525', NULL, 0, 1, NULL, CAST(N'1975-03-05' AS Date), 1, N'軟體工程師', N'ENTJ', NULL, 1, 0, CAST(N'2025-07-30T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (119, N'user119@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃宇翔', N'0923099532', NULL, 0, 1, NULL, CAST(N'1988-04-20' AS Date), 2, N'設計師', N'INFP', NULL, 1, 0, CAST(N'2026-06-27T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (120, N'user120@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張家豪', N'0934337707', NULL, 0, 1, NULL, CAST(N'1975-07-18' AS Date), 2, N'教師', N'ENTJ', NULL, 1, 0, CAST(N'2026-04-10T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (121, N'user121@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王欣儀', N'0994621268', NULL, 0, 1, NULL, CAST(N'2001-04-26' AS Date), 2, N'業務經理', N'INFP', NULL, 1, 0, CAST(N'2026-05-14T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (122, N'user122@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳宇翔', N'0967202914', NULL, 0, 1, NULL, CAST(N'1996-11-19' AS Date), 2, N'設計師', N'ESFJ', NULL, 1, 0, CAST(N'2025-09-26T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (123, N'user123@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳冠宇', N'0940066930', NULL, 0, 1, NULL, CAST(N'1989-12-08' AS Date), 2, N'護理師', N'ENFP', NULL, 1, 0, CAST(N'2026-07-01T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (124, N'user124@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉怡君', N'0953257301', NULL, 0, 1, NULL, CAST(N'2000-12-05' AS Date), 1, N'護理師', N'ESFJ', NULL, 1, 0, CAST(N'2025-10-26T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (125, N'user125@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王柏翰', N'0917514629', NULL, 0, 1, NULL, CAST(N'1980-07-27' AS Date), 1, N'行銷企劃', N'ESFJ', NULL, 1, 0, CAST(N'2026-06-19T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (126, N'user126@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳宇翔', N'0943620593', NULL, 0, 1, NULL, CAST(N'1998-03-13' AS Date), 1, N'業務經理', N'ESFJ', NULL, 1, 0, CAST(N'2026-06-14T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (127, N'user127@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王柏翰', N'0921661769', NULL, 0, 1, NULL, CAST(N'1989-03-09' AS Date), 1, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2025-12-12T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (128, N'user128@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃雅婷', N'0964559685', NULL, 0, 1, NULL, CAST(N'1984-11-09' AS Date), 2, N'行銷企劃', N'ESFJ', NULL, 1, 0, CAST(N'2025-12-18T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (129, N'user129@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張怡君', N'0910454862', NULL, 0, 1, NULL, CAST(N'1987-01-21' AS Date), 1, N'學生', N'INTJ', NULL, 1, 0, CAST(N'2026-05-17T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (130, N'user130@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林俊傑', N'0931615196', NULL, 0, 1, NULL, CAST(N'1976-07-07' AS Date), 2, N'設計師', N'ESTP', NULL, 1, 0, CAST(N'2026-03-12T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (131, N'user131@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王俊傑', N'0912294450', NULL, 0, 1, NULL, CAST(N'1980-12-26' AS Date), 1, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2025-09-26T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (132, N'user132@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張柏翰', N'0926582782', NULL, 0, 1, NULL, CAST(N'1975-07-23' AS Date), 2, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2025-12-23T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (133, N'user133@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉欣儀', N'0935489725', NULL, 0, 1, NULL, CAST(N'1990-01-20' AS Date), 2, N'學生', N'INTJ', NULL, 1, 0, CAST(N'2026-07-17T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (134, N'user134@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李欣儀', N'0920064792', NULL, 0, 1, NULL, CAST(N'2004-03-13' AS Date), 1, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2025-10-18T13:51:50.893' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (135, N'user135@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡俊傑', N'0957137492', NULL, 0, 1, NULL, CAST(N'1996-04-20' AS Date), 1, N'教師', N'ISFJ', NULL, 1, 0, CAST(N'2026-06-30T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (136, N'user136@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊宇翔', N'0932251548', NULL, 0, 1, NULL, CAST(N'1980-06-03' AS Date), 2, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2026-03-25T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (137, N'user137@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡家豪', N'0943611676', NULL, 0, 1, NULL, CAST(N'1993-01-05' AS Date), 2, N'業務經理', N'INTJ', NULL, 1, 0, CAST(N'2025-12-18T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (138, N'user138@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王俊傑', N'0955604150', NULL, 0, 1, NULL, CAST(N'1986-08-07' AS Date), 1, N'護理師', N'ENFP', NULL, 1, 0, CAST(N'2025-09-21T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (139, N'user139@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張雅婷', N'0969713766', NULL, 0, 1, NULL, CAST(N'1994-04-25' AS Date), 2, N'教師', N'ESFJ', NULL, 1, 0, CAST(N'2026-01-28T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (140, N'user140@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李柏翰', N'0910517418', NULL, 0, 1, NULL, CAST(N'1993-06-06' AS Date), 2, N'學生', N'ISFJ', NULL, 1, 0, CAST(N'2025-09-19T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (141, N'user141@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃詩涵', N'0953150255', NULL, 0, 1, NULL, CAST(N'2003-04-18' AS Date), 1, N'學生', N'ESFJ', NULL, 1, 0, CAST(N'2026-06-27T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (142, N'user142@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李宇翔', N'0938409507', NULL, 0, 1, NULL, CAST(N'1996-07-28' AS Date), 2, N'軟體工程師', N'ENTJ', NULL, 1, 0, CAST(N'2025-10-05T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (143, N'user143@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡佩蓉', N'0971161828', NULL, 0, 1, NULL, CAST(N'2003-10-20' AS Date), 2, N'業務經理', N'ESTP', NULL, 1, 0, CAST(N'2025-11-22T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (144, N'user144@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉佩蓉', N'0937192763', NULL, 0, 1, NULL, CAST(N'1979-07-14' AS Date), 2, N'教師', N'ENFP', NULL, 1, 0, CAST(N'2026-04-19T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (145, N'user145@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊詩涵', N'0913381812', NULL, 0, 1, NULL, CAST(N'1982-09-18' AS Date), 1, N'自由工作者', N'ISTJ', NULL, 1, 0, CAST(N'2025-12-30T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (146, N'user146@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳家豪', N'0996172887', NULL, 0, 1, NULL, CAST(N'1981-09-02' AS Date), 1, N'教師', N'ENTJ', NULL, 1, 0, CAST(N'2026-06-06T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (147, N'user147@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊宇翔', N'0927029949', NULL, 0, 1, NULL, CAST(N'1987-08-20' AS Date), 1, N'自由工作者', N'ENTJ', NULL, 1, 0, CAST(N'2025-09-04T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (148, N'user148@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳冠宇', N'0945946846', NULL, 0, 1, NULL, CAST(N'1988-11-22' AS Date), 2, N'軟體工程師', N'ENTJ', NULL, 1, 0, CAST(N'2026-03-22T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (149, N'user149@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林雅婷', N'0921400158', NULL, 0, 1, NULL, CAST(N'1986-04-16' AS Date), 1, N'學生', N'ISFJ', NULL, 1, 0, CAST(N'2026-02-16T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (150, N'user150@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張宇翔', N'0999790422', NULL, 0, 1, NULL, CAST(N'1985-10-11' AS Date), 2, N'自由工作者', N'ENFP', NULL, 1, 0, CAST(N'2025-12-16T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (151, N'user151@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡佩蓉', N'0927600141', NULL, 0, 1, NULL, CAST(N'1997-05-07' AS Date), 2, N'護理師', N'ENTJ', NULL, 1, 0, CAST(N'2025-07-23T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (152, N'user152@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳柏翰', N'0983599002', NULL, 0, 1, NULL, CAST(N'1991-05-21' AS Date), 2, N'自由工作者', N'INTJ', NULL, 1, 0, CAST(N'2025-12-06T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (153, N'user153@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳宇翔', N'0955703648', NULL, 0, 1, NULL, CAST(N'1980-12-03' AS Date), 1, N'設計師', N'INTJ', NULL, 1, 0, CAST(N'2025-12-26T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (154, N'user154@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊宇翔', N'0983502327', NULL, 0, 1, NULL, CAST(N'1994-09-28' AS Date), 2, N'學生', N'ISFJ', NULL, 1, 0, CAST(N'2026-06-17T13:51:50.897' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (155, N'user155@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王柏翰', N'0921798496', NULL, 0, 1, NULL, CAST(N'2005-10-04' AS Date), 2, N'行銷企劃', N'INFP', NULL, 1, 0, CAST(N'2025-10-29T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (156, N'user156@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃家豪', N'0957563798', NULL, 0, 1, NULL, CAST(N'2002-04-13' AS Date), 2, N'自由工作者', N'ESFJ', NULL, 1, 0, CAST(N'2025-11-08T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (157, N'user157@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡柏翰', N'0936365024', NULL, 0, 1, NULL, CAST(N'1985-12-12' AS Date), 1, N'自由工作者', N'INTJ', NULL, 1, 0, CAST(N'2026-07-03T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (158, N'user158@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳詩涵', N'0968819801', NULL, 0, 1, NULL, CAST(N'1996-01-07' AS Date), 1, N'行銷企劃', N'ISFJ', NULL, 1, 0, CAST(N'2025-11-21T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (159, N'user159@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張詩涵', N'0922026180', NULL, 0, 1, NULL, CAST(N'2001-12-19' AS Date), 2, N'教師', N'ESTP', NULL, 1, 0, CAST(N'2026-07-14T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (160, N'user160@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王冠宇', N'0996660041', NULL, 0, 1, NULL, CAST(N'1981-08-23' AS Date), 2, N'行銷企劃', N'ISTJ', NULL, 1, 0, CAST(N'2026-02-09T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (161, N'user161@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳冠宇', N'0948756600', NULL, 0, 1, NULL, CAST(N'1997-05-06' AS Date), 2, N'設計師', N'ENFP', NULL, 1, 0, CAST(N'2025-09-17T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (162, N'user162@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張欣儀', N'0916894982', NULL, 0, 1, NULL, CAST(N'1975-09-13' AS Date), 2, N'行銷企劃', N'ESFJ', NULL, 1, 0, CAST(N'2025-10-09T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (163, N'user163@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王冠宇', N'0990000737', NULL, 0, 1, NULL, CAST(N'1977-06-10' AS Date), 2, N'護理師', N'ISTJ', NULL, 1, 0, CAST(N'2025-08-04T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (164, N'user164@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳怡君', N'0988663226', NULL, 0, 1, NULL, CAST(N'1994-06-01' AS Date), 2, N'護理師', N'ESTP', NULL, 1, 0, CAST(N'2025-12-08T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (165, N'user165@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃俊傑', N'0910826039', NULL, 0, 1, NULL, CAST(N'1995-06-16' AS Date), 2, N'行銷企劃', N'INFP', NULL, 1, 0, CAST(N'2026-02-18T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (166, N'user166@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡佩蓉', N'0932054526', NULL, 0, 1, NULL, CAST(N'1991-12-03' AS Date), 2, N'行銷企劃', N'ENFP', NULL, 1, 0, CAST(N'2025-10-29T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (167, N'user167@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林家豪', N'0925975482', NULL, 0, 1, NULL, CAST(N'1982-12-27' AS Date), 1, N'行銷企劃', N'ISFJ', NULL, 1, 0, CAST(N'2026-02-11T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (168, N'user168@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張怡君', N'0937380235', NULL, 0, 1, NULL, CAST(N'1990-01-07' AS Date), 1, N'軟體工程師', N'INFP', NULL, 1, 0, CAST(N'2026-06-14T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (169, N'user169@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃欣儀', N'0976205326', NULL, 0, 1, NULL, CAST(N'1975-06-27' AS Date), 2, N'教師', N'ESTP', NULL, 1, 0, CAST(N'2025-11-29T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (170, N'user170@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉詩涵', N'0967456432', NULL, 0, 1, NULL, CAST(N'1994-06-22' AS Date), 2, N'護理師', N'ENFP', NULL, 1, 0, CAST(N'2025-12-18T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (171, N'user171@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張雅婷', N'0970149261', NULL, 0, 1, NULL, CAST(N'1989-11-27' AS Date), 2, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2025-10-21T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (172, N'user172@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'陳佩蓉', N'0920872266', NULL, 0, 1, NULL, CAST(N'2003-12-06' AS Date), 2, N'學生', N'ENTJ', NULL, 1, 0, CAST(N'2026-06-06T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (173, N'user173@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊柏翰', N'0989210008', NULL, 0, 1, NULL, CAST(N'1995-01-02' AS Date), 1, N'軟體工程師', N'ENTJ', NULL, 1, 0, CAST(N'2025-12-28T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (174, N'user174@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張宇翔', N'0983656635', NULL, 0, 1, NULL, CAST(N'2002-01-16' AS Date), 2, N'設計師', N'ENTJ', NULL, 2, 0, CAST(N'2026-03-23T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (175, N'user175@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王俊傑', N'0993251159', NULL, 0, 1, NULL, CAST(N'1986-07-23' AS Date), 1, N'設計師', N'ESFJ', NULL, 1, 0, CAST(N'2026-04-29T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (176, N'user176@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃怡君', N'0983554196', NULL, 0, 1, NULL, CAST(N'1979-11-06' AS Date), 2, N'行銷企劃', N'ESTP', NULL, 1, 0, CAST(N'2026-03-09T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (177, N'user177@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張家豪', N'0994796339', NULL, 0, 1, NULL, CAST(N'2003-02-27' AS Date), 2, N'設計師', N'ISTJ', NULL, 1, 0, CAST(N'2026-01-28T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (178, N'user178@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡佩蓉', N'0962656374', NULL, 0, 1, NULL, CAST(N'1991-09-16' AS Date), 1, N'設計師', N'ESFJ', NULL, 1, 0, CAST(N'2026-06-22T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (179, N'user179@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡冠宇', N'0984416873', NULL, 0, 1, NULL, CAST(N'1976-07-27' AS Date), 1, N'行銷企劃', N'INTJ', NULL, 1, 0, CAST(N'2026-05-04T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (180, N'user180@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林家豪', N'0916364910', NULL, 0, 1, NULL, CAST(N'1997-10-02' AS Date), 1, N'學生', N'INFP', NULL, 1, 0, CAST(N'2025-09-22T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (181, N'user181@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉欣儀', N'0919009544', NULL, 0, 1, NULL, CAST(N'1991-01-16' AS Date), 1, N'業務經理', N'ESFJ', NULL, 1, 0, CAST(N'2026-04-05T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (182, N'user182@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王欣儀', N'0934297803', NULL, 0, 1, NULL, CAST(N'1979-09-28' AS Date), 2, N'業務經理', N'ENFP', NULL, 1, 0, CAST(N'2025-12-11T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (183, N'user183@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃俊傑', N'0927949884', NULL, 0, 1, NULL, CAST(N'1988-05-20' AS Date), 1, N'自由工作者', N'ESTP', NULL, 1, 0, CAST(N'2026-04-24T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (184, N'user184@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'張宇翔', N'0973902217', NULL, 0, 1, NULL, CAST(N'1975-10-13' AS Date), 2, N'教師', N'ESTP', NULL, 2, 0, CAST(N'2025-11-16T13:51:50.900' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (185, N'user185@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡柏翰', N'0955150118', NULL, 0, 1, NULL, CAST(N'1981-11-17' AS Date), 2, N'自由工作者', N'ISTJ', NULL, 1, 0, CAST(N'2026-06-14T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (186, N'user186@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林詩涵', N'0953499732', NULL, 0, 1, NULL, CAST(N'1990-02-23' AS Date), 2, N'教師', N'ISTJ', NULL, 1, 0, CAST(N'2026-01-22T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (187, N'user187@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡家豪', N'0949930104', NULL, 0, 1, NULL, CAST(N'2005-02-25' AS Date), 1, N'學生', N'ESTP', NULL, 1, 0, CAST(N'2026-07-13T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (188, N'user188@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊欣儀', N'0967422974', NULL, 0, 1, NULL, CAST(N'1995-07-19' AS Date), 1, N'軟體工程師', N'INTJ', NULL, 1, 0, CAST(N'2026-07-15T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (189, N'user189@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'李俊傑', N'0969918222', NULL, 0, 1, NULL, CAST(N'1982-07-14' AS Date), 1, N'學生', N'INFP', NULL, 1, 0, CAST(N'2025-10-31T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (190, N'user190@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃柏翰', N'0927871401', NULL, 0, 1, NULL, CAST(N'2004-03-27' AS Date), 2, N'教師', N'ISFJ', NULL, 1, 0, CAST(N'2026-05-25T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (191, N'user191@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳宇翔', N'0951131858', NULL, 0, 1, NULL, CAST(N'1986-05-01' AS Date), 2, N'學生', N'ENFP', NULL, 1, 0, CAST(N'2026-04-03T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (192, N'user192@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'蔡柏翰', N'0970278007', NULL, 0, 1, NULL, CAST(N'1985-05-20' AS Date), 1, N'學生', N'INTJ', NULL, 1, 0, CAST(N'2026-04-19T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (193, N'user193@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王冠宇', N'0999513058', NULL, 0, 1, NULL, CAST(N'1975-07-18' AS Date), 2, N'軟體工程師', N'ISTJ', NULL, 1, 0, CAST(N'2025-12-08T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (194, N'user194@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'林俊傑', N'0948001182', NULL, 0, 1, NULL, CAST(N'2001-05-17' AS Date), 1, N'軟體工程師', N'ESTP', NULL, 1, 0, CAST(N'2026-03-21T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (195, N'user195@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊俊傑', N'0915661959', NULL, 0, 1, NULL, CAST(N'1985-12-24' AS Date), 2, N'教師', N'ESTP', NULL, 1, 0, CAST(N'2025-11-29T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (196, N'user196@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'楊佩蓉', N'0938213050', NULL, 0, 1, NULL, CAST(N'1994-08-04' AS Date), 1, N'設計師', N'ENFP', NULL, 1, 0, CAST(N'2026-06-24T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (197, N'user197@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'劉詩涵', N'0960574660', NULL, 0, 1, NULL, CAST(N'2000-05-04' AS Date), 1, N'自由工作者', N'ESTP', NULL, 1, 0, CAST(N'2025-12-04T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (198, N'user198@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'黃宇翔', N'0972605382', NULL, 0, 1, NULL, CAST(N'1986-01-13' AS Date), 1, N'學生', N'ENFP', NULL, 1, 0, CAST(N'2026-05-01T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (199, N'user199@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'王柏翰', N'0930164520', NULL, 0, 1, NULL, CAST(N'2000-04-19' AS Date), 2, N'行銷企劃', N'INTJ', NULL, 1, 0, CAST(N'2025-12-04T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
INSERT [dbo].[Members] ([MemberID], [Email], [PasswordHash], [Name], [Phone], [LineId], [IsPrivateAccount], [IsEmailConfirmed], [AvatarUrl], [BirthDate], [Gender], [Occupation], [MBTI], [Bio], [Status], [Role], [CreatedAt], [InstagramUrl], [FacebookUrl], [ContactBookVisibility], [LastLoginAt], [LastLoginIp], [FailedLoginCount], [LockoutEndDate]) VALUES (200, N'user200@lazy.com', N'$2a$11$w1./z1.U.uC4u12345678.oJ2VlK6b3k9L/2zH7Z/8Q.1fC8XmO', N'吳佩蓉', N'0945543842', NULL, 0, 1, NULL, CAST(N'1993-06-16' AS Date), 2, N'設計師', N'ENFP', NULL, 1, 0, CAST(N'2025-08-17T13:51:50.903' AS DateTime), NULL, NULL, 0, NULL, NULL, 0, NULL)
GO
SET IDENTITY_INSERT [dbo].[Members] OFF
GO
SET ANSI_PADDING ON
GO
/****** 物件:  Index [UQ__Members__A9D10534268668CA]    指令碼日期: 2026/7/19 下午 09:16:38 ******/
ALTER TABLE [dbo].[Members] ADD UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AdminLogs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[AdminPermissions] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Blocks] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Expenses] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Expenses] ADD  DEFAULT ((0)) FOR [IsDelete]
GO
ALTER TABLE [dbo].[ExpenseSplits] ADD  DEFAULT ((0)) FOR [IsPaid]
GO
ALTER TABLE [dbo].[ExternalLogins] ADD  DEFAULT (getdate()) FOR [CreatedAt]
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
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [IsPrivateAccount]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [IsEmailConfirmed]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [Gender]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((1)) FOR [Status]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [Role]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [ContactBookVisibility]
GO
ALTER TABLE [dbo].[Members] ADD  DEFAULT ((0)) FOR [FailedLoginCount]
GO
ALTER TABLE [dbo].[MemberSubscriptions] ADD  DEFAULT ((0)) FOR [Status]
GO
ALTER TABLE [dbo].[MemberTokens] ADD  DEFAULT (getdate()) FOR [CreatedAt]
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
ALTER TABLE [dbo].[SubscriptionPlans] ADD  DEFAULT ((0)) FOR [Price]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  DEFAULT ((0)) FOR [DurationDays]
GO
ALTER TABLE [dbo].[SubscriptionPlans] ADD  DEFAULT ((1)) FOR [IsActive]
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
ALTER TABLE [dbo].[AdminLogs]  WITH CHECK ADD FOREIGN KEY([AdminID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[AdminPermissions]  WITH CHECK ADD  CONSTRAINT [FK_AdminPermissions_Members] FOREIGN KEY([AdminID])
REFERENCES [dbo].[Members] ([MemberID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AdminPermissions] CHECK CONSTRAINT [FK_AdminPermissions_Members]
GO
ALTER TABLE [dbo].[Blocks]  WITH CHECK ADD FOREIGN KEY([BlockedID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Blocks]  WITH CHECK ADD FOREIGN KEY([BlockerID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD FOREIGN KEY([GroupID])
REFERENCES [dbo].[TravelGroups] ([GroupID])
GO
ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD FOREIGN KEY([PayerID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[ExpenseSplits]  WITH CHECK ADD FOREIGN KEY([ExpenseID])
REFERENCES [dbo].[Expenses] ([ExpenseID])
GO
ALTER TABLE [dbo].[ExpenseSplits]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[ExternalLogins]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Follows]  WITH CHECK ADD FOREIGN KEY([FollowerID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Follows]  WITH CHECK ADD FOREIGN KEY([FolloweeID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[ForumComments]  WITH CHECK ADD FOREIGN KEY([ForumPostID])
REFERENCES [dbo].[ForumPosts] ([ForumPostID])
GO
ALTER TABLE [dbo].[ForumComments]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[ForumImages]  WITH CHECK ADD FOREIGN KEY([ForumPostID])
REFERENCES [dbo].[ForumPosts] ([ForumPostID])
GO
ALTER TABLE [dbo].[ForumInteracts]  WITH CHECK ADD FOREIGN KEY([ForumPostID])
REFERENCES [dbo].[ForumPosts] ([ForumPostID])
GO
ALTER TABLE [dbo].[ForumInteracts]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[ForumPosts]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[FriendRequests]  WITH CHECK ADD FOREIGN KEY([ReceiverID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[FriendRequests]  WITH CHECK ADD FOREIGN KEY([RequesterID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Friendships]  WITH CHECK ADD FOREIGN KEY([MemberID1])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Friendships]  WITH CHECK ADD FOREIGN KEY([MemberID2])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[GroupMembers]  WITH CHECK ADD FOREIGN KEY([GroupID])
REFERENCES [dbo].[TravelGroups] ([GroupID])
GO
ALTER TABLE [dbo].[GroupMembers]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[GroupMembers]  WITH CHECK ADD FOREIGN KEY([RemovedByMemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[ItineraryNodes]  WITH CHECK ADD FOREIGN KEY([PostID])
REFERENCES [dbo].[VlogPosts] ([PostID])
GO
ALTER TABLE [dbo].[JoinRequests]  WITH CHECK ADD FOREIGN KEY([GroupID])
REFERENCES [dbo].[TravelGroups] ([GroupID])
GO
ALTER TABLE [dbo].[JoinRequests]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[JoinRequests]  WITH CHECK ADD FOREIGN KEY([ReviewedByMemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[LoginHistories]  WITH CHECK ADD  CONSTRAINT [FK_LoginHistories_Members] FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[LoginHistories] CHECK CONSTRAINT [FK_LoginHistories_Members]
GO
ALTER TABLE [dbo].[MemberSubscriptions]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[MemberSubscriptions]  WITH CHECK ADD FOREIGN KEY([PlanID])
REFERENCES [dbo].[SubscriptionPlans] ([PlanID])
GO
ALTER TABLE [dbo].[MemberTokens]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Notifications]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[PostInteractions]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[PostInteractions]  WITH CHECK ADD FOREIGN KEY([PostID])
REFERENCES [dbo].[VlogPosts] ([PostID])
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD FOREIGN KEY([ReporterID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[Reports]  WITH CHECK ADD FOREIGN KEY([ReportedMemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[TravelGroups]  WITH CHECK ADD FOREIGN KEY([OwnerMemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
ALTER TABLE [dbo].[VlogPosts]  WITH CHECK ADD FOREIGN KEY([MemberID])
REFERENCES [dbo].[Members] ([MemberID])
GO
USE [master]
GO
ALTER DATABASE [LazyTravelDB] SET  READ_WRITE 
GO
