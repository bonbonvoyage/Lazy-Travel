/*
	在 Members 加上 City 欄位（居住地，例如「台北・大安」），
	會員個人頁面的「居住地」欄位原本沒有資料庫欄位可以存，這支補上去。

	跟之前的 alter_travelskills_add_description.sql 一樣，可以重複執行：
	欄位已經存在就不會重複加，不會出錯。
*/

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

	IF NOT EXISTS (
		SELECT 1 FROM sys.columns
		WHERE object_id = OBJECT_ID('dbo.Members') AND name = 'City'
	)
	BEGIN
		ALTER TABLE dbo.Members ADD City NVARCHAR(100) NULL;
		PRINT N'Members：已新增 City 欄位。';
	END
	ELSE
	BEGIN
		PRINT N'Members.City 欄位已經存在，跳過新增。';
	END

	COMMIT TRANSACTION;
	PRINT N'完成。';

END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	PRINT N'發生錯誤，已全部回復：' + ERROR_MESSAGE();
	THROW;
END CATCH
