-- ============================================================================
-- 修正 Members.ContactBookVisibility 的預設值：0（公開）→ 1（私密，僅好友/同團）
-- ============================================================================
-- 背景：ContactBookVisibility 這欄原本的資料庫預設值是 0（公開），代表任何
-- 從未在個人頁把「通訊錄公開」關掉的會員，通訊錄（電話／LINE／IG／FB）預設
-- 就是任何人都看得到，不限於好友或追蹤者。前端 scene.dc.html 那邊其實已經
-- 假設「預設私密」在寫（state 初始值 contactPublic: false），只有資料庫這邊的
-- 預設值和舊資料跟前端假設對不起來。
--
-- 這支腳本做兩件事：
--   1. 把 ContactBookVisibility 欄位的預設值從 0 改成 1（之後新增的會員都會是
--      私密起始，除非自己在個人頁打開公開）。
--   2. 把目前資料庫裡所有還是 0（公開）的既有會員，一次性改成 1（私密）。
--      ⚠️ 這一步是「全部重設」，沒辦法分辨「本來就沒改過設定」跟「使用者自己
--      主動選了公開」——因為資料庫沒有另外記錄「有沒有被使用者手動改過」這件
--      事。適合現在這種開發/展示資料的情境；如果之後正式上線、已經有真實使用者
--      主動選擇公開通訊錄，這一步要拿掉或改寫，不要不分青紅皂白全部重設。
--
-- 建議先在 SSMS 檢查一下 SELECT 結果、確認受影響筆數符合預期，再執行 UPDATE。
-- ============================================================================

USE LazyTravelDB;
GO

-- Step 0：檢查目前狀態（執行前先看一下，確認受影響的是你預期的那些帳號）
SELECT Id, Name, Email, ContactBookVisibility
FROM dbo.Members
WHERE ContactBookVisibility = 0
ORDER BY Id;
GO

-- Step 1：找到 ContactBookVisibility 目前的預設值約束（沒有明確命名，用系統
-- 目錄查出實際名稱），刪掉它。
DECLARE @constraintName NVARCHAR(200);

SELECT @constraintName = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Members')
  AND c.name = N'ContactBookVisibility';

IF @constraintName IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(500) = N'ALTER TABLE dbo.Members DROP CONSTRAINT ' + QUOTENAME(@constraintName);
    EXEC sp_executesql @sql;
END
GO

-- Step 2：加回新的預設值 1（私密）。
ALTER TABLE dbo.Members
    ADD DEFAULT ((1)) FOR [ContactBookVisibility];
GO

-- Step 3：既有資料一次性重設——把目前還是 0（公開）的會員全部改成 1（私密）。
UPDATE dbo.Members
SET ContactBookVisibility = 1
WHERE ContactBookVisibility = 0;
GO

-- Step 4：驗證——執行完應該回傳 0 筆（代表已經沒有任何會員停留在舊的公開狀態）。
SELECT COUNT(*) AS StillPublicCount
FROM dbo.Members
WHERE ContactBookVisibility = 0;
GO
