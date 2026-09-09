/* ============================================================================
   檢舉審核台測試資料 —— 重建版（2026-07-29）

   為什麼重寫：舊版把 TargetID 寫死成 1、2，但 TravelGroups 實際的 GroupID 是
   8~22（沒有 1 號），所以「揪團管理」那筆檢舉點進去對不到任何團。這版改成一律
   從 VlogPosts / TravelGroups / Members JOIN 出來，TargetID、TargetTitle、
   被檢舉會員都保證是資料庫裡真實存在、而且互相對得上的資料。

   對照表數值（程式的 enum 已對齊資料庫）：
     ReportType     1=會員  2=Vlog 行程文章  4=揪團管理
     ReportStatus   0=待處理  1=檢舉成立  2=不成立
     ReasonCategory 0=廣告垃圾  1=詐騙  2=騷擾  3=版權  4=服務行程糾紛  5=其他

   連動關係：
     Vlog 檢舉 → TargetID = VlogPosts.PostID，被檢舉人 = 該篇的 VlogPosts.MemberID
     揪團檢舉 → TargetID = TravelGroups.GroupID，被檢舉人 = 該團的 OwnerMemberID
     會員檢舉 → TargetID = Members.MemberID，TargetTitle = 該會員姓名
     檢舉人一律取 Members 裡狀態正常（Status=1）的真實會員，且不會等於被檢舉人

   ⚠️ 這支會「先刪除再重建」檢舉模組的資料（Reports 全表 + 檢舉模組寫的 AdminLogs）。
      整支包在交易裡，最後一行才 COMMIT。跑完先看驗證結果，覺得不對就改成 ROLLBACK。
   ============================================================================ */

SET NOCOUNT ON;
BEGIN TRANSACTION;


/* ---------------------------------------------------------------------------
   1. 清掉舊資料
   AdminLogs 只刪檢舉模組自己寫的那些（TargetTable='Reports'，以及檢舉流程觸發、
   但記在 Members 上的停權類紀錄），不會動到 Vlog、揪團、會員管理其他模組的紀錄。
   --------------------------------------------------------------------------- */
DELETE FROM dbo.AdminLogs
WHERE  TargetTable = 'Reports'
   OR  Action IN (N'審核檢舉', N'提出檢舉', N'自動停權', N'自動停權(惡意檢舉)', N'接近停權門檻');

DELETE FROM dbo.Reports;
DBCC CHECKIDENT ('dbo.Reports', RESEED, 0) WITH NO_INFOMSGS;


/* ---------------------------------------------------------------------------
   2. Vlog 行程文章的檢舉
   s.PostID 對到 VlogPosts，標題與被檢舉人（作者）都由 JOIN 帶出來，不寫死。
   --------------------------------------------------------------------------- */
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
SELECT s.ReporterID, p.MemberID, 2, p.PostID, p.Title,
       s.Cat, s.Reason, s.Description, s.EvidenceUrl,
       s.Status, s.IsMalicious, s.AdminNotes, DATEADD(DAY, -s.DaysAgo, GETDATE())
FROM (VALUES
    (1 , 7 , 4, 0, 0, 12, N'內文行程與實際不符，疑似誇大宣傳', N'照片與描述的景點對不上，實際去了完全不同的地方。', N'/uploads/reports/demo-evidence-8.svg', N'查證屬實，已請作者修正內文並發出違規通知。'),
    (6 , 9 , 2, 1, 0, 10, N'留言區與內文出現辱罵字眼',       N'多則留言重複出現人身攻擊。',                     NULL, N'查證屬實，已隱藏違規留言。'),
    (7 , 11, 3, 2, 0, 9 , N'內文疑似抄襲他站部落格',         N'段落與某旅遊部落格高度雷同。',                   NULL, N'比對後為作者本人於他站的同一篇文章，檢舉不成立。'),
    (11, 12, 5, 0, 0, 3 , N'照片疑似非本人拍攝',             NULL,                                              NULL, NULL),
    (13, 8 , 1, 1, 0, 8 , N'文末推銷不明訂房連結',           N'連結導向非官方訂房頁面，疑似釣魚。',             N'/uploads/reports/demo-evidence-8.svg', N'查證屬實，已移除連結。'),
    (24, 15, 0, 2, 1, 7 , N'這篇是業配文',                   NULL,                                              NULL, N'內容未涉及商業推廣，檢舉不成立；檢舉人短期內重複提出無依據檢舉，標記為惡意檢舉。'),
    (28, 17, 3, 0, 0, 2 , N'封面圖疑似取自官方網站',         N'圖片有其他網站浮水印痕跡。',                     NULL, NULL),
    (35, 6 , 4, 1, 0, 6 , N'行程天數與標題不符，內容灌水',   N'標題寫 3 天 2 夜，內文只有一天的紀錄。',         NULL, N'查證屬實，已請作者補齊內容。'),
    (41, 19, 2, 0, 0, 1 , N'內文對特定族群有歧視性描述',     NULL,                                              NULL, NULL)
) AS s(PostID, ReporterID, Cat, Status, IsMalicious, DaysAgo, Reason, Description, EvidenceUrl, AdminNotes)
JOIN dbo.VlogPosts p ON p.PostID = s.PostID AND ISNULL(p.IsDelete, 0) = 0;


