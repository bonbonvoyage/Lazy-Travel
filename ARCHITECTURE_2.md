# LazyTravel 架構二：分離式後端 API

## 概述

從單一整合式專案升級為 **獨立的兩個後端 API** + **共享類庫** 架構。

```
SQL Server (共用資料庫)
    ↓
LazyTravel.Shared (Models, Services, DbContext)
    ↓
    ├─ tours-api (前台 API)
    └─ admin-api (後台 API)
```

## 項目構成

### 1️⃣ LazyTravel.Shared (類庫)
**用途**：集中管理共享代碼，避免重複

**包含**：
- `Models/` - 資料模型 (Models, ViewModels, EfModels)
- `Services/` - 業務邏輯層 (所有 Service 實現)

**依賴**：
- Entity Framework Core 8.0
- BCrypt.Net
- AWSSDK.S3
- 兩個 API 都會引用此項目

---

### 2️⃣ tours-api (前台 API - 旅遊者端)

**目的**：向旅遊者提供數據 API

**控制器**：
- `ExploreController` - 探索/搜尋旅遊資訊
- `HomeController` - 首頁數據
- `ReportController` - 用戶舉報功能

**特性**：
- ✅ 不需登入認證（部分 public endpoints）
- ✅ ReportService 用於用戶舉報
- ✅ 圖床支援（Vlog 圖片上傳）
- ✅ 簡輕配置（無 Admin Auth）

**啟動指令**（待 launchSettings.json 設定）：
```bash
cd tours-api
dotnet run
```

**預期 Port**：5001 (HTTPS)

---

### 3️⃣ admin-api (後台 API - 管理者端)

**目的**：提供後台管理功能

**控制器** (在 Areas/Admin 下)：
- `AuthController` - 登入 / 登出
- `DashboardController` - 總覽數據
- `MembersController` - 會員管理
- `EmployeesController` - 員工管理
- `RolesController` - 角色與權限
- `VlogPostsController` - 旅遊日誌審核
- `ReportsController` - 檢舉中心
- `TravelGroupsController` - 旅遊團管理

**特性**：
- 🔐 強制 Cookie 認證 ("AdminAuth")
- 🛡️ 細粒度權限檢查 (RBAC Policies)
- 📝 操作審計日誌
- 📧 通知系統

**啟動指令**（待 launchSettings.json 設定）：
```bash
cd admin-api
dotnet run
```

**預期 Port**：5002 (HTTPS)

---

## 遷移檢查清單

### 資料庫配置
```json
// 兩個 API 的 appsettings.Development.json 都需要這行：
"ConnectionStrings": {
  "DefaultConnection": "Server={YOUR_SQL_SERVER};Database=LazyTravel;..."
}
```

### launchSettings.json
需要為 tours-api 和 admin-api 各設定一個 profile：
```json
{
  "profiles": {
    "tours-api": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "admin-api": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "https://localhost:5002;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Cloudflare R2 配置
```json
// appsettings.json
"CloudflareR2": {
  "ServiceUrl": "https://...",
  "AccessKey": "...",
  "SecretKey": "..."
}
```

---

## 編譯與執行

### 編譯所有項目
```bash
dotnet build
```

### 個別執行
```bash
# 前台 API (Port 5001)
cd tours-api
dotnet run

# 後台 API (Port 5002) - 另一個終端
cd admin-api
dotnet run
```

### 同時執行（VS 多啟動項目）
在 Visual Studio 中：
1. 右鍵 `LazyTravel.sln`
2. Properties → Startup Project
3. Select "Multiple startup projects"
4. tours-api: Start
5. admin-api: Start

---

## 前端連接

### 前台 Vue 專案
指向 `https://localhost:5001/`

```javascript
// 主要端點
GET    /Home/Index           // 首頁數據
GET    /Explore/Search       // 搜尋旅遊資訊
POST   /Report/Submit        // 舉報內容
```

### 後台 Vue 專案
指向 `https://localhost:5002/`

```javascript
// 主要端點
GET    /Admin/Auth/Login           // 登入頁
POST   /Admin/Members/List         // 會員列表
POST   /Admin/VlogPosts/Audit      // Vlog 審核
POST   /Admin/Reports/List         // 檢舉列表
```

---

## 共享代碼管理

### 添加新 Model
1. 寫在 `LazyTravel.Shared/Models/`
2. 兩個 API 自動可用（通過 `dotnet restore`）

### 添加新 Service
1. 寫在 `LazyTravel.Shared/Services/`
2. 在 tours-api 或 admin-api 的 `Program.cs` 中註冊

### DbContext 升級
```bash
# 在 LazyTravel.Shared 目錄執行
dotnet ef migrations add [MigrationName] -o Migrations
dotnet ef database update
```

---

## 常見問題

**Q: 為什麼要分開兩個 API？**
- 獨立部署 (前台不受後台故障影響)
- 獨立擴展 (不同負載可獨立調整)
- 權限隔離 (不同認證機制)

**Q: 兩個 API 都需要部署嗎？**
- 是。它們是獨立的服務，都要運行才能提供完整功能。

**Q: 如何處理跨 API 的資料同步？**
- 暫時共享同一個資料庫（SQLServer）
- 未來可考慮 Event Bus / Message Queue 進階方案

**Q: 能用舊的單一 LazyTravel 項目嗎？**
- 可以，但建議遷移到新架構，舊項目可作參考保留。

---

## 下一步

1. ✅ 設定 launchSettings.json（各 API 獨立 Port）
2. ✅ 確認資料庫連接字串
3. ✅ `dotnet build` 驗證編譯
4. ✅ 前後端分別啟動測試
5. 📋 前台 Vue 連接 tours-api
6. 📋 後台 Vue 連接 admin-api
