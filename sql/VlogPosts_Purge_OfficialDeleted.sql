/* ============================================================
   清空「官方文章 → 已刪除」頁籤:把軟刪除的官方文章真的從資料庫移除

   ⚠️ 硬刪除,而且是「還原」按鈕按不回來的那種。
      後台的刪除只是 IsDelete = 1,這支是真的 DELETE。

   範圍:官方帳號(official@lazytravel.local)且 IsDelete = 1 的文章。
   官方身份用 Email 判斷,跟程式的 MemberLookup.IsOfficial 一致
   (不用 MemberID,不同資料庫的 IDENTITY 編號可能不一樣)。

   執行前的 7 筆:
     62 哈哈哈哈                    節點 0
     61 測試完整送出流程            節點 0
     56 阿里山的姑娘                節點 1
     58 台北大稻埕迪化街一日散策    節點 6
     59 測試新增後跳轉行程頁        節點 0
     57 1                           節點 0
     55 花蓮                        節點 0
   共 7 篇、ItineraryNodes 共 7 筆,PostInteractions 全部 0 筆。

   會員文章的已刪除不受影響(這裡只清官方)。
   封面圖在 Cloudflare R2 上,SQL 刪不到,會變成孤兒檔案。
   ============================================================ */

-- 先看一次要刪哪些,確認清單符合預期再往下跑
SELECT p.PostID, p.Title, p.Destination, p.UpdatedAt
FROM dbo.VlogPosts p
JOIN dbo.Members m ON m.MemberID = p.MemberID
WHERE m.Email = 'official@lazytravel.local' AND p.IsDelete = 1
ORDER BY p.UpdatedAt DESC;

BEGIN TRANSACTION;

-- 目標 PostID 先撈進暫存表,後面兩個 DELETE 共用同一份名單
DECLARE @targets TABLE (PostID int PRIMARY KEY);

INSERT INTO @targets (PostID)
SELECT p.PostID
FROM dbo.VlogPosts p
JOIN dbo.Members m ON m.MemberID = p.MemberID
WHERE m.Email = 'official@lazytravel.local' AND p.IsDelete = 1;

-- 1. 行程節點
DELETE FROM dbo.ItineraryNodes WHERE PostID IN (SELECT PostID FROM @targets);

-- 2. 文章本體
DELETE FROM dbo.VlogPosts WHERE PostID IN (SELECT PostID FROM @targets);

-- 確認:兩個都要是 0
SELECT
    (SELECT COUNT(*) FROM dbo.VlogPosts p
        JOIN dbo.Members m ON m.MemberID = p.MemberID
        WHERE m.Email = 'official@lazytravel.local' AND p.IsDelete = 1) AS 剩餘已刪除官方文章,
    (SELECT COUNT(*) FROM dbo.ItineraryNodes WHERE PostID IN (SELECT PostID FROM @targets)) AS 剩餘節點;

/* 兩個都是 0 就 COMMIT;有任何不對就改跑 ROLLBACK TRANSACTION。 */
COMMIT TRANSACTION;

-- 刪完後剩下的官方文章
SELECT p.PostID, p.Title, p.Status, p.IsDelete, p.UpdatedAt
FROM dbo.VlogPosts p
JOIN dbo.Members m ON m.MemberID = p.MemberID
WHERE m.Email = 'official@lazytravel.local'
ORDER BY p.UpdatedAt DESC;
