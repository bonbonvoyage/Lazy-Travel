/* ============================================================
   補回「系統管理員」佔位會員 system-admin@lazytravel.local

   為什麼需要它:後台員工存在 Employees 表,不在 Members 裡,
   但 dbo.Reports.ReporterID 是 NOT NULL 外鍵指向 Members。
   所以後台模組(Vlog「提出檢舉」等)由員工送出檢舉時,
   ReportService.SubmitAsync 查不到檢舉人,就退回這筆佔位會員,
   真正的操作人姓名記在 AdminLogs 裡不會遺失。
   見 ReportService.cs 第 258-261 行的註解。

   這筆之前是 MemberID 202,被 Members_Delete_202_203.sql 刪掉,
   導致送出檢舉噴 InvalidOperationException。

   程式是用 Email 找這筆資料(不是 MemberID),所以重建後拿到新的
   IDENTITY 編號沒關係,不用去對 202。

   可重複執行。
   ============================================================ */

IF NOT EXISTS (SELECT 1 FROM dbo.Members WHERE Email = 'system-admin@lazytravel.local')
BEGIN
    INSERT INTO dbo.Members (Email, Name, Status, Gender, IsEmailConfirmed, IsPrivateAccount, CreatedAt)
    VALUES ('system-admin@lazytravel.local', N'系統管理員', 1, 0, 0, 0, GETDATE());
END
GO

/* 確認:要有 1 筆 */
SELECT MemberID, Name, Email, Status, CreatedAt
FROM dbo.Members
WHERE Email = 'system-admin@lazytravel.local';
