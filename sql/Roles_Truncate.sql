/* ============================================================
   清空 Roles（連同兩張子表），準備重新植入新的角色資料

   Roles 被兩張表用 FK 指著，不先清子表 DELETE 會被擋：
     EmployeeRoles.RoleID   → 員工的角色指派
     RolePermissions.RoleID → 角色的權限對應

   ⚠ 清完之前所有員工都沒有任何角色 = 沒有任何權限,
     後台會全部 403,要等新的角色 + EmployeeRoles 植入才會恢復。

   IDENTITY 一併重設,新資料的 RoleID 會從 1 開始。
   ============================================================ */

BEGIN TRAN;

DELETE FROM dbo.EmployeeRoles;
DELETE FROM dbo.RolePermissions;
DELETE FROM dbo.Roles;

DBCC CHECKIDENT ('dbo.Roles', RESEED, 0);

-- 確認三張表都空了再 COMMIT
SELECT 'Roles' AS 資料表, COUNT(*) AS 筆數 FROM dbo.Roles
UNION ALL SELECT 'RolePermissions', COUNT(*) FROM dbo.RolePermissions
UNION ALL SELECT 'EmployeeRoles',   COUNT(*) FROM dbo.EmployeeRoles;

COMMIT;
-- 結果不對就改跑 ROLLBACK;
