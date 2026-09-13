# LazyTravel Docker 使用指南

## 這套 Docker 化做了什麼

Docker 將 LazyTravel 的執行環境包成可重複使用的 image；同一個 image 可以在開發機、GitHub Actions 與雲端主機執行，降低「在我的電腦可以跑」的環境差異。

本專案包含三個容器服務：

| 服務 | Docker 角色 | 對外連接埠 |
| --- | --- | --- |
| `tours-api` | 使用者旅遊與揪團網站 | `8080` |
| `admin-api` | 後台管理網站 | `8081` |
| `sqlserver` | SQL Server 資料庫 | `1433` |

兩個 ASP.NET Core image 都採用 multi-stage build：SDK 階段只負責 restore / publish，最終 runtime image 只保留執行所需檔案；資料庫、Cookie 資料保護金鑰和敏感設定則由 volume / 環境變數提供，而非寫入 image。

## 第一次本機執行

1. 安裝並啟動 Docker Desktop。
2. 在專案根目錄複製環境變數範本：

   ```bash
   cp .env.example .env
   ```

3. 開啟 `.env`，將 `MSSQL_SA_PASSWORD` 改為自己的強密碼。不要將 `.env` 提交到 Git。
4. 啟動服務：

   ```bash
   docker compose up --build -d
   ```

5. 查看狀態與健康檢查：

   ```bash
   docker compose ps
   curl http://localhost:8080/health/live
   curl http://localhost:8081/health/live
   ```

6. 停止服務但保留資料：

   ```bash
   docker compose down
   ```

資料庫 volume 會保留資料。若只是想清除這組 Docker 的本機資料後重新開始，才使用：

```bash
docker compose down --volumes
```

## 資料庫注意事項

本專案目前以 SQL 建置腳本維護 schema，而不是 EF Core Migrations。Docker Compose 會建立 SQL Server 容器與永久資料 volume；第一次啟動後，仍要以 SQL Server Management Studio 或 `sqlcmd` 對該容器執行目前資料庫建置腳本 `sql/LazyTravelDB_Build.sql`，再依需要套用後續增量 SQL。

因為建置腳本會刪除既有資料表，絕不能在有正式資料的資料庫直接執行。正式環境應採備份、審核後的 migration / release SQL 流程。

## CI / CD 如何驗證 Docker

每次 push 與針對 `main`、`feature_TourGroups_怡茜` 的 Pull Request 都會執行 CI：

1. restore、build、test、publish .NET 專案。
2. 建置 `tours-api` 與 `admin-api` Docker image。
3. 實際啟動兩個 image，呼叫 `/health/live` 驗證容器可運行。

當建立 `vX.Y.Z` Git tag 時，CD 會將兩個 image 推送至 GitHub Container Registry (GHCR)。正式雲端部署尚需在 GitHub 環境設定中配置部署 hook、健康檢查 URL 與機密值；這些機密不會放入 repository。
