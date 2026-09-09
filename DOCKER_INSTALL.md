# Docker 安裝指南

## Windows 10 Pro 安裝步驟

### 步驟 1: 下載 Docker Desktop
1. 前往 https://www.docker.com/products/docker-desktop
2. 點擊 "Download for Windows"
3. 下載 `Docker Desktop Installer.exe`

### 步驟 2: 安裝
1. 雙擊安裝檔
2. 勾選 "WSL 2" 選項（預設已勾）
3. 完成安裝並重啟電腦

### 步驟 3: 驗證安裝
```powershell
docker --version
docker run hello-world
```

---

## 部署指令

### 從此目錄執行：

```bash
# 構建映像
docker-compose build

# 啟動服務（包含 SQL Server）
docker-compose up -d

# 檢查服務狀態
docker-compose ps

# 查看日誌
docker-compose logs -f web

# 停止
docker-compose down
```

### 訪問應用
- Web: http://localhost:5000
- HTTPS: https://localhost:5001

### SQL Server 連線
- Server: localhost,1433
- User: sa
- Password: LazyTravel@123 (請改強密碼)
- Database: LazyTravel
