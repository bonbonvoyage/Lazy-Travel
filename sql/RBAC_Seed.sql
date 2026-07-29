/* ============================================================
   後台 RBAC 完整 Seed（取代 RolePermissions_Seed.sql）

   為什麼需要：Permissions / Roles / EmployeeRoles 三張字典表沒有
   任何 seed，RolePermissions_Seed.sql 是 SELECT FROM Permissions，
   字典表空的就插 0 筆 → 登入拿不到 Permission claim → 進後台 403。

   可重複執行。全部用 Code 對應，不依賴 IDENTITY 產出的 ID。
   ============================================================ */

/* --- 1. 權限字典（18 個，對應 Program.cs 的 Policy） --- */
MERGE dbo.Permissions AS t
USING (VALUES
    ('system:dashboard:read',    N'系統',   N'檢視總覽儀表板'),
    ('system:notification:read', N'系統',   N'檢視通知中心'),
    ('system:analytics:read',    N'系統',   N'檢視數據分析'),
    ('system:employee:read',     N'系統',   N'檢視員工帳號'),
    ('system:employee:manage',   N'系統',   N'管理員工帳號'),
    ('system:role:manage',       N'系統',   N'管理角色與權限'),
    ('member:account:read',      N'會員',   N'檢視會員資料'),
    ('member:account:block',     N'會員',   N'停權/解除停權會員'),
    ('member:pii:unmask',        N'會員',   N'解遮罩個資'),
    ('content:forum:read',       N'內容',   N'檢視論壇文章'),
    ('content:vlog:read',        N'內容',   N'檢視 Vlog'),
    ('content:vlog:delete',      N'內容',   N'刪除 Vlog'),
    ('content:report:read',      N'內容',   N'檢視檢舉案件'),
    ('content:report:audit',     N'內容',   N'審核檢舉案件'),
    ('social:travelgroup:read',  N'社群',   N'檢視旅遊團'),
    ('social:travelgroup:manage',N'社群',   N'管理旅遊團'),
    ('finance:plan:read',        N'財務',   N'檢視行程預算'),
    ('finance:split:read',       N'財務',   N'檢視分帳紀錄')
) AS s (PermissionCode, ModuleName, Description)
ON t.PermissionCode = s.PermissionCode
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, ModuleName, Description)
    VALUES (s.PermissionCode, s.ModuleName, s.Description);

/* --- 2. 角色字典 --- */
MERGE dbo.Roles AS t
USING (VALUES
    ('OWNER',       N'系統擁有者', N'擁有全部權限'),
    ('CONTENT_MOD', N'內容審核',   N'論壇/Vlog/檢舉/旅遊團'),
    ('MEMBER_CS',   N'會員客服',   N'會員資料與停權'),
    ('FINANCE',     N'財務人員',   N'預算、分帳與數據'),
    ('SECURITY',    N'資安人員',   N'員工、角色與個資')
) AS s (RoleCode, RoleName, Description)
ON t.RoleCode = s.RoleCode
WHEN NOT MATCHED THEN
    INSERT (RoleCode, RoleName, Description)
    VALUES (s.RoleCode, s.RoleName, s.Description);

/* --- 3. 角色 → 權限 --- */
;WITH map AS (
    SELECT RoleCode, PermissionCode FROM (VALUES
        ('CONTENT_MOD','system:dashboard:read'),
        ('CONTENT_MOD','system:notification:read'),
        ('CONTENT_MOD','content:forum:read'),
        ('CONTENT_MOD','content:vlog:read'),
        ('CONTENT_MOD','content:vlog:delete'),
        ('CONTENT_MOD','content:report:read'),
        ('CONTENT_MOD','content:report:audit'),
        ('CONTENT_MOD','social:travelgroup:read'),
        ('CONTENT_MOD','social:travelgroup:manage'),
        ('MEMBER_CS','system:dashboard:read'),
        ('MEMBER_CS','system:notification:read'),
        ('MEMBER_CS','member:account:read'),
        ('MEMBER_CS','member:account:block'),
        ('MEMBER_CS','content:report:read'),
        ('FINANCE','system:dashboard:read'),
        ('FINANCE','system:notification:read'),
        ('FINANCE','system:analytics:read'),
        ('FINANCE','finance:plan:read'),
        ('FINANCE','finance:split:read'),
        ('SECURITY','system:dashboard:read'),
        ('SECURITY','system:notification:read'),
        ('SECURITY','system:employee:read'),
        ('SECURITY','system:employee:manage'),
        ('SECURITY','system:role:manage'),
        ('SECURITY','member:pii:unmask')
    ) v (RoleCode, PermissionCode)
    UNION ALL
    -- OWNER 拿全部
    SELECT 'OWNER', PermissionCode FROM dbo.Permissions
)
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT r.RoleID, p.PermissionID
FROM map m
JOIN dbo.Roles r       ON r.RoleCode = m.RoleCode
JOIN dbo.Permissions p ON p.PermissionCode = m.PermissionCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = r.RoleID AND rp.PermissionID = p.PermissionID);

/* --- 4. 沒有任何角色的員工一律給 OWNER ---
   ponytail: 專題 Demo 的做法。正式環境要改成一人一角色手動指派。 */
INSERT INTO dbo.EmployeeRoles (EmployeeID, RoleID)
SELECT e.EmployeeID, (SELECT RoleID FROM dbo.Roles WHERE RoleCode = 'OWNER')
FROM dbo.Employees e
WHERE NOT EXISTS (SELECT 1 FROM dbo.EmployeeRoles er WHERE er.EmployeeID = e.EmployeeID);

/* --- 結果確認：每個員工實際拿到幾個權限 --- */
SELECT e.EmployeeID, e.Email, e.Name,
       COUNT(DISTINCT p.PermissionCode) AS 權限數,
       MAX(CASE WHEN p.PermissionCode = 'system:dashboard:read' THEN 'OK' ELSE '' END) AS 能進總覽
FROM dbo.Employees e
LEFT JOIN dbo.EmployeeRoles er   ON er.EmployeeID = e.EmployeeID
LEFT JOIN dbo.RolePermissions rp ON rp.RoleID = er.RoleID
LEFT JOIN dbo.Permissions p      ON p.PermissionID = rp.PermissionID
GROUP BY e.EmployeeID, e.Email, e.Name
ORDER BY e.EmployeeID;
