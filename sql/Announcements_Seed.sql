/* ============================================================
   後台總覽「公告」資料表 + 一筆示範資料

   為什麼不用 dbo.Notifications：那張是會員通知(MemberID NOT NULL),
   每筆都屬於某個會員；後台總覽要的是全站公告,沒有歸屬對象。

   欄位只留總覽真正會用到的。之後做「公告與通知」模組要標題、
   對象、上下架時間再加欄位。

   可重複執行。
   ============================================================ */

IF OBJECT_ID('dbo.Announcements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Announcements (
        AnnouncementID int IDENTITY(1,1) NOT NULL,
        Content        nvarchar(500) NOT NULL,
        IsActive       bit           NOT NULL DEFAULT 1,
        CreatedAt      datetime      NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_Announcements PRIMARY KEY (AnnouncementID)
    );
END
GO

/* 示範公告一筆(已經有資料就跳過,不會重複塞) */
IF NOT EXISTS (SELECT 1 FROM dbo.Announcements)
    INSERT INTO dbo.Announcements (Content, IsActive, CreatedAt)
    VALUES (N'系統維護通知:本週五 02:00–04:00 進行資料庫例行維護,後台將暫停服務約 2 小時,請提前完成審核作業。',
            1, GETDATE());
GO

/* 確認 */
SELECT AnnouncementID, Content, IsActive, CreatedAt
FROM dbo.Announcements
ORDER BY CreatedAt DESC;
