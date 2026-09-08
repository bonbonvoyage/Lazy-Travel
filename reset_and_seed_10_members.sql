/*
	【危險操作】把 Members 資料全部清空，重新種 10 筆測試會員，
	並且補上好友／追蹤／黑名單的關聯資料，方便之後測試個人頁面、
	通訊錄權限、關係狀態等功能。

	=== 執行前必看 ===
	1. 這支腳本會刪光下面列出的所有資料表的「全部資料」，
	   不是只刪 Members，因為其他資料表都有 NOT NULL 的外鍵指到 Members，
	   刪不掉 Members 就會連帶把這些關聯資料一起清空：
	   VlogPosts / VlogPostImages / VlogPostTags / PostInteractions、
	   Expenses / ExpenseSplits、
	   TravelGroups 以及它底下的 GroupMembers / JoinRequests /
	     TravelGroupImages / TravelGroupBudgets / TravelGroupItineraryItems /
	     TravelGroupTags / TravelGroupsLog / ItineraryNodes、
	   Notifications、Reports、LoginHistories、
	   MemberSkills、MemberSubscriptions、MemberTravelDNA、
	   FriendRequests、Friendships、Follows、Blocks、
	   以及 ASP.NET Identity 的 MemberClaims / MemberLogins / MemberTokens / MemberUserRoles。
	   已經跟你確認過（"就要全部重置"），這支腳本就是照這個範圍做的。
	2. 這是不可逆的操作，麻煩先備份一次資料庫再執行（SSMS 右鍵資料庫 →
	   工作 → 產生指令碼，或直接完整備份）。
	3. 執行完畢後，Members 會重新從 Id = 1 開始編號，剛好對應到下面
	   好友/追蹤/黑名單關聯用的 1~10。
	4. 10 個帳號共用同一組測試密碼：Test1234!
	   （PasswordHash 是照 ASP.NET Core Identity V3 格式產生的，可以直接登入，
	   前提是登入功能那邊也是用標準的 PasswordHasher<Member> 驗證。）
	5. 大頭貼用的是 https://i.pravatar.cc/ 的線上佔位圖，需要能連網才看得到圖片；
	   如果環境沒有對外網路，大頭貼會顯示不出來，但不影響資料本身。

	=== 10 筆會員（Id 1~10，依下面 INSERT 順序）===
	1 陳品睿 ENTJ 男 台北・信義 通訊錄公開
	2 林芷晴 INFP 女 台中・西區 通訊錄私人
	3 黃冠廷 ISTP 男 高雄・鹽埕 通訊錄公開
	4 王思穎 ENFJ 女 新竹・東區 通訊錄私人
	5 李昱翔 ESTP 男 台北・大安 通訊錄公開
	6 張家瑜 INTJ 女 台南・東區 通訊錄私人
	7 吳柏勳 ISFJ 男 桃園・中壢 通訊錄公開
	8 許雅涵 ENFP 女 台北・松山 通訊錄公開
	9 蔡侑霖 INTP 其他 新北・板橋 通訊錄私人
	10 周郁棠 ISFP 未知 台北・北投 通訊錄公開

	=== 關聯資料設計（方便測試用）===
	好友 Friendships（互為好友，MemberID1 < MemberID2 是資料庫的 CHECK 限制）：
	  1-2、1-3、2-3   → 1、2、3 三人互為好友的小圈圈
	  4-5             → 4、5 互為好友
	  6-7             → 6、7 互為好友

	追蹤 Follows（Status: 1=已追蹤、0=待核准）：
	  8  追蹤 1（已追蹤）
	  9  追蹤 2（已追蹤）
	  3  追蹤 8（已追蹤，測試「我是好友圈但也追蹤別人」的情境）
	  10 追蹤 5（待核准，測試「還沒核准」的情境）

	黑名單 Blocks：
	  10 封鎖 4
	  5  封鎖 9

	這樣每個人身上大致都有資料可以測（1、2、3 是好友圈也有人追蹤；
	4、5 是好友但 4 被封鎖、5 也封鎖了別人；6、7 是單純的好友對；
	8、9、10 主要是追蹤者/封鎖者的角色），實際要不要調整關聯，
	之後看測試需求再說。
*/

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

	-- Step 1：先關掉所有資料表的外鍵/檢查條件檢查，這樣才能不管順序直接刪資料。
	EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';

	-- Step 2：清空所有跟 Members 有關聯（直接或間接）的資料表。
	DELETE FROM dbo.PostInteractions;
	DELETE FROM dbo.ItineraryNodes;
	DELETE FROM dbo.VlogPostTags;
	DELETE FROM dbo.VlogPostImages;
	DELETE FROM dbo.VlogPosts;

	DELETE FROM dbo.ExpenseSplits;
	DELETE FROM dbo.Expenses;

	DELETE FROM dbo.TravelGroupTags;
	DELETE FROM dbo.TravelGroupItineraryItems;
	DELETE FROM dbo.TravelGroupImages;
	DELETE FROM dbo.TravelGroupBudgets;
	DELETE FROM dbo.TravelGroupsLog;
	DELETE FROM dbo.JoinRequests;
	DELETE FROM dbo.GroupMembers;
	DELETE FROM dbo.TravelGroups;

	DELETE FROM dbo.Notifications;
	DELETE FROM dbo.Reports;
	DELETE FROM dbo.LoginHistories;

	DELETE FROM dbo.MemberSkills;
	DELETE FROM dbo.MemberSubscriptions;
	DELETE FROM dbo.MemberTravelDNA;

	DELETE FROM dbo.FriendRequests;
	DELETE FROM dbo.Friendships;
	DELETE FROM dbo.Follows;
	DELETE FROM dbo.Blocks;

	DELETE FROM dbo.MemberClaims;
	DELETE FROM dbo.MemberLogins;
	DELETE FROM dbo.MemberTokens;
	DELETE FROM dbo.MemberUserRoles;

	DELETE FROM dbo.Members;

	-- Step 3：身分證號（Id）重新從 1 開始編號。
	DBCC CHECKIDENT ('dbo.Members', RESEED, 0);

	-- Step 4：重新種 10 筆會員，每個欄位（含通訊錄 Phone/LineId/InstagramUrl/FacebookUrl）都有資料。
	INSERT INTO dbo.Members
		(UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
		 PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount,
		 Name, LineId, InstagramUrl, FacebookUrl, ContactBookVisibility, IsPrivateAccount, AvatarUrl,
		 BirthDate, Gender, Occupation, MBTI, Bio, Status, CreatedAt, LastLoginAt, LastLoginIp, IsDelete, City)
	VALUES
		(N'pinrui.chen@example.com', N'PINRUI.CHEN@EXAMPLE.COM', N'pinrui.chen@example.com', N'PINRUI.CHEN@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEFrNDPuYBpkjHV/NY6fx0F3fxlZiuyHjgLGIZmcaZ2pYC/b+2EUicTnMQ88Tq+z1fg==', N'06CC7A48F71E4394A9A6EA0217386D2F', N'e78d93e2-5ec9-49f8-8b52-31667a0c1276',
		 N'0912-345-601', 1, 0, NULL, 1, 0,
		 N'陳品睿', N'pinrui_chen', N'pinrui.travels', N'陳品睿', 0, 0,
		 N'https://i.pravatar.cc/300?img=11',
		 '1994-03-12', 1, N'軟體工程師', N'ENTJ', N'喜歡排效率最高的行程，出國前一定會做好交通轉乘的試算表。', 1,
		 DATEADD(DAY, -60, GETDATE()), DATEADD(DAY, -5, GETDATE()), N'36.226.10.100', 0,
		 N'台北・信義'),
		(N'zhiqing.lin@example.com', N'ZHIQING.LIN@EXAMPLE.COM', N'zhiqing.lin@example.com', N'ZHIQING.LIN@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEMrIrmeQcfJiXqcjOd5A3tvVBFz4bOObboTT/Wwgrb00k/fxq0RHLrHfnIb1KYWdnw==', N'4BFBE6411B6442CEBC0DE89B9C223D3E', N'1adf04b3-7a7d-43de-bed2-eced3ca0303a',
		 N'0912-345-602', 1, 0, NULL, 1, 0,
		 N'林芷晴', N'zhiqing_lin', N'zhiqing.sketches', N'林芷晴', 1, 0,
		 N'https://i.pravatar.cc/300?img=12',
		 '1997-07-22', 2, N'插畫家', N'INFP', N'每到一個城市都要找一間有貓的咖啡廳畫畫，行李箱一半是畫具。', 1,
		 DATEADD(DAY, -57, GETDATE()), DATEADD(DAY, -6, GETDATE()), N'36.226.11.101', 0,
		 N'台中・西區'),
		(N'guanting.huang@example.com', N'GUANTING.HUANG@EXAMPLE.COM', N'guanting.huang@example.com', N'GUANTING.HUANG@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAED+dArASNTFxIBm8xxbzoG3xZOov6/pM6wMANaQKE1ZqwWmEx7bl4yNJ2QogoEBhdg==', N'3866A03ACC6A494CB1E75B2D5B1368EB', N'd3db9474-0e7b-446d-bbc0-89c702f41d5f',
		 N'0912-345-603', 1, 0, NULL, 1, 0,
		 N'黃冠廷', N'guanting_h', N'guanting.rides', N'黃冠廷', 0, 0,
		 N'https://i.pravatar.cc/300?img=13',
		 '1990-11-05', 1, N'機車行老闆', N'ISTP', N'能騎車就不搭車，環島騎過三次，最喜歡花東的海岸線。', 1,
		 DATEADD(DAY, -54, GETDATE()), DATEADD(DAY, -7, GETDATE()), N'36.226.12.102', 0,
		 N'高雄・鹽埕'),
		(N'siying.wang@example.com', N'SIYING.WANG@EXAMPLE.COM', N'siying.wang@example.com', N'SIYING.WANG@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEJ3eUc6bHJNGNPqoVssCE75xWORypBDW1tD+qCi++kpjeyMaTcftCJByHlP99k/9Hg==', N'2A11D7A7F4384CCFA21F713D751C807D', N'6068d2ad-b6c3-440c-b781-b887d94e6848',
		 N'0912-345-604', 1, 0, NULL, 1, 0,
		 N'王思穎', N'siying_wang', N'siying.teaches', N'王思穎', 1, 0,
		 N'https://i.pravatar.cc/300?img=14',
		 '1993-01-30', 2, N'小學老師', N'ENFJ', N'暑假是我的旅遊季，喜歡帶學生的那種細心規劃每一站行程。', 1,
		 DATEADD(DAY, -51, GETDATE()), DATEADD(DAY, -8, GETDATE()), N'36.226.13.103', 0,
		 N'新竹・東區'),
		(N'yuxiang.li@example.com', N'YUXIANG.LI@EXAMPLE.COM', N'yuxiang.li@example.com', N'YUXIANG.LI@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAELd/2OkM6nRCjlWAy1nIVpKneV8juHdpsbYZsG9HRw8fK9hOO8cO8n9Jx75wf6VFqA==', N'D2222BFAFA8C48D2B139291C4DC42D3B', N'f647d0e6-411c-4da6-9148-3942733490c0',
		 N'0912-345-605', 1, 0, NULL, 1, 0,
		 N'李昱翔', N'yuxiang_li', N'yuxiang.fit', N'李昱翔', 0, 0,
		 N'https://i.pravatar.cc/300?img=15',
		 '1996-05-18', 1, N'健身教練', N'ESTP', N'出國一定找得到當地的健身房，行程可以彈性但運動不能少。', 1,
		 DATEADD(DAY, -48, GETDATE()), DATEADD(DAY, -9, GETDATE()), N'36.226.14.104', 0,
		 N'台北・大安'),
		(N'jiayu.zhang@example.com', N'JIAYU.ZHANG@EXAMPLE.COM', N'jiayu.zhang@example.com', N'JIAYU.ZHANG@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEK2zAnqk9wLMrJ8CjGLx2/uAnyOKTaGBhd5/baLungefN8uWWbAXad5MsTnzxr5Fng==', N'8F0621DC93EF4FC1892BB4EB22B8DAF4', N'2d4a9495-e2a3-4536-a349-825cae459a3a',
		 N'0912-345-606', 1, 0, NULL, 1, 0,
		 N'張家瑜', N'jiayu_zhang', N'jiayu.data', N'張家瑜', 1, 0,
		 N'https://i.pravatar.cc/300?img=16',
		 '1995-09-09', 2, N'資料分析師', N'INTJ', N'每趟旅行都會做行前資料整理，連平均氣溫都會事先查好。', 1,
		 DATEADD(DAY, -45, GETDATE()), DATEADD(DAY, -10, GETDATE()), N'36.226.15.105', 0,
		 N'台南・東區'),
		(N'boxun.wu@example.com', N'BOXUN.WU@EXAMPLE.COM', N'boxun.wu@example.com', N'BOXUN.WU@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEPCqZC1i77bicXhaFbA37WUHdz3ZNaXqzRdi40K5hEi0q5K4Oj/X0MIVueXNi7HS5Q==', N'6A257856D4C042509D06C33A3D665C81', N'39c919e2-445b-4514-9a18-9ed8ca246720',
		 N'0912-345-607', 1, 0, NULL, 1, 0,
		 N'吳柏勳', N'boxun_wu', N'boxun.eats', N'吳柏勳', 0, 0,
		 N'https://i.pravatar.cc/300?img=17',
		 '1992-12-01', 1, N'廚師', N'ISFJ', N'旅行的重點永遠是吃，會提前排好每一餐要吃什麼。', 1,
		 DATEADD(DAY, -42, GETDATE()), DATEADD(DAY, -11, GETDATE()), N'36.226.16.106', 0,
		 N'桃園・中壢'),
		(N'yahan.xu@example.com', N'YAHAN.XU@EXAMPLE.COM', N'yahan.xu@example.com', N'YAHAN.XU@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEECNXQqsxR8EA9+tBsRvk3SOYPdtWyyRUMSCxfaMTDFJBox93f1i6qIUdO1+TMXYEw==', N'0D77234C076B4F06912F3987825EEE04', N'045d1101-3321-4c29-9dc4-abc6545a3920',
		 N'0912-345-608', 1, 0, NULL, 1, 0,
		 N'許雅涵', N'yahan_xu', N'yahan.plans', N'許雅涵', 0, 0,
		 N'https://i.pravatar.cc/300?img=18',
		 '1998-04-14', 2, N'行銷企劃', N'ENFP', N'喜歡認識新朋友，追蹤很多旅遊帳號找靈感，隨時準備下一趟。', 1,
		 DATEADD(DAY, -39, GETDATE()), DATEADD(DAY, -12, GETDATE()), N'36.226.17.107', 0,
		 N'台北・松山'),
		(N'youlin.cai@example.com', N'YOULIN.CAI@EXAMPLE.COM', N'youlin.cai@example.com', N'YOULIN.CAI@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEHd4DqAxRr4lpQy/qzFL+T3qw+1LHM6xRzRV+a1VxIUbnBAkJu8Y1HYTQeEUdugx/A==', N'BE431C0E329E4CDF89E3D5FFB1A1866C', N'94cdcd9b-f441-43dd-9101-fc2823e94c3f',
		 N'0912-345-609', 1, 0, NULL, 1, 0,
		 N'蔡侑霖', N'youlin_cai', N'youlin.reads', N'蔡侑霖', 1, 0,
		 N'https://i.pravatar.cc/300?img=19',
		 '2000-02-27', 3, N'研究生', N'INTP', N'窮學生旅行，青年旅館跟夜巴是標配，能省則省。', 1,
		 DATEADD(DAY, -36, GETDATE()), DATEADD(DAY, -13, GETDATE()), N'36.226.18.108', 0,
		 N'新北・板橋'),
		(N'yutang.zhou@example.com', N'YUTANG.ZHOU@EXAMPLE.COM', N'yutang.zhou@example.com', N'YUTANG.ZHOU@EXAMPLE.COM', 1,
		 N'AQAAAAEAAYagAAAAEDigGK8eEn2kNblZ6iQgfXPIRx77mu/OK52IjrBfwYmtd+kv/i0QL2tr8J4kRcEiPg==', N'64ADC90196D747F1B8695237D025031F', N'ac311507-b452-4817-ae87-c7360b08e197',
		 N'0912-345-610', 1, 0, NULL, 1, 0,
		 N'周郁棠', N'yutang_zhou', N'yutang.design', N'周郁棠', 0, 0,
		 N'https://i.pravatar.cc/300?img=20',
		 '1999-08-08', 0, N'自由接案設計師', N'ISFP', N'時間彈性，常常臨時決定就出發，喜歡沒有規劃的旅行。', 1,
		 DATEADD(DAY, -33, GETDATE()), DATEADD(DAY, -14, GETDATE()), N'36.226.19.109', 0,
		 N'台北・北投');

	-- Step 5：好友關聯（MemberID1 < MemberID2，符合 CHK_Friendship_Order）。
	-- 1、2、3 三人互為好友；4-5 一對；6-7 一對。
	INSERT INTO dbo.Friendships (MemberID1, MemberID2, CreatedAt) VALUES
		(1, 2, DATEADD(DAY, -40, GETDATE())),
		(1, 3, DATEADD(DAY, -38, GETDATE())),
		(2, 3, DATEADD(DAY, -35, GETDATE())),
		(4, 5, DATEADD(DAY, -30, GETDATE())),
		(6, 7, DATEADD(DAY, -20, GETDATE()));

	-- Step 6：追蹤關聯（Status: 1=已追蹤、0=待核准）。
	-- 8 追蹤 1、9 追蹤 2、3 追蹤 8 都已生效；10 追蹤 5 還沒核准，方便測「待核准」的畫面。
	INSERT INTO dbo.Follows (FollowerID, FolloweeID, Status, CreatedAt, UpdatedAt) VALUES
		(8, 1, 1, DATEADD(DAY, -25, GETDATE()), DATEADD(DAY, -25, GETDATE())),
		(9, 2, 1, DATEADD(DAY, -22, GETDATE()), DATEADD(DAY, -22, GETDATE())),
		(3, 8, 1, DATEADD(DAY, -18, GETDATE()), DATEADD(DAY, -18, GETDATE())),
		(10, 5, 0, DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, -2, GETDATE()));

	-- Step 7：黑名單。10 封鎖 4、5 封鎖 9。
	INSERT INTO dbo.Blocks (BlockerID, BlockedID, CreatedAt) VALUES
		(10, 4, DATEADD(DAY, -15, GETDATE())),
		(5, 9, DATEADD(DAY, -10, GETDATE()));

	-- Step 8：重新打開所有資料表的外鍵/檢查條件檢查，並且順便驗證一次資料一致性。
	-- 如果這裡的資料有任何不一致（理論上不會），會直接丟錯、整個 ROLLBACK，不會留下半套資料。
	EXEC sp_msforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';

	COMMIT TRANSACTION;
	PRINT N'完成：Members 已重置為 10 筆測試資料，好友/追蹤/黑名單關聯也建立好了。';

END TRY
BEGIN CATCH
	IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
	PRINT N'發生錯誤，已全部回復：' + ERROR_MESSAGE();
	THROW;
END CATCH
