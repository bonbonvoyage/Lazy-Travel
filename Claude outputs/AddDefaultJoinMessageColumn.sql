-- 這支腳本要在程式碼重新編譯／執行之前先在資料庫執行一次。
-- 目的：讓「給團長的話」草稿改成跟自我介紹（Bio）一樣，真的存進 Members 資料表，
-- 不再依賴瀏覽器 localStorage（換裝置、換瀏覽器、或用巢狀 iframe 打開申請加入的
-- 機票時看不到之前編輯的內容）。

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Members')
      AND name = 'DefaultJoinMessage'
)
BEGIN
    ALTER TABLE dbo.Members ADD DefaultJoinMessage NVARCHAR(500) NULL;
END
GO
