/* 揪團管理後台測試資料：新增一筆揪團
   GroupID 是 IDENTITY，不用自己給。
   OwnerMemberID 要對到 Members 裡真的存在的會員，這裡取第一筆現有會員。 */

DECLARE @OwnerId int = (SELECT MIN(MemberID) FROM dbo.Members);

INSERT INTO dbo.TravelGroups
    (OwnerMemberID, GroupTitle, Description,
     StartDate, EndDate,
     MinPeople, MaxPeople, CurrentPeople,
     JoinRule, GroupStatus, IsPublic,
     ReviewStatus, Country, Region)
VALUES
    (@OwnerId, N'京都賞楓五日遊', N'搭配嵐山、清水寺，走輕鬆行程，歡迎新手參加。',
     '2026-11-20', '2026-11-24',
     2, 8, 1,
     1, 0, 1,              -- JoinRule 1:需審核 / GroupStatus 0:等待中 / IsPublic 1:公開
     N'正常', N'日本', N'京都');

SELECT GroupID, GroupTitle, OwnerMemberID, ReviewStatus, IsDelete, CreatedAt
FROM dbo.TravelGroups
WHERE GroupID = SCOPE_IDENTITY();
