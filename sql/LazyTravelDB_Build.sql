/* ============================================================
   LazyTravel 旅遊平台 - 資料庫完整建置腳本
   來源：LazyTravel_DB.docx「Lazy Travel 旅遊平台 - 全模組資料庫規格書 (DBA 嚴謹審核版) v2」
   用途：在 SSMS 對 LazyTravelDB 一次建立/重建全部 32 張資料表

   ⚠️ 注意：這是「重建腳本」，開頭會把同名的舊資料表整個 DROP 掉再重新 CREATE，
      舊資料表裡的資料會全部消失！正式使用前請先確認：
        1. 目前 LazyTravelDB 裡沒有需要保留的資料，或已經備份
        2. 不會誤刪別人正在用的資料庫
      如果只是想新增缺少的表（例如 TravelGroupsLog、Employees 系列），
      而不想清掉現有資料，請不要整份執行，改成只挑需要的 CREATE TABLE 區塊來跑。

   舊檔案關係：
     - schema_core.sql      → 只有 4 張表的舊版，已被本檔取代
     - sql/SQLQuery0719.sql → 7/19 SSMS 匯出的舊版（26 張表），
                               少了 TravelGroupsLog 與模組 E 的 RBAC 6 張表，
                               TravelGroups/VlogPosts 也少了 v2 新欄位。
                               本檔以規格書 v2 為準，是目前最新版本。
   ============================================================ */

IF DB_ID(N'LazyTravelDB') IS NULL
BEGIN
    CREATE DATABASE LazyTravelDB;
END
GO

USE LazyTravelDB;
GO

-- ------------------------------------------------------------
-- 第一步：依「先刪子表、後刪父表」的順序刪除舊表，讓腳本可重複執行
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.AdminAuditLogs', 'U')    IS NOT NULL DROP TABLE dbo.AdminAuditLogs;
IF OBJECT_ID('dbo.EmployeeRoles', 'U')     IS NOT NULL DROP TABLE dbo.EmployeeRoles;
IF OBJECT_ID('dbo.RolePermissions', 'U')   IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID('dbo.Permissions', 'U')       IS NOT NULL DROP TABLE dbo.Permissions;
IF OBJECT_ID('dbo.Roles', 'U')             IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Employees', 'U')         IS NOT NULL DROP TABLE dbo.Employees;

IF OBJECT_ID('dbo.AdminLogs', 'U')         IS NOT NULL DROP TABLE dbo.AdminLogs;
IF OBJECT_ID('dbo.AdminPermissions', 'U')  IS NOT NULL DROP TABLE dbo.AdminPermissions; -- 舊設計，已被模組 E 取代
IF OBJECT_ID('dbo.MemberSubscriptions', 'U') IS NOT NULL DROP TABLE dbo.MemberSubscriptions;
IF OBJECT_ID('dbo.SubscriptionPlans', 'U') IS NOT NULL DROP TABLE dbo.SubscriptionPlans;
IF OBJECT_ID('dbo.ForumInteracts', 'U')    IS NOT NULL DROP TABLE dbo.ForumInteracts;
IF OBJECT_ID('dbo.ForumComments', 'U')     IS NOT NULL DROP TABLE dbo.ForumComments;
IF OBJECT_ID('dbo.ForumImages', 'U')       IS NOT NULL DROP TABLE dbo.ForumImages;
IF OBJECT_ID('dbo.ForumPosts', 'U')        IS NOT NULL DROP TABLE dbo.ForumPosts;
IF OBJECT_ID('dbo.ExpenseSplits', 'U')     IS NOT NULL DROP TABLE dbo.ExpenseSplits;
IF OBJECT_ID('dbo.Expenses', 'U')          IS NOT NULL DROP TABLE dbo.Expenses;

IF OBJECT_ID('dbo.PostInteractions', 'U')  IS NOT NULL DROP TABLE dbo.PostInteractions;
IF OBJECT_ID('dbo.ItineraryNodes', 'U')    IS NOT NULL DROP TABLE dbo.ItineraryNodes;
IF OBJECT_ID('dbo.VlogPosts', 'U')         IS NOT NULL DROP TABLE dbo.VlogPosts;

