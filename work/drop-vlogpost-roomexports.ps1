$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($env:LAZYTRAVEL_DB_CONNECTION)) { throw "Set LAZYTRAVEL_DB_CONNECTION before running this script." }
$conn = New-Object System.Data.SqlClient.SqlConnection $env:LAZYTRAVEL_DB_CONNECTION
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
