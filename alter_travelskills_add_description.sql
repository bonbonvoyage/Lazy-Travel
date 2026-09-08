/*
	在 TravelSkills 加上 Description 欄位(技能說明文字，例如「首席攝影師」配「很會抓角度，不嫌煩」)，
	並把 33 個技能的說明文字補回去。

	跟 seed_travel_skills_and_dna.sql 一樣，這支腳本可以重複執行：
	- 欄位已經存在就不會重複加。
	- UPDATE 是照 SkillName 對應，不管執行幾次結果都一樣，不會出錯。

	執行順序：這支腳本可以在 seed_travel_skills_and_dna.sql 之前或之後執行都沒差，
	反正 UPDATE 那段是找已經存在的資料列去補說明文字，如果 TravelSkills 還是空的，
	UPDATE 自然就是 0 筆受影響，之後執行過 seed 腳本插入資料後，說明文字仍然是空的，
	要再執行一次這支才會補上——保險起見，建議兩支都跑過一次。
*/

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

	------------------------------------------------------------
	-- 1. 加欄位
	------------------------------------------------------------
	IF NOT EXISTS (
		SELECT 1 FROM sys.columns
		WHERE object_id = OBJECT_ID('dbo.TravelSkills') AND name = 'Description'
	)
	BEGIN
		ALTER TABLE dbo.TravelSkills ADD Description NVARCHAR(100) NULL;
		PRINT N'TravelSkills：已新增 Description 欄位。';
	END
	ELSE
	BEGIN
		PRINT N'TravelSkills.Description 欄位已經存在，跳過新增。';
	END

	------------------------------------------------------------
	-- 2. 補上每個技能的說明文字（照 SkillName 對應）
	------------------------------------------------------------
	;WITH SkillDescriptions AS (
		SELECT * FROM (VALUES
			-- 語言天賦
			(N'中文', N'母語或精通'),
			(N'英文', N'可與當地人日常溝通'),
			(N'日文', N'可處理訂房、點餐、問路'),
			(N'韓文', N'可處理日常溝通'),
			(N'粵語', N'香港、廣東區域溝通'),
			(N'西語', N'西語系國家溝通'),
			(N'法語', N'法語區溝通'),
			(N'德語', N'德語區溝通'),
			(N'手語', N'可以手語溝通'),
			(N'泰語', N'泰國旅行可溝通'),
			(N'越語', N'越南旅行可溝通'),
			(N'印尼語', N'印尼、馬來區域溝通'),

			-- 行程推進
			(N'行程總召', N'負責排定整體路線'),
			(N'攻略達人', N'負責查秘境、查資料'),
			(N'人肉導航', N'方向感極佳，找路擔當'),
			(N'訂房/機票好手', N'擅長比價、搶票'),
			(N'票務達人', N'處理各類當地通票、景點票'),
			(N'網卡處理器', N'出國前負責搞定全團網路'),
			(N'外語溝通', N'負責跟當地人、飯店、店家交涉'),
			(N'危機處理', N'遇到班機取消、弄丟護照時能冷靜解決'),

			-- 團隊角色
			(N'首席攝影師', N'很會抓角度，不嫌煩'),
			(N'Vlog 大師', N'負責記錄旅途並剪輯'),
			(N'扛行李硬漢', N'搬行李上樓梯不抱怨'),
			(N'無情晨喚機', N'負責準時叫大家起床'),
			(N'氣氛製造機', N'不怕冷場，超會接話'),
			(N'財務大臣', N'負責管公費、分帳'),
			(N'美食探測器', N'找餐廳絕不踩雷'),
			(N'隨和綠葉', N'去哪都說好，隨和不挑剔'),

			-- 交通駕駛
			(N'左駕老司機', N'習慣台灣/美國等左駕模式'),
			(N'右駕老司機', N'習慣日本/英國等右駕模式'),
			(N'機車騎士', N'擅長在東南亞或離島騎機車'),
			(N'國際駕照持有者', N'已經準備好可以出國租車'),
			(N'副駕 DJ', N'不會開車但會幫忙看路、播音樂、餵食司機')
		) AS v(SkillName, Description)
	)
	UPDATE ts
	SET ts.Description = sd.Description
	FROM dbo.TravelSkills ts
	INNER JOIN SkillDescriptions sd ON sd.SkillName = ts.SkillName;

	PRINT N'TravelSkills：已補上 ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' 筆說明文字（如果 TravelSkills 目前是空的，這裡會是 0，跑完 seed 腳本後要再執行一次這支）。';

	COMMIT TRANSACTION;
	PRINT N'完成。';

END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	PRINT N'發生錯誤，已全部回復：' + ERROR_MESSAGE();
	THROW;
END CATCH
