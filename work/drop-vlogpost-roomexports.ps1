$ErrorActionPreference = "Stop"
$conn = New-Object System.Data.SqlClient.SqlConnection "Server=.\SQL2025;Database=LazyTravelDB;User Id=sa5;Password=123456;TrustServerCertificate=True;"
$conn.Open()
$cmd=$conn.CreateCommand()
$cmd.CommandText=@"
IF OBJECT_ID(N'dbo.VlogPostRoomExports', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.VlogPostRoomExports;
END;
SELECT CASE WHEN OBJECT_ID(N'dbo.VlogPostRoomExports', N'U') IS NULL THEN 0 ELSE 1 END;
"@
$exists=$cmd.ExecuteScalar()
$conn.Close()
Write-Output "VlogPostRoomExportsExists=$exists"
