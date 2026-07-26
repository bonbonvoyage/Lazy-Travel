# LazyTravel 

旅遊揪團社群平台,以後台管理系統為主。

## 技術棧
- ASP.NET Core MVC (.NET 8)
- Entity Framework Core(待接入)
- Bootstrap 5(CDN)+ 品牌樣式 brand.css / admin.css
- Cookie 認證(待接入)

## 環境需求
- Visual Studio 2022(含「ASP.NET 與網頁程式開發」工作負載)
- .NET 8 SDK
- SQL Server LocalDB(VS 安裝時通常內含)

## 環境建置(每位組員第一次 clone 後)
1. Clone 本專案
2. 在 `LazyTravel/` 專案根目錄(與 LazyTravel.csproj 同層)自行建立 `appsettings.Development.json`,內容:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.\\SQL2025;Database=LazyTravelDB;User Id=sa5;Password=123456;TrustServerCertificate=True;MultipleActiveResultSets=true"
     }
   }
   ```
   統一用 `sa5`/`123456` 這組 SQL 帳密連線,方便大家在 SSMS 用同一組帳密互相排查。`Server=` 請改成自己電腦實際的 SQL Server 執行個體名稱(在 SSMS 伺服器總管裡看得到,可能是 `.`、`localhost` 或 `.\你的執行個體名稱`);`sa5` 帳號要先在自己的 SQL Server 上建立(開啟「SQL Server 及 Windows 驗證模式」+ 新增登入),且需要是 `LazyTravelDB` 的 sysadmin 或 db_owner。此檔已被 .gitignore 排除,不會進版控。
3. 開啟 `LazyTravel.sln`,按 F5 執行
4. 前台:`/`;後台:`/Admin`

## 分支規則
- `main`:穩定可 demo 版本,禁止直接 push
- `dev`:整合分支
- `feature/xxx`:個人功能分支,完成後發 Pull Request 合併回 dev

## 分工
各組員在自己的 feature 分支開發負責模組,後台側欄選單對應的 Controller 請依 `_AdminLayout.cshtml` 內既定名稱建立(Reports、VlogPosts、Members、TravelGroups、Notifications、Subscriptions、Analytics、Forum、AdminLogs)。

## 設計規範
- 色票與元件樣式見 `wwwroot/css/brand.css`、`admin.css`
- 後台底色定案:深一階海藍 sea-200 (#ABC9D6)
- 修改共用檔(Layout、brand.css)前請先在群組告知
