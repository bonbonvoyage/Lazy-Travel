#nullable disable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LazyTravel.Models;

public partial class TravelGroupsLog
{
    [Key]
    [Column("LogID")]
    public int LogId { get; set; }

    [Column("GroupID")]
    public int GroupId { get; set; }

    // 目前後台尚未串接管理員登入驗證，暫時允許為 NULL。
    // 待後續串接登入驗證後，改由後端寫入實際操作的管理員 MemberID。
    [Column("ChangeByMemberID")]
    public int? ChangedByMemberId { get; set; }

    [Required]
    [StringLength(30)]
    public string ChangeType { get; set; }

    [StringLength(50)]
    public string FieldName { get; set; }

    [StringLength(300)]
    public string OldValue { get; set; }

    [StringLength(300)]
    public string NewValue { get; set; }

    [StringLength(300)]
    public string Remark { get; set; }

    [Column("CreateAt", TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("TravelGroupsLogs")]
    public virtual TravelGroup Group { get; set; }

    [ForeignKey("ChangedByMemberId")]
    public virtual Member ChangedByMember { get; set; }
}
