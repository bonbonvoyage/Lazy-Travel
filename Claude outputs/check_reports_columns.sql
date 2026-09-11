-- 確認「Reports」表在你實際的資料庫裡，有沒有 Report.cs 註解說「本機額外用 ALTER TABLE
-- 加的」那 5 個欄位：ReasonCategory、IsMalicious、TargetTitle、Description、EvidenceUrl。
-- 在 SSMS 連到你的資料庫後執行這段，看回傳結果有哪些資料列即可，不會修改任何資料。

SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reports'
ORDER BY ORDINAL_POSITION;

-- 你可以直接看結果清單裡有沒有下面這 5 個名字：
--   ReasonCategory
--   IsMalicious
--   TargetTitle
--   Description
--   EvidenceUrl
--
-- 如果 5 個都在 → 代表你資料庫已經跟這支分支的本機開發環境同步過那次 ALTER TABLE，
--   可以直接沿用現成的 IReportService / ReportService，我接下來只要幫會員自己頁面
--   多做一個「不需要 employeeId」的送出檢舉入口就好。
--
-- 如果缺了任何一個（最有可能是全部都缺，因為註解寫的是「還沒跟團隊/DBA 提案正式收錄」）
--   → 代表 ReportService 目前依賴的是本機專屬欄位，不能直接套用在你的資料庫，
--   我會另外幫會員檢舉頁面設計一個只用官方 9 欄位（Reports 表本來就有的欄位）的簡化版本。
