# LazyTravel 前台 UI 側邊欄風格規格書

## 1. 修改範圍

本次調整以 `tours-api` 前台使用者介面為主，套用附件參考圖中的固定左側欄位、毛玻璃材質、白色卡片區塊與羅勒綠重點色。

不調整後台管理系統與其他組員功能邏輯。此次主要修改頁面與樣式：

- `tours-api/Views/Explore/Index.cshtml`
- `tours-api/Views/Explore/Details.cshtml`
- `tours-api/wwwroot/css/home.css`
- `tours-api/wwwroot/css/travelgroup-details.css`

另外新增建置排除設定：

- `Directory.Build.props`

## 2. 整體視覺方向

### 2.1 設計風格

- 主視覺：清爽旅遊感、白色留白、柔和水藍毛玻璃側欄。
- 重點色：羅勒綠，用於搜尋按鈕、頁籤選取、狀態與主要操作。
- 卡片風格：白色或半透明白底，低飽和陰影，避免過度厚重。
- 側邊欄：固定在左側，使用水藍色半透明毛玻璃效果。
- 主要內容：置於右側內容區，保留明確板塊分割，但不使用過多硬線條。

### 2.2 色彩規格

| 用途 | 色碼 | 說明 |
| --- | --- | --- |
| 羅勒綠主色 | `#5F7F52` | 主要按鈕、頁籤選取、重點狀態 |
| 羅勒綠深色 | `#46643E` | Hover、文字重點、圖示強調 |
| 羅勒綠淡色 | `#E6F0E2` | 標籤、淡底提示 |
| 文字主色 | `#0F172A` | 主要標題與重要文字 |
| 文字次色 | `#64748B` | 輔助資訊、日期、說明 |
| 背景白 | `#F8FBFC` | 前台頁面底色 |
| 毛玻璃白 | `rgba(255,255,255,.62)` | 搜尋列、按鈕、卡片局部 |
| 毛玻璃邊線 | `rgba(255,255,255,.58)` | 半透明板塊邊界 |

## 3. 版面結構

### 3.1 App Shell

前台頁面採用左右分欄：

- 左側：固定側邊欄。
- 右側：主要內容區。
- 桌機版主內容使用 `margin-left` 保留側欄空間。
- 小螢幕時側欄縮小，主內容同步縮排。

主要 CSS 類別：

- `.app-shell`
- `.shell-wrap`
- `.shell-topbar`
- `.shell-title`
- `.shell-user`
- `.glass-panel`

### 3.2 固定側邊欄

側邊欄使用 `_HomeSidebar.cshtml` 作為共用 partial view。

視覺規格：

- 寬度：展開約 `224px`。
- 縮合寬度：約 `76px`。
- 位置：`fixed`，固定左側。
- 高度：接近視窗高度，與主要頁面視覺高度一致。
- 背景：水藍毛玻璃。
- 選取項目：白色半透明毛玻璃底，文字可維持深色以保證可讀性。

側欄內容：

- Logo 與 `LazyTravel` 品牌名稱。
- 三條線收合按鈕。
- 主選單：
  - 首頁
  - 揪團找旅伴
  - 揪團行程分享
  - 收藏行程
  - 個人中心
  - 關於我們
- 底部：
  - 通知
  - 使用者頭像與名稱

### 3.3 頂部使用者區

右上角保留：

- 小鈴鐺通知 icon。
- 圓形使用者頭像。
- 使用者名稱。

此區主要用於登入後會員狀態提示，不放大型導覽列，以維持左側固定導覽的一致性。

## 4. 揪團行程分享列表頁

檔案：

- `tours-api/Views/Explore/Index.cshtml`

頁面標題：

- `揪團行程分享`

### 4.1 搜尋引擎

搜尋列參考附件樣式，使用橢圓長條形外框與毛玻璃卡片底。

欄位：

- 區域：下拉選單，資料來源為 `Model.Regions`。
- 國家：文字輸入，目前對應 `keyword` 搜尋。
- 行程區間：目前為 UI 預留欄位。
- 搜尋按鈕：圓形羅勒綠放大鏡按鈕。

表單方法：

- `GET`

送出欄位：

- `region`
- `keyword`

### 4.2 書籤頁籤

頁籤文字：

- `所有文章`
- `我的文章`
- `文章推薦`

設計：

- 書籤式分頁外觀。
- 選取狀態使用羅勒綠底線與淡色背景。
- `揪團推薦` 已調整為 `文章推薦`。

### 4.3 新增文章按鈕

按鈕位置：

- 文章清單區塊上方。

設計：

- 毛玻璃外觀。
- 圖示為 `+`。
- 文字為 `新增文章`。
- 後續可串接新增揪團文章頁。

### 4.4 文章卡片

卡片欄位：

- 封面圖片
- 收藏愛心
- 文章標題
- 國家/地區
- 旅遊日期
- 作者頭像
- 作者名稱
- 愛心數
- 留言數

資料來源：

- 優先顯示 `Model.Ranking.Take(3)`。
- 若排名資料不足，改由 `Model.SideList` 與 `Model.Featured` 補足。

使用資料模型：

- `VlogPost.PostId`
- `VlogPost.Title`
- `VlogPost.Destination`
- `VlogPost.MediaUrl`
- `VlogPost.TravelDate`
- `VlogPost.TravelDays`
- `VlogPost.Member`

## 5. 揪團文章詳細頁

檔案：