/* ---------------------------------------------------------------------------
   3. 揪團管理的檢舉
   s.GroupID 對到 TravelGroups，團名與被檢舉人（團主 OwnerMemberID）由 JOIN 帶出來。
   --------------------------------------------------------------------------- */
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
SELECT s.ReporterID, g.OwnerMemberID, 4, g.GroupID, g.GroupTitle,
       s.Cat, s.Reason, s.Description, s.EvidenceUrl,
       s.Status, s.IsMalicious, s.AdminNotes, DATEADD(DAY, -s.DaysAgo, GETDATE())
FROM (VALUES
    (8 , 16, 4, 1, 0, 11, N'團主收訂金後失聯',             N'已匯款兩週，訊息不讀不回。',           NULL, N'查證屬實，已停止該團並協助團員申請退款。'),
    (9 , 18, 1, 1, 0, 9 , N'要求私下匯款到個人帳戶',       N'團主要求跳過平台直接轉帳。',           N'/uploads/reports/demo-evidence-8.svg', N'查證屬實，已對團主發出違規通知。'),
    (10, 6 , 4, 2, 0, 8 , N'實際行程與揪團說明落差很大',   N'說好的景點取消了兩個。',               NULL, N'雙方認知落差，非惡意變更，檢舉不成立。'),
    (11, 13, 0, 0, 0, 4 , N'揪團說明夾帶其他平台廣告',     NULL,                                    NULL, NULL),
    (13, 14, 2, 1, 0, 5 , N'團主在群組內言語騷擾團員',     N'多次對女性團員發表不當言論。',         NULL, N'查證屬實，已對團主停權處理。'),
    (15, 10, 5, 2, 1, 6 , N'覺得這團很可疑',               NULL,                                    NULL, N'未提出任何具體事證，且同一檢舉人近期重複亂檢舉，標記為惡意檢舉。'),
    (21, 9 , 4, 0, 0, 2 , N'集合時間一直更改，聯絡不上團主', NULL,                                  NULL, NULL)
) AS s(GroupID, ReporterID, Cat, Status, IsMalicious, DaysAgo, Reason, Description, EvidenceUrl, AdminNotes)
JOIN dbo.TravelGroups g ON g.GroupID = s.GroupID AND ISNULL(g.IsDelete, 0) = 0;


/* ---------------------------------------------------------------------------
   4. 會員本身的檢舉
   TargetID 與被檢舉人都是同一個 MemberID，標題取該會員姓名。
   --------------------------------------------------------------------------- */
INSERT INTO dbo.Reports
    (ReporterID, ReportedMemberID, ReportType, TargetID, TargetTitle,
     ReasonCategory, Reason, Description, EvidenceUrl,
     ReportStatus, IsMalicious, AdminNotes, CreatedAt)
SELECT s.ReporterID, m.MemberID, 1, m.MemberID, m.Name,
       s.Cat, s.Reason, s.Description, NULL,
       s.Status, s.IsMalicious, s.AdminNotes, DATEADD(DAY, -s.DaysAgo, GETDATE())
FROM (VALUES
    (3 , 12, 2, 1, 0, 7, N'留言區持續人身攻擊',     N'多篇文章底下重複出現辱罵字眼。', N'查證屬實，已對該會員發出違規通知。'),
    (20, 7 , 1, 2, 0, 5, N'頭像與暱稱冒用他人身分', NULL,                              N'經確認為本人帳號，檢舉不成立。')
) AS s(MemberID, ReporterID, Cat, Status, IsMalicious, DaysAgo, Reason, Description, AdminNotes)
JOIN dbo.Members m ON m.MemberID = s.MemberID;


