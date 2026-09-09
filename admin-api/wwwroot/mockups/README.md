# 前台首頁 Mockup — 三版

三個版本都只是靜態 HTML，**沒有動到任何既有程式碼**。假資料寫死在 HTML 裡，挑定之後再轉成 `.cshtml`。

## 怎麼看

直接用瀏覽器開 `home-a-sand.html`，畫面下方有版本切換條可以在三版之間跳。

## 共同前提

三版用的是同一套 token，全部來自 `wwwroot/css/brand.css`，沒有新增任何顏色或字體：

- 色：sea 海藍（主）、sand 暖沙（底）、coral 珊瑚橘（一頁只出現一次的 CTA）、leaf 葉綠（次要）
- 面：`--surface` #FFFDF8 暖白卡片，不用純白；圓角 `--radius-lg` 24px；陰影 `rgba(31,61,78,.06)`
- 字：標題 LXGW WenKai TC、內文 Noto Sans TC
- 元件：pill 按鈕、`badge-sea/sand/coral/leaf`、`official-badge`

`_shared.css` 只放兩件事：圖片佔位（品牌色漸層 + 山景剪影，離線也看得到）和版本切換條。正式落地時整支刪掉，佔位換成真的 `MediaUrl`。

## 三版差在哪

| | A 暖沙雜誌 | B 海藍沉浸 | C Bento 網格 |
|---|---|---|---|
| 主色場 | sand 為主，sea 退為配角 | sea 深色滿版開場 | sea-100 淺底 + 白卡 |
| Hero | 左文右圖不對稱，搜尋列是一張暖白卡 | 全幅深海藍 + 毛玻璃搜尋列 + 波浪收尾 | 沒有 Hero，直接進 Bento 方塊牆 |
| 資訊密度 | 低，留白多 | 中，一段一段推進 | 高，首屏看完全部重點 |
| 揪團呈現 | 橫式長條 + 左側日期方塊 | 橫向捲動卡片 + 倒數章 | 方塊 + 篩選籤 + 卡片牆 |
| 與後台的關係 | 同色票，氣質最遠 | 同色票，Hero 是後台 `dash-notice` 漸層的放大版 | **最近**：直接引用 `admin.css` 的 `filter-row` / `filter-chip` / `stat-card` / `status-pill` |
| 適合 | 想強調「慢」的品牌調性 | 想要第一眼有記憶點 | 想讓前後台看起來是同一個產品 |

C 版是唯一一支有 `<link href="../css/admin.css">` 的。它沒有掛 `body.admin-body`，所以不會吃到後台的 sea-200 底色，只借元件樣式。

## 挑定之後要做的事

1. 把選中那版的 `<style>` 區塊搬進 `wwwroot/css/site.css`（或另開 `home.css`）
2. HTML 本體搬進 `Views/Home/Index.cshtml`，導覽列和 footer 刪掉（`_Layout.cshtml` 已經有了）
3. 假資料換成 ViewModel；依 `FRONTEND_BACKEND_SPLIT.md`，Controller 回 `Ok(vm)`，前端 JS 放 `wwwroot/js/home.js`
4. 圖片佔位 `.ph .ph-sea` 這類 div 換成 `<img src="@p.MediaUrl">`，記得補 `alt`、`loading="lazy"`、`width`/`height`
5. 刪掉這整個 `mockups/` 資料夾
