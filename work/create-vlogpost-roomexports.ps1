$ErrorActionPreference = "Stop"
$conn = New-Object System.Data.SqlClient.SqlConnection "Server=.\SQL2025;Database=LazyTravelDB;User Id=sa5;Password=123456;TrustServerCertificate=True;"
$conn.Open()
$cmd=$conn.CreateCommand()
$cmd.CommandText=@"
IF OBJECT_ID(N'dbo.VlogPostRoomExports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VlogPostRoomExports
    (
        PostId int NOT NULL CONSTRAINT PK_VlogPostRoomExports PRIMARY KEY,
        GroupId int NOT NULL,
        Country nvarchar(100) NOT NULL CONSTRAINT DF_VlogPostRoomExports_Country DEFAULT(N''),
        Region nvarchar(200) NOT NULL CONSTRAINT DF_VlogPostRoomExports_Region DEFAULT(N''),
        People int NOT NULL CONSTRAINT DF_VlogPostRoomExports_People DEFAULT(0),
        ExportedAt datetime2 NOT NULL CONSTRAINT DF_VlogPostRoomExports_ExportedAt DEFAULT(SYSDATETIME()),
        CONSTRAINT FK_VlogPostRoomExports_Post FOREIGN KEY(PostId) REFERENCES dbo.VlogPosts(PostID),
        CONSTRAINT FK_VlogPostRoomExports_Group FOREIGN KEY(GroupId) REFERENCES dbo.TravelGroups(GroupID)
    );
    CREATE UNIQUE INDEX IX_VlogPostRoomExports_GroupId ON dbo.VlogPostRoomExports(GroupId);
END;
SELECT OBJECT_ID(N'dbo.VlogPostRoomExports', N'U') AS ObjectId;
"@
$id=$cmd.ExecuteScalar()
$conn.Close()
Write-Output "VlogPostRoomExports ObjectId=$id"
