# 前後端分離共同規則

寫給要把自己負責的 Controller 從 Razor View 改成 JSON API 的組員看。除了下面 3 條,其他細節(JS 怎麼寫、用 fetch 還是 axios、HTML 排版)自己決定即可。

## 1. 路由不變、不加 /api 前綴

Controller 的路由維持現在的樣子(例如 `/Admin/VlogPosts/Index`),只改回傳內容,不要另外設計新路由。

## 2. 回傳格式統一用 `Ok(vm)`

```csharp
return Ok(vm);
```

`vm` 就是你原本傳給 View 的那個 ViewModel,直接回傳,不用包 `{success, data}` 這種外層。

失敗的情況用對應的狀態碼,例如:

```csharp
return NotFound();          // 404
return BadRequest("原因");   // 400
```

## 3. 前端檔案放的位置固定

- 前台:`wwwroot/js/{功能名稱}.js`
- 後台:`wwwroot/admin/{功能名稱}/index.html` + `index.js`

例如 VlogPosts 就放 `wwwroot/admin/vlogposts/index.html`、`index.js`。

## 注意

原本的 `.cshtml` 先不要刪,等前端 JS 確定能正常抓到資料、畫面也對了,再刪掉舊的 View。