- `tours-api/Views/Explore/Details.cshtml`

### 5.1 頁面結構

主要區塊：

- 返回連結：`回揪團行程分享`
- 文章標題
- 作者資訊
- Hero 封面圖片
- 文章資訊側欄
- 文章內容
- 每日行程內容
- 收藏與按讚操作

### 5.2 Hero 圖片

使用：

- `Model.MediaUrl`

若沒有圖片：

- 使用空白或預設背景，不影響頁面結構。

### 5.3 文章資訊區

顯示欄位：

- 國家/地區：`Model.Destination`
- 旅遊天數：`Model.TravelDays`
- 日期：`Model.TravelDate`
- 作者：`Model.Member`
- 按讚數：`ViewBag.LikeCount`
- 收藏數：`ViewBag.FavoriteCount`

### 5.4 行程內容

每日行程資料來源：

- `ViewBag.Nodes`

呈現方式：

- 依日期或 Day 分組。
- 每日用 `DAY 1`、`DAY 2` 等標籤顯示。
- 每個停靠點以簡潔卡片呈現。

## 6. 揪團詳細頁局部調整

檔案：

- `tours-api/wwwroot/css/travelgroup-details.css`

### 6.1 更多圖片按鈕

原本更多圖片按鈕若佔滿圖片區塊，會影響圖片觀看。

調整後：

- 改為右下角小型毛玻璃按鈕。
- 不再覆蓋整張圖片。
- 保留 `更多圖片` 文案。

### 6.2 操作按鈕

操作按鈕包含：

- 申請加入
- 退出揪團
- 私訊團主
- 收藏房間
- 檢舉揪團

樣式方向：

- 使用與左側欄位相近的毛玻璃材質。
- Hover 改為羅勒綠系。
- 保留圖示與文字。

## 7. 共用 CSS 元件

檔案：

- `tours-api/wwwroot/css/home.css`

新增或強化的主要類別：

| 類別 | 用途 |
| --- | --- |
| `.app-shell` | 前台左右分欄主容器 |
| `.shell-wrap` | 右側主要內容容器 |
| `.shell-topbar` | 頁面標題與會員狀態區 |
| `.shell-title` | 大標題 |
| `.shell-user` | 右上角會員資訊 |
| `.shell-bell` | 通知鈴鐺 |
| `.shell-avatar` | 圓形頭像 |
| `.glass-panel` | 毛玻璃內容板塊 |
| `.front-search` | 搜尋引擎列 |
| `.bookmark-tabs` | 書籤式頁籤 |
| `.bookmark-tab` | 單一頁籤 |
| `.article-list-panel` | 文章列表外層 |
| `.article-grid` | 文章卡片 grid |
| `.front-article-card` | 文章卡片 |
| `.front-card-heart` | 收藏愛心按鈕 |
| `.front-card-meta` | 卡片資訊列 |
| `.front-card-foot` | 卡片底部作者與統計 |
| `.btn-glass` | 毛玻璃操作按鈕 |
| `.front-footer` | 頁尾版權聲明 |

## 8. RWD 規格

### 8.1 桌機版

- 左側側欄固定顯示。
- 右側內容區使用三欄卡片。
- 搜尋列橫向排列。

### 8.2 平板版

- 文章卡片改為兩欄。
- 搜尋列可自動換行。
- 右上角會員資訊保持在標題列右側。

### 8.3 手機版

- 側邊欄縮合為窄版。
- 側欄文字可隱藏，只保留 icon。
- 主內容改為單欄。
- 搜尋條件改為垂直排列。

## 9. 無障礙與可讀性

目前已保留或建議保留：

- 圖片 `alt` 文字。
- 通知、搜尋等 icon 按鈕需有 `aria-label`。
- 文字顏色與背景需維持可讀對比。
- 表單欄位保留可辨識 placeholder。
- 互動元件需有 hover/focus 狀態。

建議後續補強：

- 鍵盤 tab 操作焦點樣式。
- 收藏、按讚後的狀態文字提示。
- 搜尋結果為空時的明確空狀態。

## 10. 資料與後端串接

### 10.1 列表頁資料

Controller：

- `ExploreController.Index`

ViewModel：

- `ExploreIndexViewModel`

目前使用欄位：

- `Featured`
- `SideList`
- `Ranking`
- `Regions`
- `SelectedRegion`
- `Keyword`

### 10.2 詳細頁資料

Controller：

- `ExploreController.Details`

Model：

- `VlogPost`

額外資料：

- `ViewBag.Nodes`
- `ViewBag.LikeCount`
- `ViewBag.FavoriteCount`

## 11. 後續建議

可再補強的功能：

- 搜尋列的國家欄位可改為正式國家下拉選單。
- 行程區間可串接日期起訖篩選。
- 文章推薦可改為依收藏數、留言數或使用者偏好推薦。
- 新增文章按鈕可串接揪團結束後匯出文章流程。
- 卡片統計數可改為正式資料表欄位，不使用前端暫時計算。
- 側欄收合狀態可記錄到 `localStorage`。

## 12. 驗證結果

已使用以下指令驗證 `tours-api` 專案可建置：

```powershell
dotnet build .\tours-api\tours-api.csproj --artifacts-path 'C:\Users\ispan\Documents\Codex\2026-07-29\new-chat\verify-artifacts' -v:minimal
```

結果：

- 建置成功
- 0 個警告
- 0 個錯誤

