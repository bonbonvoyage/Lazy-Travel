/* ============================================================
   刪除會員 #201(姓名與 Email 都是空白的測試資料)

   ⚠️ 硬刪除。Members 沒有 IsDelete 欄位,刪掉就沒了。

   依賴鏈(已查過,只有這一條):
     dbo.Members #201
       └─ dbo.VlogPosts  PostID = 63(標題「111」,IsDelete 已經是 1)
            └─ dbo.ItineraryNodes  1 筆

   ItineraryNodes 沒有被任何表參照,所以到此為止。
   其他 27 張參照 Members 的表對 #201 都是 0 筆。

   註:VlogPost 63 的封面圖在 Cloudflare R2 上,SQL 刪不到,
       會變成孤兒檔案。不影響功能,要清就去 R2 後台砍。
   ============================================================ */

BEGIN TRANSACTION;

-- 1. 行程節點
DELETE FROM dbo.ItineraryNodes WHERE PostID = 63;

-- 2. Vlog 文章
DELETE FROM dbo.VlogPosts WHERE PostID = 63;

-- 3. 會員本體
DELETE FROM dbo.Members WHERE MemberID = 201;

-- 確認:三個都要是 0
SELECT
    (SELECT COUNT(*) FROM dbo.Members        WHERE MemberID = 201) AS 剩餘會員,
    (SELECT COUNT(*) FROM dbo.VlogPosts      WHERE PostID   = 63)  AS 剩餘文章,
    (SELECT COUNT(*) FROM dbo.ItineraryNodes WHERE PostID   = 63)  AS 剩餘節點;

/* 三個都是 0 就 COMMIT;有任何不對就改跑 ROLLBACK TRANSACTION。 */
COMMIT TRANSACTION;

-- 刪完後最新的幾筆會員
SELECT TOP 5 MemberID, Name, Email, Status, CreatedAt
FROM dbo.Members
ORDER BY MemberID DESC;
