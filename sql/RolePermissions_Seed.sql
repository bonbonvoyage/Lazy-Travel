/* ============================================================
   角色 → 權限 綁定（RolePermissions）

   為什麼需要：後台側邊欄每個項目都綁 Policy 判斷，
   RolePermissions 是空的 → 登入後拿不到任何權限 Claim
   → canSee* 全 false → 側邊欄一項都不顯示。

   可重複執行，已存在的綁定會自動略過。
   ⚠️ 下面的分配是依角色名稱推測的，正式內容請跟浚翔確認。
   ============================================================ */

/* 角色 1 OWNER 系統擁有者 —— 全部 18 個權限 */
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT 1, p.PermissionID
FROM dbo.Permissions p
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = 1 AND rp.PermissionID = p.PermissionID);

/* 角色 2 CONTENT_MOD 內容審核 */
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT 2, p.PermissionID
FROM dbo.Permissions p
WHERE p.PermissionCode IN (
        'system:dashboard:read',
        'system:notification:read',
        'content:forum:read',
        'content:vlog:read',
        'content:vlog:delete',
        'content:report:read',
        'content:report:audit',
        'social:travelgroup:read',
        'social:travelgroup:manage')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = 2 AND rp.PermissionID = p.PermissionID);

/* 角色 3 MEMBER_CS 會員客服 */
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT 3, p.PermissionID
FROM dbo.Permissions p
WHERE p.PermissionCode IN (
        'system:dashboard:read',
        'system:notification:read',
        'member:account:read',
        'member:account:block',
        'content:report:read')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = 3 AND rp.PermissionID = p.PermissionID);

/* 角色 4 FINANCE 財務人員 */
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT 4, p.PermissionID
FROM dbo.Permissions p
WHERE p.PermissionCode IN (
        'system:dashboard:read',
        'system:notification:read',
        'system:analytics:read',
        'finance:plan:read',
        'finance:split:read')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = 4 AND rp.PermissionID = p.PermissionID);

/* 角色 5 SECURITY 資安人員 */
INSERT INTO dbo.RolePermissions (RoleID, PermissionID)
SELECT 5, p.PermissionID
FROM dbo.Permissions p
WHERE p.PermissionCode IN (
        'system:dashboard:read',
        'system:notification:read',
        'system:employee:read',
        'system:employee:manage',
        'system:role:manage',
        'member:pii:unmask')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleID = 5 AND rp.PermissionID = p.PermissionID);


/* 結果確認 */
SELECT r.RoleID, r.RoleCode, r.RoleName, COUNT(rp.PermissionID) AS 權限數
FROM dbo.Roles r
LEFT JOIN dbo.RolePermissions rp ON r.RoleID = rp.RoleID
GROUP BY r.RoleID, r.RoleCode, r.RoleName
ORDER BY r.RoleID;
