/*
    TravelGroupsLog 操作員改連 Employees

    程式端仍沿用既有欄位 dbo.TravelGroupsLog.ChangeByMemberID，
    但該欄位現在代表後台員工 Employees.EmployeeID。
*/

IF OBJECT_ID(N'dbo.TravelGroupsLog', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_TravelGroupsLog_ChangeByMember'
          AND parent_object_id = OBJECT_ID(N'dbo.TravelGroupsLog')
    )
    BEGIN
        ALTER TABLE dbo.TravelGroupsLog
        DROP CONSTRAINT FK_TravelGroupsLog_ChangeByMember;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_TravelGroupsLog_ChangeByEmployee'
          AND parent_object_id = OBJECT_ID(N'dbo.TravelGroupsLog')
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeID = 1)
        BEGIN
            UPDATE l
            SET ChangeByMemberID = 1
            FROM dbo.TravelGroupsLog AS l
            LEFT JOIN dbo.Employees AS e
                ON e.EmployeeID = l.ChangeByMemberID
            WHERE l.ChangeByMemberID IS NOT NULL
              AND e.EmployeeID IS NULL;
        END;

        ALTER TABLE dbo.TravelGroupsLog WITH CHECK
        ADD CONSTRAINT FK_TravelGroupsLog_ChangeByEmployee
        FOREIGN KEY (ChangeByMemberID) REFERENCES dbo.Employees(EmployeeID);

        ALTER TABLE dbo.TravelGroupsLog
        CHECK CONSTRAINT FK_TravelGroupsLog_ChangeByEmployee;
    END;
END;
