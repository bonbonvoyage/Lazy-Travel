# LazyTravel CI/CD

## 目前完成範圍

這套流程把前台 `tours-api`、後台 `admin-api` 與共用類別庫 `LazyTravel.Shared` 納入同一條交付鏈。

- Pull Request 與 `main` / `dev` push 會執行 restore、build、test、publish，並啟動兩個發布產物做健康檢查。
- CI 會分別建置兩個 production Docker image，但不會推送。
- 建立 `vX.Y.Z` tag 或手動執行 CD 時，會把兩個 image 推送到 GitHub Container Registry。
- production deployment 使用兩個通用 deploy hook，因此可接 Render、Railway 或其他能以 webhook 觸發部署的平台。
- 部署後可用 `/health/live` 驗證兩個服務是否啟動。

尚未設定雲端主機、SQL Server 與 deploy hook 前，CD 會停在「產生並發布可部署映像」。這屬於 continuous delivery。設定 `ENABLE_DEPLOY=true` 與 deploy hook 後，流程才會自動部署到 production。

## 流程

```text
Pull Request / push
        |
        +-- dotnet restore -> build -> test -> publish
        |
        +-- Docker build: tours-api + admin-api

vX.Y.Z tag / manual release
        |
        +-- Push images to GHCR
        |
        +-- production environment approval (optional)
        |
        +-- deploy hooks -> /health/live verification
```

## GitHub 設定

### Actions

Repository Settings > Actions > General：

1. 啟用 GitHub Actions。
2. Workflow permissions 保持最小權限即可，`cd.yml` 已明確要求 `packages: write`。
3. 建議對 `main` 啟用 branch protection，要求 CI 的 .NET 與兩個 container jobs 通過。

### Production environment

Repository Settings > Environments > New environment，建立 `production`。

建議加入 Required reviewers，讓正式部署需要人工核准。

Environment secrets：

| 名稱 | 用途 |
| --- | --- |
| `TOURS_DEPLOY_HOOK_URL` | 前台服務的部署 webhook |
| `ADMIN_DEPLOY_HOOK_URL` | 後台服務的部署 webhook |

Environment variables：

| 名稱 | 範例 | 用途 |
| --- | --- | --- |
| `ENABLE_DEPLOY` | `false` | 確認雲端設定完成後改成 `true` |
| `TOURS_PUBLIC_URL` | `https://travel.example.com` | GitHub deployment 顯示網址 |
| `TOURS_HEALTH_URL` | `https://travel.example.com/health/live` | 前台部署驗證 |
| `ADMIN_HEALTH_URL` | `https://admin.example.com/health/live` | 後台部署驗證 |

應用程式需要的資料庫、R2 與 OAuth 憑證應設定在部署平台，不要放進 workflow 或 repository：

```text
ConnectionStrings__DefaultConnection
CloudflareR2__ServiceUrl
CloudflareR2__AccessKey
CloudflareR2__SecretKey
CloudflareR2__BucketName
CloudflareR2__PublicUrl
Authentication__Google__ClientId
Authentication__Google__ClientSecret
Authentication__Facebook__AppId
Authentication__Facebook__AppSecret
Cors__AllowedOrigins__0
DataProtection__KeyPath
```

`DataProtection__KeyPath` 必須指向可持久化的 volume。否則每次重新部署後 Cookie key 會消失，所有使用者都會被登出。前台與後台應使用各自的 key volume。

## 發布方式

先確認 CI 全部通過，再建立語意化版本 tag：

```bash
git tag v0.1.0
git push origin v0.1.0
```

CD 會發布：

```text
ghcr.io/<owner>/lazytravel-tours-api:v0.1.0
ghcr.io/<owner>/lazytravel-admin-api:v0.1.0
```

也可以從 Actions > CD > Run workflow 手動發布。`deploy=false` 只發布映像，不觸發 production。

## 本機容器驗證

```bash
cp .env.example .env
# 編輯 .env，至少提供符合 SQL Server 密碼規則的 MSSQL_SA_PASSWORD
docker compose build
docker compose up -d
curl http://localhost:8080/health/live
curl http://localhost:8081/health/live
```

資料庫 schema 目前仍由 `sql/` 中的 SQL 腳本管理。為避免誤改 production 資料，pipeline 不會自動執行 schema 腳本。正式部署前應先建立 migration strategy，並把 migration 做成需要人工核准的獨立 job。

## 密鑰事件處理

舊版 `appsettings.json` 曾提交 SQL 密碼與 R2 access key / secret key。刪除目前版本中的字串無法清除 Git 歷史。

1. 立即到 Cloudflare R2 撤銷並重建舊金鑰。
2. 修改資料庫密碼。
3. 把新值只放在部署平台或 GitHub environment secrets。
4. 若 repository 曾公開，評估使用 `git filter-repo` 清理歷史。這會重寫 commit，必須由全組協調後執行。

## 對外說法

在 production 尚未完成前，建議描述為：

> 我為前後台建立 GitHub Actions CI/CD。Pull Request 會自動編譯、測試並驗證 Docker image；版本 tag 會發布兩個 image 到 GHCR。正式環境採受保護的 deploy hook 與健康檢查，目前雲端資料庫和主機憑證仍在設定，所以尚未宣稱已成功上線。
