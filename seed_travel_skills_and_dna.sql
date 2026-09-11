/*
	把「旅行技能標籤」(33 個，四大類) 跟「旅遊 DNA」(4 個維度、各 4 個分數區間稱號)
	的資料塞進 LazyTravelDB。

	資料來源：
	- 技能標籤：對照 wwwroot/members/scene.dc.html 裡的 SKILL_GROUPS（前端已經在用的那份，
	  跟你上傳的《旅行技能標籤.docx》內容一致，一共 9(精通)+3(擴充)+8+8+5 = 33 個，
	  跟畫面上「10 / 33」的 33 對得起來）。
	- 旅遊 DNA：對照 scene.dc.html 的 DNA_DEFS，只留《旅遊DNA》文件裡沒被標記
	  「刪除不必加入資料庫」的四個維度：行程規劃、消費價值觀、景點偏好、飲食追求
	  （探索動機、體力消耗、作息時間這三個維度文件裡有註明先不做，這裡也沒放）。

	這支腳本可以重複執行不會重複塞資料——每個區塊都先檢查該表是不是空的才會插入。
	執行前記得先在 SSMS 選到 LazyTravelDB 這個資料庫。

	⚠️ 有一點要提醒：TravelSkills 表目前只有 SkillName / SkillCategory / IconCode /
	IsActive 四個欄位，沒有「說明文字」欄位。scene.dc.html 裡每個技能其實都配了一句
	說明（例如「首席攝影師」配「很會抓角度，不嫌煩」），這裡沒地方放，IconCode 也先
	留 NULL（設計裡目前是「整個分類」有一個 emoji，不是每個技能都有專屬圖示）。
	如果之後想把每個技能的說明文字也存進資料庫，要請組長評估加一個 Description 欄位。
*/

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

	------------------------------------------------------------
	-- 1. 旅行技能標籤 TravelSkills
	--    SkillCategory：1=語言天賦　2=行程推進　3=團隊角色　4=交通駕駛
	------------------------------------------------------------
	IF NOT EXISTS (SELECT 1 FROM dbo.TravelSkills)
	BEGIN
		INSERT INTO dbo.TravelSkills (SkillName, SkillCategory, IconCode, IsActive) VALUES
		-- 1) 語言天賦 LANGUAGE
		(N'中文', 1, NULL, 1),
		(N'英文', 1, NULL, 1),
		(N'日文', 1, NULL, 1),
		(N'韓文', 1, NULL, 1),
		(N'粵語', 1, NULL, 1),
		(N'西語', 1, NULL, 1),
		(N'法語', 1, NULL, 1),
		(N'德語', 1, NULL, 1),
		(N'手語', 1, NULL, 1),
		(N'泰語', 1, NULL, 1),
		(N'越語', 1, NULL, 1),
		(N'印尼語', 1, NULL, 1),

		-- 2) 行程推進 EXECUTION
		(N'行程總召', 2, NULL, 1),
		(N'攻略達人', 2, NULL, 1),
		(N'人肉導航', 2, NULL, 1),
		(N'訂房/機票好手', 2, NULL, 1),
		(N'票務達人', 2, NULL, 1),
		(N'網卡處理器', 2, NULL, 1),
		(N'外語溝通', 2, NULL, 1),
		(N'危機處理', 2, NULL, 1),

		-- 3) 團隊角色 TEAM ROLE
		(N'首席攝影師', 3, NULL, 1),
		(N'Vlog 大師', 3, NULL, 1),
		(N'扛行李硬漢', 3, NULL, 1),
		(N'無情晨喚機', 3, NULL, 1),
		(N'氣氛製造機', 3, NULL, 1),
		(N'財務大臣', 3, NULL, 1),
		(N'美食探測器', 3, NULL, 1),
		(N'隨和綠葉', 3, NULL, 1),

		-- 4) 交通駕駛 TRANSPORT
		(N'左駕老司機', 4, NULL, 1),
		(N'右駕老司機', 4, NULL, 1),
		(N'機車騎士', 4, NULL, 1),
		(N'國際駕照持有者', 4, NULL, 1),
		(N'副駕 DJ', 4, NULL, 1);

		PRINT N'TravelSkills：已插入 33 筆。';
	END
	ELSE
	BEGIN
		PRINT N'TravelSkills 已經有資料了，跳過（沒有重複插入）。';
	END

	------------------------------------------------------------
	-- 2. 旅遊 DNA 維度 TravelDNADimensions
	--    DimensionID：1=行程規劃　2=消費價值觀　3=景點偏好　4=飲食追求
	------------------------------------------------------------
	IF NOT EXISTS (SELECT 1 FROM dbo.TravelDNADimensions)
	BEGIN
		INSERT INTO dbo.TravelDNADimensions (DimensionID, DimensionName, LeftLabel, RightLabel) VALUES
		(1, N'行程規劃', N'隨機應變', N'按照計畫'),
		(2, N'消費價值觀', N'精打細算', N'奢華享受'),
		(3, N'景點偏好', N'擁抱山海', N'都會霓虹'),
		(4, N'飲食追求', N'巷弄尋寶', N'指標名店');

		PRINT N'TravelDNADimensions：已插入 4 筆。';
	END
	ELSE
	BEGIN
		PRINT N'TravelDNADimensions 已經有資料了，跳過。';
	END

	------------------------------------------------------------
	-- 3. 旅遊 DNA 各維度、各分數區間對應的稱號 TravelDNAOptions
	--    每個維度 4 個區間：0-25 / 26-50 / 51-75 / 76-100
	------------------------------------------------------------
	IF NOT EXISTS (SELECT 1 FROM dbo.TravelDNAOptions)
	BEGIN
		INSERT INTO dbo.TravelDNAOptions (DimensionID, OptionName, Description, MinScore, MaxScore) VALUES
		-- 維度 1：行程規劃
		(1, N'佛系盲遊', N'走到哪算到哪，不做功課不查地圖，相信一切都是最好的安排。', 0, 25),
		(1, N'大綱走起', N'只定好大方向（如機票、住宿），每天要去哪看當天起床的心情決定。', 26, 50),
		(1, N'按圖索驥', N'有明確的每日行程規劃，但允許行程根據體力或天氣微調。', 51, 75),
		(1, N'表格魔人', N'行程精確到分鐘，Excel 表格排好排滿，任何延誤都會讓他焦慮。', 76, 100),

		-- 維度 2：消費價值觀
		(2, N'極致窮遊', N'青年旅館、廉價航空、走路代步，最高指導原則是「能省則省」。', 0, 25),
		(2, N'CP 精算師', N'該花花該省省，擅長比價找優惠，追求每一塊錢都花在刀口上。', 26, 50),
		(2, N'質感微奢', N'願意花錢住幾晚好飯店或吃頓大餐，認為出來玩就是要對自己好一點。', 51, 75),
		(2, N'財富自由', N'五星級飯店、商務艙、高級包車，旅行的目的是極致的享受與服務。', 76, 100),

		-- 維度 3：景點偏好
		(3, N'野外探險', N'熱愛爬山、潛水、遠離塵囂的秘境，比起人潮更愛看風景。', 0, 25),
		(3, N'踏青放空', N'喜歡好山好水，但不用太累，搭車能到的風景區最棒。', 26, 50),
		(3, N'走跳街區', N'喜歡穿梭城市巷弄、逛街買東西、感受都市特有的文化氛圍。', 51, 75),
		(3, N'不夜城迷', N'只愛大都會，高樓大廈、百貨商場、繁華便利是旅行的唯一選擇。', 76, 100),

		-- 維度 4：飲食追求
		(4, N'在地老饕', N'專挑沒有招牌的路邊攤或傳統菜市場，越是在地人吃的越愛。', 0, 25),
		(4, N'隨機盲測', N'肚子餓了看順眼就走進去吃，不依賴網路評價，隨遇而安。', 26, 50),
		(4, N'評星指南', N'吃飯前一定要先查 Google Maps 星星數，4 顆星以上的才吃，不吃雷店。', 51, 75),
		(4, N'名店執著', N'為了吃米其林或網紅名店，排隊兩三個小時也在所不惜，絕不妥協。', 76, 100);

		PRINT N'TravelDNAOptions：已插入 16 筆。';
	END
	ELSE
	BEGIN
		PRINT N'TravelDNAOptions 已經有資料了，跳過。';
	END

	COMMIT TRANSACTION;
	PRINT N'完成。';

END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	PRINT N'發生錯誤，已全部回復：' + ERROR_MESSAGE();
	THROW;
END CATCH
