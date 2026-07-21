-- LazyTravel 核心資料表：Members / TravelGroups / GroupMembers / JoinRequests
-- 依據「Lazy Travel 旅遊平台 資料表(1).pdf」規格書建立，對應現有 LazyTravelContext.cs

USE LazyTravelDB;
GO

IF OBJECT_ID('dbo.JoinRequests', 'U') IS NOT NULL DROP TABLE dbo.JoinRequests;
IF OBJECT_ID('dbo.GroupMembers', 'U') IS NOT NULL DROP TABLE dbo.GroupMembers;
IF OBJECT_ID('dbo.TravelGroups', 'U') IS NOT NULL DROP TABLE dbo.TravelGroups;
IF OBJECT_ID('dbo.Members', 'U') IS NOT NULL DROP TABLE dbo.Members;
GO

CREATE TABLE dbo.Members (
    MemberID          int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Email              nvarchar(100) NOT NULL,
    PasswordHash       nvarchar(255) NULL,
    Name               nvarchar(50)  NOT NULL,
    Phone              varchar(20)   NULL,
    IsPhonePublic      bit           NOT NULL DEFAULT 0,
    InstagramUrl       nvarchar(255) NULL,
    FacebookUrl        nvarchar(255) NULL,
    LineId             nvarchar(50)  NULL,
    SocialLinksPrivacy tinyint       NOT NULL DEFAULT 0,
    IsEmailConfirmed   bit           NOT NULL DEFAULT 0,
    AvatarUrl          nvarchar(max) NULL,
    BirthDate          date          NULL,
    Gender             tinyint       NOT NULL DEFAULT 0,
    Occupation         nvarchar(50)  NULL,
    MBTI               varchar(4)    NULL,
    Bio                nvarchar(500) NULL,
    Status             tinyint       NOT NULL DEFAULT 1,
    CreatedAt          datetime      NOT NULL DEFAULT GETDATE(),
    AuthProvider       nvarchar(50)  NULL DEFAULT 'Local',
    ProviderKey        nvarchar(255) NULL,
    CONSTRAINT UQ_Members_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.TravelGroups (
    GroupID        int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OwnerMemberID  int            NOT NULL,
    GroupTitle     nvarchar(100)  NOT NULL,
    Description    nvarchar(1000) NULL,
    MinPeople      int            NOT NULL DEFAULT 2,
    MaxPeople      int            NOT NULL DEFAULT 10,
    CurrentPeople  int            NOT NULL DEFAULT 1,
    JoinRule       nvarchar(50)   NOT NULL DEFAULT N'需團主審核',
    GroupStatus    nvarchar(30)   NOT NULL DEFAULT N'等待中',
    IsPublic       bit            NOT NULL DEFAULT 1,
    CreatedAt      datetime       NOT NULL DEFAULT GETDATE(),
    UpdatedAt      datetime       NOT NULL DEFAULT GETDATE(),
    IsDelete       bit            NOT NULL DEFAULT 0,
    ReviewStatus   nvarchar(30)   NOT NULL CONSTRAINT DF_TravelGroups_ReviewStatus DEFAULT (N'正常'),
    -- 以下 4 欄不在原始規格書內，是 hljh910215 在 TravelGroup.cs 手動加的搜尋欄位
    Country        nvarchar(100)  NULL,
    Region         nvarchar(100)  NULL,
    StartDate      datetime       NULL,
    EndDate        datetime       NULL,
    CONSTRAINT FK_TravelGroups_OwnerMember FOREIGN KEY (OwnerMemberID) REFERENCES dbo.Members(MemberID)
);
GO

CREATE TABLE dbo.GroupMembers (
    GroupMemberID      int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    GroupID            int           NOT NULL,
    MemberID           int           NOT NULL,
    MemberRole         nvarchar(30)  NOT NULL DEFAULT N'成員',
    JoinedAt           datetime      NOT NULL DEFAULT GETDATE(),
    LeftAt             datetime      NULL,
    IsRemoved          bit           NOT NULL DEFAULT 0,
    RemovedByMemberID  int           NULL,
    RemovedAt          datetime      NULL,
    RemoveReason       nvarchar(300) NULL,
    CreatedAt          datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_GroupMembers_Group FOREIGN KEY (GroupID) REFERENCES dbo.TravelGroups(GroupID),
    CONSTRAINT FK_GroupMembers_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_GroupMembers_RemovedByMember FOREIGN KEY (RemovedByMemberID) REFERENCES dbo.Members(MemberID)
);
GO

CREATE TABLE dbo.JoinRequests (
    RequestID          int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    GroupID            int           NOT NULL,
    MemberID           int           NOT NULL,
    Message            nvarchar(500) NULL,
    RequestStatus      nvarchar(30)  NOT NULL DEFAULT N'待審核',
    ReviewedByMemberID int           NULL,
    ReviewedAt         datetime      NULL,
    CreatedAt          datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_JoinRequests_Group FOREIGN KEY (GroupID) REFERENCES dbo.TravelGroups(GroupID),
    CONSTRAINT FK_JoinRequests_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_JoinRequests_ReviewedByMember FOREIGN KEY (ReviewedByMemberID) REFERENCES dbo.Members(MemberID)
);
GO
