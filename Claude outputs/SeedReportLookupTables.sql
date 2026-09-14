-- 修正「送出檢舉」500 錯誤：INSERT 陳述式與 FOREIGN KEY 條件約束 "FK_Reports_Type" 衝突。
--
-- 根本原因：檢舉功能牽涉的三張字典表（ReportTargetTypes／ReportReasonCategories／
-- ReportStatuses）本身結構都跟資料字典 V5 版一致、也都正確建好了，但從來沒有真的
-- 塞過資料進去，全部都是空表。Reports 表的 ReportType／ReasonCategory／ReportStatus
-- 這三欄都有外鍵指向這三張字典表，所以只要送出任何檢舉，寫入 Reports 就一定會撞到
-- 外鍵限制而 500——這是資料庫缺種子資料的問題，不是程式碼寫錯。
--
-- 這支腳本一次把三張表都補上，數值跟顯示文字對齊
-- LazyTravel.Shared/Models/Report.cs 裡三個 enum 的官方定義／ToDisplayName 後備文字：
--   ReportTargetType（1:會員, 2:Vlog文章, 3:論壇貼文, 4:揪團, 5:留言；3、5 目前系統
--     還沒有對應模組，先把字典值補齊，之後功能做出來就不用再回來改這張表）
--   ReportReasonCategory（0:廣告垃圾訊息 ... 5:其他）
--   ReportStatus（0:待處理, 1:已處分, 2:已退回）
-- 用 MERGE 是為了可以重複執行不會出錯（已存在的 TypeID/CategoryID/StatusID 不會被
-- 覆蓋或重複插入）。

MERGE dbo.ReportTargetTypes AS target
USING (VALUES
    (1, N'會員'),
    (2, N'Vlog 行程文章'),
    (3, N'論壇貼文'),
    (4, N'揪團管理'),
    (5, N'留言')
) AS source (TypeID, TypeName)
ON target.TypeID = source.TypeID
WHEN NOT MATCHED THEN
    INSERT (TypeID, TypeName) VALUES (source.TypeID, source.TypeName);
GO

MERGE dbo.ReportReasonCategories AS target
USING (VALUES
    (0, N'廣告垃圾訊息'),
    (1, N'詐騙/安全疑慮'),
    (2, N'騷擾/不當言論'),
    (3, N'版權/抄襲爭議'),
    (4, N'服務/行程糾紛'),
    (5, N'其他')
) AS source (CategoryID, CategoryName)
ON target.CategoryID = source.CategoryID
WHEN NOT MATCHED THEN
    INSERT (CategoryID, CategoryName) VALUES (source.CategoryID, source.CategoryName);
GO

MERGE dbo.ReportStatuses AS target
USING (VALUES
    (0, N'待處理'),
    (1, N'已處分'),
    (2, N'已退回')
) AS source (StatusID, StatusName)
ON target.StatusID = source.StatusID
WHEN NOT MATCHED THEN
    INSERT (StatusID, StatusName) VALUES (source.StatusID, source.StatusName);
GO
