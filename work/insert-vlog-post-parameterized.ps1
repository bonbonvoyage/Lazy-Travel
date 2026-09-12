$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($env:LAZYTRAVEL_DB_CONNECTION)) { throw "Set LAZYTRAVEL_DB_CONNECTION before running this script." }
$conn = New-Object System.Data.SqlClient.SqlConnection $env:LAZYTRAVEL_DB_CONNECTION
$conn.Open()
$tx = $conn.BeginTransaction()
try {
  function Scalar($sql, $params=@{}) {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx; $cmd.CommandText = $sql
    foreach($k in $params.Keys){ $null = $cmd.Parameters.AddWithValue($k, $params[$k]) }
    return $cmd.ExecuteScalar()
  }
  function NonQuery($sql, $params=@{}) {
    $cmd = $conn.CreateCommand(); $cmd.Transaction = $tx; $cmd.CommandText = $sql
    foreach($k in $params.Keys){ $null = $cmd.Parameters.AddWithValue($k, $params[$k]) }
    return $cmd.ExecuteNonQuery()
  }
  $memberId = Scalar "SELECT TOP 1 Id FROM dbo.Members WHERE Email <> @official ORDER BY Id" @{ '@official'='official@lazytravel.local' }
  if($null -eq $memberId -or [int]$memberId -eq 0){ $memberId = Scalar "SELECT TOP 1 Id FROM dbo.Members ORDER BY Id" }
  if($null -eq $memberId){ throw 'Members table has no rows.' }

  $postId = Scalar @"
INSERT INTO dbo.VlogPosts
(MemberID, Title, MediaUrl, MediaType, Content, Destination, TravelDays, Status, CreatedAt, UpdatedAt, TravelDate, TravelPeople, IsDelete)
OUTPUT INSERTED.PostID
VALUES (@MemberId, @Title, @MediaUrl, 0, @Content, @Destination, 4, 2, GETDATE(), GETDATE(), @TravelDate, @TravelPeople, 0)
"@ @{
    '@MemberId'=[int]$memberId
    '@Title'='秋日首爾慢旅行：咖啡巷、古宮與漢江夜景'
    '@MediaUrl'='https://images.unsplash.com/photo-1538485399081-7c8ed112c4e6?w=1600&q=80'
    '@Content'='這趟首爾旅行安排得很鬆，早上從安國與北村的巷弄散步開始，下午留給咖啡店與展覽，晚上再到漢江邊看城市燈光。比起趕景點，這次更想記錄每個小停留：一杯熱拿鐵、一段銀杏路、一個慢慢變亮的夜晚。'
    '@Destination'='韓國'
    '@TravelDate'=[datetime]'2026-10-18'
    '@TravelPeople'='Small'
  }

  $images = @(
    @('https://images.unsplash.com/photo-1538485399081-7c8ed112c4e6?w=1600&q=80','首爾秋日街景',0,$true),
    @('https://images.unsplash.com/photo-1499696010180-025ef6e1a8f9?w=1200&q=80','韓屋與巷弄',1,$false),
    @('https://images.unsplash.com/photo-1548115184-bc6544d06a58?w=1200&q=80','城市夜景',2,$false)
  )
  foreach($img in $images){
    NonQuery "INSERT INTO dbo.VlogPostImages (VlogPostID, ImageUrl, ImageType, AltText, SortOrder, IsCover, IsDeleted, UploadedByMemberID, CreatedAt, UpdatedAt) VALUES (@PostId, @Url, 0, @Alt, @Sort, @Cover, 0, @MemberId, GETDATE(), GETDATE())" @{ '@PostId'=[int]$postId; '@Url'=$img[0]; '@Alt'=$img[1]; '@Sort'=[int]$img[2]; '@Cover'=[bool]$img[3]; '@MemberId'=[int]$memberId } | Out-Null
  }
  $nodes = @(
    @(1,'安國・北村韓屋村','09:30',180,'12:30','https://images.unsplash.com/photo-1499696010180-025ef6e1a8f9?w=1200&q=80','沿著韓屋巷弄慢慢散步，早上的人潮不多，很適合拍照與找小店。','建議穿好走的鞋，坡道不少。'),
    @(1,'益善洞咖啡街','14:00',150,'16:30','https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=1200&q=80','下午安排咖啡店與甜點，保留彈性時間走進喜歡的小店。','熱門店可能需要候位。'),
    @(2,'景福宮','10:00',180,'13:00','https://images.unsplash.com/photo-1578637387939-43c525550085?w=1200&q=80','租韓服進景福宮，慢慢逛宮殿與庭院，秋天色調很漂亮。','可提早預約韓服店。'),
    @(3,'聖水洞','11:00',240,'15:00','https://images.unsplash.com/photo-1511920170033-f8396924c348?w=1200&q=80','聖水洞適合安排選物店、品牌店與咖啡廳，步調舒服。','店與店距離不遠，適合散步。'),
    @(4,'漢江公園','17:30',180,'20:30','https://images.unsplash.com/photo-1548115184-bc6544d06a58?w=1200&q=80','最後一晚到漢江邊看夜景，買炸雞和飲料坐著聊天。','晚上風大，記得帶外套。')
  )
  foreach($n in $nodes){
    NonQuery "INSERT INTO dbo.ItineraryNodes (PostID, DayNumber, LocationName, ArrivalTime, StayTime, DepartureTime, MediaUrl, MediaType, Description, Remarks) VALUES (@PostId, @Day, @Loc, @Arrive, @Stay, @Depart, @Url, 0, @Desc, @Remarks)" @{ '@PostId'=[int]$postId; '@Day'=[int]$n[0]; '@Loc'=$n[1]; '@Arrive'=$n[2]; '@Stay'=[int]$n[3]; '@Depart'=$n[4]; '@Url'=$n[5]; '@Desc'=$n[6]; '@Remarks'=$n[7] } | Out-Null
  }
  $tx.Commit()
  Write-Output "Inserted PostId=$postId MemberId=$memberId"
} catch {
  $tx.Rollback()
  throw
} finally {
  $conn.Close()
}
