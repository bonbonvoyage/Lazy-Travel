using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LazyTravel.Models
{
    // 「類型/類別/狀態」的中文對照表,對應資料庫的 ReportTargetTypes/ReportReasonCategories/ReportStatuses,
    // 文字內容存在資料庫,不是寫死在程式碼裡。

    [Table("ReportTargetTypes")]
    public class ReportTargetTypeLookup
    {
        [Key]
        public byte TypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
    }

    [Table("ReportReasonCategories")]
    public class ReportReasonCategoryLookup
    {
        [Key]
        public byte CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    [Table("ReportStatuses")]
    public class ReportStatusLookup
    {
        [Key]
        public byte StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
