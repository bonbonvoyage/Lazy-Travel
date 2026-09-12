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
    '@Title'='Seoul Autumn Slow Trip'
    '@MediaUrl'='https://images.unsplash.com/photo-1538485399081-7c8ed112c4e6?w=1600'
    '@Content'='A relaxed four-day Seoul itinerary with hanok alleys, quiet cafes, palace walks, Seongsu shops, and a Han River sunset. This test article is created for checking the frontend article card and details page.'
    '@Destination'='Korea'
    '@TravelDate'=[datetime]'2026-10-18'
    '@TravelPeople'='Small'
  }
  $images = @(
    @('https://images.unsplash.com/photo-1538485399081-7c8ed112c4e6?w=1600','Seoul autumn street',0,$true),
    @('https://images.unsplash.com/photo-1499696010180-025ef6e1a8f9?w=1200','Hanok alley',1,$false),
    @('https://images.unsplash.com/photo-1548115184-bc6544d06a58?w=1200','City night view',2,$false)
  )
  foreach($img in $images){
    NonQuery "INSERT INTO dbo.VlogPostImages (VlogPostID, ImageUrl, ImageType, AltText, SortOrder, IsCover, IsDeleted, UploadedByMemberID, CreatedAt, UpdatedAt) VALUES (@PostId, @Url, 0, @Alt, @Sort, @Cover, 0, @MemberId, GETDATE(), GETDATE())" @{ '@PostId'=[int]$postId; '@Url'=$img[0]; '@Alt'=$img[1]; '@Sort'=[int]$img[2]; '@Cover'=[bool]$img[3]; '@MemberId'=[int]$memberId } | Out-Null
  }
  $nodes = @(
    @(1,'Anguk and Bukchon','09:30',180,'12:30','https://images.unsplash.com/photo-1499696010180-025ef6e1a8f9?w=1200','Morning walk through hanok alleys and small shops.','Wear comfortable shoes.'),
    @(1,'Ikseon cafe street','14:00',150,'16:30','https://images.unsplash.com/photo-1509042239860-f550ce710b93?w=1200','Coffee, dessert, and flexible afternoon time.','Popular cafes may require waiting.'),
    @(2,'Gyeongbokgung Palace','10:00',180,'13:00','https://images.unsplash.com/photo-1578637387939-43c525550085?w=1200','Palace walk with autumn colors and photo spots.','Book hanbok rental early.'),
    @(3,'Seongsu-dong','11:00',240,'15:00','https://images.unsplash.com/photo-1511920170033-f8396924c348?w=1200','Concept stores, cafes, and slow walking.','Good area for shopping.'),
    @(4,'Han River Park','17:30',180,'20:30','https://images.unsplash.com/photo-1548115184-bc6544d06a58?w=1200','Sunset picnic and night view by the river.','Bring a light jacket.')
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
