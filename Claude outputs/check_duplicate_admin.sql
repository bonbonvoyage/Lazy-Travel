-- 純檢查用，不會修改任何資料，先執行這份看看重複的狀況長什麼樣子。

SELECT EmployeeID, EmployeeNo, Email, Status, LastLoginAt, CreatedAt, IsDelete
FROM dbo.Employees
WHERE Email = 'LazyTravel01@gmail.com';

SELECT r.RoleID, r.RoleCode, r.RoleName,
       (SELECT COUNT(*) FROM dbo.RolePermissions rp WHERE rp.RoleID = r.RoleID) AS 掛了幾個權限,
       (SELECT COUNT(*) FROM dbo.EmployeeRoles er WHERE er.RoleID = r.RoleID) AS 有幾個員工在用
FROM dbo.Roles r
WHERE r.RoleCode = 'SUPER_ADMIN';

SELECT PermissionCode, COUNT(*) AS 重複幾筆
FROM dbo.Permissions
GROUP BY PermissionCode
HAVING COUNT(*) > 1;
