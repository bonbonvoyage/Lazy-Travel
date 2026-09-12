if ([string]::IsNullOrWhiteSpace($env:LAZYTRAVEL_DB_CONNECTION)) { throw "Set LAZYTRAVEL_DB_CONNECTION before running this script." }
$conn = New-Object System.Data.SqlClient.SqlConnection $env:LAZYTRAVEL_DB_CONNECTION
$conn.Open()
$cmd=$conn.CreateCommand()
$cmd.CommandText="SELECT TOP 5 PostID, Title, Destination, TravelDays, Status, IsDelete, CreatedAt FROM dbo.VlogPosts ORDER BY PostID DESC"
$r=$cmd.ExecuteReader()
while($r.Read()){ Write-Output ("PostID={0}; Title={1}; Destination={2}; Days={3}; Status={4}; IsDelete={5}; Created={6}" -f $r[0],$r[1],$r[2],$r[3],$r[4],$r[5],$r[6]) }
$conn.Close()
