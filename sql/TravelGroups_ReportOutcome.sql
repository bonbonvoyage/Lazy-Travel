/* ============================================================================
   依檢舉判定結果回寫揪團審核狀態（2026-07-29）

   取代原本那支寫死欄位值的腳本，修正四件事：
     1. ReviewStatus 只能用程式認得的值：正常 / 檢舉審核中 / 違規
        （見 Areas/Admin/Controllers/TravelGroupsController.cs 的 AllowedReviewStatuses）
        原本寫的「無違規」不在清單裡，篩選下拉找不到、還原功能也會直接覆蓋掉。
     2. GroupStatus 對照表以 View 為準：0=等待中 1=成行中 2=分帳中 3=行程結束
        （見 Areas/Admin/Views/TravelGroups/_TravelGroupsTabContent.cshtml）
        沒有「已額滿」這個狀態，額滿要用人數表達，不能寫 2（那是分帳中）。
     3. CurrentPeople 不寫死，一律從 GroupMembers 實際筆數算（排除已退出/被移除），
        否則詳細頁會出現「顯示 6 人、成員清單只有 3 筆」。
     4. 補寫 TravelGroupsLog，否則後台「異動紀錄」分頁查不到是誰把團改成違規的。

   不動的欄位（刻意）：
     IsPublic / IsDelete —— 這兩個是後台「軟刪除／還原」在管的（TravelGroupsController
     的 Delete 設 IsPublic=false、Restore 設回 true）。判定違規 ≠ 下架，直接改會造出
     一個後台操作永遠產不出來的中間狀態（未刪除但不公開）。要下架請走後台的刪除功能。
     GroupTitle / Description / Country / Region / 日期 —— 已經是正確資料，沒必要覆寫。

   整支包在交易裡，最後才 COMMIT；跑完先看驗證結果，不對就改成 ROLLBACK。
   ============================================================================ */

SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @Now datetime = GETDATE();

/* 異動紀錄的操作人。TravelGroupsLog.ChangeByMemberID 是外鍵指向 Members，
   跟 AdminLogs 一樣挑「Email 在 Employees 對得到人」的會員，畫面才顯示得出真實員工。 */
DECLARE @OperatorMemberID int =
(
    SELECT TOP 1 m.MemberID
    FROM   dbo.Members m
    JOIN   dbo.Employees e ON e.Email = m.Email
    ORDER  BY m.MemberID
);


/* ---------------------------------------------------------------------------
   要套用的判定結果。ReportID 是重建後的新編號（舊的 23~27 已經不存在了）。
   MakeFull=1 表示這團要呈現「已額滿」，做法是把 MaxPeople 壓到實際人數，
   不是去改 GroupStatus。
   --------------------------------------------------------------------------- */
DECLARE @Plan TABLE
(
    GroupID      int          PRIMARY KEY,
    ReportID     int          NOT NULL,
    ReviewStatus nvarchar(60) NOT NULL,
    GroupStatus  tinyint      NOT NULL,
    MakeFull     bit          NOT NULL,
    Remark       nvarchar(200) NOT NULL
);

INSERT INTO @Plan (GroupID, ReportID, ReviewStatus, GroupStatus, MakeFull, Remark)
VALUES
    (8 , 10, N'違規', 1, 0, N'檢舉單 #10 判定成立（服務／行程糾紛：團主收訂金後失聯），審核狀態改為違規'),
    (9 , 11, N'違規', 1, 1, N'檢舉單 #11 判定成立（詐騙／安全疑慮：要求私下匯款到個人帳戶），審核狀態改為違規'),
    (10, 12, N'正常', 0, 0, N'檢舉單 #12 判定不成立（雙方認知落差，非惡意變更），審核狀態維持正常');


/* ---------------------------------------------------------------------------
   1. 先寫異動紀錄 —— 要保留舊值，所以一定要排在 UPDATE 前面。
      只有真的會變動的才寫，重複執行不會灌出一堆一模一樣的紀錄。
   --------------------------------------------------------------------------- */
INSERT INTO dbo.TravelGroupsLog
    (GroupID, ChangeByMemberID, FieldName, OldValue, NewValue, ChangeType, CreatedAt, Remark)
SELECT g.GroupID, @OperatorMemberID, 'ReviewStatus', g.ReviewStatus, p.ReviewStatus,
       N'檢舉判定', @Now, p.Remark