/* ---------------------------------------------------------------------------
   5. 已判定的案件補上操作紀錄
   後台「審核人員」欄位不是存在 Reports 上，是靠 AdminLogs 反查的（見 AdminLogService）。
   AdminLogs.AdminID 是外鍵指向 Members，畫面顯示時再用該會員的 Email 反查 Employees
   拿員工姓名，所以這裡只挑「Email 在 Employees 裡對得到人」的會員當審核人，
   不然畫面又會退回顯示「系統管理員」。
   --------------------------------------------------------------------------- */
WITH Reviewers AS (
    SELECT m.MemberID,
           ROW_NUMBER() OVER (ORDER BY m.MemberID) - 1 AS Seq,
           COUNT(*)    OVER ()                          AS Total
    FROM   dbo.Members m
    JOIN   dbo.Employees e ON e.Email = m.Email
)
INSERT INTO dbo.AdminLogs (AdminID, Action, TargetTable, TargetID, Description, IPAddress, CreatedAt)
SELECT v.MemberID,
       N'審核檢舉',
       'Reports',
       r.ReportID,
       N'檢舉單 #' + CAST(r.ReportID AS nvarchar(10)) + N'（' + t.TypeName + N'：' + r.TargetTitle + N'）判定為「' + s.StatusName + N'」',
       '127.0.0.1',
       DATEADD(HOUR, 6, r.CreatedAt)
FROM   dbo.Reports r
JOIN   dbo.ReportTargetTypes t ON t.TypeID   = r.ReportType
JOIN   dbo.ReportStatuses    s ON s.StatusID = r.ReportStatus
JOIN   Reviewers             v ON v.Seq      = r.ReportID % v.Total
WHERE  r.ReportStatus <> 0;


/* ---------------------------------------------------------------------------
   6. 驗證：每一筆檢舉都要對得到真實的標的與會員
   NG 欄位全部應該是 0；審核人員欄位不該出現「系統管理員」或空白。
   --------------------------------------------------------------------------- */
SELECT COUNT(*) AS 檢舉總數,
       SUM(CASE WHEN r.ReportType = 2 AND p.PostID  IS NULL THEN 1 ELSE 0 END) AS NG_Vlog對不到文章,
       SUM(CASE WHEN r.ReportType = 4 AND g.GroupID IS NULL THEN 1 ELSE 0 END) AS NG_揪團對不到團,
       SUM(CASE WHEN rep.MemberID IS NULL THEN 1 ELSE 0 END)                   AS NG_檢舉人不存在,
       SUM(CASE WHEN r.ReportedMemberID IS NOT NULL AND rm.MemberID IS NULL THEN 1 ELSE 0 END) AS NG_被檢舉人不存在,
       SUM(CASE WHEN r.ReporterID = r.ReportedMemberID THEN 1 ELSE 0 END)      AS NG_自己檢舉自己
FROM   dbo.Reports r
LEFT JOIN dbo.VlogPosts    p  ON r.ReportType = 2 AND p.PostID  = r.TargetID
LEFT JOIN dbo.TravelGroups g  ON r.ReportType = 4 AND g.GroupID = r.TargetID
LEFT JOIN dbo.Members      rep ON rep.MemberID = r.ReporterID
LEFT JOIN dbo.Members      rm  ON rm.MemberID  = r.ReportedMemberID;

SELECT r.ReportID,
       t.TypeName      AS 類型,
       s.StatusName    AS 狀態,
       c.CategoryName  AS 類別,
       r.TargetID,
       r.TargetTitle   AS 檢舉標的,
       rep.Name        AS 檢舉人,
       rm.Name         AS 被檢舉人,
       ISNULL(e.Name, ISNULL(am.Name, N'—')) AS 審核人員,
       r.IsMalicious   AS 惡意檢舉,
       r.CreatedAt
FROM   dbo.Reports r
LEFT JOIN dbo.ReportTargetTypes      t   ON t.TypeID     = r.ReportType
LEFT JOIN dbo.ReportStatuses         s   ON s.StatusID   = r.ReportStatus
LEFT JOIN dbo.ReportReasonCategories c   ON c.CategoryID = r.ReasonCategory
LEFT JOIN dbo.Members                rep ON rep.MemberID = r.ReporterID
LEFT JOIN dbo.Members                rm  ON rm.MemberID  = r.ReportedMemberID
LEFT JOIN dbo.AdminLogs              l   ON l.TargetTable = 'Reports' AND l.TargetID = r.ReportID AND l.Action = N'審核檢舉'
LEFT JOIN dbo.Members                am  ON am.MemberID  = l.AdminID
LEFT JOIN dbo.Employees              e   ON e.Email      = am.Email
ORDER BY r.ReportID;


COMMIT TRANSACTION;
