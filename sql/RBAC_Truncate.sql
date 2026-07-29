/* ============================================================
   淨空 RBAC 五張表：EmployeeRoles / Employees / RolePermissions / Roles / Permissions
   準備重新植入新資料。

   刪除順序照 FK 由子到父。除了這五張，Employees 還被另外兩張表指著：
     AdminAuditLogs.EmployeeID       → NOT NULL，只能連同刪掉（後台稽核紀錄）
     TravelGroupsLog.ChangeByMemberID → 可為 NULL，改成 SET NULL 保留旅遊團異動紀錄本體

   ⚠ 這會刪掉所有後台員工帳號，包含你自己的登入帳號，
     新資料植入前完全無法登入後台。

   ⚠ dbo.AdminLogs（操作紀錄）不受影響，它的 AdminID 指向 Members 不是 Employees。

   IDENTITY 一併重設，新資料的 ID 從 1 開始。
   ============================================================ */

BEGIN TRAN;

/* --- 1. 擋路的外部參考 ---
   TravelGroupsLog.ChangeByMemberID 資料庫端是 NOT NULL，但 EF 模型
   (TravelGroupsLog.ChangedByEmployeeId) 宣告成 int?，兩邊本來就不一致。
   這裡照 EF 的定義把欄位改成可為 NULL：既解開 FK 的鎖，也順手修掉這個落差。
   欄位被 FK 指著不能直接 ALTER，要先卸掉 FK、改完再掛回去。 */
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = N'FK_TravelGroupsLog_ChangeByEmployee'
             AND parent_object_id = OBJECT_ID(N'dbo.TravelGroupsLog'))
    ALTER TABLE dbo.TravelGroupsLog DROP CONSTRAINT FK_TravelGroupsLog_ChangeByEmployee;

ALTER TABLE dbo.TravelGroupsLog ALTER COLUMN ChangeByMemberID int NULL;

UPDATE dbo.TravelGroupsLog SET ChangeByMemberID = NULL WHERE ChangeByMemberID IS NOT NULL;

ALTER TABLE dbo.TravelGroupsLog WITH CHECK
ADD CONSTRAINT FK_TravelGroupsLog_ChangeByEmployee
FOREIGN KEY (ChangeByMemberID) REFERENCES dbo.Employees(EmployeeID);

DELETE FROM dbo.AdminAuditLogs;

/* --- 2. 五張表，子表先刪 --- */
DELETE FROM dbo.EmployeeRoles;
DELETE FROM dbo.RolePermissions;
DELETE FROM dbo.Employees;
DELETE FROM dbo.Roles;
DELETE FROM dbo.Permissions;

/* --- 3. 重設 IDENTITY（中介表是複合主鍵，沒有 IDENTITY） --- */
DBCC CHECKIDENT ('dbo.Employees',      RESEED, 0);
DBCC CHECKIDENT ('dbo.Roles',          RESEED, 0);
DBCC CHECKIDENT ('dbo.Permissions',    RESEED, 0);
DBCC CHECKIDENT ('dbo.AdminAuditLogs', RESEED, 0);

/* --- 4. 確認都空了再 COMMIT --- */
SELECT 'EmployeeRoles'   AS 資料表, COUNT(*) AS 筆數 FROM dbo.EmployeeRoles
UNION ALL SELECT 'Employees',       COUNT(*) FROM dbo.Employees
UNION ALL SELECT 'RolePermissions', COUNT(*) FROM dbo.RolePermissions
UNION ALL SELECT 'Roles',           COUNT(*) FROM dbo.Roles
UNION ALL SELECT 'Permissions',     COUNT(*) FROM dbo.Permissions
UNION ALL SELECT 'AdminAuditLogs',  COUNT(*) FROM dbo.AdminAuditLogs;

COMMIT;
-- 結果不對就改跑 ROLLBACK;
