/* ============================================================
   LazyTravel 資料庫 v2 增量更新腳本
   目的：把目前 SQL2025\LazyTravelDB 的實際結構，補齊到
        LazyTravel_DB.docx 規格書 v2 的樣子。

   跟 LazyTravelDB_Build.sql（整份重建）不一樣，這份「只新增」：
     1. TravelGroups 補 3 個 v2 新欄位（ReviewStatus / Country / Region）
     2. VlogPosts   補 2 個 v2 新欄位（TravelDate / TravelPeople，
        TravelPeople 依最新規格書是 tinyint，不是文字）
     3. 新增 TravelGroupsLog（揪團異動紀錄表）
     4. 新增模組 E 企業級 RBAC 6 張表：
        Employees / Roles / Permissions / RolePermissions /
        EmployeeRoles / AdminAuditLogs

   不會動到：AdminLogs、AdminPermissions、Members 等既有表的既有欄位，
   現有資料、現有功能都不受影響（TravelGroups / VlogPosts 目前都是 0 筆，
   補 NOT NULL 欄位不會有舊資料衝突的問題）。
   ============================================================ */

USE LazyTravelDB;
GO

-- 1. TravelGroups 補 v2 新欄位
IF COL_LENGTH('dbo.TravelGroups', 'ReviewStatus') IS NULL
    ALTER TABLE dbo.TravelGroups ADD ReviewStatus nvarchar(30) NOT NULL CONSTRAINT DF_TravelGroups_ReviewStatus DEFAULT (N'正常');
GO
IF COL_LENGTH('dbo.TravelGroups', 'Country') IS NULL
    ALTER TABLE dbo.TravelGroups ADD Country nvarchar(50) NOT NULL DEFAULT (N'');
GO
IF COL_LENGTH('dbo.TravelGroups', 'Region') IS NULL
    ALTER TABLE dbo.TravelGroups ADD Region nvarchar(100) NOT NULL DEFAULT (N'');
GO

-- 2. VlogPosts 補 v2 新欄位
IF COL_LENGTH('dbo.VlogPosts', 'TravelDate') IS NULL
    ALTER TABLE dbo.VlogPosts ADD TravelDate datetime NULL;
GO
IF COL_LENGTH('dbo.VlogPosts', 'TravelPeople') IS NULL
    ALTER TABLE dbo.VlogPosts ADD TravelPeople tinyint NOT NULL DEFAULT (0);
GO

-- 3. 揪團異動紀錄表
IF OBJECT_ID('dbo.TravelGroupsLog', 'U') IS NULL
BEGIN
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
        CONSTRAINT FK_TravelGroupsLog_ChangeByMember FOREIGN KEY (ChangeByMemberID) REFERENCES dbo.Members(MemberID)
    );
END
GO

-- 4. 模組 E：企業級後台員工與權限管理 (Enterprise RBAC)
IF OBJECT_ID('dbo.Employees', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees (
        EmployeeID   int IDENTITY(1,1) NOT NULL,
        EmployeeNo   varchar(20)   NOT NULL,
        Email        nvarchar(100) NOT NULL,
        PasswordHash nvarchar(255) NOT NULL,
        Name         nvarchar(50)  NOT NULL,
        Department   nvarchar(50)  NOT NULL,
        Status       tinyint       NOT NULL DEFAULT 1,
        LastLoginAt  datetime      NULL,
        CreatedAt    datetime      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_Employees PRIMARY KEY (EmployeeID),
        CONSTRAINT UQ_Employees_Email UNIQUE (Email)
    );
END
GO

IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleID      int IDENTITY(1,1) NOT NULL,
        RoleCode    varchar(50)   NOT NULL,
        RoleName    nvarchar(50)  NOT NULL,
        Description nvarchar(200) NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RoleID),
        CONSTRAINT UQ_Roles_RoleCode UNIQUE (RoleCode)
    );
END
GO

IF OBJECT_ID('dbo.Permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions (
        PermissionID   int IDENTITY(1,1) NOT NULL,
        PermissionCode varchar(100)  NOT NULL,
        ModuleName     nvarchar(50)  NOT NULL,
        Description    nvarchar(200) NULL,
        CONSTRAINT PK_Permissions PRIMARY KEY (PermissionID),
        CONSTRAINT UQ_Permissions_PermissionCode UNIQUE (PermissionCode)
    );
END
GO

IF OBJECT_ID('dbo.RolePermissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions (
        RoleID       int NOT NULL,
        PermissionID int NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleID, PermissionID),
        CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID),
        CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionID) REFERENCES dbo.Permissions(PermissionID)
    );
END
GO

IF OBJECT_ID('dbo.EmployeeRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeRoles (
        EmployeeID int      NOT NULL,
        RoleID     int      NOT NULL,
        GrantedAt  datetime NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_EmployeeRoles PRIMARY KEY (EmployeeID, RoleID),
        CONSTRAINT FK_EmployeeRoles_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employees(EmployeeID),
        CONSTRAINT FK_EmployeeRoles_Role FOREIGN KEY (RoleID) REFERENCES dbo.Roles(RoleID)
    );
END
GO

IF OBJECT_ID('dbo.AdminAuditLogs', 'U') IS NULL
BEGIN
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
END
GO

PRINT N'v2 增量更新完成：TravelGroups/VlogPosts 補欄位，新增 TravelGroupsLog + RBAC 6 張表。';
GO
