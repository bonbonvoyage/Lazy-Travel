using LazyTravel.Models;

namespace LazyTravel.Services;

// 對應規格書第 13 表 ItineraryNodes 的假資料倉儲，用法同 VlogPostStore。
public static class ItineraryNodeStore
{
    private static readonly List<ItineraryNode> _nodes = new();
    private static int _nextId = 1;

    static ItineraryNodeStore()
    {
        Seed();
    }

    public static List<ItineraryNode> GetByPostId(int postId) =>
        _nodes.Where(n => n.PostID == postId).ToList();

    public static ItineraryNode? GetById(int nodeId) =>
        _nodes.FirstOrDefault(n => n.NodeID == nodeId);

    public static ItineraryNode Add(ItineraryNode node)
    {
        node.NodeID = _nextId++;
        _nodes.Add(node);
        return node;
    }

    // 只更新允許被編輯的欄位，NodeID / PostID 不從表單覆蓋回來
    public static bool Update(ItineraryNode updated)
    {
        var existing = GetById(updated.NodeID);
        if (existing is null)
        {
            return false;
        }

        existing.DayNumber = updated.DayNumber;
        existing.LocationName = updated.LocationName;
        existing.ArrivalTime = updated.ArrivalTime;
        existing.StayTime = updated.StayTime;
        existing.DepartureTime = updated.DepartureTime;
        existing.MediaUrl = updated.MediaUrl;
        existing.MediaType = updated.MediaType;
        existing.Description = updated.Description;
        existing.Remarks = updated.Remarks;
        return true;
    }

    public static bool Delete(int nodeId)
    {
        var node = _nodes.FirstOrDefault(n => n.NodeID == nodeId);
        return node is not null && _nodes.Remove(node);
    }

