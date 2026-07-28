# 檢舉審核台(Reports)交接說明

負責人:藍培碩。這份文件寫給要接手「共用 Service 真正實作」或「資料庫串接」的同事看,說明目前做到哪裡、還缺什麼、要怎麼接。

## 目前狀態

檢舉審核台(`/Admin/Reports`)UI、篩選、分頁、判定流程都已經完成並可運作,但資料層還是**記憶體假資料**,尚未接資料庫。商業邏輯全部在 `LazyTravel/Services/ReportService.cs`,對外只依賴三個介面,方便之後直接抽換,不用改 Controller。

## 你要接手的檔案

### 1. 通知服務 — 歸屬 14 洪欣茹(`/Admin/Notifications`)

- 介面:`LazyTravel/Services/INotificationService.cs`
- 目前暫時實作:`LazyTravel/Services/NotificationService.cs`(只寫 log,沒有真的發通知)
- 方法簽章:`Task SendAsync(string account, string title, string message)`
- 被檢舉審核台呼叫的時機:判定結果出爐時通知檢舉人、判定成立時通知被檢舉會員、觸發自動停權時通知當事人、惡意檢舉警告

### 2. 操作紀錄服務 — 歸屬 14 洪欣茹(`/Admin/AdminLogs`)

- 介面:`LazyTravel/Services/IAdminLogService.cs`
- 目前暫時實作:`LazyTravel/Services/AdminLogService.cs`(存在記憶體 List,重啟就消失)
- 方法簽章:
  - `Task WriteAsync(string operatorName, string action, string detail, string? targetTable = null, int? targetId = null)`
  - `Task<List<AdminLog>> GetRecentAsync(int take = 50)`
  - `Task<List<AdminLog>> GetForTargetAsync(string targetTable, int targetId)`
- 注意:檢舉審核台的「審核人員」「審核時間」**沒有存在 Reports 表上**,是靠 `GetForTargetAsync("Reports", 案件Id)` 反查 AdminLogs 撈出來的,這是刻意設計(官方 Reports schema 本來就沒有審核人欄位),真正實作也要維持這個查詢方式才能正常顯示

### 3. 會員停權服務 — 歸屬 10 黃浚翔(`/Admin/Members`)

- 介面:`LazyTravel/Services/IMemberModerationService.cs`
- 目前暫時實作:`LazyTravel/Services/MemberModerationService.cs`
- 方法簽章:`Task SuspendAsync(string account, int days, string reason)`
- 呼叫時機:同一被檢舉會員累犯查證屬實超過門檻(3 次),或同一檢舉人被標記惡意檢舉超過門檻時,自動觸發停權

## 怎麼換成真正的實作

三個都一樣,不用動 `ReportService.cs`/`ReportsController.cs`:

1. 寫一個新 class 實作對應介面
2. 打開 `LazyTravel/Program.cs`,把對應那行 DI 註冊的實作類別換掉,例如:
   ```csharp
   builder.Services.AddScoped<INotificationService, NotificationService>();
   ```
   換成你寫的真正實作類別即可

## 資料庫串接(Reports / AdminLogs 兩張表都還沒接)

- `LazyTravel/Models/LazyTravelContext.cs` 目前只有 `GroupMembers`、`JoinRequests`、`Members`、`TravelGroups` 四個 `DbSet`,**沒有 `Reports` 跟 `AdminLogs`**,因為這兩張表在實際資料庫應該還沒建
- `Report.cs`、`AdminLog.cs` 的欄位命名已經照《Lazy Travel 旅遊平台資料表.docx》官方 schema 對過,細節與差異都寫在各自檔案的註解裡,重點如下:
  - **`Report.ReasonCategory`(檢舉類別分類籤)不在官方 schema 裡**,是待跟團隊/DBA 提案新增的欄位,接資料庫前要先確認到底要不要真的加這個欄位,不然這部分篩選/顯示邏輯要重寫
  - **`AdminLog.IPAddress`** 官方 schema 有這個欄位,目前 Model **沒加**,要接資料庫的話記得補上
  - `AdminLog.OperatorName`(字串代稱)之後要換成 `AdminID`(`int`,外鍵接 `Members`),等會員驗證/Cookie 認證做好再改
  - `Report.Reason`/`AdminNotes` 目前收緊在 200 字(業務規則),官方欄位是 `nvarchar(500)`,資料庫端不用改
- 資料庫表建好、`DbSet<Report>`/`DbSet<AdminLog>` 加進 `LazyTravelContext` 之後,把 `ReportService.cs`/`AdminLogService.cs` 裡操作 `_reports`/`_logs` 這兩個靜態記憶體 List 的地方,換成真的 `_context.Reports`/`_context.AdminLogs` 查詢即可,方法簽章不用變

## 已知小問題(不影響主流程,但值得知道)

- `AdminLogService.GetRecentAsync` 目前固定抓「全站最新 50 筆」,`ReportService.GetRecentReportLogsAsync` 再從裡面篩 Reports 的。如果之後 AdminLogs 資料量變大、其他功能寫入頻繁,Reports 頁面的「最近操作紀錄」可能篩不到足夠筆數。等接資料庫、可以直接用 `WHERE TargetTable='Reports'` 查詢時,這個限制就會自然解決,目前先不處理。