IF OBJECT_ID('dbo.Blocks', 'U')            IS NOT NULL DROP TABLE dbo.Blocks;
IF OBJECT_ID('dbo.Friendships', 'U')       IS NOT NULL DROP TABLE dbo.Friendships;
IF OBJECT_ID('dbo.FriendRequests', 'U')    IS NOT NULL DROP TABLE dbo.FriendRequests;
IF OBJECT_ID('dbo.Follows', 'U')           IS NOT NULL DROP TABLE dbo.Follows;
IF OBJECT_ID('dbo.TravelGroupsLog', 'U')   IS NOT NULL DROP TABLE dbo.TravelGroupsLog;
IF OBJECT_ID('dbo.JoinRequests', 'U')      IS NOT NULL DROP TABLE dbo.JoinRequests;
IF OBJECT_ID('dbo.GroupMembers', 'U')      IS NOT NULL DROP TABLE dbo.GroupMembers;
IF OBJECT_ID('dbo.TravelGroups', 'U')      IS NOT NULL DROP TABLE dbo.TravelGroups;

IF OBJECT_ID('dbo.Notifications', 'U')     IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.Reports', 'U')           IS NOT NULL DROP TABLE dbo.Reports;
IF OBJECT_ID('dbo.MemberTokens', 'U')      IS NOT NULL DROP TABLE dbo.MemberTokens;
IF OBJECT_ID('dbo.ExternalLogins', 'U')    IS NOT NULL DROP TABLE dbo.ExternalLogins;
IF OBJECT_ID('dbo.LoginHistories', 'U')    IS NOT NULL DROP TABLE dbo.LoginHistories;
IF OBJECT_ID('dbo.Members', 'U')           IS NOT NULL DROP TABLE dbo.Members;
GO

/* ============================================================
   模組 A：會員系統與平台安全 (Member & Security)
   ============================================================ */

-- 1. 會員主表：整合聯絡資訊、隱私設定、資安欄位（登入失敗次數/鎖定時間防暴力破解）
CREATE TABLE dbo.Members (
    MemberID              int IDENTITY(1,1) NOT NULL,
    Email                 nvarchar(100)  NOT NULL,               -- 登入帳號，需唯一
    PasswordHash          nvarchar(255)  NULL,                    -- 只用第三方登入的話可為 NULL
    Name                  nvarchar(50)   NOT NULL,
    Phone                 varchar(20)    NULL,
    InstagramUrl          nvarchar(255)  NULL,
    LineId                nvarchar(50)   NULL,
    FacebookUrl           nvarchar(255)  NULL,
    ContactBookVisibility tinyint        NOT NULL DEFAULT 0,      -- 0:全公開 1:限雙向好友與同團
    IsPrivateAccount      bit            NOT NULL DEFAULT 0,      -- 0:公開 1:私密
    IsEmailConfirmed      bit            NOT NULL DEFAULT 0,
    AvatarUrl             nvarchar(max)  NULL,
    BirthDate             date           NULL,
    Gender                tinyint        NOT NULL DEFAULT 0,      -- 0:未知 1:男 2:女 3:其他
    Occupation            nvarchar(50)   NULL,
    MBTI                  varchar(4)     NULL,
    Bio                   nvarchar(500)  NULL,
    Status                tinyint        NOT NULL DEFAULT 1,      -- 1:正常 2:停權/封鎖
    CreatedAt             datetime       NOT NULL DEFAULT GETDATE(),
    LastLoginAt           datetime       NULL,
    LastLoginIp           varchar(50)    NULL,
    FailedLoginCount      int            NOT NULL DEFAULT 0,      -- [資安] 連續登入失敗次數
    LockoutEndDate        datetime       NULL,                    -- [資安] 帳號鎖定到期時間
    CONSTRAINT PK_Members PRIMARY KEY (MemberID),
    CONSTRAINT UQ_Members_Email UNIQUE (Email)
);
GO

