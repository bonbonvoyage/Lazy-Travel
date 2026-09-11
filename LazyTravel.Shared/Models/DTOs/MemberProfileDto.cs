using System;
using System.Collections.Generic;

namespace LazyTravel.Shared.Models.DTOs
{
    // 會員個人頁面用的資料模型：自己看、跟看別人看到的都是這個形狀，
    // 差別在後端已經先把「聯絡方式看不看得到」算好了塞進 ContactVisible，
    // 前端不用自己判斷關係、也不用自己重算隱私規則。
    public class MemberProfileDto
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateOnly? BirthDate { get; set; }
        public int? Age { get; set; }
        public byte Gender { get; set; }
        public string? Occupation { get; set; }
        public string? Mbti { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; } // 居住地，例如「台北・大安」

        // 技能標籤（旅行技能）
        public int SkillCount { get; set; }
        public int SkillTotal { get; set; }
        public List<string> SkillNames { get; set; } = new();

        // 全部可選技能標籤（含 ID），前端畫面存檔要用 SkillId，
        // 但畫面上的技能名稱清單（SKILL_GROUPS）是設計稿寫死的，沒有 ID，
        // 所以把「名稱 → ID」的對照表一起回傳，前端存檔時用名稱查表轉成 ID。
        public List<SkillOptionDto> SkillCatalog { get; set; } = new();

        // 旅遊 DNA 四個維度
        public List<TravelDnaDto> TravelDna { get; set; } = new();

        // 統計數字
        public int GroupsOwnedCount { get; set; }   // 揪團次數：自己開的團
        public int TripsJoinedCount { get; set; }   // 旅遊次數：實際參加過（含自己開的）且沒被移除的團數

        // 關係與隱私
        public bool IsSelf { get; set; }
        public string RelationshipToViewer { get; set; } = "Stranger"; // Self / Friend / GroupMate / Following / Stranger
        public bool ContactBookPublic { get; set; }
        public bool ContactVisible { get; set; }

        // 查看「別人」頁面時，追蹤／申請好友／封鎖這幾個按鈕要顯示成什麼狀態用的旗標，
        // 只有 IsSelf 是 false 才有意義（看自己的頁面這幾個都是 false，前端也不會用到）。
        // 這裡只算「目前登入的自己（viewer）對這個人做了什麼」，不算對方對我做了什麼。
        public bool ViewerIsFollowing { get; set; }         // 我是不是已經在追蹤這個人
        public bool ViewerHasPendingFriendRequest { get; set; } // 我是不是已經送出好友申請、對方還沒審核
        public bool ViewerHasBlocked { get; set; }          // 我是不是已經封鎖這個人
        public bool ViewerIsBlockedByTarget { get; set; }   // 對方是不是封鎖了我（封鎖我的話不能追蹤/加好友）

        // 聯絡方式：ContactVisible 是 false 的時候，這幾個欄位一律是 null，
        // 不會把資料撈出來又靠前端藏起來——遮蔽是在後端就做掉的。
        public string? Phone { get; set; }
        public string? LineId { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }

        // 手帳第二頁（好友列表／追蹤名單／黑名單／好友申請）用的資料。
        // IsSelf 是 true（看自己）→ 完整四份清單都有值。
        // IsSelf 是 false（看別人）→ 依 ContactVisible 決定：
        //   true（對方通訊錄公開，或彼此是好友/同團）→ 只有 Friends／Following 有值，
        //     Blocked／Requests 固定是空清單（那兩份是對方的隱私，看別人的頁面不該看到）。
        //   false（沒有權限）→ 整個 ContactBook 是 null，前端顯示「對方的通訊錄未公開」。
        public ContactBookDto? ContactBook { get; set; }
    }

    // 手帳第二頁「搜尋陌生人」用的單筆搜尋結果（見 MemberProfileService.SearchMembers）。
    // RelationshipStatus 只有四種值，前端 scene.dc.html 的 strangerList 靠這個決定
    // 每一列要顯示「加好友」「已送出申請」「已是好友」還是「接受好友申請」：
    //   Stranger        - 彼此還沒有任何好友關聯，可以送出好友申請
    //   PendingSent     - 我已經送出申請、對方還沒審核
    //   PendingReceived - 對方已經送申請給我、我還沒審核
    //   Friend          - 雙方已經是好友
    public class MemberSearchResultDto
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? City { get; set; }
        public string? Mbti { get; set; }
        public string RelationshipStatus { get; set; } = "Stranger";
    }

    public class TravelDnaDto
    {
        public byte DimensionId { get; set; }
        public string DimensionName { get; set; } = string.Empty;
        public string LeftLabel { get; set; } = string.Empty;
        public string RightLabel { get; set; } = string.Empty;
        public byte Score { get; set; }
        public string? OptionName { get; set; } // 分數落在哪個區間對應的稱號，例如「按圖索驥」
    }

    public class SkillOptionDto
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
    }

    // 旅遊 DNA 拉桿存檔用：畫面上四個維度一次全部送回來，
    // 沒填過的維度後端會自己新增一筆，已經有的就更新分數。
    public class UpdateTravelDnaDto
    {
        public List<TravelDnaScoreItem> Scores { get; set; } = new();
    }

    public class TravelDnaScoreItem
    {
        public byte DimensionId { get; set; }
        public byte Score { get; set; }
    }
}
