/* ============================================================
   讓所有角色都能進「總覽」

   問題：DashboardController 掛 [Authorize(Policy="RequireDashboardRead")]，
   要的是 system:dashboard:read。目前只有 OWNER 有這筆，其他角色 403。

   總覽本身不含機密內容，卡片會再依各自權限過濾（見 DashboardController），
   所以這裡直接把 system:dashboard:read 發給全部角色。

   可重複執行。
   ============================================================ */

/* --- 0. 執行前：目前哪些角色能進總覽 --- */
SELECT r.RoleCode, r.RoleName,
       CASE WHEN EXISTS (
           SELECT 1 FROM dbo.RolePermissions rp
           JOIN dbo.Permissions p ON p.PermissionID = rp.PermissionID
           WHERE rp.RoleID = r.RoleID AND p.PermissionCode = 'system:dashboard:read'
       ) THEN 'OK' ELSE '' END AS 執行前_能進總覽
FROM dbo.Roles r
ORDER BY r.RoleCode;

/* --- 1. 權限字典缺這筆就補上 --- */
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE PermissionCode = 'system:dashboard:read')
    INSERT INTO dbo.Permissions (PermissionCode, ModuleName, Description)
    VALUES ('system:dashboard:read', N'系統', N'檢視總覽儀表板');

/* --- 2. 發給所有角色（已經有的跳過） --- */
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE p.PermissionCode = 'system:dashboard:read'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

/* --- 3. 執行後：每個員工實際拿到幾個權限、能不能進總覽 ---
   權限數 = 0 或 沒有角色 → 是 EmployeeRoles 沒指派，不是這支腳本的問題，
   要另外指派角色（或跑 RBAC_Seed.sql 第 4 段）。 */
SELECT e.EmployeeID, e.Email, e.Name,
       ISNULL(STUFF((SELECT ', ' + r2.RoleCode
                     FROM dbo.EmployeeRoles er2
                     JOIN dbo.Roles r2 ON r2.RoleID = er2.RoleID
                     WHERE er2.EmployeeID = e.EmployeeID
                     FOR XML PATH('')), 1, 2, ''), N'(沒有角色)') AS 角色,
       COUNT(DISTINCT p.PermissionCode) AS 權限數,
       MAX(CASE WHEN p.PermissionCode = 'system:dashboard:read' THEN 'OK' ELSE '' END) AS 能進總覽
FROM dbo.Employees e
LEFT JOIN dbo.EmployeeRoles er   ON er.EmployeeID = e.EmployeeID
LEFT JOIN dbo.RolePermissions rp ON rp.RoleID = er.RoleID
LEFT JOIN dbo.Permissions p      ON p.PermissionID = rp.PermissionID
GROUP BY e.EmployeeID, e.Email, e.Name
ORDER BY e.EmployeeID;
