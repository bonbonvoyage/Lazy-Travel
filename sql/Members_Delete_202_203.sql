/* ============================================================
   刪除會員 #202(系統管理員)與 #203(培碩)

   ⚠️ 這是硬刪除。Members 沒有 IsDelete 欄位,只有 Status,
      刪掉就沒了,請先確認資料庫有備份。

   Members 被 28 張表參照,而且幾乎都是 NO_ACTION,
   所以要先清掉子資料才刪得掉。這兩位實際卡住的只有兩張表:

     dbo.AdminLogs  #202 有 9 筆、#203 有 7 筆   → 共 16 筆稽核紀錄會消失
     dbo.Reports    #202 是 #19/#20/#21 的檢舉人 → 這 3 筆檢舉案會消失

   AdminPermissions 與 LoginHistories 是 CASCADE,會自己跟著刪。

   只想刪 #203(培碩)的話,把下面的 (202, 203) 全部改成 (203),
   這樣就不會動到那 3 筆檢舉案。
   ============================================================ */

BEGIN TRANSACTION;

-- 1. 檢舉案(#202 是檢舉人)
DELETE FROM dbo.Reports
WHERE ReporterID IN (202, 203) OR ReportedMemberID IN (202, 203);

-- 2. 後台稽核紀錄
DELETE FROM dbo.AdminLogs
WHERE AdminID IN (202, 203);

-- 3. 會員本體
DELETE FROM dbo.Members
WHERE MemberID IN (202, 203);

-- 確認:應該是 0 筆
SELECT COUNT(*) AS 剩餘會員數 FROM dbo.Members WHERE MemberID IN (202, 203);

/* 上面數字是 0 就 COMMIT;有任何不對就改跑 ROLLBACK TRANSACTION。 */
COMMIT TRANSACTION;

-- 刪完後的會員列表
SELECT MemberID, Name, Email, Status, CreatedAt
FROM dbo.Members
ORDER BY MemberID DESC;