    // 對應到 VlogPostStore.Seed() 裡固定的 PostID 1、2，示範用
    private static void Seed()
    {
        Add(new ItineraryNode
        {
            PostID = 1,
            DayNumber = 1,
            LocationName = "七星潭",
            ArrivalTime = new TimeOnly(9, 0),
            StayTime = "2 小時",
            DepartureTime = new TimeOnly(11, 0),
            Description = "撿石頭、聽海浪聲，很適合放空。",
        });
        Add(new ItineraryNode
        {
            PostID = 1,
            DayNumber = 1,
            LocationName = "花蓮市區小吃",
            ArrivalTime = new TimeOnly(18, 0),
            StayTime = "1.5 小時",
            DepartureTime = new TimeOnly(19, 30),
            Description = "公正包子、液香扁食都在附近。",
        });
        Add(new ItineraryNode
        {
            PostID = 1,
            DayNumber = 2,
            LocationName = "清水斷崖",
            ArrivalTime = new TimeOnly(10, 0),
            StayTime = "1 小時",
            DepartureTime = new TimeOnly(11, 0),
            Remarks = "建議在退潮時段拍照。",
        });

        Add(new ItineraryNode
        {
            PostID = 2,
            DayNumber = 1,
            LocationName = "伏見稻荷大社",
            ArrivalTime = new TimeOnly(8, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(11, 0),
            Description = "早點到人比較少，千本鳥居很好拍。",
        });
        Add(new ItineraryNode
        {
            PostID = 2,
            DayNumber = 2,
            LocationName = "嵐山竹林",
            ArrivalTime = new TimeOnly(9, 0),
            StayTime = "1 小時",
            DepartureTime = new TimeOnly(10, 0),
        });

        // PostID = 3：台南巷弄美食兩天一夜
        Add(new ItineraryNode
        {
            PostID = 3,
            DayNumber = 1,
            LocationName = "阿明豬心冬粉",
            ArrivalTime = new TimeOnly(7, 30),
            StayTime = "30 分鐘",
            DepartureTime = new TimeOnly(8, 0),
            Description = "在地人推薦的早餐名店，湯頭清甜。",
        });
        Add(new ItineraryNode
        {
            PostID = 3,
            DayNumber = 1,
            LocationName = "神農街",
            ArrivalTime = new TimeOnly(15, 0),
            StayTime = "2 小時",
            DepartureTime = new TimeOnly(17, 0),
            Description = "老屋改建的文青小店很多，適合慢慢逛。",
        });
        Add(new ItineraryNode
        {
            PostID = 3,
            DayNumber = 2,
            LocationName = "花園夜市",
            ArrivalTime = new TimeOnly(18, 0),
            StayTime = "2 小時",
            DepartureTime = new TimeOnly(20, 0),
            Remarks = "假日限定，記得先查營業日。",
        });

        // PostID = 4：武陵農場露營紀錄
        Add(new ItineraryNode
        {
            PostID = 4,
            DayNumber = 1,
            LocationName = "武陵農場遊客中心",
            ArrivalTime = new TimeOnly(13, 0),
            StayTime = "30 分鐘",
            DepartureTime = new TimeOnly(13, 30),
            Remarks = "先辦入園手續，旺季建議提早出發。",
        });
        Add(new ItineraryNode
        {
            PostID = 4,
            DayNumber = 1,
            LocationName = "露營區紮營",
            ArrivalTime = new TimeOnly(14, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(17, 0),
            Description = "第一次搭帳篷花了不少時間，建議兩人以上一起搭。",
        });
        Add(new ItineraryNode
        {
            PostID = 4,
            DayNumber = 2,
            LocationName = "武陵吊橋",
            ArrivalTime = new TimeOnly(9, 0),
            StayTime = "1 小時",
            DepartureTime = new TimeOnly(10, 0),
        });

        // PostID = 5：【官方精選】秋季賞楓 5 條路線推薦
        Add(new ItineraryNode
        {
            PostID = 5,
            DayNumber = 1,
            LocationName = "奧萬大森林遊樂區",
            ArrivalTime = new TimeOnly(9, 0),
            StayTime = "2 小時",
            DepartureTime = new TimeOnly(11, 0),
            Description = "全台最著名的賞楓地點，11 月中下旬最佳。",
        });
        Add(new ItineraryNode
        {
            PostID = 5,
            DayNumber = 1,
            LocationName = "拉拉山恩愛農場",
            ArrivalTime = new TimeOnly(13, 0),
            StayTime = "1.5 小時",
            DepartureTime = new TimeOnly(14, 30),
        });

        // PostID = 6：墾丁夏日海邊放空計畫
        Add(new ItineraryNode
        {
            PostID = 6,
            DayNumber = 1,
            LocationName = "白沙灣",
            ArrivalTime = new TimeOnly(10, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(13, 0),
            Description = "沙子很細，適合浮潛，記得帶蛙鏡。",
        });
        Add(new ItineraryNode
        {
            PostID = 6,
            DayNumber = 2,
            LocationName = "墾丁大街",
            ArrivalTime = new TimeOnly(19, 0),
            StayTime = "2 小時",
            DepartureTime = new TimeOnly(21, 0),
        });

        // PostID = 7：沖繩親子自駕 5 天 4 夜
        Add(new ItineraryNode
        {
            PostID = 7,
            DayNumber = 1,
            LocationName = "美國村",
            ArrivalTime = new TimeOnly(15, 0),
            StayTime = "2 小時",
            DepartureTime = new TimeOnly(17, 0),
            Description = "摩天輪跟親子友善的餐廳都在這一區。",
        });
        Add(new ItineraryNode
        {
            PostID = 7,
            DayNumber = 2,
            LocationName = "沖繩美麗海水族館",
            ArrivalTime = new TimeOnly(10, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(13, 0),
            Remarks = "建議先在官網買票，可以節省排隊時間。",
        });

        // PostID = 9：首爾冬季滑雪＋逛街行程
        Add(new ItineraryNode
        {
            PostID = 9,
            DayNumber = 1,
            LocationName = "南怡島",
            ArrivalTime = new TimeOnly(9, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(12, 0),
        });
        Add(new ItineraryNode
        {
            PostID = 9,
            DayNumber = 3,
            LocationName = "明洞",
            ArrivalTime = new TimeOnly(14, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(17, 0),
            Description = "血拼跟街頭小吃的天堂，晚餐前先墊墊胃。",
        });

        // PostID = 11：澎湖跳島兩天一夜：花火節限定
        Add(new ItineraryNode
        {
            PostID = 11,
            DayNumber = 1,
            LocationName = "吉貝嶼",
            ArrivalTime = new TimeOnly(9, 0),
            StayTime = "3 小時",
            DepartureTime = new TimeOnly(12, 0),
            Description = "沙尾很漂亮，水上活動選擇也很多。",
        });
        Add(new ItineraryNode
        {
            PostID = 11,
            DayNumber = 1,
            LocationName = "觀音亭花火節會場",
            ArrivalTime = new TimeOnly(20, 0),
            StayTime = "1 小時",
            DepartureTime = new TimeOnly(21, 0),
            Remarks = "花火節期間人潮較多，建議提早卡位。",
        });

        // PostID = 12：【官方精選】九份老街半日散策地圖
        Add(new ItineraryNode
        {
            PostID = 12,
            DayNumber = 1,
            LocationName = "豎崎路階梯",
            ArrivalTime = new TimeOnly(15, 0),
            StayTime = "1 小時",
            DepartureTime = new TimeOnly(16, 0),
            Description = "傍晚時分燈籠點亮，是九份最經典的拍照角度。",
        });
        Add(new ItineraryNode
        {
            PostID = 12,
            DayNumber = 1,
            LocationName = "阿妹茶樓",
            ArrivalTime = new TimeOnly(16, 30),
            StayTime = "1 小時",
            DepartureTime = new TimeOnly(17, 30),
        });
    }
}
