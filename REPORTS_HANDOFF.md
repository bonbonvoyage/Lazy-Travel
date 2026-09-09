# 檢舉審核台(Reports)交接說明

負責人:藍培碩。這份文件寫給要接手「共用 Service 真正實作」或「資料庫串接」的同事看,說明目前做到哪裡、還缺什麼、要怎麼接。

## 目前狀態

檢舉審核台(`/Admin/Reports`)UI、篩選、分頁、判定流程都已經完成並可運作。**資料層已經在 2026-07-21 接上真的 SQL Server 資料庫**(本機的 `LazyTravelDB`,不是記憶體假資料了),`ReportService.cs` 改成透過 `LazyTravelContext.Reports` 查詢/寫入。商業邏輯對外仍只依賴三個介面(通知、操作紀錄、會員停權),方便之後直接抽換,不用改 Controller。

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

**⚠️ 已知落差(2026-07-21 比對過 10 黃浚翔提供的《檢舉同步會員狀態.docx》發現)**:文件裡描述的正式介接契約,跟目前這個暫時介面**不一致**,真正對接時很可能要整個重寫,不是單純換掉實作類別:

| 項目 | 文件描述(正式契約) | 目前暫時實作 |
|---|---|---|
| 介面名稱 | `IMemberService` | `IMemberModerationService` |
| 方法 | `EditMember(MemberEditDto dto)` | `SuspendAsync(string account, int days, string reason)` |
| 參數 | `MemberEditDto { MemberID(int), Status(int), AdminReason(string) }` | `account(string), days(int), reason(string)` |
| 會員識別方式 | `Reports.ReportedMemberID`(**int**,外鍵指向 Members) | `Report.ReportedMemberAccount`(**string** 帳號) |
| 停權天數 | 沒有天數概念,只有 `Status` 狀態值(如 2=封鎖) | 有「累犯 3 次停 3 天、5 次以上停 5 天」的天數計算邏輯 |

真正要對接 `IMemberService` 時,除了換 DI 註冊,還需要:
1. `Report.cs` 的 `ReportedMemberAccount`(string)要改成 `ReportedMemberID`(int),連帶假資料、View 顯示帳號的地方都要調整
2. 確認真正的 `IMemberService` 有沒有支援「停權天數」,如果沒有,`CalculateSuspendDays` 這套累犯天數邏輯要重新設計(可能天數要自己存在 Reports 側或另外的機制,而不是丟給 Member Service 處理)
3. `ReportService.cs` 呼叫停權的地方(`JudgeAsync` 裡呼叫 `_memberModerationService.SuspendAsync(...)`)要改成呼叫 `EditMember(MemberEditDto)`

## 怎麼換成真正的實作

三個都一樣,不用動 `ReportService.cs`/`ReportsController.cs`:

1. 寫一個新 class 實作對應介面
2. 打開 `LazyTravel/Program.cs`,把對應那行 DI 註冊的實作類別換掉,例如:
   ```csharp
   builder.Services.AddScoped<INotificationService, NotificationService>();
   ```
   換成你寫的真正實作類別即可

## 資料庫串接(Reports 已接;AdminLogs 還沒)

### Reports 已經接上真的資料庫(2026-07-21)

本機用 14 洪欣茹/DBA 那份《Lazy Travel 旅遊平台 - 全模組資料庫規格書》配套的 `SQLQuery0721.sql`(同學產生的建表腳本)在本機 SQL Server 建了一份 `LazyTravelDB`,並在上面做了以下事情:

1. 直接執行 `SQLQuery0721.sql` 建出全部 30 張官方表(**執行時要用 `sqlcmd -f 65001` 指定 UTF-8 編碼**,不然中文姓名的種子資料會讓 IDENTITY_INSERT 那段炸掉,详见下方踩雷紀錄)
2. 在本機 `Reports` 表用 `ALTER TABLE` 額外加了 4 個官方沒有的欄位(**只在這台機器的本機資料庫,沒有動共用的 SQL 腳本或 Excel/Word 設計文件**):
   ```sql
   ALTER TABLE [dbo].[Reports] ADD [ReasonCategory] [tinyint] NOT NULL DEFAULT (5);
   ALTER TABLE [dbo].[Reports] ADD [IsMalicious] [bit] NOT NULL DEFAULT (0);
   ALTER TABLE [dbo].[Reports] ADD [TargetTitle] [nvarchar](200) NULL;
   ALTER TABLE [dbo].[Reports] ADD [Description] [nvarchar](500) NULL;
   ```
   `TargetTitle`/`Description` 是額外發現的缺口:官方 `Reports` 表只有 9 欄,沒有「被檢舉對象名稱快取」跟「補充說明」,但畫面本來就有這兩個欄位,先用同一套做法(本機 ALTER TABLE)補上
