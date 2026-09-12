$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($env:LAZYTRAVEL_DB_CONNECTION)) { throw "Set LAZYTRAVEL_DB_CONNECTION before running this script." }
$conn = New-Object System.Data.SqlClient.SqlConnection $env:LAZYTRAVEL_DB_CONNECTION
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
DECLARE @MemberId int = (
    SELECT TOP 1 Id FROM dbo.Members
    WHERE Email <> 'official@lazytravel.local'
    ORDER BY Id
);
IF @MemberId IS NULL
BEGIN
    SELECT @MemberId = TOP_ID FROM (SELECT TOP 1 Id AS TOP_ID FROM dbo.Members ORDER BY Id) x;
END;

INSERT INTO dbo.VlogPosts
    (MemberID, Title, MediaUrl, MediaType, Content, Destination, TravelDays, Status, CreatedAt, UpdatedAt, TravelDate, TravelPeople, IsDelete)
VALUES
    (@MemberId,
     N'秋日首爾慢旅行：咖啡巷、古宮與漢江夜景',
     N'https://images.unsplash.com/photo-1538485399081-7c8ed112c4e6?w=1600&q=80',
     0,
     N'這趟首爾旅行安排得很鬆，早上從安國與北村的巷弄散步開始，下午留給咖啡店與展覽，晚上再到漢江邊看城市燈光。比起趕景點，這次更想記錄每個小停留：一杯熱拿鐵、一段銀杏路、一個慢慢變亮的夜晚。',
     N'韓國',
     4,
     2,
     GETDATE(),
     GETDATE(),
     '2026-10-18',
     N'Small',
     0);

DECLARE @PostId int = SCOPE_IDENTITY();

INSERT INTO dbo.VlogPostImages
    (VlogPostID, ImageUrl, ImageType, AltText, SortOrder, IsCover, IsDeleted, UploadedByMemberID, CreatedAt, UpdatedAt)
VALUES
(@PostId, N'https://images.unsplash.com/photo-1538485399081-7c8ed112c4e6?w=1600&q=80', 0, N'首爾秋日街景', 0, 1, 0, @MemberId, GETDATE(), GETDATE()),
(@PostId, N'https://images.unsplash.com/photo-1499696010180-025ef6e1a8f9?w=1200&q=80', 0, N'韓屋與巷弄', 1, 0, 0, @MemberId, GETDATE(), GETDATE()),
(@PostId, N'https://images.unsplash.com/photo-1548115184-bc6544d06a58?w=1200&q=80', 0, N'城市夜景', 2, 0, 0, @MemberId, GETDATE(), GETDATE());

INSERT INTO dbo.ItineraryNodes
    (PostID, DayNumber, LocationName, ArrivalTime, StayTime, DepartureTime, MediaUrl, MediaType, Description, Remarks)
VALUES
(@PostId, 1, N'安國・北村韓屋村', '09:30', 180, '12:30', N'https://images.unsplash.com/photo-1499696010180-025ef6e1a8f9?w=1200&q=80', 0, N'沿著韓屋巷弄慢慢散步，早上的人潮不多，很適合拍照與找小店。', N'建議穿好走的鞋，坡道不少。'),
(@PostId, 1, N'益善洞咖啡街', '14:00', 150, '16:30', N'https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=1200&q=80', 0, N'下午安排咖啡店與甜點，保留彈性時間走進喜歡的小店。', N'熱門店可能需要候位。'),
(@PostId, 2, N'景福宮', '10:00', 180, '13:00', N'https://images.unsplash.com/photo-1578637387939-43c525550085?w=1200&q=80', 0, N'租韓服進景福宮，慢慢逛宮殿與庭院，秋天色調很漂亮。', N'可提早預約韓服店。'),
(@PostId, 3, N'聖水洞', '11:00', 240, '15:00', N'https://images.unsplash.com/photo-1511920170033-f8396924c348?w=1200&q=80', 0, N'聖水洞適合安排選物店、品牌店與咖啡廳，步調舒服。', N'店與店距離不遠，適合散步。'),
(@PostId, 4, N'漢江公園', '17:30', 180, '20:30', N'https://images.unsplash.com/photo-1548115184-bc6544d06a58?w=1200&q=80', 0, N'最後一晚到漢江邊看夜景，買炸雞和飲料坐著聊天。', N'晚上風大，記得帶外套。');

SELECT @PostId AS PostId;
"@
$id = $cmd.ExecuteScalar()
$conn.Close()
Write-Output "Inserted PostId=$id"