FROM   dbo.TravelGroups g
JOIN   @Plan p ON p.GroupID = g.GroupID
WHERE  g.ReviewStatus <> p.ReviewStatus;


/* ---------------------------------------------------------------------------
   2. 更新揪團本身
      CurrentPeople 一律用 GroupMembers 實際筆數；MaxPeople 只在「要呈現額滿」時
      壓到實際人數，其餘情況只保證不會小於實際人數（避免 5/4 這種顯示）。
   --------------------------------------------------------------------------- */
UPDATE g
SET    g.ReviewStatus  = p.ReviewStatus,
       g.GroupStatus   = p.GroupStatus,
       g.CurrentPeople = c.Cnt,
       g.MaxPeople     = CASE WHEN p.MakeFull = 1 THEN c.Cnt
                              WHEN g.MaxPeople < c.Cnt THEN c.Cnt
                              ELSE g.MaxPeople END,
       g.UpdatedAt     = @Now
FROM   dbo.TravelGroups g
JOIN   @Plan p ON p.GroupID = g.GroupID
CROSS APPLY
(
    SELECT COUNT(*) AS Cnt
    FROM   dbo.GroupMembers gm
    WHERE  gm.GroupID   = g.GroupID
      AND  gm.IsRemoved = 0
      AND  gm.LeftAt IS NULL
) c;


/* ---------------------------------------------------------------------------
   3. 驗證：NG 欄位應該全部是 0
   --------------------------------------------------------------------------- */
SELECT SUM(CASE WHEN g.ReviewStatus NOT IN (N'正常', N'檢舉審核中', N'違規') THEN 1 ELSE 0 END) AS NG_審核狀態非法值,
       SUM(CASE WHEN g.GroupStatus > 3 THEN 1 ELSE 0 END)                                      AS NG_揪團狀態超出對照表,
       SUM(CASE WHEN g.CurrentPeople <> c.Cnt THEN 1 ELSE 0 END)                               AS NG_人數與成員清單不符,
       SUM(CASE WHEN g.CurrentPeople > g.MaxPeople THEN 1 ELSE 0 END)                          AS NG_人數超過上限,
       SUM(CASE WHEN g.IsDelete = 0 AND g.IsPublic = 0 THEN 1 ELSE 0 END)                      AS NG_未刪除卻不公開
FROM   dbo.TravelGroups g
CROSS APPLY (SELECT COUNT(*) AS Cnt FROM dbo.GroupMembers gm
             WHERE gm.GroupID = g.GroupID AND gm.IsRemoved = 0 AND gm.LeftAt IS NULL) c;

SELECT g.GroupID,
       g.GroupTitle       AS 房間名稱,
       m.Name             AS 團主,
       CASE g.GroupStatus WHEN 0 THEN N'等待中' WHEN 1 THEN N'成行中'
                          WHEN 2 THEN N'分帳中' WHEN 3 THEN N'行程結束'
                          ELSE N'** 未定義 **' END AS 揪團狀態,
       g.ReviewStatus     AS 審核狀態,
       CAST(g.CurrentPeople AS nvarchar(10)) + N' / ' + CAST(g.MaxPeople AS nvarchar(10)) AS 人數,
       c.Cnt              AS 成員清單筆數,
       CASE WHEN g.IsPublic = 1 THEN N'公開' ELSE N'不公開' END AS 公開狀態,
       p.ReportID         AS 來源檢舉單
FROM   dbo.TravelGroups g
JOIN   @Plan p ON p.GroupID = g.GroupID
LEFT JOIN dbo.Members m ON m.MemberID = g.OwnerMemberID
CROSS APPLY (SELECT COUNT(*) AS Cnt FROM dbo.GroupMembers gm
             WHERE gm.GroupID = g.GroupID AND gm.IsRemoved = 0 AND gm.LeftAt IS NULL) c
ORDER BY g.GroupID;

SELECT TOP 10 l.LogID, l.GroupID, l.ChangeType, l.FieldName, l.OldValue, l.NewValue,
       ISNULL(e.Name, m.Name) AS 操作人, l.CreatedAt, l.Remark
FROM   dbo.TravelGroupsLog l
LEFT JOIN dbo.Members   m ON m.MemberID = l.ChangeByMemberID
LEFT JOIN dbo.Employees e ON e.Email    = m.Email
ORDER BY l.LogID DESC;


COMMIT TRANSACTION;
