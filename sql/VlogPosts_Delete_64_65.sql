/* ============================================================
   刪除 Vlog 行程文章「花蓮」(PostID 65)與「111」(PostID 64)

   ⚠️ 硬刪除。後台「檢視 → 刪除」是軟刪除(IsDelete = 1),
      文章會留在「已刪除」頁籤裡;這支是真的從資料庫移除。

   為什麼是 64 / 65:資料庫有三筆同名文章,
     PostID 55  花蓮  IsDelete = 1  ← 已經在「已刪除」頁籤,不動它
     PostID 64  111   待審核        ← 刪
     PostID 65  花蓮  待審核        ← 刪
   畫面上「待審核 2」就是 64 和 65。

   依賴鏈:VlogPosts(64,65) └─ ItineraryNodes 共 2 筆。
   其他參照 VlogPosts 的表(PostInteractions 等)都是 0 筆。

   註:封面圖在 Cloudflare R2 上,SQL 刪不到,會變成孤兒檔案。
   ============================================================ */

BEGIN TRANSACTION;

-- 1. 行程節點
DELETE FROM dbo.ItineraryNodes WHERE PostID IN (64, 65);

-- 2. 文章本體
DELETE FROM dbo.VlogPosts WHERE PostID IN (64, 65);

-- 確認:兩個都要是 0
SELECT
    (SELECT COUNT(*) FROM dbo.VlogPosts      WHERE PostID IN (64, 65)) AS 剩餘文章,
    (SELECT COUNT(*) FROM dbo.ItineraryNodes WHERE PostID IN (64, 65)) AS 剩餘節點;

/* 兩個都是 0 就 COMMIT;有任何不對就改跑 ROLLBACK TRANSACTION。 */
COMMIT TRANSACTION;

-- 刪完後的官方文章清單
SELECT PostID, Title, Destination, Status, IsDelete, UpdatedAt
FROM dbo.VlogPosts
WHERE MemberID = 5
ORDER BY UpdatedAt DESC;
