# 任務:建立 LazyTravel 專案骨架(ASP.NET Core MVC)

你是協助建立團隊專案初始骨架的工程師。請嚴格依照本文件執行,**檔案內容一律使用本文件內嵌的最終定案版本,不要自行改寫視覺樣式或命名**。

## 背景

這是一個五人學生團隊的全端期中專題「LazyTravel」旅遊揪團平台,以後台管理系統為主。技術棧:ASP.NET Core MVC(.NET 8)、EF Core、Bootstrap 5、Cookie 認證(認證後續才做)。本任務只建立「能跑起來的骨架」,頁面內容留空由組員各自開發。

## Step 1:建立專案

```bash
dotnet new mvc -n LazyTravel
cd LazyTravel
```

## Step 2:建立檔案

依照以下結構建立/覆蓋檔案,內容在本文件最下方「檔案內容」區,**逐字使用**:

```
LazyTravel/
├── wwwroot/css/brand.css                        (新增,附件 A)
├── wwwroot/css/admin.css                        (新增,附件 B)
├── Views/Shared/_Layout.cshtml                  (覆蓋原檔,附件 C)
├── Areas/Admin/Controllers/DashboardController.cs (新增,附件 D)
├── Areas/Admin/Views/_ViewStart.cshtml          (新增,附件 E)
├── Areas/Admin/Views/Shared/_AdminLayout.cshtml (新增,附件 F)
└── Areas/Admin/Views/Dashboard/Index.cshtml     (新增,附件 G)
```

另外,`Areas/Admin/Views/` 需要一份 `_ViewImports.cshtml`(從 `Views/_ViewImports.cshtml` 複製一份過來),否則 Tag Helper(asp-controller 等)在後台 View 不會生效。

## Step 3:修改 Program.cs

在 `app.MapControllerRoute(name: "default", ...)` **之前**加入 Area 路由:

```csharp
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
```

## Step 4:appsettings 範本化

1. `appsettings.json` 的 ConnectionStrings 區塊改為範本(值留提示文字):
```json
"ConnectionStrings": {
  "DefaultConnection": "請在 appsettings.Development.json 填入你自己的連線字串"
}
```
2. `appsettings.Development.json` 填入 LocalDB 連線字串:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LazyTravel;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

## Step 5:Git 初始化

1. `git init`
2. 建立 .NET 標準 `.gitignore`(可用 `dotnet new gitignore`),並**確認包含** `appsettings.Development.json` 這一行(沒有就加上)
3. 建立 `README.md`,包含:專案簡介、環境需求(.NET 8 SDK、SQL Server LocalDB)、環境建置步驟(clone → 自行建立 appsettings.Development.json 填連線字串 → dotnet run)、分支規則(main 穩定版 / dev 整合 / feature/xxx 個人功能分支,經 PR 合併)
4. 首次 commit:`git add -A && git commit -m "init: LazyTravel 專案骨架(前後台 Layout + 品牌樣式)"`

## Step 6:驗收(必做)

