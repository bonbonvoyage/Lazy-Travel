-- 修正「送出檢舉」500 錯誤：INSERT 陳述式與 FOREIGN KEY 條件約束 "FK_Reports_Type" 衝突。
--
-- 原因：dbo.ReportTargetTypes 這張字典表本身有正確建立（結構跟資料字典 V5 版一致），
-- 但一直沒有真的塞資料進去，是一張空表。Reports.ReportType 有外鍵指向這張表的 TypeID，
-- 所以只要送出任何檢舉（會員/Vlog/揪團），寫入 Reports 就一定會因為 TypeID 對不到
-- 任何一列而違反外鍵，整支 API 500。
--
-- 數值對齊 LazyTravel.Shared/Models/Report.cs 裡 ReportTargetType enum 註解的官方定義：
-- 1:會員, 2:Vlog文章, 3:論壇貼文, 4:揪團, 5:留言（3、5 目前系統還沒有對應模組，
-- 先把字典值補齊，之後功能做出來就不用再回來改這張表）。

MERGE dbo.ReportTargetTypes AS target
USING (VALUES
    (1, N'會員'),
    (2, N'Vlog文章'),
    (3, N'論壇貼文'),
    (4, N'揪團'),
    (5, N'留言')
) AS source (TypeID, TypeName)
ON target.TypeID = source.TypeID
WHEN NOT MATCHED THEN
    INSERT (TypeID, TypeName) VALUES (source.TypeID, source.TypeName);
GO