3. 寫了 41 筆 Reports 種子資料,`ReporterID`/`ReportedMemberID` 都對應到 `SQLQuery0721.sql` 裡真實存在的 `Members`(1~200)
4. `LazyTravel/Models/Report.cs` 改成對應實際資料表:`ReporterAccount`/`ReportedMemberAccount` 保留原本的屬性名稱,但改成從 `Reporter`/`ReportedMember` 這兩個 `Member` 關聯計算出來(顯示 Email),`ReportService.cs`/`ReportsController.cs`/Views 幾乎不用因此再改
5. `ReportTargetType`/`ReportStatus`/`ReportReasonCategory` 三個 enum 都改成 `: byte` 底層型別(對應資料庫 `tinyint`,enum 預設是 `int` 會讀取失敗),`ReportTargetType` 數值也改成對齊官方定義(`Member=1, VlogPost=2, TravelGroup=4`,官方還有 `3:論壇貼文`、`5:留言`,系統還沒有這兩個模組先不加)

**⚠️ 這只是這個分支/這台機器的本機資料庫狀態**,不是團隊共用的。其他同事的本機資料庫如果也用 `SQLQuery0721.sql` 建,不會自動有這 4 個額外欄位跟種子資料,要嘛自己照上面的 SQL 補,要嘛等團隊正式討論完、由 DBA 統一改共用腳本。連線字串設定在 `LazyTravel/appsettings.Development.json`(已被 gitignore 排除,不會進版控,每個人要自己建)。

**踩雷紀錄,順手修的共用檔案**:`LazyTravel/Models/Member.cs`(EF Core Power Tools 從舊版資料庫自動產生的)欄位跟 `SQLQuery0721.sql` 建出來的 `Members` 表對不上(`AuthProvider`、`ProviderKey`、`IsPhonePublic`、`SocialLinksPrivacy` 這幾個舊欄位在新表裡不存在),因為 Reports 要 join `Members` 撈帳號,這個落差直接擋住查詢,所以順手修正了 `Member.cs`(改成對應新欄位,`SocialLinksPrivacy`改名`ContactBookVisibility`,補上`LastLoginAt`/`LastLoginIp`/`FailedLoginCount`/`LockoutEndDate`)。這是修正一個本來就對不上真實資料庫的共用檔案,對用到 `Member` 的其他功能(例如揪團管理)只有好處。**但 `TravelGroup.cs`/`GroupMember.cs`/`JoinRequest.cs` 這三個一樣有欄位對不上的問題(`JoinRule`/`GroupStatus`/`MemberRole`/`RequestStatus` 型別從 string 改成 tinyint,`ReviewStatus`/`Country`/`Region` 官方新表沒有)完全沒動**,因為 Reports 功能不會查到這幾張表,不在這次的處理範圍,揪團管理那位同事之後接資料庫時要注意這個問題(建議重新用 EF Core Power Tools 掃描資料庫,不要手動改)。

### AdminLogs 還沒接

- `Report.cs` 的欄位命名已經照官方 schema 對過,細節與差異寫在檔案註解裡
- `AdminLog.IPAddress` 官方 schema 有這個欄位,目前 Model 沒加,要接資料庫的話記得補上
- `AdminLog.OperatorName`(字串代稱)之後要換成 `AdminID`(int,外鍵接 `Members`),等會員驗證/Cookie 認證做好再改
- ⚠️ 官方最新規格書(《Lazy Travel 旅遊平台 - 全模組資料庫規格書》)裡寫明 `AdminLogs` 已經被 `AdminAuditLogs` 取代,改成關聯全新的 `Employees` 表(不是 `Members`),而且 `AdminAuditLogs` **沒有自由文字說明欄位**,只有 `Action`。真正要接的時候,`IAdminLogService` 這個介面本身大概率要重新設計,不只是換實作類別

## ReportType/IsMalicious 未來還可能要調整的已知缺口

跟團隊/組長 14 洪欣茹討論後才能定案,目前先照現況記錄:

1. **官方 `ReportType` 有 5 種(1:會員, 2:Vlog文章, 3:論壇貼文, 4:揪團, 5:留言)**,系統目前只做了 3 種,**缺「論壇貼文」跟「留言」**。等這兩個模組做出來、需要支援檢舉時,`ReportTargetType`、`Index.cshtml` 篩選下拉選單、`Details.cshtml` 被檢舉對象卡都要補上
2. **`IsMalicious`(惡意檢舉標記)不在官方欄位設計裡**,目前是本機 `ALTER TABLE` 加的,還沒跟團隊/DBA 提案正式收錄進共用資料庫

## 已知小問題(不影響主流程,但值得知道)

- `AdminLogService.GetRecentAsync` 目前固定抓「全站最新 50 筆」,`ReportService.GetRecentReportLogsAsync` 再從裡面篩 Reports 的。如果之後 AdminLogs 資料量變大、其他功能寫入頻繁,Reports 頁面的「最近操作紀錄」可能篩不到足夠筆數。等接資料庫、可以直接用 `WHERE TargetTable='Reports'` 查詢時,這個限制就會自然解決,目前先不處理。