-- 2. 登入歷史紀錄表：每一筆登入嘗試都記錄，供資安追查
CREATE TABLE dbo.LoginHistories (
    HistoryID   int IDENTITY(1,1) NOT NULL,
    MemberID    int           NOT NULL,
    LoginIP     varchar(50)   NOT NULL,
    IsSuccess   bit           NOT NULL,
    UserAgent   nvarchar(255) NULL,
    AttemptedAt datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_LoginHistories PRIMARY KEY (HistoryID),
    CONSTRAINT FK_LoginHistories_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 3. 外部登入綁定表：Google/FB/Line 第三方登入憑證
CREATE TABLE dbo.ExternalLogins (
    LoginID       int IDENTITY(1,1) NOT NULL,
    MemberID      int           NOT NULL,
    LoginProvider nvarchar(50)  NOT NULL,
    ProviderKey   nvarchar(255) NOT NULL,
    CreatedAt     datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_ExternalLogins PRIMARY KEY (LoginID),
    CONSTRAINT FK_ExternalLogins_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 4. 會員驗證碼表：Email 驗證、忘記密碼等一次性驗證碼/Token
CREATE TABLE dbo.MemberTokens (
    TokenID       int IDENTITY(1,1) NOT NULL,
    MemberID      int           NOT NULL,
    LoginProvider nvarchar(50)  NOT NULL,
    Name          nvarchar(50)  NOT NULL,   -- 驗證用途，如 EmailConfirmation
    Value         nvarchar(max) NOT NULL,
    ExpiryTime    datetime      NULL,
    CreatedAt     datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_MemberTokens PRIMARY KEY (TokenID),
    CONSTRAINT FK_MemberTokens_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 5. 檢舉與審核表：可檢舉會員/文章/揪團/留言，用 ReportType+TargetID 找目標
CREATE TABLE dbo.Reports (
    ReportID         int IDENTITY(1,1) NOT NULL,
    ReporterID       int           NOT NULL,
    ReportedMemberID int           NULL,
    ReportType       tinyint       NOT NULL,   -- 1:會員 2:Vlog文章 3:論壇貼文 4:揪團 5:留言
    TargetID         int           NULL,       -- 依 ReportType 決定要去哪張表查
    Reason           nvarchar(500) NOT NULL,
    ReportStatus     tinyint       NOT NULL DEFAULT 0,  -- 0:待處理 1:已處分 2:退回
    AdminNotes       nvarchar(500) NULL,
    CreatedAt        datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Reports PRIMARY KEY (ReportID),
    CONSTRAINT FK_Reports_Reporter FOREIGN KEY (ReporterID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_Reports_ReportedMember FOREIGN KEY (ReportedMemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 6. 通知紀錄表
CREATE TABLE dbo.Notifications (
    NotificationID int IDENTITY(1,1) NOT NULL,
    MemberID       int           NOT NULL,
    Type           tinyint       NOT NULL,   -- 1:系統 2:揪團 3:檢舉
    RelatedID      int           NULL,       -- 點擊通知要跳轉去的目標 ID
    Content        nvarchar(255) NOT NULL,
    IsRead         bit           NOT NULL DEFAULT 0,
    CreatedAt      datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationID),
    CONSTRAINT FK_Notifications_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

/* ============================================================
   模組 B：揪團與社群互動 (Travel Groups & Social)
   ============================================================ */

-- 7. 揪團資料主表
CREATE TABLE dbo.TravelGroups (
    GroupID       int IDENTITY(1,1) NOT NULL,
    OwnerMemberID int            NOT NULL,
    GroupTitle    nvarchar(100)  NOT NULL,
    Description   nvarchar(1000) NULL,
    StartDate     date           NULL,
    EndDate       date           NULL,
    MinPeople     int            NOT NULL DEFAULT 2,
    MaxPeople     int            NOT NULL DEFAULT 10,
    CurrentPeople int            NOT NULL DEFAULT 1,
    JoinRule      tinyint        NOT NULL DEFAULT 1,   -- 0:直接加入 1:需審核
    GroupStatus   tinyint        NOT NULL DEFAULT 0,   -- 0:等待中 1:成行/進行中 2:結束
    IsPublic      bit            NOT NULL DEFAULT 1,
    CreatedAt     datetime       NOT NULL DEFAULT GETDATE(),
    UpdatedAt     datetime       NOT NULL DEFAULT GETDATE(),
    IsDelete      bit            NOT NULL DEFAULT 0,
    ReviewStatus  nvarchar(30)   NOT NULL DEFAULT N'正常',  -- 檢舉審核狀態；規格書未給預設值，沿用既有慣例
    Country       nvarchar(50)   NOT NULL,               -- v2 新增：國家
    Region        nvarchar(100)  NOT NULL,               -- v2 新增：國家下的地區
    CONSTRAINT PK_TravelGroups PRIMARY KEY (GroupID),
    CONSTRAINT FK_TravelGroups_OwnerMember FOREIGN KEY (OwnerMemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 8. 揪團成員明細表
CREATE TABLE dbo.GroupMembers (
    GroupMemberID     int IDENTITY(1,1) NOT NULL,
    GroupID           int           NOT NULL,
    MemberID          int           NOT NULL,
    MemberRole        tinyint       NOT NULL DEFAULT 0,   -- 0:一般成員 1:團主
    JoinedAt          datetime      NOT NULL DEFAULT GETDATE(),
    LeftAt            datetime      NULL,
    IsRemoved         bit           NOT NULL DEFAULT 0,
    RemovedByMemberID int           NULL,
    RemovedAt         datetime      NULL,
    RemoveReason      nvarchar(300) NULL,
    CreatedAt         datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_GroupMembers PRIMARY KEY (GroupMemberID),
    CONSTRAINT FK_GroupMembers_Group FOREIGN KEY (GroupID) REFERENCES dbo.TravelGroups(GroupID),
    CONSTRAINT FK_GroupMembers_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_GroupMembers_RemovedByMember FOREIGN KEY (RemovedByMemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 9A. 加入揪團申請表
CREATE TABLE dbo.JoinRequests (
    RequestID          int IDENTITY(1,1) NOT NULL,
    GroupID            int           NOT NULL,
    MemberID           int           NOT NULL,
    Message            nvarchar(500) NULL,
    RequestStatus      tinyint       NOT NULL DEFAULT 0,  -- 0:待審核 1:已核准 2:已拒絕
    ReviewedByMemberID int           NULL,
    ReviewedAt         datetime      NULL,
    CreatedAt          datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_JoinRequests PRIMARY KEY (RequestID),
    CONSTRAINT FK_JoinRequests_Group FOREIGN KEY (GroupID) REFERENCES dbo.TravelGroups(GroupID),
    CONSTRAINT FK_JoinRequests_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_JoinRequests_ReviewedByMember FOREIGN KEY (ReviewedByMemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 9B. 揪團異動紀錄表：誰在什麼時間把哪個欄位從舊值改成新值（稽核用）
CREATE TABLE dbo.TravelGroupsLog (
    LogID            int IDENTITY(1,1) NOT NULL,
    GroupID          int           NOT NULL,
    ChangeByMemberID int           NOT NULL,
    FieldName        nvarchar(50)  NOT NULL,
    OldValue         nvarchar(300) NOT NULL,
    NewValue         nvarchar(300) NOT NULL,
    ChangeType       nvarchar(30)  NOT NULL,   -- 例：更新、下架、審核
    Remark           nvarchar(300) NULL,
    CreatedAt        datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_TravelGroupsLog PRIMARY KEY (LogID),
    CONSTRAINT FK_TravelGroupsLog_Group FOREIGN KEY (GroupID) REFERENCES dbo.TravelGroups(GroupID),
    -- 規格書原文未標註 FK，但邏輯上就是操作者會員，這裡補上約束避免髒資料
    CONSTRAINT FK_TravelGroupsLog_ChangeByMember FOREIGN KEY (ChangeByMemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 10. 單向追蹤表
CREATE TABLE dbo.Follows (
    FollowID   int IDENTITY(1,1) NOT NULL,
    FollowerID int      NOT NULL,   -- 粉絲（追蹤者）
    FolloweeID int      NOT NULL,   -- 被追蹤者
    Status     tinyint  NOT NULL DEFAULT 1,  -- 0:待核准 1:已追蹤
    CreatedAt  datetime NOT NULL DEFAULT GETDATE(),
    UpdatedAt  datetime NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Follows PRIMARY KEY (FollowID),
    CONSTRAINT FK_Follows_Follower FOREIGN KEY (FollowerID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_Follows_Followee FOREIGN KEY (FolloweeID) REFERENCES dbo.Members(MemberID)
);
GO

-- 11A. 好友邀請表
CREATE TABLE dbo.FriendRequests (
    RequestID     int IDENTITY(1,1) NOT NULL,
    RequesterID   int           NOT NULL,
    ReceiverID    int           NOT NULL,
    Message       nvarchar(300) NULL,
    RequestStatus tinyint       NOT NULL DEFAULT 0,  -- 0:待回覆 1:已接受 2:已拒絕
    CreatedAt     datetime      NOT NULL DEFAULT GETDATE(),
    ReviewedAt    datetime      NULL,
    CONSTRAINT PK_FriendRequests PRIMARY KEY (RequestID),
    CONSTRAINT FK_FriendRequests_Requester FOREIGN KEY (RequesterID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_FriendRequests_Receiver FOREIGN KEY (ReceiverID) REFERENCES dbo.Members(MemberID)
);
GO

-- 11B. 好友關係表：只存已互相同意的雙向好友，封鎖邏輯獨立在 Blocks 表
CREATE TABLE dbo.Friendships (
    FriendshipID int IDENTITY(1,1) NOT NULL,
    MemberID1    int      NOT NULL,   -- ID 較小者
    MemberID2    int      NOT NULL,   -- ID 較大者
    CreatedAt    datetime NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Friendships PRIMARY KEY (FriendshipID),
    CONSTRAINT FK_Friendships_Member1 FOREIGN KEY (MemberID1) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_Friendships_Member2 FOREIGN KEY (MemberID2) REFERENCES dbo.Members(MemberID)
);
GO

-- 12. 封鎖名單表：不受好友關係限制，任何人都能封鎖任何人，複合主鍵防重複封鎖
CREATE TABLE dbo.Blocks (
    BlockerID int      NOT NULL,
    BlockedID int      NOT NULL,
    CreatedAt datetime NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Blocks PRIMARY KEY (BlockerID, BlockedID),
    CONSTRAINT FK_Blocks_Blocker FOREIGN KEY (BlockerID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_Blocks_Blocked FOREIGN KEY (BlockedID) REFERENCES dbo.Members(MemberID)
);
GO

/* ============================================================
   模組 C：旅遊行程分享 (Travel Itineraries)
   ============================================================ */

-- 13. 旅遊行程文章主檔
CREATE TABLE dbo.VlogPosts (
    PostID       int IDENTITY(1,1) NOT NULL,
    MemberID     int           NOT NULL,
    Title        nvarchar(150) NOT NULL,
    MediaUrl     nvarchar(500) NULL,
    MediaType    tinyint       NOT NULL DEFAULT 0,   -- 0:圖片 1:影片
    Content      nvarchar(max) NULL,
    Destination  nvarchar(100) NOT NULL,
    TravelDays   int           NOT NULL DEFAULT 1,
    Status       tinyint       NOT NULL DEFAULT 0,   -- 0:草稿 1:發布
    CreatedAt    datetime      NOT NULL DEFAULT GETDATE(),
    UpdatedAt    datetime      NULL,
    TravelDate   datetime      NULL,                 -- v2 新增：旅遊日期
    TravelPeople nvarchar(50)  NOT NULL,              -- v2 新增：旅遊人數
    IsDelete     bit           NOT NULL DEFAULT 0,
    CONSTRAINT PK_VlogPosts PRIMARY KEY (PostID),
    CONSTRAINT FK_VlogPosts_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 14. 每日行程節點明細表
CREATE TABLE dbo.ItineraryNodes (
    NodeID        int IDENTITY(1,1) NOT NULL,
    PostID        int           NOT NULL,
    DayNumber     int           NOT NULL,
    LocationName  nvarchar(100) NOT NULL,
    ArrivalTime   time          NULL,
    StayTime      nvarchar(50)  NULL,
    DepartureTime time          NULL,
    MediaUrl      nvarchar(500) NULL,
    MediaType     tinyint       NOT NULL DEFAULT 0,
    Description   nvarchar(max) NULL,
    Remarks       nvarchar(max) NULL,
    CONSTRAINT PK_ItineraryNodes PRIMARY KEY (NodeID),
    CONSTRAINT FK_ItineraryNodes_Post FOREIGN KEY (PostID) REFERENCES dbo.VlogPosts(PostID)
);
GO

-- 15. 按讚與收藏互動表：複合主鍵(文章+會員+動作)從資料庫層防止重複按讚/收藏
CREATE TABLE dbo.PostInteractions (
    PostID     int      NOT NULL,
    MemberID   int      NOT NULL,
    ActionType tinyint  NOT NULL,   -- 1:按讚 2:收藏
    CreatedAt  datetime NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_PostInteractions PRIMARY KEY (PostID, MemberID, ActionType),
    CONSTRAINT FK_PostInteractions_Post FOREIGN KEY (PostID) REFERENCES dbo.VlogPosts(PostID),
    CONSTRAINT FK_PostInteractions_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

/* ============================================================
   模組 D：期末擴充功能區 (Extensions)
   ============================================================ */

-- 16A. 花費紀錄主表（分帳系統）
CREATE TABLE dbo.Expenses (
    ExpenseID   int IDENTITY(1,1) NOT NULL,
    GroupID     int            NOT NULL,
    PayerID     int            NOT NULL,   -- 先代墊/付款者
    Title       nvarchar(100)  NOT NULL,
    Amount      decimal(18,2)  NOT NULL,
    ExpenseDate date           NOT NULL,
    CreatedAt   datetime       NOT NULL DEFAULT GETDATE(),
    UpdatedAt   datetime       NULL,
    IsDelete    bit            NOT NULL DEFAULT 0,
    CONSTRAINT PK_Expenses PRIMARY KEY (ExpenseID),
    CONSTRAINT FK_Expenses_Group FOREIGN KEY (GroupID) REFERENCES dbo.TravelGroups(GroupID),
    CONSTRAINT FK_Expenses_Payer FOREIGN KEY (PayerID) REFERENCES dbo.Members(MemberID)
);
GO

-- 16B. 分攤明細表
CREATE TABLE dbo.ExpenseSplits (
    SplitID   int IDENTITY(1,1) NOT NULL,
    ExpenseID int           NOT NULL,
    MemberID  int           NOT NULL,
    OweAmount decimal(18,2) NOT NULL,
    IsPaid    bit           NOT NULL DEFAULT 0,
    CONSTRAINT PK_ExpenseSplits PRIMARY KEY (SplitID),
    CONSTRAINT FK_ExpenseSplits_Expense FOREIGN KEY (ExpenseID) REFERENCES dbo.Expenses(ExpenseID),
    CONSTRAINT FK_ExpenseSplits_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 17A. 論壇文章主檔
CREATE TABLE dbo.ForumPosts (
    ForumPostID int IDENTITY(1,1) NOT NULL,
    MemberID    int           NOT NULL,
    Category    int           NOT NULL,   -- 主題分類 Enum
    Title       nvarchar(150) NOT NULL,
    Content     nvarchar(max) NOT NULL,
    IsPinned    bit           NOT NULL DEFAULT 0,
    CreatedAt   datetime      NOT NULL DEFAULT GETDATE(),
    UpdatedAt   datetime      NULL,
    IsDelete    bit           NOT NULL DEFAULT 0,
    CONSTRAINT PK_ForumPosts PRIMARY KEY (ForumPostID),
    CONSTRAINT FK_ForumPosts_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 17B. 論壇圖片表
CREATE TABLE dbo.ForumImages (
    ImageID     int IDENTITY(1,1) NOT NULL,
    ForumPostID int           NOT NULL,
    ImageUrl    nvarchar(500) NOT NULL,
    SortOrder   int           NOT NULL DEFAULT 0,
    CONSTRAINT PK_ForumImages PRIMARY KEY (ImageID),
    CONSTRAINT FK_ForumImages_Post FOREIGN KEY (ForumPostID) REFERENCES dbo.ForumPosts(ForumPostID)
);
GO

-- 17C. 論壇留言表
CREATE TABLE dbo.ForumComments (
    CommentID   int IDENTITY(1,1) NOT NULL,
    ForumPostID int           NOT NULL,
    MemberID    int           NOT NULL,
    Content     nvarchar(500) NOT NULL,
    CreatedAt   datetime      NOT NULL DEFAULT GETDATE(),
    UpdatedAt   datetime      NULL,
    IsDelete    bit           NOT NULL DEFAULT 0,
    CONSTRAINT PK_ForumComments PRIMARY KEY (CommentID),
    CONSTRAINT FK_ForumComments_Post FOREIGN KEY (ForumPostID) REFERENCES dbo.ForumPosts(ForumPostID),
    CONSTRAINT FK_ForumComments_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 17D. 論壇互動表（按讚/收藏），複合主鍵防重複
CREATE TABLE dbo.ForumInteracts (
    ForumPostID int      NOT NULL,
    MemberID    int      NOT NULL,
    ActionType  tinyint  NOT NULL,   -- 1:按讚 2:收藏
    CreatedAt   datetime NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_ForumInteracts PRIMARY KEY (ForumPostID, MemberID, ActionType),
    CONSTRAINT FK_ForumInteracts_Post FOREIGN KEY (ForumPostID) REFERENCES dbo.ForumPosts(ForumPostID),
    CONSTRAINT FK_ForumInteracts_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID)
);
GO

-- 18A. 訂閱方案表
CREATE TABLE dbo.SubscriptionPlans (
    PlanID       int IDENTITY(1,1) NOT NULL,
    PlanName     nvarchar(50)  NOT NULL,
    Price        decimal(10,2) NOT NULL DEFAULT 0,
    DurationDays int           NOT NULL DEFAULT 0,
    Description  nvarchar(300) NULL,
    IsActive     bit           NOT NULL DEFAULT 1,
    CONSTRAINT PK_SubscriptionPlans PRIMARY KEY (PlanID)
);
GO

-- 18B. 會員訂閱表
CREATE TABLE dbo.MemberSubscriptions (
    SubscriptionID int      IDENTITY(1,1) NOT NULL,
    MemberID       int      NOT NULL,
    PlanID         int      NOT NULL,
    StartDate      datetime NOT NULL,
    EndDate        datetime NOT NULL,
    Status         int      NOT NULL DEFAULT 0,
    CONSTRAINT PK_MemberSubscriptions PRIMARY KEY (SubscriptionID),
    CONSTRAINT FK_MemberSubscriptions_Member FOREIGN KEY (MemberID) REFERENCES dbo.Members(MemberID),
    CONSTRAINT FK_MemberSubscriptions_Plan FOREIGN KEY (PlanID) REFERENCES dbo.SubscriptionPlans(PlanID)
);
GO

-- 18C. 管理員操作紀錄表（舊版，AdminID 直接關聯 Members）
-- 對應 REPORTS_HANDOFF.md 裡目前 AdminLogService 已經在用的設計，先保留相容
-- 之後若正式切換到模組 E 的 Employees/AdminAuditLogs，這張表可以停用
CREATE TABLE dbo.AdminLogs (
    LogID       int IDENTITY(1,1) NOT NULL,
    AdminID     int           NOT NULL,
    Action      nvarchar(100) NOT NULL,
    TargetTable nvarchar(50)  NOT NULL,
    TargetID    int           NULL,
    Description nvarchar(300) NULL,
    IPAddress   varchar(50)   NULL,
    CreatedAt   datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_AdminLogs PRIMARY KEY (LogID),
    CONSTRAINT FK_AdminLogs_Admin FOREIGN KEY (AdminID) REFERENCES dbo.Members(MemberID)
);
GO

/* ============================================================
   模組 E：企業級後台員工與權限管理 (Enterprise RBAC)
   取代原本依附在 Members 上的管理員設計，後台帳號跟消費者會員完全分離
   ============================================================ */

-- 19. 員工主表：後台系統獨立帳號
CREATE TABLE dbo.Employees (
    EmployeeID   int IDENTITY(1,1) NOT NULL,
    EmployeeNo   varchar(20)   NOT NULL,   -- 工號，如 EMP-001
    Email        nvarchar(100) NOT NULL,   -- 公司信箱，登入帳號
    PasswordHash nvarchar(255) NOT NULL,
    Name         nvarchar(50)  NOT NULL,
    Department   nvarchar(50)  NOT NULL,
    Status       tinyint       NOT NULL DEFAULT 1,  -- 1:正常在職 2:停權/離職
    LastLoginAt  datetime      NULL,
    CreatedAt    datetime      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Employees PRIMARY KEY (EmployeeID),
    CONSTRAINT UQ_Employees_Email UNIQUE (Email)
);
GO

-- 20. 角色表：職務角色，如客服主管、內容審核員
CREATE TABLE dbo.Roles (
    RoleID      int IDENTITY(1,1) NOT NULL,
    RoleCode    varchar(50)   NOT NULL,   -- 如 ROLE_CS_MANAGER
    RoleName    nvarchar(50)  NOT NULL,
    Description nvarchar(200) NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleID),
    CONSTRAINT UQ_Roles_RoleCode UNIQUE (RoleCode)  -- 角色代碼作為字典表，須唯一
);
GO

-- 21. 權限字典表：所有細粒度權限，格式 Resource:Action
CREATE TABLE dbo.Permissions (
    PermissionID   int IDENTITY(1,1) NOT NULL,
    PermissionCode varchar(100)  NOT NULL,   -- 如 member:pii:unmask
    ModuleName     nvarchar(50)  NOT NULL,
    Description    nvarchar(200) NULL,
    CONSTRAINT PK_Permissions PRIMARY KEY (PermissionID),
    CONSTRAINT UQ_Permissions_PermissionCode UNIQUE (PermissionCode)  -- 權限代碼作為字典表，須唯一
);
GO

-- 22. 角色與權限關聯表（多對多）
CREATE TABLE dbo.RolePermissions (
    RoleID       int NOT NULL,
    PermissionID int NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleID, PermissionID),
    CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID),
    CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionID) REFERENCES dbo.Permissions(PermissionID)
);
GO

-- 23. 員工與角色關聯表（多對多，一位員工可兼任多職）
CREATE TABLE dbo.EmployeeRoles (
    EmployeeID int      NOT NULL,
    RoleID     int      NOT NULL,
    GrantedAt  datetime NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_EmployeeRoles PRIMARY KEY (EmployeeID, RoleID),
    CONSTRAINT FK_EmployeeRoles_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employees(EmployeeID),
    CONSTRAINT FK_EmployeeRoles_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
);
GO

-- 24. 後台稽核日誌：取代 AdminLogs，關聯 Employees 而非 Members
CREATE TABLE dbo.AdminAuditLogs (
    LogID          bigint IDENTITY(1,1) NOT NULL,
    EmployeeID     int          NOT NULL,
    Action         varchar(100) NOT NULL,
    TargetResource varchar(50)  NOT NULL,
    TargetID       varchar(50)  NULL,
    IPAddress      varchar(50)  NOT NULL,
    CreatedAt      datetime     NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_AdminAuditLogs PRIMARY KEY (LogID),
    CONSTRAINT FK_AdminAuditLogs_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employees(EmployeeID)
);
GO

PRINT N'LazyTravelDB 建置完成：共 32 張資料表（模組 A~E）。';
GO
