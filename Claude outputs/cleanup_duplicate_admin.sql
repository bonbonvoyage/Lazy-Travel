/* ========================================================================
   清掉種子資料腳本不小心跑了兩次留下的重複資料：
     Employees（重複的系統管理員帳號）
     EmployeeRoles（重複帳號掛的職務）
     Roles（重複的 SUPER_ADMIN 角色）
     RolePermissions（重複角色掛的權限清單）
     Permissions（重複的權限代碼本身，例如 content:vlog:read 變成兩筆）
   ------------------------------------------------------------------------
   安全機制：每一步刪除前都先檢查「重複的形狀」符不符合預期，只要有一個條件
   對不上（例如重複的員工其實不只 2 筆、或者那個角色還有別的員工在用），
   就整個 ROLLBACK 什麼都不刪，並印出原因——不會因為我猜錯資料形狀就
   誤刪你正在用的那筆資料。
   結尾自己 COMMIT/ROLLBACK，不會像上次那樣留下一筆沒結束的交易卡住後台。
   ======================================================================== */

BEGIN TRY
    BEGIN TRAN;

    DECLARE @DupEmail NVARCHAR(200) = 'LazyTravel01@gmail.com';
    DECLARE @DupEmployeeId INT;
    DECLARE @DupRoleId INT;

    -- 檢查 1：這個 Email 必須剛好有 2 筆，多於或少於都不處理
    IF (SELECT COUNT(*) FROM dbo.Employees WHERE Email = @DupEmail) <> 2
    BEGIN
        RAISERROR(N'Employees 裡這個 Email 的筆數不是預期的 2 筆，為了安全不執行刪除，請人工確認後再處理。', 16, 1);
    END

    -- 要刪的是「從未登入」的那筆（保留有登入紀錄、畫面上標示「受保護帳號」的那筆）
    SELECT @DupEmployeeId = EmployeeID
    FROM dbo.Employees
    WHERE Email = @DupEmail AND LastLoginAt IS NULL;

    -- 檢查 2：一定要找得到「從未登入」的那筆，而且只有一筆
    IF @DupEmployeeId IS NULL OR (
        SELECT COUNT(*) FROM dbo.Employees WHERE Email = @DupEmail AND LastLoginAt IS NULL
    ) <> 1
    BEGIN
        RAISERROR(N'找不到唯一一筆「從未登入」的重複員工，為了安全不執行刪除，請人工確認後再處理。', 16, 1);
    END

    -- 這筆重複員工掛的角色
    SELECT @DupRoleId = RoleID FROM dbo.EmployeeRoles WHERE EmployeeID = @DupEmployeeId;

    IF @DupRoleId IS NULL
    BEGIN
        RAISERROR(N'這筆重複員工沒有掛任何角色，資料形狀跟預期不同，為了安全不執行刪除，請人工確認。', 16, 1);
    END

    -- 檢查 3：這個角色不能還有「別的」員工在用，不然就不是單純的重複角色，不能刪
    IF EXISTS (
        SELECT 1 FROM dbo.EmployeeRoles
        WHERE RoleID = @DupRoleId AND EmployeeID <> @DupEmployeeId
    )
    BEGIN
        RAISERROR(N'這個角色還有其他員工在使用，不是單純的重複角色，為了安全不執行刪除，請人工確認。', 16, 1);
    END

    PRINT N'確認過形狀沒問題，準備刪除 → EmployeeID=' + CAST(@DupEmployeeId AS NVARCHAR(10))
        + N'，RoleID=' + CAST(@DupRoleId AS NVARCHAR(10));

    -- 1) 刪掉這筆重複員工掛的職務指派
    DELETE FROM dbo.EmployeeRoles WHERE EmployeeID = @DupEmployeeId;

    -- 2) 刪掉這筆重複員工本身
    DELETE FROM dbo.Employees WHERE EmployeeID = @DupEmployeeId;

    -- 3) 刪掉這個重複角色掛的權限清單
    DELETE FROM dbo.RolePermissions WHERE RoleID = @DupRoleId;

    -- 4) 刪掉這個重複角色本身
    DELETE FROM dbo.Roles WHERE RoleID = @DupRoleId;

    -- 5) 清掉現在沒有任何角色在用、而且 PermissionCode 重複的多餘 Permissions
    --    （保留每個代碼裡 PermissionID 最小的那筆，且只刪「已經沒有任何 RolePermissions
    --    參考到」的那些，不會動到目前還在用的權限）
    ;WITH DupPermissions AS (
        SELECT PermissionID, PermissionCode,
               ROW_NUMBER() OVER (PARTITION BY PermissionCode ORDER BY PermissionID) AS rn
        FROM dbo.Permissions
    )
    DELETE p
    FROM dbo.Permissions p
    JOIN DupPermissions d ON d.PermissionID = p.PermissionID
    WHERE d.rn > 1
      AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.PermissionID = p.PermissionID);

    COMMIT TRAN;
    PRINT N'清理完成，已經 COMMIT，鎖已經放開。';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT N'沒有刪除任何資料（已經 ROLLBACK）。原因：';
    PRINT ERROR_MESSAGE();
END CATCH;
