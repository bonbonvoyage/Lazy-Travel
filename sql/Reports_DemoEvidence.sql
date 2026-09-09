/* ============================================================
   補上檢舉案件的證據截圖（Demo 用）

   Reports.EvidenceUrl 是 NULL 的案件,詳細頁會顯示「檢舉人未附上截圖」。
   這裡依 ReasonCategory 補上對應的模擬截圖,讓 Demo 每一案都有圖可看。

   圖檔在 wwwroot/uploads/reports/,每張右下角都有
   「※ 此為專題展示用模擬截圖,非真實內容」字樣,不會被誤認成真實證據。

   可重複執行（只動 EvidenceUrl 是空的那些）。
   ============================================================ */

UPDATE dbo.Reports
SET EvidenceUrl = CASE ReasonCategory
        WHEN 0 THEN N'/uploads/reports/demo-evidence-spam.svg'        -- 廣告垃圾訊息
        WHEN 1 THEN N'/uploads/reports/demo-evidence-8.svg'          -- 詐騙/安全疑慮(沿用既有那張私下匯款對話)
        WHEN 2 THEN N'/uploads/reports/demo-evidence-harassment.svg' -- 騷擾/不當言論
        WHEN 3 THEN N'/uploads/reports/demo-evidence-copyright.svg'  -- 版權/抄襲爭議
        WHEN 4 THEN N'/uploads/reports/demo-evidence-dispute.svg'    -- 服務/行程糾紛
        ELSE        N'/uploads/reports/demo-evidence-other.svg'      -- 其他
    END
WHERE EvidenceUrl IS NULL OR LTRIM(RTRIM(EvidenceUrl)) = '';
-- 想留幾件當「未附截圖」的空狀態 Demo,在上面加：AND ReportID NOT IN (11, 12)

/* --- 確認每一類補了幾件 --- */
SELECT ReasonCategory, EvidenceUrl, COUNT(*) AS 件數
FROM dbo.Reports
GROUP BY ReasonCategory, EvidenceUrl
ORDER BY ReasonCategory;
