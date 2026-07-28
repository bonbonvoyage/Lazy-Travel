using LazyTravel.Models;

namespace LazyTravel.Services
{
    // TODO(後續):改用 DbContext 從 Reports 資料表查詢/更新,目前先用假資料讓畫面可運作
    public class ReportService : IReportService
    {
        private const int PageSize = 10;

        // 累犯自動停權門檻:同一帳號「檢舉成立」超過 3 次才停權,依累犯次數決定停權天數
        // (第 4 次:3 天,第 5 次以上:5 天)。只算「查證屬實」的次數,單純被檢舉但不成立的不算
        public int SuspendThreshold => 3;

        // 尚未接 Cookie 認證,判定人先用隨機代稱,不使用真實隊員姓名
        private static readonly string[] _reviewerAliases = { "審核員A", "審核員B", "審核員C", "審核員D", "審核員E" };
        private static readonly Random _random = new();

        // 保護 _reports 的讀寫,避免多人同時判定同一筆造成競態
        private static readonly object _reportsLock = new();

        private static readonly List<Report> _reports = new()
        {
            new Report { Id = 1, TargetType = ReportTargetType.VlogPost, TargetId = 12, TargetTitle = "花蓮三天兩夜慢遊路線", ReportedMemberAccount = "traveler_hualien", ReporterAccount = "ming88", ReasonCategory = ReportReasonCategory.Spam, Reason = "內容含商業廣告連結", Description = "文章底部貼了購物網站連結,疑似置入行銷", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 14, 9, 0, 0) },
            new Report { Id = 2, TargetType = ReportTargetType.TravelGroup, TargetId = 88, TargetTitle = "揪團去墾丁三天兩夜", ReportedMemberAccount = "group_owner_kt", ReporterAccount = "traveler_a01", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "團主疑似棄團失聯", Description = "揪團成立後團主已讀不回超過一週,成員無法退款", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 15, 15, 20, 0) },
            new Report { Id = 3, TargetType = ReportTargetType.Member, TargetId = 205, TargetTitle = "user_travel99", ReportedMemberAccount = "user_travel99", ReporterAccount = "mei_chen23", ReasonCategory = ReportReasonCategory.Fraud, Reason = "疑似詐騙帳號", Description = "私訊要求匯款訂房", Status = ReportStatus.Upheld, CreatedAt = new DateTime(2026, 7, 10, 11, 0, 0), AdminNotes = "查證屬實,已停權" },
            new Report { Id = 4, TargetType = ReportTargetType.TravelGroup, TargetId = 501, TargetTitle = "澎湖跳島揪團", ReportedMemberAccount = "penghu_leader", ReporterAccount = "xiang_tw", ReasonCategory = ReportReasonCategory.Other, Reason = "檢舉錯誤", Description = "只是單純抱怨行程規劃,不涉及違規", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 7, 8, 20, 0, 0), AdminNotes = "屬個人意見表達,不成立" },
            new Report { Id = 5, TargetType = ReportTargetType.Member, TargetId = 205, TargetTitle = "user_travel99", ReportedMemberAccount = "user_travel99", ReporterAccount = "lee_kevin", ReasonCategory = ReportReasonCategory.Harassment, Reason = "重複私訊騷擾", Description = "同一天內私訊超過 10 次要求轉帳", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 12, 8, 40, 0) },
            new Report { Id = 6, TargetType = ReportTargetType.VlogPost, TargetId = 12, TargetTitle = "花蓮三天兩夜慢遊路線", ReportedMemberAccount = "traveler_hualien", ReporterAccount = "kai0723", ReasonCategory = ReportReasonCategory.Copyright, Reason = "抄襲他人文章", Description = "段落與另一篇部落格幾乎相同", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 13, 10, 5, 0) },
            new Report { Id = 7, TargetType = ReportTargetType.VlogPost, TargetId = 45, TargetTitle = "台南美食巷弄探索", ReportedMemberAccount = "tainan_foodie", ReporterAccount = "hua_lin", ReasonCategory = ReportReasonCategory.Copyright, Reason = "圖片版權爭議", Description = "照片疑似盜用攝影師作品未標註來源", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 11, 9, 15, 0) },
            new Report { Id = 8, TargetType = ReportTargetType.Member, TargetId = 310, TargetTitle = "kevin_tw", ReportedMemberAccount = "kevin_tw", ReporterAccount = "ming_wang", ReasonCategory = ReportReasonCategory.Fraud, Reason = "假帳號詐騙", Description = "自稱代訂機票要求私下匯款", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 14, 13, 50, 0) },
            new Report { Id = 9, TargetType = ReportTargetType.TravelGroup, TargetId = 120, TargetTitle = "宜蘭溫泉小旅行揪團", ReportedMemberAccount = "yilan_owner", ReporterAccount = "fang99", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "團費金額不透明", Description = "收費項目與原公告不符", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 10, 16, 0, 0) },
            new Report { Id = 10, TargetType = ReportTargetType.VlogPost, TargetId = 67, TargetTitle = "九份老街一日遊", ReportedMemberAccount = "jiufen_blogger", ReporterAccount = "traveler_b02", ReasonCategory = ReportReasonCategory.Copyright, Reason = "內容農場抄襲", Description = "整篇貼文疑似從其他網站複製", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 9, 11, 30, 0) },
            new Report { Id = 11, TargetType = ReportTargetType.Member, TargetId = 410, TargetTitle = "backpacker_x", ReportedMemberAccount = "backpacker_x", ReporterAccount = "lin_jay", ReasonCategory = ReportReasonCategory.Harassment, Reason = "騷擾私訊女性會員", Description = "多名會員反映收到不當訊息", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 15, 20, 10, 0) },
            new Report { Id = 12, TargetType = ReportTargetType.TravelGroup, TargetId = 88, TargetTitle = "揪團去墾丁三天兩夜", ReportedMemberAccount = "group_owner_kt", ReporterAccount = "zhang_tw", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "團主消失聯絡不到", Description = "出發前三天團主已讀不回", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 16, 8, 0, 0) },
            new Report { Id = 13, TargetType = ReportTargetType.VlogPost, TargetId = 12, TargetTitle = "花蓮三天兩夜慢遊路線", ReportedMemberAccount = "traveler_hualien", ReporterAccount = "kai0723", ReasonCategory = ReportReasonCategory.Spam, Reason = "內容含賭博廣告", Description = "文末附上博弈網站連結", Status = ReportStatus.Upheld, CreatedAt = new DateTime(2026, 7, 5, 9, 0, 0), AdminNotes = "連結查證屬實,已下架文章" },
            new Report { Id = 14, TargetType = ReportTargetType.Member, TargetId = 205, TargetTitle = "user_travel99", ReportedMemberAccount = "user_travel99", ReporterAccount = "lee_kevin", ReasonCategory = ReportReasonCategory.Spam, Reason = "疑似機器人帳號", Description = "短時間內大量加好友", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 7, 4, 15, 0, 0), AdminNotes = "查證為真人帳號,不成立" },
            new Report { Id = 15, TargetType = ReportTargetType.TravelGroup, TargetId = 233, TargetTitle = "阿里山賞櫻揪團", ReportedMemberAccount = "alishan_owner", ReporterAccount = "chen_xiao", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "行程與描述不符", Description = "實際景點比公告少兩個", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 12, 14, 20, 0) },
            new Report { Id = 16, TargetType = ReportTargetType.VlogPost, TargetId = 89, TargetTitle = "墾丁衝浪體驗記", ReportedMemberAccount = "surf_kenting", ReporterAccount = "chenms01", ReasonCategory = ReportReasonCategory.Copyright, Reason = "內容農場抄襲", Description = "經比對與另一篇文章高度雷同", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 7, 3, 10, 0, 0), AdminNotes = "經查為原作者本人轉載,不成立" },
            new Report { Id = 17, TargetType = ReportTargetType.Member, TargetId = 512, TargetTitle = "fake_agent007", ReportedMemberAccount = "fake_agent007", ReporterAccount = "traveler_c03", ReasonCategory = ReportReasonCategory.Fraud, Reason = "假冒旅行社詐騙", Description = "盜用知名旅行社名稱招攬團員", Status = ReportStatus.Upheld, CreatedAt = new DateTime(2026, 7, 2, 9, 0, 0), AdminNotes = "查證屬實,已停權並通報" },
            new Report { Id = 18, TargetType = ReportTargetType.TravelGroup, TargetId = 345, TargetTitle = "日月潭單車揪團", ReportedMemberAccount = "sunmoon_owner", ReporterAccount = "mei_chen23", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "臨時取消未告知", Description = "出發當天才通知取消", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 14, 7, 45, 0) },
            new Report { Id = 19, TargetType = ReportTargetType.VlogPost, TargetId = 100, TargetTitle = "清境農場親子遊記", ReportedMemberAccount = "qingjing_family", ReporterAccount = "wang_mr", ReasonCategory = ReportReasonCategory.Spam, Reason = "內容含賭博廣告連結", Description = "文章留言區出現博弈廣告連結", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 15, 12, 0, 0) },
            new Report { Id = 20, TargetType = ReportTargetType.Member, TargetId = 620, TargetTitle = "spam_bot_99", ReportedMemberAccount = "spam_bot_99", ReporterAccount = "chen_xiao", ReasonCategory = ReportReasonCategory.Spam, Reason = "大量發送廣告私訊", Description = "疑似機器人帳號批量傳送廣告", Status = ReportStatus.Upheld, CreatedAt = new DateTime(2026, 7, 6, 8, 0, 0), AdminNotes = "確認為廣告機器人,已停權" },
            new Report { Id = 21, TargetType = ReportTargetType.TravelGroup, TargetId = 88, TargetTitle = "揪團去墾丁三天兩夜", ReportedMemberAccount = "group_owner_kt", ReporterAccount = "hua88", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "團主惡意超收", Description = "實際收費比公告金額多兩千元", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 7, 7, 13, 0, 0), AdminNotes = "查證為代訂保險費用,誤會一場" },
            new Report { Id = 22, TargetType = ReportTargetType.VlogPost, TargetId = 134, TargetTitle = "礁溪一日遊全攻略", ReportedMemberAccount = "jiaoxi_guide", ReporterAccount = "traveler_d04", ReasonCategory = ReportReasonCategory.Copyright, Reason = "文章內容抄襲", Description = "段落結構與另一篇熱門文章相似度高", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 13, 18, 30, 0) },
            new Report { Id = 23, TargetType = ReportTargetType.Member, TargetId = 205, TargetTitle = "user_travel99", ReportedMemberAccount = "user_travel99", ReporterAccount = "wang_xiao", ReasonCategory = ReportReasonCategory.Spam, Reason = "疑似分身帳號洗版", Description = "用不同帳號重複留言推廣同一篇文章", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 16, 9, 20, 0) },
            new Report { Id = 24, TargetType = ReportTargetType.TravelGroup, TargetId = 456, TargetTitle = "太魯閣健行揪團", ReportedMemberAccount = "taroko_leader", ReporterAccount = "ling_tw", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "行程領隊失聯", Description = "健行當天領隊未出現", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 16, 10, 15, 0) },
            new Report { Id = 25, TargetType = ReportTargetType.Member, TargetId = 205, TargetTitle = "user_travel99", ReportedMemberAccount = "user_travel99", ReporterAccount = "hua_lin", ReasonCategory = ReportReasonCategory.Fraud, Reason = "私訊詐騙代訂機票", Description = "又一起要求私下匯款的檢舉,同一帳號累犯", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 16, 11, 0, 0) },
            new Report { Id = 26, TargetType = ReportTargetType.VlogPost, TargetId = 150, TargetTitle = "台東熱氣球嘉年華遊記", ReportedMemberAccount = "taitung_balloon", ReporterAccount = "wu_tommy", ReasonCategory = ReportReasonCategory.Spam, Reason = "內容置入賭博廣告連結", Description = "文章附上非法博弈網站連結", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 6, 25, 9, 0, 0) },
            new Report { Id = 27, TargetType = ReportTargetType.TravelGroup, TargetId = 600, TargetTitle = "小琉球潛水揪團", ReportedMemberAccount = "liuqiu_diver", ReporterAccount = "chiu_grace", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "潛水教練證照造假", Description = "帶隊教練無合格潛水證照", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 6, 26, 10, 30, 0) },
            new Report { Id = 28, TargetType = ReportTargetType.Member, TargetId = 700, TargetTitle = "scam_alex01", ReportedMemberAccount = "scam_alex01", ReporterAccount = "peng_alan", ReasonCategory = ReportReasonCategory.Fraud, Reason = "假冒客服詐騙儲值金", Description = "私訊假冒官方客服要求儲值驗證", Status = ReportStatus.Upheld, CreatedAt = new DateTime(2026, 6, 20, 8, 0, 0), AdminNotes = "查證屬實,已停權" },
            new Report { Id = 29, TargetType = ReportTargetType.VlogPost, TargetId = 151, TargetTitle = "合歡山雲海攻略", ReportedMemberAccount = "hehuan_hiker", ReporterAccount = "tsai_ivy", ReasonCategory = ReportReasonCategory.Copyright, Reason = "地圖圖片未經授權使用", Description = "文中登山地圖疑似盜用他人繪製作品", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 6, 27, 14, 0, 0) },
            new Report { Id = 30, TargetType = ReportTargetType.TravelGroup, TargetId = 601, TargetTitle = "北投泡湯揪團", ReportedMemberAccount = "beitou_owner", ReporterAccount = "ho_ray", ReasonCategory = ReportReasonCategory.Other, Reason = "檢舉內容與事實不符", Description = "檢舉描述與實際行程紀錄不符", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 6, 21, 9, 0, 0), AdminNotes = "查無違規,誤判" },
            new Report { Id = 31, TargetType = ReportTargetType.Member, TargetId = 701, TargetTitle = "fake_tour_guide", ReportedMemberAccount = "fake_tour_guide", ReporterAccount = "yang_sunny", ReasonCategory = ReportReasonCategory.Fraud, Reason = "假冒領隊收取訂金", Description = "自稱特約領隊要求先付訂金", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 6, 28, 11, 20, 0) },
            new Report { Id = 32, TargetType = ReportTargetType.VlogPost, TargetId = 152, TargetTitle = "新竹貢丸美食地圖", ReportedMemberAccount = "hsinchu_foodmap", ReporterAccount = "lai_ben", ReasonCategory = ReportReasonCategory.Spam, Reason = "留言區被灌爆賭博廣告", Description = "文章留言區出現大量博弈廣告留言", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 6, 29, 13, 0, 0) },
            new Report { Id = 33, TargetType = ReportTargetType.TravelGroup, TargetId = 602, TargetTitle = "台中文青半日遊揪團", ReportedMemberAccount = "taichung_owner", ReporterAccount = "kuo_jenny", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "行程時間表隨意更改", Description = "出發前一天臨時更改集合時間地點", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 6, 30, 10, 0, 0) },
            new Report { Id = 34, TargetType = ReportTargetType.Member, TargetId = 702, TargetTitle = "bot_account_55", ReportedMemberAccount = "bot_account_55", ReporterAccount = "cheng_max", ReasonCategory = ReportReasonCategory.Spam, Reason = "自動私訊發送廣告連結", Description = "短時間內對大量會員發送相同私訊", Status = ReportStatus.Upheld, CreatedAt = new DateTime(2026, 6, 22, 9, 30, 0), AdminNotes = "確認為機器人帳號,已停權" },
            new Report { Id = 35, TargetType = ReportTargetType.VlogPost, TargetId = 153, TargetTitle = "台中彩虹眷村散策", ReportedMemberAccount = "taichung_blogger", ReporterAccount = "su_amy", ReasonCategory = ReportReasonCategory.Copyright, Reason = "文字段落抄襲他人遊記", Description = "多段文字與另一篇熱門遊記幾乎相同", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 1, 8, 45, 0) },
            new Report { Id = 36, TargetType = ReportTargetType.TravelGroup, TargetId = 603, TargetTitle = "屏東墾丁後灣秘境揪團", ReportedMemberAccount = "pingtung_owner", ReporterAccount = "wu_tommy", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "團費收取後拖延退款", Description = "揪團取消後退款拖延超過兩週", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 1, 15, 10, 0) },
            new Report { Id = 37, TargetType = ReportTargetType.Member, TargetId = 703, TargetTitle = "harass_mike", ReportedMemberAccount = "harass_mike", ReporterAccount = "chiu_grace", ReasonCategory = ReportReasonCategory.Harassment, Reason = "多次騷擾女性會員私訊", Description = "多名會員檢舉收到不當追問私訊", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 2, 9, 0, 0) },
            new Report { Id = 38, TargetType = ReportTargetType.VlogPost, TargetId = 154, TargetTitle = "南投妖怪村探險", ReportedMemberAccount = "nantou_ghostvillage", ReporterAccount = "peng_alan", ReasonCategory = ReportReasonCategory.Spam, Reason = "文章內嵌賭博廣告banner", Description = "文章內嵌入式廣告連到博弈網站", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 2, 16, 40, 0) },
            new Report { Id = 39, TargetType = ReportTargetType.TravelGroup, TargetId = 604, TargetTitle = "南投妖怪村鬼屋揪團", ReportedMemberAccount = "ghosttour_owner", ReporterAccount = "tsai_ivy", ReasonCategory = ReportReasonCategory.Other, Reason = "純屬個人喜好爭議,非違規", Description = "團員對行程安排有意見,非違規事項", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 6, 24, 9, 0, 0), AdminNotes = "屬個人評價,不成立" },
            new Report { Id = 40, TargetType = ReportTargetType.Member, TargetId = 704, TargetTitle = "clone_acc_22", ReportedMemberAccount = "clone_acc_22", ReporterAccount = "ho_ray", ReasonCategory = ReportReasonCategory.Spam, Reason = "疑似分身帳號大量留言", Description = "多個帳號留言內容與語氣高度相似", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 3, 10, 20, 0) },
            new Report { Id = 41, TargetType = ReportTargetType.VlogPost, TargetId = 155, TargetTitle = "墾丁後灣浮潛紀錄", ReportedMemberAccount = "kenting_snorkel", ReporterAccount = "yang_sunny", ReasonCategory = ReportReasonCategory.Copyright, Reason = "影片配樂未取得授權", Description = "影片使用有版權疑慮的背景音樂", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 3, 17, 0, 0) },
            new Report { Id = 42, TargetType = ReportTargetType.TravelGroup, TargetId = 605, TargetTitle = "花蓮秒殺一日遊揪團", ReportedMemberAccount = "hualien_owner2", ReporterAccount = "lai_ben", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "揪團人數不足仍不退費", Description = "未達最低成行人數卻不退還團費", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 4, 9, 30, 0) },
            new Report { Id = 43, TargetType = ReportTargetType.Member, TargetId = 705, TargetTitle = "fake_agent007", ReportedMemberAccount = "fake_agent007", ReporterAccount = "kuo_jenny", ReasonCategory = ReportReasonCategory.Fraud, Reason = "再度冒充旅行社招攬", Description = "同一帳號再次以旅行社名義招攬團員", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 4, 14, 15, 0) },
            new Report { Id = 44, TargetType = ReportTargetType.VlogPost, TargetId = 156, TargetTitle = "澎湖花火節攻略", ReportedMemberAccount = "penghu_blogger", ReporterAccount = "cheng_max", ReasonCategory = ReportReasonCategory.Spam, Reason = "文章末端置入博弈廣告", Description = "文末附上非法博弈網站連結", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 5, 8, 50, 0) },
            new Report { Id = 45, TargetType = ReportTargetType.TravelGroup, TargetId = 606, TargetTitle = "金門戰地一日遊揪團", ReportedMemberAccount = "kinmen_owner", ReporterAccount = "su_amy", ReasonCategory = ReportReasonCategory.ServiceDispute, Reason = "行程景點與公告不符", Description = "實際行程少了兩個公告景點", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 5, 15, 30, 0) },
            new Report { Id = 46, TargetType = ReportTargetType.Member, TargetId = 706, TargetTitle = "spam_bot_99", ReportedMemberAccount = "spam_bot_99", ReporterAccount = "wu_tommy", ReasonCategory = ReportReasonCategory.Spam, Reason = "重複發送同一則廣告私訊", Description = "同一帳號再次被檢舉發送廣告私訊", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 6, 9, 0, 0) },
            new Report { Id = 47, TargetType = ReportTargetType.VlogPost, TargetId = 157, TargetTitle = "蘭嶼獨木舟體驗", ReportedMemberAccount = "lanyu_kayak", ReporterAccount = "chiu_grace", ReasonCategory = ReportReasonCategory.Copyright, Reason = "遊記內容整段複製他人網誌", Description = "多段文字與另一部落格文章完全相同", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 6, 13, 40, 0) },
            new Report { Id = 48, TargetType = ReportTargetType.TravelGroup, TargetId = 607, TargetTitle = "綠島潛水揪團", ReportedMemberAccount = "ludao_owner", ReporterAccount = "peng_alan", ReasonCategory = ReportReasonCategory.Other, Reason = "檢舉理由不明確", Description = "檢舉內容過於簡略,無法判斷具體違規事項", Status = ReportStatus.Dismissed, CreatedAt = new DateTime(2026, 6, 23, 8, 0, 0), AdminNotes = "檢舉內容不足以構成違規" },
            new Report { Id = 49, TargetType = ReportTargetType.Member, TargetId = 205, TargetTitle = "user_travel99", ReportedMemberAccount = "user_travel99", ReporterAccount = "tsai_ivy", ReasonCategory = ReportReasonCategory.Harassment, Reason = "在多篇文章下留言辱罵作者", Description = "多名發文者反映收到辱罵留言", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 7, 10, 10, 0) },
            new Report { Id = 50, TargetType = ReportTargetType.VlogPost, TargetId = 158, TargetTitle = "九族文化村遊記", ReportedMemberAccount = "jiuzu_blogger", ReporterAccount = "ho_ray", ReasonCategory = ReportReasonCategory.Spam, Reason = "文章附上非法博弈網站連結", Description = "文章結尾附上非法博弈網站連結", Status = ReportStatus.Pending, CreatedAt = new DateTime(2026, 7, 7, 16, 20, 0) }
        };

        private readonly INotificationService _notificationService;
        private readonly IAdminLogService _adminLogService;
        private readonly IMemberModerationService _memberModerationService;

        public ReportService(
            INotificationService notificationService,
            IAdminLogService adminLogService,
            IMemberModerationService memberModerationService)
        {
            _notificationService = notificationService;
            _adminLogService = adminLogService;
            _memberModerationService = memberModerationService;
        }

        public string GetRandomReviewerAlias() => _reviewerAliases[_random.Next(_reviewerAliases.Length)];

        public ReportQueryResult Query(ReportQueryOptions options)
        {
            List<Report> filtered;
            int totalCount, pendingCount, upheldCount, dismissedCount;

            lock (_reportsLock)
            {
                totalCount = _reports.Count;
                pendingCount = _reports.Count(r => r.Status == ReportStatus.Pending);
                upheldCount = _reports.Count(r => r.Status == ReportStatus.Upheld);
                dismissedCount = _reports.Count(r => r.Status == ReportStatus.Dismissed);

                var reports = _reports.AsEnumerable();
                if (options.Type.HasValue)
                {
                    reports = reports.Where(r => r.TargetType == options.Type.Value);
                }
                if (options.Status.HasValue)
                {
                    reports = reports.Where(r => r.Status == options.Status.Value);
                }
                if (options.ReasonCategory.HasValue)
                {
                    reports = reports.Where(r => r.ReasonCategory == options.ReasonCategory.Value);
                }
                if (!string.IsNullOrWhiteSpace(options.Keyword))
                {
                    // Keyword 已由 Controller trim 過,這裡直接用;只查檢舉人帳號、被檢舉會員帳號兩個欄位
                    reports = reports.Where(r =>
                        r.ReporterAccount.Contains(options.Keyword, StringComparison.OrdinalIgnoreCase) ||
                        r.ReportedMemberAccount.Contains(options.Keyword, StringComparison.OrdinalIgnoreCase));
                }
                if (options.StartDate.HasValue)
                {
                    reports = reports.Where(r => r.CreatedAt.Date >= options.StartDate.Value.Date);
                }
                if (options.EndDate.HasValue)
                {
                    reports = reports.Where(r => r.CreatedAt.Date <= options.EndDate.Value.Date);
                }

                // 狀態優先:待處理排最前面,方便優先處理;同一個狀態內再依編號由新到舊排,
                // 兩層排序疊在一起,同一區塊內的編號就不會跳來跳去
                filtered = reports
                    .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
                    .ThenByDescending(r => r.Id)
                    .ToList();
            }

            var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
            var page = Math.Clamp(options.Page, 1, totalPages);
            var paged = filtered.Skip((page - 1) * PageSize).Take(PageSize).ToList();

            return new ReportQueryResult
            {
                Items = paged,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PendingCount = pendingCount,
                UpheldCount = upheldCount,
                DismissedCount = dismissedCount
            };
        }

        public Report? GetById(int id)
        {
            lock (_reportsLock)
            {
                return _reports.FirstOrDefault(r => r.Id == id);
            }
        }

        public List<Report> GetRelatedReports(Report report)
        {
            lock (_reportsLock)
            {
                return _reports
                    .Where(r => r.TargetType == report.TargetType && r.TargetId == report.TargetId && r.Id != report.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToList();
            }
        }

        public int CountUpheldForAccount(string account)
        {
            lock (_reportsLock)
            {
                return _reports.Count(r => r.ReportedMemberAccount == account && r.Status == ReportStatus.Upheld);
            }
        }

        public int? GetPendingSuspensionDays(Report report)
        {
            // +1 是把「這筆案件如果被判定成立」也算進累犯次數
            var upheldCountIfUpheld = CountUpheldForAccount(report.ReportedMemberAccount) + 1;
            return CalculateSuspendDays(upheldCountIfUpheld);
        }

        public int CountMaliciousForAccount(string reporterAccount)
        {
            lock (_reportsLock)
            {
                return _reports.Count(r => r.ReporterAccount == reporterAccount && r.IsMalicious);
            }
        }

        public int? GetPendingReporterSuspensionDays(Report report)
        {
            // +1 是把「這筆案件如果被標記惡意檢舉」也算進累犯次數
            var maliciousCountIfFlagged = CountMaliciousForAccount(report.ReporterAccount) + 1;
            return CalculateSuspendDays(maliciousCountIfFlagged);
        }

        // 第 4 次違規停 3 天(輕度),第 5 次以上停 5 天(重度);未達門檻回傳 null
        private int? CalculateSuspendDays(int upheldCount)
        {
            if (upheldCount <= SuspendThreshold)
            {
                return null;
            }
            return upheldCount == SuspendThreshold + 1 ? 3 : 5;
        }

        public async Task<JudgeOutcome> JudgeAsync(int id, ReportStatus decision, string? note, bool isMalicious, string reviewerName)
        {
            Report? report;
            int upheldCount;
            int maliciousCount;

            lock (_reportsLock)
            {
                report = _reports.FirstOrDefault(r => r.Id == id);
                if (report == null)
                {
                    return new JudgeOutcome { Found = false };
                }

                if (report.Status != ReportStatus.Pending)
                {
                    // 已經被處理過,避免重複判定(例如兩個管理員同時點開同一筆)
                    return new JudgeOutcome { Found = true, Message = $"檢舉單 #{report.Id} 已經被判定過,無法重複處理" };
                }

                report.Status = decision;
                report.AdminNotes = note;
                // 惡意檢舉標記只有在「不成立」時才有意義,判定成立時直接忽略這個勾選
                report.IsMalicious = decision == ReportStatus.Dismissed && isMalicious;

                // 累犯次數以「同一個被檢舉會員帳號」+「檢舉成立」計算,不成立的不算違規
                upheldCount = _reports.Count(r =>
                    r.ReportedMemberAccount == report.ReportedMemberAccount && r.Status == ReportStatus.Upheld);

                // 檢舉人累犯次數以「同一個檢舉人帳號」+「被標記惡意檢舉」計算
                maliciousCount = _reports.Count(r => r.ReporterAccount == report.ReporterAccount && r.IsMalicious);
            }

            var resultText = decision.ToDisplayName();

            // 判定 → 呼叫組長 Service 發通知 + 寫 Log
            await _notificationService.SendAsync(
                report.ReporterAccount,
                "檢舉處理結果通知",
                $"您於 {report.CreatedAt:yyyy/MM/dd} 檢舉的「{report.TargetTitle}」,審核結果為:{resultText}");

            await _adminLogService.WriteAsync(
                reviewerName,
                "審核檢舉",
                $"檢舉單 #{report.Id}({report.TargetType.ToDisplayName()}:{report.TargetTitle})判定為「{resultText}」",
                targetTable: "Reports",
                targetId: report.Id);

            var message = $"檢舉單 #{report.Id} 已判定為「{resultText}」";

            if (decision == ReportStatus.Upheld)
            {
                // 通知被檢舉的會員:此次違規查證屬實
                await _notificationService.SendAsync(
                    report.ReportedMemberAccount,
                    "違規通知",
                    $"您於 {report.CreatedAt:yyyy/MM/dd} 因「{report.Reason}」遭檢舉,經審核查證屬實,請留意平台規範。");

                var suspendDaysIfAny = CalculateSuspendDays(upheldCount);
                if (suspendDaysIfAny.HasValue)
                {
                    var suspendDays = suspendDaysIfAny.Value;
                    var suspendReason = $"累計 {upheldCount} 次檢舉成立(超過門檻 {SuspendThreshold} 次)";

                    await _memberModerationService.SuspendAsync(report.ReportedMemberAccount, suspendDays, suspendReason);

                    await _notificationService.SendAsync(
                        report.ReportedMemberAccount,
                        "帳號停權通知",
                        $"您的帳號因{suspendReason},已停權 {suspendDays} 天。");

                    await _adminLogService.WriteAsync(
                        reviewerName,
                        "自動停權",
                        $"帳號 {report.ReportedMemberAccount} 累犯 {upheldCount} 次,停權 {suspendDays} 天(觸發自檢舉單 #{report.Id})",
                        targetTable: "Members",
                        targetId: null);

                    message += $";帳號「{report.ReportedMemberAccount}」累犯 {upheldCount} 次,已自動停權 {suspendDays} 天";
                }
            }
            else if (report.IsMalicious)
            {
                // 通知檢舉人:這次檢舉被標記為惡意檢舉,跟單純誤判/證據不足分開處理
                await _notificationService.SendAsync(
                    report.ReporterAccount,
                    "惡意檢舉警告",
                    $"您於 {report.CreatedAt:yyyy/MM/dd} 提出的檢舉經審核為惡意檢舉,請勿濫用檢舉功能。");

                var reporterSuspendDaysIfAny = CalculateSuspendDays(maliciousCount);
                if (reporterSuspendDaysIfAny.HasValue)
                {
                    var suspendDays = reporterSuspendDaysIfAny.Value;
                    var suspendReason = $"累計 {maliciousCount} 次惡意檢舉(超過門檻 {SuspendThreshold} 次)";

                    await _memberModerationService.SuspendAsync(report.ReporterAccount, suspendDays, suspendReason);

                    await _notificationService.SendAsync(
                        report.ReporterAccount,
                        "帳號停權通知",
                        $"您的帳號因{suspendReason},已停權 {suspendDays} 天。");

                    await _adminLogService.WriteAsync(
                        reviewerName,
                        "自動停權(惡意檢舉)",
                        $"帳號 {report.ReporterAccount} 惡意檢舉累犯 {maliciousCount} 次,停權 {suspendDays} 天(觸發自檢舉單 #{report.Id})",
                        targetTable: "Members",
                        targetId: null);

                    message += $";檢舉人「{report.ReporterAccount}」惡意檢舉累犯 {maliciousCount} 次,已自動停權 {suspendDays} 天";
                }
            }

            return new JudgeOutcome { Found = true, Message = message };
        }

        // 新增一筆檢舉（例如小編對會員文章提出檢舉）。跟 JudgeAsync 共用同一個 _reports 記憶體清單，
        // 不動既有的判定/查詢邏輯，純粹多一筆 Pending 狀態的紀錄進去。
        public async Task<Report> SubmitAsync(ReportTargetType targetType, int targetId, string targetTitle,
            string reportedMemberAccount, string reporterAccount, ReportReasonCategory reasonCategory, string reason)
        {
            Report newReport;
            lock (_reportsLock)
            {
                newReport = new Report
                {
                    Id = _reports.Count == 0 ? 1 : _reports.Max(r => r.Id) + 1,
                    TargetType = targetType,
                    TargetId = targetId,
                    TargetTitle = targetTitle,
                    ReportedMemberAccount = reportedMemberAccount,
                    ReporterAccount = reporterAccount,
                    ReasonCategory = reasonCategory,
                    Reason = reason,
                    Status = ReportStatus.Pending,
                    CreatedAt = DateTime.Now,
                };
                _reports.Add(newReport);
            }

            await _adminLogService.WriteAsync(
                reporterAccount,
                "提出檢舉",
                $"檢舉 {targetType.ToDisplayName()}「{targetTitle}」：{reason}",
                targetTable: "Reports",
                targetId: newReport.Id);

            return newReport;
        }

        public async Task<List<AdminLog>> GetRecentReportLogsAsync(int take)
        {
            var recentLogs = await _adminLogService.GetRecentAsync(50);
            return recentLogs.Where(l => l.TargetTable == "Reports").Take(take).ToList();
        }

        public async Task<AdminLog?> GetReviewLogAsync(int reportId)
        {
            var logs = await _adminLogService.GetForTargetAsync("Reports", reportId);
            return logs.FirstOrDefault();
        }
    }
}