1. `dotnet build` 需零錯誤
2. `dotnet run` 後開 `/Admin`,應看到:深一階海藍底(#ABC9D6)、白色側欄標題「LazyTravel 後台」、分組選單(總覽/審核中心/內容管理/會員與社群/系統)、內容區顯示「此頁內容待設計」
3. 開 `/` 前台應看到毛玻璃導覽列「慢遊 Lazy Travel」
4. `git status` 確認 `appsettings.Development.json` 未被追蹤

## 注意事項

- 側欄連結指向的 Controller(Reports、VlogPosts、Members、TravelGroups、Notifications、Subscriptions、Analytics、Forum、AdminLogs)**尚未建立**,點了 404 是預期行為,由各組員在自己的分支建立,不要在本任務中補建。
- 不要安裝 EF Core 以外用不到的套件;EF Core 相關(Entity 類別、DbContext、Migration)不在本任務範圍。
- 不要改動附件檔案裡的色票、字型、class 名稱——它們對應團隊 UI 規範 v3.0 與後台規格 v1.2.1,已經過團隊定案。

---

# 檔案內容

## 附件 A:wwwroot/css/brand.css

```css
/* ============================================================
   慢遊 Lazy Travel — 品牌樣式(brand.css)
   依據 UI 規範 v3.0「有機自然 × 海岸旅行」
   放置:wwwroot/css/brand.css,於 _Layout.cshtml 在 bootstrap 之後引入
   ============================================================ */

/* ---------- 1. 設計 Token(CSS 變數) ---------- */
:root {
  /* 海洋藍 Sea(主) */
  --sea-50:  #EDF3F6;
  --sea-100: #D5E5EC;
  --sea-200: #ABC9D6;
  --sea-300: #7BA8BC;
  --sea-400: #4F869F;
  --sea-500: #356E88;   /* 品牌主色(logo、圖示) */
  --sea-600: #2B5A72;   /* 主要按鈕底(配白字,過 AA) */
  --sea-700: #244A5E;
  --sea-800: #1F3D4E;
  --sea-900: #1B3341;

  /* 暖沙黃 Sand(中性基底) */
  --sand-50:  #FAF7F0;  /* 頁面主背景(暖紙白) */
  --sand-100: #F2EDE1;
  --sand-200: #E7DECB;
  --sand-300: #D8C9AC;
  --sand-400: #C6B187;
  --sand-500: #A8916A;

  /* 珊瑚橘 Coral(暖強調,一個畫面最多一個重點) */
  --coral-50:  #FBEDE7;
  --coral-100: #F8DDD1;
  --coral-200: #F1BFA9;
  --coral-300: #EBA07F;
  --coral-400: #E28461;
  --coral-500: #D46A47;
  --coral-600: #B85433;  /* 強調按鈕底(配白字,過 AA) */

  /* 葉綠 Leaf(次要) */
  --leaf-100: #E0E8D6;
  --leaf-300: #A9C09A;
  --leaf-500: #6E8F5E;
  --leaf-600: #567049;

  /* 表面 Surface(絕不用純白 #FFF 當大面積背景) */
  --page:        #FAF7F0;
  --surface:     #FFFDF8;  /* 卡片、彈窗(暖白) */
  --surface-alt: #F2EDE1;
  --border-soft: #E4DBCB;  /* 少用,層級優先靠留白與色塊過渡 */

  /* 文字(不用純黑) */
  --ink:            #22333B;  /* 主要文字(深海炭) */
  --text-secondary: #4E5A5E;
  --text-muted:     #8A9195;

  /* 語意色 */
  --success: #567049;
  --info:    #356E88;
  --warning: #D99A4E;
  --danger:  #C15F4E;  /* 柔和磚紅,非刺眼正紅 */

  /* 字體 */
  --font-display: "LXGW WenKai TC", "Iansui", serif;      /* 標題 */
  --font-body: "Noto Sans TC", "PingFang TC", sans-serif; /* 內文、數據 */

  /* 圓角(曲線打破邊界) */
  --radius-sm: 10px;
  --radius-md: 16px;
  --radius-lg: 24px;
  --radius-pill: 999px;
}

/* ---------- 2. Bootstrap 5.3 變數覆蓋 ---------- */
:root {
  --bs-primary: #2B5A72;
  --bs-primary-rgb: 43, 90, 114;
  --bs-secondary: #A8916A;
  --bs-secondary-rgb: 168, 145, 106;
  --bs-success: #567049;
  --bs-success-rgb: 86, 112, 73;
  --bs-info: #356E88;
  --bs-info-rgb: 53, 110, 136;
  --bs-warning: #D99A4E;
  --bs-warning-rgb: 217, 154, 78;
  --bs-danger: #C15F4E;
  --bs-danger-rgb: 193, 95, 78;

  --bs-body-bg: var(--page);
  --bs-body-color: var(--ink);
  --bs-body-font-family: var(--font-body);
  --bs-border-color: var(--border-soft);
  --bs-border-radius: var(--radius-sm);
  --bs-border-radius-lg: var(--radius-md);
  --bs-link-color: var(--sea-600);
  --bs-link-hover-color: var(--sea-700);
}

/* ---------- 3. 全域基礎 ---------- */
body {
  background-color: var(--page);
  color: var(--ink);
  font-family: var(--font-body);
}

h1, h2, h3, h4, h5, h6,
.navbar-brand {
  font-family: var(--font-display);
  color: var(--sea-800);
}

/* ---------- 4. 按鈕 ---------- */
/* 主要按鈕:海洋藍(白字最低用 -600 才過 WCAG AA) */
.btn-primary {
  --bs-btn-bg: var(--sea-600);
  --bs-btn-border-color: var(--sea-600);
  --bs-btn-hover-bg: var(--sea-700);
  --bs-btn-hover-border-color: var(--sea-700);
  --bs-btn-active-bg: var(--sea-800);
  --bs-btn-active-border-color: var(--sea-800);
  --bs-btn-color: #fff;
  border-radius: var(--radius-pill);
  padding-inline: 1.5rem;
}

/* 強調按鈕:珊瑚橘(關鍵情感 CTA 專用,一頁最多一個) */
.btn-coral {
  --bs-btn-bg: var(--coral-600);
  --bs-btn-border-color: var(--coral-600);
  --bs-btn-hover-bg: var(--coral-500);
  --bs-btn-hover-border-color: var(--coral-500);
  --bs-btn-active-bg: var(--coral-600);
  --bs-btn-color: #fff;
  border-radius: var(--radius-pill);
  padding-inline: 1.5rem;
}

.btn-outline-primary {
  --bs-btn-color: var(--sea-600);
  --bs-btn-border-color: var(--sea-300);
  --bs-btn-hover-bg: var(--sea-50);
  --bs-btn-hover-color: var(--sea-700);
  --bs-btn-hover-border-color: var(--sea-400);
  border-radius: var(--radius-pill);
}

/* ---------- 5. 卡片與表面 ---------- */
.card {
  background-color: var(--surface);
  border: none;                      /* 層級靠色塊與留白,不靠線 */
  border-radius: var(--radius-lg);
  box-shadow: 0 4px 20px rgba(31, 61, 78, 0.06);
}

.bg-surface-alt { background-color: var(--surface-alt); }
.bg-sea-soft    { background-color: var(--sea-50); }
.bg-sand-soft   { background-color: var(--sand-100); }

/* ---------- 6. 導覽列 ---------- */
.navbar-lazy {
  background-color: rgba(250, 247, 240, 0.85);  /* 暖紙白毛玻璃 */
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
}

.navbar-lazy .nav-link {
  color: var(--text-secondary);
  border-radius: var(--radius-pill);
  padding-inline: 1rem;
  transition: background-color .25s ease, color .25s ease;
}

.navbar-lazy .nav-link:hover,
.navbar-lazy .nav-link.active {
  color: var(--sea-700);
  background-color: var(--sea-50);
}

/* ---------- 7. 表單 ---------- */
.form-control, .form-select {
  background-color: var(--surface);
  border-color: var(--border-soft);
  border-radius: var(--radius-md);
  color: var(--ink);
}

.form-control::placeholder { color: var(--text-muted); }

.form-control:focus, .form-select:focus {
  border-color: var(--sea-300);
  box-shadow: 0 0 0 .25rem rgba(53, 110, 136, .12);
}

/* ---------- 8. 標籤(Tag / Badge) ---------- */
.badge-sea   { background-color: var(--sea-100);   color: var(--sea-800);   border-radius: var(--radius-pill); font-weight: 500; }
.badge-sand  { background-color: var(--sand-200);  color: var(--sand-500);  border-radius: var(--radius-pill); font-weight: 500; }
.badge-coral { background-color: var(--coral-100); color: var(--coral-600); border-radius: var(--radius-pill); font-weight: 500; }
.badge-leaf  { background-color: var(--leaf-100);  color: var(--leaf-600);  border-radius: var(--radius-pill); font-weight: 500; }

/* ---------- 9. Footer ---------- */
.footer-lazy {
  background-color: var(--sea-900);
  color: var(--sea-200);
}
.footer-lazy a { color: var(--sea-100); }

/* ---------- 10. 波浪分隔(區塊間用波浪 SVG 取代分割線) ---------- */
.wave-divider {
  display: block;
  width: 100%;
  height: 48px;
  line-height: 0;
}
.wave-divider svg { width: 100%; height: 100%; }

/* ---------- 11. 動效基礎(尊重 reduced-motion) ---------- */
@media (prefers-reduced-motion: no-preference) {
  .btn { transition: transform .2s ease, background-color .25s ease; }
  .btn:hover { transform: translateY(-1px); }
  .card { transition: transform .3s ease, box-shadow .3s ease; }
}

```

## 附件 B:wwwroot/css/admin.css

```css
/* ============================================================
   慢遊 Lazy Travel — 後台管理系統樣式(admin.css)
   依據後台規格 v1.2.1:Ant Design 風格白色側欄 + 麵包屑 + 篩選列
   放置:wwwroot/css/admin.css
   引入順序:bootstrap → brand.css → admin.css(僅後台頁面引入)
   ============================================================ */

/* ---------- 1. 整體版面:側欄 + 主內容 ---------- */
body.admin-body {
  background-color: var(--sea-200);  /* 底色定案:深一階海藍 sea-200 */
  min-height: 100vh;
}

.admin-wrapper {
  display: flex;
  min-height: 100vh;
}

/* ---------- 2. 白色側欄(固定 240px) ---------- */
.admin-sidebar {
  width: 240px;
  flex-shrink: 0;
  background-color: var(--surface);          /* 暖白,非純白 */
  border-right: 1px solid var(--border-soft);
  display: flex;
  flex-direction: column;
  position: sticky;
  top: 0;
  height: 100vh;
}

.admin-sidebar .sidebar-brand {
  font-family: var(--font-display);
  font-size: 1.25rem;
  color: var(--sea-800);
  padding: 1.25rem 1.5rem;
  text-decoration: none;
}

.admin-sidebar .nav {
  flex-direction: column;
  padding: 0 .75rem;
  gap: 2px;
}

.admin-sidebar .nav-link {
  color: var(--text-secondary);
  border-radius: var(--radius-sm);
  padding: .6rem 1rem;
  font-size: .95rem;
  display: flex;
  align-items: center;
  gap: .6rem;
}

.admin-sidebar .nav-link:hover {
  background-color: var(--sand-100);
  color: var(--sea-700);
}

/* Ant Design 式選中狀態:淡底 + 左側短色條 */
.admin-sidebar .nav-link.active {
  background-color: var(--sea-50);
  color: var(--sea-700);
  font-weight: 500;
  position: relative;
}
.admin-sidebar .nav-link.active::before {
  content: "";
  position: absolute;
  left: 0; top: 20%; bottom: 20%;
  width: 3px;
  border-radius: 2px;
  background-color: var(--sea-600);
}

.admin-sidebar .sidebar-footer {
  margin-top: auto;
  padding: 1rem 1.5rem;
  font-size: .8rem;
  color: var(--text-muted);
  border-top: 1px solid var(--border-soft);
}

/* ---------- 3. 主內容區 ---------- */
.admin-main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

/* 頂欄:頁面標題 + 使用者選單 */
.admin-topbar {
  background-color: var(--surface);
  border-bottom: 1px solid var(--border-soft);
  padding: .75rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.admin-topbar .page-title {
  font-family: var(--font-display);
  font-size: 1.15rem;
  color: var(--sea-800);
  margin: 0;
}

/* 麵包屑 */
.admin-breadcrumb {
  padding: .9rem 1.5rem 0;
}
.admin-breadcrumb .breadcrumb {
  margin: 0;
  font-size: .85rem;
  --bs-breadcrumb-divider-color: var(--text-muted);
}
.admin-breadcrumb .breadcrumb-item a {
  color: var(--text-secondary);
  text-decoration: none;
}
.admin-breadcrumb .breadcrumb-item.active { color: var(--ink); }

.admin-content {
  padding: 1rem 1.5rem 2rem;
}

/* ---------- 4. 篩選列(filter row) ---------- */
.filter-row {
  background-color: var(--surface);
  border-radius: var(--radius-md);
  padding: .9rem 1.1rem;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: .6rem;
  margin-bottom: 1rem;
}

.filter-chip {
  border: 1px solid var(--border-soft);
  background-color: var(--surface);
  color: var(--text-secondary);
  border-radius: var(--radius-pill);
  padding: .3rem .95rem;
  font-size: .85rem;
  cursor: pointer;
  transition: background-color .2s ease, color .2s ease;
}
.filter-chip:hover { background-color: var(--sand-100); }
.filter-chip.on {
  background-color: var(--sea-600);
  border-color: var(--sea-600);
  color: #fff;
}

/* ---------- 5. 統計卡與資料表 ---------- */
.stat-card {
  background-color: var(--surface);
  border-radius: var(--radius-md);
  padding: 1.1rem 1.25rem;
}
.stat-card .stat-label { font-size: .82rem; color: var(--text-muted); }
.stat-card .stat-value {
  font-size: 1.6rem;
  font-weight: 600;
  color: var(--sea-800);
  font-variant-numeric: tabular-nums;
}

.admin-table-card {
  background-color: var(--surface);
  border-radius: var(--radius-md);
  overflow: hidden;
}
.admin-table-card .table { margin: 0; --bs-table-bg: transparent; }
.admin-table-card thead th {
  background-color: var(--sea-50);
  color: var(--text-secondary);
  font-weight: 500;
  font-size: .85rem;
  border-bottom: none;
  padding: .75rem 1rem;
}
.admin-table-card tbody td {
  padding: .8rem 1rem;
  border-color: var(--sand-100);
  vertical-align: middle;
}
.admin-table-card tbody tr:hover { background-color: var(--sea-50); }

/* 狀態章(高風險用珊瑚橘) */
.status-pill {
  display: inline-block;
  border-radius: var(--radius-pill);
  padding: .2rem .7rem;
  font-size: .8rem;
  font-weight: 500;
}
.status-ok      { background-color: var(--leaf-100);  color: var(--leaf-600); }
.status-pending { background-color: var(--sand-200);  color: var(--sand-500); }
.status-risk    { background-color: var(--coral-100); color: var(--coral-600); }

/* ---------- 6. 響應式:窄螢幕收合側欄 ---------- */
@media (max-width: 991.98px) {
  .admin-sidebar {
    position: fixed;
    left: -240px;
    z-index: 1040;
    transition: left .25s ease;
  }
  .admin-sidebar.show { left: 0; }
}

/* ---------- 7-1. 側欄分組與標籤 ---------- */
.admin-sidebar .nav-group-label {
  font-size: .72rem;
  color: var(--text-muted);
  padding: .9rem 1rem .25rem;
  letter-spacing: .05em;
}

.nav-badge {
  background-color: var(--coral-600);
  color: #fff;
  border-radius: var(--radius-pill);
  font-size: .7rem;
  padding: .1rem .45rem;
  margin-left: auto;
}

.nav-tag {
  background-color: var(--sand-200);
  color: var(--sand-500);
  border-radius: var(--radius-pill);
  font-size: .68rem;
  padding: .08rem .4rem;
  margin-left: auto;
}

```

## 附件 C:Views/Shared/_Layout.cshtml

```cshtml
<!DOCTYPE html>
<html lang="zh-Hant">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - 慢遊 Lazy Travel</title>

    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=LXGW+WenKai+TC&family=Noto+Sans+TC:wght@400;500;700&display=swap" rel="stylesheet">

    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/brand.css" asp-append-version="true" />
    @await RenderSectionAsync("Styles", required: false)
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-lg navbar-lazy sticky-top">
            <div class="container">
                <a class="navbar-brand" asp-controller="Home" asp-action="Index">慢遊 Lazy Travel</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#mainNav"
                        aria-controls="mainNav" aria-expanded="false" aria-label="切換導覽">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="mainNav">
                    <ul class="navbar-nav ms-auto align-items-lg-center gap-lg-1">
                        <li class="nav-item"><a class="nav-link" asp-controller="Home" asp-action="Index">首頁</a></li>
                        <li class="nav-item"><a class="nav-link" href="#">找旅伴</a></li>
                        <li class="nav-item"><a class="nav-link" href="#">揪團行程</a></li>
                        @if (User.Identity?.IsAuthenticated == true)
                        {
                            <li class="nav-item"><a class="nav-link" href="#">@User.Identity.Name</a></li>
                        }
                        else
                        {
                            <li class="nav-item ms-lg-2">
                                <a class="btn btn-primary btn-sm" href="#">登入 / 註冊</a>
                            </li>
                        }
                    </ul>
                </div>
            </div>
        </nav>
    </header>

    <main role="main">
        @RenderBody()
    </main>

    <footer class="footer-lazy mt-5 py-4">
        <div class="container d-flex flex-column flex-md-row justify-content-between gap-2">
            <span>&copy; 2026 慢遊 Lazy Travel</span>
            <span>慢慢走,才看得見風景。</span>
        </div>
    </footer>

    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>

```

## 附件 D:Areas/Admin/Controllers/DashboardController.cs

```csharp
using Microsoft.AspNetCore.Mvc;

namespace LazyTravel.Areas.Admin.Controllers
{
    // 之後 Cookie 認證與 Role 授權建好後,改成:
    // [Authorize(Roles = "Admin,SuperAdmin")]
    [Area("Admin")]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "總覽";
            return View();
        }
    }
}

```

## 附件 E:Areas/Admin/Views/_ViewStart.cshtml

```cshtml
@{
    Layout = "_AdminLayout";
}

```

## 附件 F:Areas/Admin/Views/Shared/_AdminLayout.cshtml

```cshtml
<!DOCTYPE html>
<html lang="zh-Hant">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - LazyTravel 後台管理</title>

    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=LXGW+WenKai+TC&family=Noto+Sans+TC:wght@400;500;700&display=swap" rel="stylesheet">

    <link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="~/css/brand.css" asp-append-version="true" />
    <link rel="stylesheet" href="~/css/admin.css" asp-append-version="true" />
    @await RenderSectionAsync("Styles", required: false)
</head>
<body class="admin-body">
<div class="admin-wrapper">

    @* ===== 白色側欄 ===== *@
    <aside class="admin-sidebar" id="adminSidebar">
        <a class="sidebar-brand" asp-area="Admin" asp-controller="Dashboard" asp-action="Index">LazyTravel 後台</a>
        <nav class="nav">
            @* active 判斷:比對目前 Controller 名稱 *@
            @{
                var currentController = (string?)ViewContext.RouteData.Values["controller"];
                string NavActive(string controller) =>
                    string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase) ? "active" : "";
            }
            <a class="nav-link @NavActive("Dashboard")" asp-area="Admin" asp-controller="Dashboard" asp-action="Index">總覽</a>

            <div class="nav-group-label">審核中心</div>
            <a class="nav-link @NavActive("Reports")" asp-area="Admin" asp-controller="Reports" asp-action="Index">
                檢舉處理
                @* 待處理數量徽章:由 ViewComponent 或 ViewBag 提供,尚未實作前可先註解
                <span class="nav-badge">@ViewBag.PendingReportCount</span> *@
            </a>

            <div class="nav-group-label">內容管理</div>
            <a class="nav-link @NavActive("VlogPosts")" asp-area="Admin" asp-controller="VlogPosts" asp-action="Index">Vlog 行程文章</a>
            <a class="nav-link @NavActive("Forum")" asp-area="Admin" asp-controller="Forum" asp-action="Index">論壇管理</a>

            <div class="nav-group-label">會員與社群</div>
            <a class="nav-link @NavActive("Members")" asp-area="Admin" asp-controller="Members" asp-action="Index">會員管理</a>
            <a class="nav-link @NavActive("TravelGroups")" asp-area="Admin" asp-controller="TravelGroups" asp-action="Index">揪團管理</a>

            <div class="nav-group-label">系統</div>
            <a class="nav-link @NavActive("Notifications")" asp-area="Admin" asp-controller="Notifications" asp-action="Index">公告與通知</a>
            <a class="nav-link @NavActive("Subscriptions")" asp-area="Admin" asp-controller="Subscriptions" asp-action="Index">訂閱方案</a>
            <a class="nav-link @NavActive("Analytics")" asp-area="Admin" asp-controller="Analytics" asp-action="Index">數據分析</a>
            @* 操作紀錄僅超級管理員可見:待 Role 授權機制建好後改用 policy 判斷 *@
            <a class="nav-link @NavActive("AdminLogs")" asp-area="Admin" asp-controller="AdminLogs" asp-action="Index">操作紀錄</a>
        </nav>
        <div class="sidebar-footer">LazyTravel v1.2.1</div>
    </aside>

    @* ===== 主內容區 ===== *@
    <div class="admin-main">

        <header class="admin-topbar">
            <div class="d-flex align-items-center gap-2">
                <button class="btn btn-sm btn-outline-primary d-lg-none" type="button"
                        onclick="document.getElementById('adminSidebar').classList.toggle('show')">
                    選單
                </button>
                <h1 class="page-title">@ViewData["Title"]</h1>
            </div>
            <div class="d-flex align-items-center gap-3">
                <span class="text-secondary small">@User.Identity?.Name</span>
                <form asp-controller="Account" asp-action="Logout" asp-area="" method="post" class="m-0">
                    <button type="submit" class="btn btn-sm btn-outline-primary">登出</button>
                </form>
            </div>
        </header>

        @* ===== 麵包屑:各頁用 section 覆寫,未提供則只顯示「後台」 ===== *@
        <div class="admin-breadcrumb">
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb">
                    <li class="breadcrumb-item"><a asp-area="Admin" asp-controller="Dashboard" asp-action="Index">後台</a></li>
                    @if (IsSectionDefined("Breadcrumb"))
                    {
                        @await RenderSectionAsync("Breadcrumb")
                    }
                    else
                    {
                        <li class="breadcrumb-item active" aria-current="page">@ViewData["Title"]</li>
                    }
                </ol>
            </nav>
        </div>

        <main class="admin-content">
            @RenderBody()
        </main>
    </div>
</div>

<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
@await RenderSectionAsync("Scripts", required: false)
</body>
</html>

```

## 附件 G:Areas/Admin/Views/Dashboard/Index.cshtml

```cshtml
@{
    ViewData["Title"] = "總覽";
}

<div class="admin-table-card" style="padding:4rem 2rem; text-align:center;">
    <div style="font-family:var(--font-display); font-size:1.2rem; color:var(--sea-800); margin-bottom:.5rem;">此頁內容待設計</div>
    <p style="color:var(--text-muted); margin:0;">
        「總覽」的呈現方式由負責組員自行發想。<br />
        可使用共用元件:統計卡(.stat-card)、篩選列(.filter-row)、資料表(.admin-table-card)、狀態章(.status-pill)。
    </p>
</div>

```
