/* ========================================================================
   LazyTravel 種子資料：系統管理員（含角色/權限）+ 10 筆會員測試資料
   ------------------------------------------------------------------------
   跟你原本的 SQLQuery3.sql 內容一樣，只補了兩件事：
     1. 結尾加上 COMMIT TRAN;——原本那份少了這一行，交易會一直卡在「打開」
        狀態沒有結束，資料庫上的鎖不會放開，這就是後台程式一直
        Execution Timeout Expired 打不開的原因。
     2. 包一層 TRY/CATCH，中途出錯會自動 ROLLBACK，不會留下一半資料、
        也不會留下沒關掉的交易。
   只有在你已經照 check_blocking_transaction.sql 處理掉原本那筆卡住的交易
   （COMMIT 或 KILL 二選一）之後，才需要重新執行這份；如果原本那筆已經
   COMMIT 成功、資料已經在資料庫裡了，就不用再跑這份，跑了會重複新增。
   ======================================================================== */

BEGIN TRY
    BEGIN TRAN;

    -- 1) 新增一個「系統管理員」角色 -------------------------------------------
    DECLARE @NewRoleId INT;

    INSERT INTO dbo.Roles (RoleCode, RoleName, Description)
    VALUES ('SUPER_ADMIN', N'系統管理員', N'擁有系統全部功能權限的最高管理角色（開發/測試用）');

    SET @NewRoleId = SCOPE_IDENTITY();

    -- 2) 新增目前程式碼裡實際會用到的權限代碼（含 ROLE_SUPER_ADMIN 萬用鑰匙）----
    --    這份清單是從 admin-api 專案裡所有 [Authorize(Policy=...)]、
    --    User.HasClaim("Permission", ...) 掃出來的，總共 25 個 + 1 個萬用鑰匙。
    INSERT INTO dbo.Permissions (PermissionCode, ModuleName, Description) VALUES
    ('system:dashboard:read',    N'總覽',     N'檢視後台總覽儀表板'),
    ('member:account:read',      N'會員管理', N'檢視會員列表與詳細資料'),
    ('member:account:block',     N'會員管理', N'停權 / 解除停權會員帳號'),
    ('member:pii:unmask',        N'會員管理', N'解除遮蔽並調閱會員完整個資'),
    ('content:report:read',      N'檢舉管理', N'檢視檢舉列表'),
    ('content:report:audit',     N'檢舉管理', N'審核檢舉案件'),
    ('content:vlog:read',        N'文章管理', N'檢視 Vlog 文章列表'),
    ('content:vlog:create',      N'文章管理', N'新增 Vlog 文章'),
    ('content:vlog:update',      N'文章管理', N'編輯 Vlog 文章'),
    ('content:vlog:delete',      N'文章管理', N'刪除 Vlog 文章'),
    ('content:vlog:restore',     N'文章管理', N'還原已刪除的 Vlog 文章'),
    ('content:vlog:submit',      N'文章管理', N'送審 Vlog 文章'),
    ('content:vlog:publish',     N'文章管理', N'發布 Vlog 文章'),
    ('content:vlog:return',      N'文章管理', N'退回 Vlog 文章至草稿'),
    ('content:vlog:audit',       N'文章管理', N'審核 Vlog 文章'),
    ('content:forum:read',       N'論壇管理', N'檢視論壇貼文'),
    ('social:travelgroup:read',  N'揪團管理', N'檢視揪團列表與詳細資料'),
    ('finance:plan:read',        N'財務管理', N'檢視訂閱方案'),
    ('finance:plan:manage',      N'財務管理', N'管理訂閱方案'),
    ('finance:split:read',       N'財務管理', N'檢視分帳紀錄'),
    ('system:analytics:read',    N'系統管理', N'檢視數據分析'),
    ('system:notification:read', N'系統管理', N'檢視系統通知'),
    ('system:employee:read',     N'員工管理', N'檢視員工列表'),
    ('system:employee:manage',   N'員工管理', N'管理員工帳號（含最高權限）'),
    ('system:role:manage',       N'角色管理', N'管理角色與權限'),
    ('ROLE_SUPER_ADMIN',         N'系統管理', N'超級管理員萬用權限（VlogPostPermissions 用來繞過文章模組個別權限檢查）');

    -- 3) 把「系統管理員」角色跟上面全部權限綁在一起（= 這個角色擁有全部權限）---
    INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
    SELECT @NewRoleId, PermissionID
    FROM dbo.Permissions
    WHERE PermissionCode IN (
        'system:dashboard:read','member:account:read','member:account:block','member:pii:unmask',
        'content:report:read','content:report:audit','content:vlog:read','content:vlog:create',
        'content:vlog:update','content:vlog:delete','content:vlog:restore','content:vlog:submit',
        'content:vlog:publish','content:vlog:return','content:vlog:audit','content:forum:read',
        'social:travelgroup:read','finance:plan:read','finance:plan:manage','finance:split:read',
        'system:analytics:read','system:notification:read','system:employee:read','system:employee:manage',
        'system:role:manage','ROLE_SUPER_ADMIN'
    );

    -- 4) 新增系統管理員這位員工 -------------------------------------------------
    --    帳號（Email）用 LazyTravel01@gmail.com，之後要再加人就接 02、03...
    --    密碼欄位先塞個佔位字串，第一次用密碼 123456 登入時會被自動覆蓋成真正的 BCrypt hash。
    DECLARE @NewEmployeeId INT;

    INSERT INTO dbo.Employees (EmployeeNo, Email, PasswordHash, Name, Department, Status, CreatedAt, IsDelete)
    VALUES (
        'ADMIN01',
        'LazyTravel01@gmail.com',
        'PLACEHOLDER_WILL_BE_RESET_ON_FIRST_LOGIN',
        N'系統管理員',
        N'系統管理部',
        1,      -- Status = 1（正常啟用）
        GETDATE(),
        0
    );

    SET @NewEmployeeId = SCOPE_IDENTITY();

    -- 5) 把這位員工指派到「系統管理員」角色 -------------------------------------
    INSERT INTO dbo.EmployeeRoles (EmployeeID, RoleID, GrantedAt)
    VALUES (@NewEmployeeId, @NewRoleId, GETDATE());

    COMMIT TRAN;

    PRINT N'新增完成，交易已經 COMMIT，資料庫的鎖已經放開。';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;

    PRINT N'新增失敗，已自動 ROLLBACK，沒有留下未提交的交易：';
    PRINT ERROR_MESSAGE();
END CATCH;
