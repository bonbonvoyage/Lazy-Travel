using System;
using System.Collections.Generic;

namespace LazyTravel.Shared.Models.DTOs
{
    // 會員編輯自己個人頁面用。只給「本人可以改的欄位」，
    // Status、Email、CreatedAt 這些系統/驗證欄位不開放從這裡改。
    public class MemberProfileEditDto
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public byte Gender { get; set; }
        public string? Occupation { get; set; }
        public string? Mbti { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; } // 居住地，例如「台北・大安」

        // 手機不用簡訊驗證了（產品規則已確認），開放跟 LineId 一樣讓會員自己改，
        // 存進資料庫前一樣會在 MemberProfileService.UpdateProfile 用
        // SocialLinkValidator.TryNormalizePhone 驗證台灣手機格式。
        public string? Phone { get; set; }
        public string? LineId { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }

        // true = 公開（所有人都看得到）；false = 私人（只有好友和「還在同團」的人看得到）
        public bool ContactBookPublic { get; set; }

        // 這次編輯後，會員身上應該有的技能標籤 ID 清單（整批覆蓋，不是增量）
        public List<int> SkillIds { get; set; } = new();
    }
}
