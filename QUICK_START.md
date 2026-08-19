# 架構二 - 快速開始

## 📋 項目清單

| 項目 | 說明 | Port | 路徑 |
|------|------|------|------|
| **LazyTravel.Shared** | 共享類庫（Models, Services, ViewModels） | — | `./LazyTravel.Shared/` |
| **tours-api** | 前台旅遊 API | 7001 | `./tours-api/` |
| **admin-api** | 後台管理 API | 7002 | `./admin-api/` |

---

## 🚀 快速啟動

### 前置條件
- .NET 8.0 SDK
- SQL Server (配置 appsettings.Development.json)

### 編譯
```bash
# 在專案根目錄
dotnet build
```

### 啟動前台 API (Port 7001)
```bash
cd tours-api
dotnet run
```

### 啟動後台 API (Port 7002) - 另一個終端
```bash
cd admin-api
dotnet run
```

### 同時啟動 (VS Code)
在根目錄執行：
```bash
# 終端 1
cd tours-api && dotnet run

# 終端 2
cd admin-api && dotnet run
```

---

## 🔌 API 端點

### 前台 (tours-api - Port 7001)

```
GET    /                          首頁
GET    /Explore/Index             探索旅遊資訊
GET    /Explore/Search            搜尋旅遊
GET    /Explore/Details/{id}      旅遊詳細資訊
POST   /Report/Submit             用戶舉報
```

### 後台 (admin-api - Port 7002)

```
POST   /Admin/Auth/Login                   管理員登入
GET    /Admin/Dashboard                    儀表板
GET    /Admin/Members                      會員列表
POST   /Admin/Members/Block                停權會員
GET    /Admin/VlogPosts                    旅遊日誌審核
POST   /Admin/VlogPosts/Publish            發佈日誌
GET    /Admin/Reports                      檢舉管理
POST   /Admin/Reports/Audit                審核檢舉
GET    /Admin/Employees                    員工管理
GET    /Admin/Roles                        角色管理
```

---

## 📂 項目結構

```
LazyTravel.Shared/
  ├─ Models/          # 資料模型、EF Context
  ├─ Services/        # 業務邏輯層
  ├─ ViewModels/      # 前端數據模型
  └─ *.csproj

tours-api/
  ├─ Controllers/     # 前台控制器 (Explore, Home, Report)
  ├─ Views/           # Razor 視圖
  ├─ wwwroot/         # 靜態資源
  ├─ Program.cs       # 啟動配置
  └─ tours-api.csproj

admin-api/
  ├─ Areas/Admin/     # 後台區域 (認證、會員、報表等)
  ├─ Views/           # 後台視圖
  ├─ wwwroot/         # 靜態資源
  ├─ Program.cs       # 啟動配置
  └─ admin-api.csproj
```

---

## ⚙️ 配置

### 共享資料庫連接

編輯 `tours-api/appsettings.Development.json` 和 `admin-api/appsettings.Development.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=LazyTravel;User Id=sa;Password=YOUR_PASSWORD;"
  },
  "CloudflareR2": {
    "ServiceUrl": "https://...",
    "AccessKey": "...",
    "SecretKey": "...",
    "BucketName": "lazytravel",
    "PublicUrl": "https://..."
  }
}
```

---

## 🔍 除錯

### 編譯錯誤
```bash
# 完全清理並重新建置
dotnet clean
dotnet build
```

### 資料庫連接問題
1. 確認 SQL Server 正在運行
2. 檢查 appsettings.Development.json 的 Server= 是否正確
3. 檢查防火牆允許 SQL 連接 (Port 1433)

### 前後端分離檢查
- 前台 Vue 應連接到 `https://localhost:7001`
- 後台 Vue 應連接到 `https://localhost:7002`

---

## 📚 相關文件

- [ARCHITECTURE_2.md](./ARCHITECTURE_2.md) - 詳細架構說明
- [FRONTEND_BACKEND_SPLIT.md](./FRONTEND_BACKEND_SPLIT.md) - 前後端分離規範

---

## ✅ 檢查清單

在開始開發前，確認以下項目：

- [ ] .NET 8.0 SDK 已安裝 (`dotnet --version`)
- [ ] SQL Server 已配置
- [ ] `dotnet build` 編譯成功
- [ ] tours-api 能在 http://localhost:5000 啟動
- [ ] admin-api 能在 http://localhost:5001 啟動
- [ ] 前台 Vue 專案可連接 tours-api
- [ ] 後台 Vue 專案可連接 admin-api

---

**架構二已準備就緒！** 🎉
