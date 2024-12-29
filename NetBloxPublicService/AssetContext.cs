using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml;

namespace NetBloxPublicService
{
	public enum UserMembershipType : byte
	{
		None, Banned, Admin, Tier1, Tier2, Tier3
	}
	public enum CommonAssetType : byte
	{
		Place, Universe, User, ClothingShirt, ClothingPants
	}
	public class AssetContext : DbContext
	{
		public AssetContext(DbContextOptions<AssetContext> options) : base(options) => Database.EnsureCreated();

		public DbSet<CommonAssetItem> CommonAssets { get; set; } = null!;
		public DbSet<UserItem> Users { get; set; } = null!;
		public DbSet<PlaceItem> Places { get; set; } = null!;
		public DbSet<UniverseItem> Universes { get; set; } = null!;

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => base.OnConfiguring(optionsBuilder);
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<CommonAssetItem>(b =>
			{
				b.HasKey(e => e.Id);
				b.Property(e => e.Id).ValueGeneratedOnAdd();
			});
			modelBuilder.Entity<UserItem>(b =>
			{
				b.HasKey(e => e.UserId);
				b.Property(e => e.UserId).ValueGeneratedOnAdd();
			});
		}
	}
	public class UserItem
	{
		[Key]
		public long UserId { get; set; }
		public ushort AppearanceHeadBC { get; set; }
		public ushort AppearanceTorsoBC { get; set; }
		public ushort AppearanceLeftArmBC { get; set; }
		public ushort AppearanceRightArmBC { get; set; }
		public ushort AppearanceLeftLegBC { get; set; }
		public ushort AppearanceRightLegBC { get; set; }
		public long AppearanceFaceId { get; set; }
		public long AppearanceShirtId { get; set; }
		public long AppearancePantsId { get; set; }
		public string InventoryString { get; set; }
		[JsonIgnore]
		public string HashedPassword { get; set; }
		public string UserName { get; set; }
		public UserMembershipType MembershipType { get; set; }
		public NetBlox.OnlineMode OnlineMode { get; set; }
		public string? Status { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateLastOnline { get; set; }
		[JsonIgnore]
		public Guid LoginToken { get; set; }
	}
	public class PlaceItem
	{
		[Key]
		public long AssetId { get; set; }
		public long VisitCount { get; set; }
		public long FavoriteCount { get; set; }
		public string RbxlData { get; set; }
	}
	public class UniverseItem
	{
		[Key]
		public long AssetId { get; set; }
		public long StarterPlaceId { get; set; }
		public bool AllowMultiplayer { get; set; }
	}
	public class CommonAssetItem
	{
		public long Id { get; set; }
		public CommonAssetType Type { get; set; }
		public long OwnerId { get; set; }
		public string? Name { get; set; }
		public string? Description { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime DateUpdated { get; set; }
	}
}
