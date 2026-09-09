using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LazyTravel.Shared.Models.EfModels;

[Table("VlogPostRoomExports")]
public class VlogPostRoomExport
{
    [Key] public int PostId { get; set; }
    public int GroupId { get; set; }
    [MaxLength(100)] public string Country { get; set; } = "";
    [MaxLength(200)] public string Region { get; set; } = "";
    public int People { get; set; }
    public DateTime ExportedAt { get; set; }
    public VlogPost Post { get; set; } = null!;
    public TravelGroup Group { get; set; } = null!;
}

public partial class LazyTravelDBContext
{
    public DbSet<VlogPostRoomExport> VlogPostRoomExports { get; set; } = null!;
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VlogPostRoomExport>(entity =>
        {
            entity.HasIndex(e => e.GroupId).IsUnique();
            entity.HasOne(e => e.Post).WithOne().HasForeignKey<VlogPostRoomExport>(e => e.PostId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Group).WithMany().HasForeignKey(e => e.GroupId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}