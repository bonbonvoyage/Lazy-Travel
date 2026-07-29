/* 檢舉審核台測試資料 —— 4 筆，涵蓋三種狀態與三種檢舉類型

   對照表數值（程式的 enum 已對齊資料庫）：
     ReportType     1=會員  2=Vlog 行程文章  4=揪團管理
     ReportStatus   0=待處理  1=檢舉成立  2=不成立
     ReasonCategory 0=廣告垃圾  1=詐騙  2=騷擾  3=版權  4=服務行程糾紛  5=其他

   ReportID 是 IDENTITY，不指定。
   可重複執行，同樣的檢舉原因已存在就跳過。 */

DECLARE @Vlog1Title nvarchar(400) = (SELECT Title FROM dbo.VlogPosts WHERE PostID = 1);
DECLARE @Vlog2Title nvarchar(400) = (SELECT Title FROM dbo.VlogPosts WHERE PostID = 2);
DECLARE @GroupTitle nvarchar(400) = (SELECT GroupTitle FROM dbo.TravelGroups WHERE GroupID = 1);
DECLARE @Member2Name nvarchar(400) = (SELECT Name FROM dbo.Members WHERE MemberID = 2);


/* 1. 待處理 · Vlog 文章 · 服務／行程糾紛 · 有截圖 */
IF NOT EXISTS (SELECT 1 FROM dbo.Reports WHERE Reason = N'內文行程與實際不符，疑似誇大宣傳')
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
VALUES
    (5, 2, 2, 1, @Vlog1Title,
     4, N'內文行程與實際不符，疑似誇大宣傳', N'照片與描述的景點對不上，實際去了完全不同的地方。',
     N'/uploads/reports/demo-evidence-8.svg',
     0, 0, NULL, DATEADD(day, -3, GETDATE()));


/* 2. 待處理 · 揪團 · 詐騙／安全疑慮 · 無截圖 */
IF NOT EXISTS (SELECT 1 FROM dbo.Reports WHERE Reason = N'團主收訂金後失聯')
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
VALUES
    (3, 1, 4, 1, @GroupTitle,
     1, N'團主收訂金後失聯', N'已匯款兩週，訊息不讀不回。',
     NULL,
     0, 0, NULL, DATEADD(day, -1, GETDATE()));


/* 3. 檢舉成立 · 會員 · 騷擾／不當言論 · 已寫處置備註 */
IF NOT EXISTS (SELECT 1 FROM dbo.Reports WHERE Reason = N'留言區持續人身攻擊')
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
VALUES
    (4, 2, 1, 2, @Member2Name,
     2, N'留言區持續人身攻擊', N'多篇文章底下重複出現辱罵字眼。',
     NULL,
     1, 0, N'查證屬實，已對該會員發出違規通知。', DATEADD(day, -7, GETDATE()));


/* 4. 不成立 · Vlog 文章 · 廣告垃圾訊息 · 標記為惡意檢舉 */
IF NOT EXISTS (SELECT 1 FROM dbo.Reports WHERE Reason = N'這篇是業配文')
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
VALUES
    (5, 3, 2, 2, @Vlog2Title,
     0, N'這篇是業配文', NULL,
     NULL,
     2, 1, N'內容未涉及商業推廣，檢舉不成立；檢舉人短期內重複提出無依據檢舉，標記為惡意檢舉。',
     DATEADD(day, -5, GETDATE()));


/* 結果 */
SELECT r.ReportID, t.TypeName AS 類型, c.CategoryName AS 類別, s.StatusName AS 狀態,
       r.TargetTitle AS 對象, r.Reason AS 原因, r.IsMalicious AS 惡意檢舉,
       rep.Email AS 檢舉人, rm.Email AS 被檢舉人, r.CreatedAt
FROM dbo.Reports r
LEFT JOIN dbo.ReportTargetTypes      t  ON r.ReportType     = t.TypeID
LEFT JOIN dbo.ReportReasonCategories c  ON r.ReasonCategory = c.CategoryID
LEFT JOIN dbo.ReportStatuses         s  ON r.ReportStatus   = s.StatusID
LEFT JOIN dbo.Members                rep ON r.ReporterID       = rep.MemberID
LEFT JOIN dbo.Members                rm  ON r.ReportedMemberID = rm.MemberID
ORDER BY r.ReportID;
