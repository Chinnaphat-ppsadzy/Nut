using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Chinnaphat_8272_L005.Models;

public partial class Csi402dbContext : DbContext
{
    public Csi402dbContext()
    {
    }

    public Csi402dbContext(DbContextOptions<Csi402dbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingItem> BookingItems { get; set; }

    public virtual DbSet<BookingPromotion> BookingPromotions { get; set; }

    public virtual DbSet<BookingSlot> BookingSlots { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Court> Courts { get; set; }

    public virtual DbSet<CourtBlock> CourtBlocks { get; set; }

    public virtual DbSet<CourtIssue> CourtIssues { get; set; }

    public virtual DbSet<EquipmentDamage> EquipmentDamages { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionRule> PromotionRules { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<Refund> Refunds { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SiteSetting> SiteSettings { get; set; }

    public virtual DbSet<StockLog> StockLogs { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VDailySale> VDailySales { get; set; }

    public virtual DbSet<VPeakTime> VPeakTimes { get; set; }

    public virtual DbSet<WalkInQueue> WalkInQueues { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=csi402db;user=root;password=1101801447419Pp!", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.7.1-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.BookingId).HasName("PRIMARY");

            entity.ToTable("bookings", tb => tb.HasComment("เธซเธฑเธงเธเธดเธฅเธเธฒเธฃเธเธญเธ"));

            entity.HasIndex(e => e.CheckedInBy, "fk_booking_cin");

            entity.HasIndex(e => e.CheckedOutBy, "fk_booking_cout");

            entity.HasIndex(e => new { e.PlayDate, e.Status }, "idx_booking_date_status");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_booking_user");

            entity.HasIndex(e => e.BookingCode, "uq_booking_code").IsUnique();

            entity.Property(e => e.BookingId)
                .HasComment("PK")
                .HasColumnName("booking_id");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(20)
                .HasComment("เนเธฅเธเธเธตเนเธเธฒเธฃเธเธญเธเธเธตเนเนเธชเธเธเนเธซเนเธฅเธนเธเธเนเธฒ เนเธเนเธ BK20261004001")
                .HasColumnName("booking_code");
            entity.Property(e => e.CancelReason)
                .HasMaxLength(255)
                .HasComment("เนเธซเธเธธเธเธฅเธขเธเนเธฅเธดเธ")
                .HasColumnName("cancel_reason");
            entity.Property(e => e.CancelledAt)
                .HasComment("เนเธงเธฅเธฒเธขเธเนเธฅเธดเธ")
                .HasColumnType("datetime")
                .HasColumnName("cancelled_at");
            entity.Property(e => e.CheckedInAt)
                .HasComment("เนเธงเธฅเธฒ Check-in")
                .HasColumnType("datetime")
                .HasColumnName("checked_in_at");
            entity.Property(e => e.CheckedInBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธเธเธตเน Check-in)")
                .HasColumnName("checked_in_by");
            entity.Property(e => e.CheckedOutAt)
                .HasComment("เนเธงเธฅเธฒ Check-out")
                .HasColumnType("datetime")
                .HasColumnName("checked_out_at");
            entity.Property(e => e.CheckedOutBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธเธเธตเน Check-out)")
                .HasColumnName("checked_out_by");
            entity.Property(e => e.ConfirmedAt)
                .HasComment("เธงเธฑเธเธเธตเนเธเธฑเธเธเธตเธญเธเธธเธกเธฑเธเธด")
                .HasColumnType("datetime")
                .HasColumnName("confirmed_at");
            entity.Property(e => e.CourtAmount)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธงเธกเธเนเธฒเนเธเนเธฒเธเธญเธฃเนเธ (เธเนเธญเธเธฅเธ)")
                .HasColumnName("court_amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธเธเธดเธฅ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasComment("เธชเนเธงเธเธฅเธเธฃเธงเธกเธเธฒเธเนเธเธฃเนเธกเธเธฑเนเธ")
                .HasColumnName("discount_amount");
            entity.Property(e => e.ExpiresAt)
                .HasComment("เนเธงเธฅเธฒเธซเธกเธเธญเธฒเธขเธธเธเธฒเธฃเธเธณเธฃเธฐเนเธเธดเธ (เธเธฅเนเธญเธขเธเธญเธฃเนเธเธเธทเธเธญเธฑเธเนเธเธกเธฑเธเธด)")
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.IsFirstBooking)
                .HasComment("เธเธดเธฅเธเธตเนเนเธเนเธเธเธฒเธฃเธเธญเธเธเธฃเธฑเนเธเนเธฃเธเธเธญเธเธชเธกเธฒเธเธดเธเธซเธฃเธทเธญเนเธกเน (snapshot เนเธเนเธเธฑเธเนเธเธฃเธฏ 5)")
                .HasColumnName("is_first_booking");
            entity.Property(e => e.NetAmount)
                .HasPrecision(10, 2)
                .HasComment("เธขเธญเธเธชเธธเธเธเธดเธเธตเนเธเนเธญเธเธเธณเธฃเธฐ")
                .HasColumnName("net_amount");
            entity.Property(e => e.PlayDate)
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเธฒเนเธเนเธชเธเธฒเธก")
                .HasColumnName("play_date");
            entity.Property(e => e.RentalAmount)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธงเธกเธเนเธฒเนเธเนเธฒเธญเธธเธเธเธฃเธเน (เธเนเธญเธเธฅเธ)")
                .HasColumnName("rental_amount");
            entity.Property(e => e.SaleAmount)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธงเธกเธเนเธฒเธชเธดเธเธเนเธฒเธเธตเนเธเธทเนเธญ (เธเนเธญเธเธฅเธ)")
                .HasColumnName("sale_amount");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PENDING_PAYMENT'")
                .HasComment("เธฃเธญเธเธณเธฃเธฐ/เธฃเธญเธเธฃเธงเธเธชเธฅเธดเธ/เธขเธทเธเธขเธฑเธเนเธฅเนเธง/เนเธเนเธฒเนเธเนเธเธฒเธ/เนเธชเธฃเนเธเธชเธดเนเธ/เธขเธเนเธฅเธดเธ/เธชเธฅเธดเธเนเธกเนเธเนเธฒเธ/เนเธกเนเธกเธฒ/เธซเธกเธเนเธงเธฅเธฒ")
                .HasColumnType("enum('PENDING_PAYMENT','PENDING_VERIFY','CONFIRMED','CHECKED_IN','COMPLETED','CANCELLED','REJECTED','NO_SHOW','EXPIRED')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasComment("FK -> users.user_id เธฅเธนเธเธเนเธฒ")
                .HasColumnName("user_id");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.BookingCheckedInByNavigations)
                .HasForeignKey(d => d.CheckedInBy)
                .HasConstraintName("fk_booking_cin");

            entity.HasOne(d => d.CheckedOutByNavigation).WithMany(p => p.BookingCheckedOutByNavigations)
                .HasForeignKey(d => d.CheckedOutBy)
                .HasConstraintName("fk_booking_cout");

            entity.HasOne(d => d.User).WithMany(p => p.BookingUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_booking_user");
        });

        modelBuilder.Entity<BookingItem>(entity =>
        {
            entity.HasKey(e => e.BookingItemId).HasName("PRIMARY");

            entity.ToTable("booking_items", tb => tb.HasComment("เธฃเธฒเธขเธเธฒเธฃเธชเธดเธเธเนเธฒเธเธทเนเธญ/เนเธเนเธฒเนเธเธเธดเธฅ"));

            entity.HasIndex(e => e.HandedOverBy, "fk_bi_handed");

            entity.HasIndex(e => e.ReturnedBy, "fk_bi_return");

            entity.HasIndex(e => e.BookingId, "idx_bi_booking");

            entity.HasIndex(e => e.ProductId, "idx_bi_product");

            entity.Property(e => e.BookingItemId)
                .HasComment("PK")
                .HasColumnName("booking_item_id");
            entity.Property(e => e.BookingId)
                .HasComment("FK -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasComment("เธชเนเธงเธเธฅเธเธเธญเธเธฃเธฒเธขเธเธฒเธฃเธเธตเน (เธฃเธงเธกเธเธฃเธเธตเธเธญเธเธเธฃเธต)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.HandedOverAt)
                .HasComment("เนเธงเธฅเธฒเธชเนเธเธกเธญเธ")
                .HasColumnType("datetime")
                .HasColumnName("handed_over_at");
            entity.Property(e => e.HandedOverBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธทเนเธญ-เนเธเนเธฒ)")
                .HasColumnName("handed_over_by");
            entity.Property(e => e.ItemStatus)
                .HasDefaultValueSql("'RESERVED'")
                .HasComment("เธเธญเธ/เธชเนเธเธกเธญเธเนเธฅเนเธง/เธเธทเธเนเธฅเนเธง/เนเธชเธตเธขเธซเธฒเธข/เธชเธนเธเธซเธฒเธข/เธขเธเนเธฅเธดเธ")
                .HasColumnType("enum('RESERVED','HANDED_OVER','RETURNED','DAMAGED','LOST','CANCELLED')")
                .HasColumnName("item_status");
            entity.Property(e => e.ItemType)
                .HasComment("เธเธทเนเธญ/เนเธเนเธฒ")
                .HasColumnType("enum('SALE','RENT')")
                .HasColumnName("item_type");
            entity.Property(e => e.LineTotal)
                .HasPrecision(10, 2)
                .HasComment("เธขเธญเธเธฃเธฒเธขเธเธฒเธฃเธซเธฅเธฑเธเธซเธฑเธเธชเนเธงเธเธฅเธ")
                .HasColumnName("line_total");
            entity.Property(e => e.ProductId)
                .HasComment("FK -> products.product_id")
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasComment("เธเธณเธเธงเธ")
                .HasColumnName("quantity");
            entity.Property(e => e.ReturnedAt)
                .HasComment("เนเธงเธฅเธฒเธฃเธฑเธเธเธทเธ (เนเธเธเธฒเธฐเนเธเนเธฒ)")
                .HasColumnType("datetime")
                .HasColumnName("returned_at");
            entity.Property(e => e.ReturnedBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธทเนเธญ-เนเธเนเธฒ)")
                .HasColumnName("returned_by");
            entity.Property(e => e.UnitPrice)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธฒเธเธฒเธเนเธญเธซเธเนเธงเธข เธ เธเธญเธเธชเธฑเนเธ (snapshot)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingItems)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bi_booking");

            entity.HasOne(d => d.HandedOverByNavigation).WithMany(p => p.BookingItemHandedOverByNavigations)
                .HasForeignKey(d => d.HandedOverBy)
                .HasConstraintName("fk_bi_handed");

            entity.HasOne(d => d.Product).WithMany(p => p.BookingItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bi_product");

            entity.HasOne(d => d.ReturnedByNavigation).WithMany(p => p.BookingItemReturnedByNavigations)
                .HasForeignKey(d => d.ReturnedBy)
                .HasConstraintName("fk_bi_return");
        });

        modelBuilder.Entity<BookingPromotion>(entity =>
        {
            entity.HasKey(e => e.BookingPromotionId).HasName("PRIMARY");

            entity.ToTable("booking_promotions", tb => tb.HasComment("เนเธเธฃเนเธกเธเธฑเนเธเธเธตเนเธเธนเธเนเธเนเธเธฃเธดเธเนเธเนเธเนเธฅเธฐเธเธดเธฅ"));

            entity.HasIndex(e => e.PromotionId, "fk_bp_promo");

            entity.HasIndex(e => new { e.BookingId, e.PromotionId }, "uq_bp").IsUnique();

            entity.Property(e => e.BookingPromotionId)
                .HasComment("PK")
                .HasColumnName("booking_promotion_id");
            entity.Property(e => e.BookingId)
                .HasComment("FK -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.Detail)
                .HasMaxLength(255)
                .HasComment("เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธตเนเนเธชเธเธเธฅเธนเธเธเนเธฒ (snapshot)")
                .HasColumnName("detail");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasComment("เธชเนเธงเธเธฅเธเธเธตเนเนเธเนเธเธฒเธเนเธเธฃเธฏ เธเธตเนเนเธเธเธดเธฅเธเธตเน")
                .HasColumnName("discount_amount");
            entity.Property(e => e.PromotionId)
                .HasComment("FK -> promotions.promotion_id")
                .HasColumnName("promotion_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingPromotions)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bp_booking");

            entity.HasOne(d => d.Promotion).WithMany(p => p.BookingPromotions)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bp_promo");
        });

        modelBuilder.Entity<BookingSlot>(entity =>
        {
            entity.HasKey(e => e.BookingSlotId).HasName("PRIMARY");

            entity.ToTable("booking_slots", tb => tb.HasComment("เธเธญเธฃเนเธ/เธงเธฑเธ/เธเนเธงเธเนเธงเธฅเธฒเธเธตเนเธเธนเธเธเธญเธ (เนเธเนเนเธชเธเธเธชเธตเธงเนเธฒเธ-เนเธกเนเธงเนเธฒเธ)"));

            entity.HasIndex(e => e.SlotId, "fk_bs_slot");

            entity.HasIndex(e => e.BookingId, "idx_bs_booking");

            entity.HasIndex(e => new { e.CourtId, e.PlayDate, e.SlotId, e.LockKey }, "uq_no_double_booking").IsUnique();

            entity.Property(e => e.BookingSlotId)
                .HasComment("PK")
                .HasColumnName("booking_slot_id");
            entity.Property(e => e.BookingId)
                .HasComment("FK -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.CourtId)
                .HasComment("FK -> courts.court_id")
                .HasColumnName("court_id");
            entity.Property(e => e.DiscountAmount)
                .HasPrecision(10, 2)
                .HasComment("เธชเนเธงเธเธฅเธเธเธญเธเธเนเธงเธเนเธงเธฅเธฒเธเธตเน (เนเธเนเธ เนเธเธฃเธฏ 1)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.LockKey)
                .HasDefaultValueSql("'1'")
                .HasComment("1=เธเธณเธฅเธฑเธเธเธญเธเธญเธขเธนเน / NULL=เธเธฅเนเธญเธขเธเธทเธเนเธฅเนเธง (เนเธเนเธฃเนเธงเธก UNIQUE เธเธฑเธเธเธญเธเธเนเธณ)")
                .HasColumnName("lock_key");
            entity.Property(e => e.PlayDate)
                .HasComment("เธงเธฑเธเธเธตเนเนเธฅเนเธ")
                .HasColumnName("play_date");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธฒเธเธฒเธเธญเธฃเนเธ เธ เธเธญเธเธเธญเธ (snapshot)")
                .HasColumnName("price");
            entity.Property(e => e.SlotId)
                .HasComment("FK -> time_slots.slot_id")
                .HasColumnName("slot_id");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingSlots)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bs_booking");

            entity.HasOne(d => d.Court).WithMany(p => p.BookingSlots)
                .HasForeignKey(d => d.CourtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bs_court");

            entity.HasOne(d => d.Slot).WithMany(p => p.BookingSlots)
                .HasForeignKey(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bs_slot");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PRIMARY");

            entity.ToTable("carts", tb => tb.HasComment("เธเธฐเธเธฃเนเธฒเธชเธดเธเธเนเธฒ/เธเธญเธฃเนเธเธเธญเธเธฅเธนเธเธเนเธฒ"));

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_cart_user");

            entity.Property(e => e.CartId)
                .HasComment("PK")
                .HasColumnName("cart_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVE'")
                .HasComment("เธชเธเธฒเธเธฐเธเธฐเธเธฃเนเธฒ")
                .HasColumnType("enum('ACTIVE','CHECKED_OUT','ABANDONED')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId)
                .HasComment("FK -> users.user_id เนเธเนเธฒเธเธญเธเธเธฐเธเธฃเนเธฒ")
                .HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cart_user");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PRIMARY");

            entity.ToTable("cart_items", tb => tb.HasComment("เธฃเธฒเธขเธเธฒเธฃเนเธเธเธฐเธเธฃเนเธฒ"));

            entity.HasIndex(e => e.CourtId, "fk_ci_court");

            entity.HasIndex(e => e.ProductId, "fk_ci_product");

            entity.HasIndex(e => e.SlotId, "fk_ci_slot");

            entity.HasIndex(e => e.CartId, "idx_ci_cart");

            entity.Property(e => e.CartItemId)
                .HasComment("PK")
                .HasColumnName("cart_item_id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธงเธฅเธฒเธเธตเนเนเธชเนเธเธฐเธเธฃเนเธฒ")
                .HasColumnType("datetime")
                .HasColumnName("added_at");
            entity.Property(e => e.CartId)
                .HasComment("FK -> carts.cart_id")
                .HasColumnName("cart_id");
            entity.Property(e => e.CourtId)
                .HasComment("FK -> courts (เนเธเนเนเธกเธทเนเธญ item_type=COURT_SLOT)")
                .HasColumnName("court_id");
            entity.Property(e => e.ItemType)
                .HasComment("เธเธเธดเธเธฃเธฒเธขเธเธฒเธฃ: เธเธญเธฃเนเธ / เธเธทเนเธญ / เนเธเนเธฒ")
                .HasColumnType("enum('COURT_SLOT','SALE','RENT')")
                .HasColumnName("item_type");
            entity.Property(e => e.PlayDate)
                .HasComment("เธงเธฑเธเธเธตเนเนเธฅเนเธ (เนเธเนเนเธกเธทเนเธญ item_type=COURT_SLOT)")
                .HasColumnName("play_date");
            entity.Property(e => e.ProductId)
                .HasComment("FK -> products (เนเธเนเนเธกเธทเนเธญ item_type=SALE/RENT)")
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValueSql("'1'")
                .HasComment("เธเธณเธเธงเธ")
                .HasColumnName("quantity");
            entity.Property(e => e.SlotId)
                .HasComment("FK -> time_slots (เนเธเนเนเธกเธทเนเธญ item_type=COURT_SLOT)")
                .HasColumnName("slot_id");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("fk_ci_cart");

            entity.HasOne(d => d.Court).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CourtId)
                .HasConstraintName("fk_ci_court");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("fk_ci_product");

            entity.HasOne(d => d.Slot).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("fk_ci_slot");
        });

        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasKey(e => e.CourtId).HasName("PRIMARY");

            entity.ToTable("courts", tb => tb.HasComment("เธเนเธญเธกเธนเธฅเธเธญเธฃเนเธเนเธเธเธกเธดเธเธเธฑเธ"));

            entity.HasIndex(e => e.CourtName, "uq_courts_name").IsUnique();

            entity.Property(e => e.CourtId)
                .HasComment("PK เธฃเธซเธฑเธชเธเธญเธฃเนเธ")
                .HasColumnName("court_id");
            entity.Property(e => e.CourtName)
                .HasMaxLength(50)
                .HasComment("เธเธทเนเธญเธเธญเธฃเนเธ เนเธเนเธ Court 1")
                .HasColumnName("court_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasComment("เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธ/เธเธทเนเธเธชเธเธฒเธก")
                .HasColumnName("description");
            entity.Property(e => e.PricePerHour)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธฒเธเธฒเธเนเธฒเนเธเนเธฒเธเธญเธฃเนเธเธเนเธญเธเธฑเนเธงเนเธกเธ (เธเธฒเธ)")
                .HasColumnName("price_per_hour");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'AVAILABLE'")
                .HasComment("เธชเธเธฒเธเธฐเธเธญเธฃเนเธ")
                .HasColumnType("enum('AVAILABLE','MAINTENANCE','INACTIVE')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<CourtBlock>(entity =>
        {
            entity.HasKey(e => e.BlockId).HasName("PRIMARY");

            entity.ToTable("court_blocks", tb => tb.HasComment("เธเธฒเธฃเธฅเนเธญเธเธเธญเธฃเนเธเธเธฑเนเธงเธเธฃเธฒเธง"));

            entity.HasIndex(e => e.CreatedBy, "fk_block_user");

            entity.HasIndex(e => new { e.CourtId, e.BlockStart, e.BlockEnd }, "idx_block_court_time");

            entity.Property(e => e.BlockId)
                .HasComment("PK")
                .HasColumnName("block_id");
            entity.Property(e => e.BlockEnd)
                .HasComment("เธชเธดเนเธเธชเธธเธเธเธฒเธฃเธฅเนเธญเธ")
                .HasColumnType("datetime")
                .HasColumnName("block_end");
            entity.Property(e => e.BlockStart)
                .HasComment("เนเธฃเธดเนเธกเธฅเนเธญเธ")
                .HasColumnType("datetime")
                .HasColumnName("block_start");
            entity.Property(e => e.CourtId)
                .HasComment("FK -> courts.court_id")
                .HasColumnName("court_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธเธฑเธเธเธถเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK -> users.user_id (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธเธเธตเนเธฅเนเธญเธ)")
                .HasColumnName("created_by");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasComment("เนเธซเธเธธเธเธฅ เนเธเนเธ เธชเธเธฒเธกเธเธฑเธ/เธเนเธญเธกเธเธณเธฃเธธเธ")
                .HasColumnName("reason");

            entity.HasOne(d => d.Court).WithMany(p => p.CourtBlocks)
                .HasForeignKey(d => d.CourtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_block_court");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.CourtBlocks)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_block_user");
        });

        modelBuilder.Entity<CourtIssue>(entity =>
        {
            entity.HasKey(e => e.IssueId).HasName("PRIMARY");

            entity.ToTable("court_issues", tb => tb.HasComment("เธเธฒเธฃเนเธเนเธเธญเธธเธเธเธฃเธเน/เธชเธเธฒเธกเธเธณเธฃเธธเธเธเธถเธ Admin"));

            entity.HasIndex(e => e.CourtId, "fk_issue_court");

            entity.HasIndex(e => e.ReportedBy, "fk_issue_reporter");

            entity.HasIndex(e => e.ResolvedBy, "fk_issue_resolver");

            entity.HasIndex(e => e.Status, "idx_issue_status");

            entity.Property(e => e.IssueId)
                .HasComment("PK")
                .HasColumnName("issue_id");
            entity.Property(e => e.CourtId)
                .HasComment("FK -> courts.court_id (เธเนเธฒเนเธเธตเนเธขเธงเธเธฑเธเธเธญเธฃเนเธเนเธเธเธญเธฃเนเธเธซเธเธถเนเธ)")
                .HasColumnName("court_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธฑเธเธซเธฒ/เธญเธธเธเธเธฃเธเนเธเธณเธฃเธธเธ")
                .HasColumnName("description");
            entity.Property(e => e.ReportedBy)
                .HasComment("FK -> users.user_id เธเธนเนเนเธเนเธ")
                .HasColumnName("reported_by");
            entity.Property(e => e.ResolvedAt)
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเนเธเนเธชเธฃเนเธ")
                .HasColumnType("datetime")
                .HasColumnName("resolved_at");
            entity.Property(e => e.ResolvedBy)
                .HasComment("FK -> users.user_id (Admin เธเธนเนเธเธดเธเนเธฃเธทเนเธญเธ)")
                .HasColumnName("resolved_by");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'OPEN'")
                .HasComment("เธชเธเธฒเธเธฐเธเธฒเธฃเนเธเนเนเธ")
                .HasColumnType("enum('OPEN','IN_PROGRESS','RESOLVED')")
                .HasColumnName("status");

            entity.HasOne(d => d.Court).WithMany(p => p.CourtIssues)
                .HasForeignKey(d => d.CourtId)
                .HasConstraintName("fk_issue_court");

            entity.HasOne(d => d.ReportedByNavigation).WithMany(p => p.CourtIssueReportedByNavigations)
                .HasForeignKey(d => d.ReportedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_issue_reporter");

            entity.HasOne(d => d.ResolvedByNavigation).WithMany(p => p.CourtIssueResolvedByNavigations)
                .HasForeignKey(d => d.ResolvedBy)
                .HasConstraintName("fk_issue_resolver");
        });

        modelBuilder.Entity<EquipmentDamage>(entity =>
        {
            entity.HasKey(e => e.DamageId).HasName("PRIMARY");

            entity.ToTable("equipment_damages", tb => tb.HasComment("เธเธงเธฒเธกเนเธชเธตเธขเธซเธฒเธขเธเธญเธเธญเธธเธเธเธฃเธเนเนเธเนเธฒเนเธฅเธฐเธเนเธฒเธเธฃเธฑเธ"));

            entity.HasIndex(e => e.BookingItemId, "fk_dmg_item");

            entity.HasIndex(e => e.ProcessedBy, "fk_dmg_proc");

            entity.HasIndex(e => e.ReportedBy, "fk_dmg_reporter");

            entity.HasIndex(e => e.Status, "idx_dmg_status");

            entity.Property(e => e.DamageId)
                .HasComment("PK")
                .HasColumnName("damage_id");
            entity.Property(e => e.BookingItemId)
                .HasComment("FK -> booking_items (เธญเธธเธเธเธฃเธเนเนเธเนเธฒเธเธตเนเนเธชเธตเธขเธซเธฒเธข)")
                .HasColumnName("booking_item_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธงเธฒเธกเนเธชเธตเธขเธซเธฒเธข เนเธเนเธ เนเธกเนเนเธเธเธซเธฑเธ")
                .HasColumnName("description");
            entity.Property(e => e.FineAmount)
                .HasPrecision(10, 2)
                .HasComment("เธเนเธฒเธเธฃเธฑเธเธเธตเนเธเนเธฒเธขเธเธฑเธเธเธตเธเธณเธซเธเธ")
                .HasColumnName("fine_amount");
            entity.Property(e => e.PhotoUrl)
                .HasMaxLength(255)
                .HasComment("เธฃเธนเธเธซเธฅเธฑเธเธเธฒเธ")
                .HasColumnName("photo_url");
            entity.Property(e => e.ProcessedAt)
                .HasComment("เธงเธฑเธเธเธตเนเธเธดเธเธเนเธฒเธเธฃเธฑเธ")
                .HasColumnType("datetime")
                .HasColumnName("processed_at");
            entity.Property(e => e.ProcessedBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธฑเธเธเธต)")
                .HasColumnName("processed_by");
            entity.Property(e => e.ReportedBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธทเนเธญ-เนเธเนเธฒเธเธนเนเธเธฃเธงเธเธเธ)")
                .HasColumnName("reported_by");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'REPORTED'")
                .HasComment("เนเธเนเธเนเธฅเนเธง/เธเธดเธเธเนเธฒเธเธฃเธฑเธเนเธฅเนเธง/เธเธณเธฃเธฐเนเธฅเนเธง/เธขเธเนเธงเนเธ")
                .HasColumnType("enum('REPORTED','CHARGED','PAID','WAIVED')")
                .HasColumnName("status");

            entity.HasOne(d => d.BookingItem).WithMany(p => p.EquipmentDamages)
                .HasForeignKey(d => d.BookingItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_dmg_item");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.EquipmentDamageProcessedByNavigations)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("fk_dmg_proc");

            entity.HasOne(d => d.ReportedByNavigation).WithMany(p => p.EquipmentDamageReportedByNavigations)
                .HasForeignKey(d => d.ReportedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_dmg_reporter");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PRIMARY");

            entity.ToTable("payments", tb => tb.HasComment("เธเธฒเธฃเนเธเนเธเนเธญเธเธเธณเธฃเธฐเนเธเธดเธเนเธฅเธฐเธเธฅเธเธฃเธงเธเธชเธฅเธดเธ"));

            entity.HasIndex(e => e.VerifiedBy, "fk_pay_verifier");

            entity.HasIndex(e => e.BookingId, "idx_pay_booking");

            entity.HasIndex(e => e.Status, "idx_pay_status");

            entity.Property(e => e.PaymentId)
                .HasComment("PK")
                .HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasComment("เธขเธญเธเธเธตเนเธฅเธนเธเธเนเธฒเนเธเนเธเนเธญเธ")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId)
                .HasComment("FK -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธญเธฑเธเนเธซเธฅเธเธชเธฅเธดเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.RejectReason)
                .HasMaxLength(255)
                .HasComment("เนเธซเธเธธเธเธฅเธเธตเนเนเธกเนเธญเธเธธเธกเธฑเธเธด")
                .HasColumnName("reject_reason");
            entity.Property(e => e.SlipImageUrl)
                .HasMaxLength(255)
                .HasComment("เธเธตเนเธญเธขเธนเนเนเธเธฅเนเธชเธฅเธดเธ")
                .HasColumnName("slip_image_url");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PENDING'")
                .HasComment("เธฃเธญเธเธฃเธงเธ/เธญเธเธธเธกเธฑเธเธด/เนเธกเนเธญเธเธธเธกเธฑเธเธด")
                .HasColumnType("enum('PENDING','APPROVED','REJECTED')")
                .HasColumnName("status");
            entity.Property(e => e.TransferAt)
                .HasComment("เธงเธฑเธ-เนเธงเธฅเธฒเธเธตเนเนเธญเธ")
                .HasColumnType("datetime")
                .HasColumnName("transfer_at");
            entity.Property(e => e.VerifiedAt)
                .HasComment("เนเธงเธฅเธฒเธเธตเนเธเธฃเธงเธ")
                .HasColumnType("datetime")
                .HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธฑเธเธเธตเธเธนเนเธเธฃเธงเธ)")
                .HasColumnName("verified_by");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pay_booking");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.VerifiedBy)
                .HasConstraintName("fk_pay_verifier");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("PRIMARY");

            entity.ToTable("permissions", tb => tb.HasComment("เธฃเธฒเธขเธเธฒเธฃเธชเธดเธเธเธดเนเธเธฒเธฃเนเธเนเธเธฒเธเธขเนเธญเธข"));

            entity.HasIndex(e => e.PermissionCode, "uq_permissions_code").IsUnique();

            entity.Property(e => e.PermissionId)
                .HasComment("PK เธฃเธซเธฑเธชเธชเธดเธเธเธดเน")
                .HasColumnName("permission_id");
            entity.Property(e => e.PermissionCode)
                .HasMaxLength(50)
                .HasComment("เธฃเธซเธฑเธชเธชเธดเธเธเธดเน เนเธเนเธ PAYMENT_VERIFY")
                .HasColumnName("permission_code");
            entity.Property(e => e.PermissionName)
                .HasMaxLength(150)
                .HasComment("เธเธทเนเธญเธชเธดเธเธเธดเน (เนเธชเธเธเธเธฅ)")
                .HasColumnName("permission_name");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PRIMARY");

            entity.ToTable("products", tb => tb.HasComment("เธชเธดเธเธเนเธฒเธเธฒเธข เนเธฅเธฐ เธชเธดเธเธเนเธฒเนเธเนเธฒ"));

            entity.HasIndex(e => e.CategoryId, "idx_products_cat");

            entity.HasIndex(e => e.Sku, "uq_products_sku").IsUnique();

            entity.Property(e => e.ProductId)
                .HasComment("PK")
                .HasColumnName("product_id");
            entity.Property(e => e.CategoryId)
                .HasComment("FK -> product_categories.category_id")
                .HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Deposit)
                .HasPrecision(10, 2)
                .HasComment("เธเนเธฒเธกเธฑเธเธเธณ (เนเธเธเธฒเธฐเธชเธดเธเธเนเธฒเนเธเนเธฒ, เธเนเธฒเธกเธต)")
                .HasColumnName("deposit");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasComment("เธเธตเนเธญเธขเธนเนเธฃเธนเธเธชเธดเธเธเนเธฒ")
                .HasColumnName("image_url");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1=เนเธชเธเธเธเธฒเธข/เนเธเนเธฒ")
                .HasColumnName("is_active");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasComment("เธฃเธฒเธเธฒเธเธฒเธข เธซเธฃเธทเธญ เธฃเธฒเธเธฒเนเธเนเธฒเธเนเธญเธเธฃเธฑเนเธ (เธเธฒเธ)")
                .HasColumnName("price");
            entity.Property(e => e.ProductName)
                .HasMaxLength(150)
                .HasComment("เธเธทเนเธญเธชเธดเธเธเนเธฒ/เธฃเธธเนเธ")
                .HasColumnName("product_name");
            entity.Property(e => e.ProductType)
                .HasComment("เธเธฃเธฐเนเธ�เธ: เธเธฒเธข/เนเธเนเธฒ (เธเนเธญเธเธเธฃเธเธเธฑเธเธซเธกเธงเธ)")
                .HasColumnType("enum('SALE','RENT')")
                .HasColumnName("product_type");
            entity.Property(e => e.Sku)
                .HasMaxLength(40)
                .HasComment("เธฃเธซเธฑเธชเธชเธดเธเธเนเธฒ")
                .HasColumnName("sku");
            entity.Property(e => e.StockQty)
                .HasComment("เธเธณเธเธงเธเธเธเนเธซเธฅเธทเธญ (เธเธฒเธข) / เธเธณเธเธงเธเธเธตเนเธกเธตเนเธซเนเนเธเนเธฒเธเธฑเนเธเธซเธกเธ (เนเธเนเธฒ)")
                .HasColumnName("stock_qty");
            entity.Property(e => e.Unit)
                .HasMaxLength(20)
                .HasDefaultValueSql("'เธเธดเนเธ'")
                .HasComment("เธซเธเนเธงเธข เนเธเนเธ เธซเธฅเธญเธ, เธเธงเธ, เนเธกเน, เธเธนเน")
                .HasColumnName("unit");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_products_cat");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.ToTable("product_categories", tb => tb.HasComment("เธซเธกเธงเธเธซเธกเธนเนเธชเธดเธเธเนเธฒ"));

            entity.HasIndex(e => e.CategoryCode, "uq_category_code").IsUnique();

            entity.Property(e => e.CategoryId)
                .HasComment("PK")
                .HasColumnName("category_id");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(30)
                .HasComment("เธฃเธซเธฑเธชเธซเธกเธงเธ: SHUTTLECOCK, RACKET, SHOES, BEVERAGE, GRIP (เนเธเนเธญเนเธฒเธเธญเธดเธเนเธเนเธเธฃเนเธกเธเธฑเนเธ)")
                .HasColumnName("category_code");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasComment("เธเธทเนเธญเธซเธกเธงเธ")
                .HasColumnName("category_name");
            entity.Property(e => e.ProductType)
                .HasComment("SALE=เธชเธดเธเธเนเธฒเธเธฒเธข, RENT=เธชเธดเธเธเนเธฒเนเธเนเธฒ")
                .HasColumnType("enum('SALE','RENT')")
                .HasColumnName("product_type");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PRIMARY");

            entity.ToTable("promotions", tb => tb.HasComment("เนเธเธกเนเธเธเนเธเธฃเนเธกเธเธฑเนเธ (Admin เนเธเธดเธ-เธเธดเธ/เนเธเนเนเธเนเธเน)"));

            entity.HasIndex(e => e.CreatedBy, "fk_promo_user");

            entity.HasIndex(e => new { e.IsActive, e.StartDate, e.EndDate }, "idx_promo_active");

            entity.HasIndex(e => e.PromoCode, "uq_promo_code").IsUnique();

            entity.Property(e => e.PromotionId)
                .HasComment("PK")
                .HasColumnName("promotion_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธชเธฃเนเธฒเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK -> users (Admin)")
                .HasColumnName("created_by");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasComment("เธฃเธฒเธขเธฅเธฐเนเธญเธตเธขเธเธเธตเนเนเธชเธเธเธซเธเนเธฒ Home/Checkout")
                .HasColumnName("description");
            entity.Property(e => e.DiscountType)
                .HasComment("เธเธเธดเธเธชเนเธงเธเธฅเธ: เนเธเธญเธฃเนเนเธเนเธเธเน / เธเธณเธเธงเธเนเธเธดเธ / เธเธญเธเนเธเธกเธเธฃเธต")
                .HasColumnType("enum('PERCENT','FIXED_AMOUNT','FREE_ITEM')")
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasPrecision(10, 2)
                .HasComment("เธเนเธฒเธชเนเธงเธเธฅเธ (เนเธเนเธ 20 = 20% เธซเธฃเธทเธญ 100 เธเธฒเธ / เธเธณเธเธงเธเธเธดเนเธเธเธตเนเธเธฃเธต)")
                .HasColumnName("discount_value");
            entity.Property(e => e.EndDate)
                .HasComment("เธงเธฑเธเธชเธดเนเธเธชเธธเธเนเธเธกเนเธเธ")
                .HasColumnName("end_date");
            entity.Property(e => e.FirstBookingOnly)
                .HasComment("1=เนเธเธเธฒเธฐเธชเธกเธฒเธเธดเธเนเธซเธกเนเธเธตเนเธเธญเธเธเธฃเธฑเนเธเนเธฃเธ")
                .HasColumnName("first_booking_only");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1=เนเธเธดเธเนเธเนเธเธฒเธ")
                .HasColumnName("is_active");
            entity.Property(e => e.IsStackable)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1=เนเธเนเธฃเนเธงเธกเธเธฑเธเนเธเธฃเธฏ เธญเธทเนเธเนเธเน")
                .HasColumnName("is_stackable");
            entity.Property(e => e.MinBillAmount)
                .HasPrecision(10, 2)
                .HasComment("เธขเธญเธเธเธดเธฅเธเธฑเนเธเธเนเธณ (เนเธเธฃเธฏ 5)")
                .HasColumnName("min_bill_amount");
            entity.Property(e => e.Priority)
                .HasComment("เธฅเธณเธเธฑเธเธเธฒเธฃเธเธณเธเธงเธ (เธเนเธญเธขเธเนเธญเธ)")
                .HasColumnName("priority");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(30)
                .HasComment("เธฃเธซเธฑเธชเนเธเธฃเนเธกเธเธฑเนเธ")
                .HasColumnName("promo_code");
            entity.Property(e => e.PromoName)
                .HasMaxLength(150)
                .HasComment("เธเธทเนเธญเนเธเธฃเนเธกเธเธฑเนเธ")
                .HasColumnName("promo_name");
            entity.Property(e => e.PromoType)
                .HasComment("เธฃเธนเธเนเธเธเนเธเธฃเธฏ 5 เนเธเธ")
                .HasColumnType("enum('TIME_BASED','VOLUME_SALE','BUNDLE_RENTAL','CROSS_SELL','TOTAL_BILL')")
                .HasColumnName("promo_type");
            entity.Property(e => e.StartDate)
                .HasComment("เธงเธฑเธเนเธฃเธดเนเธกเนเธเธกเนเธเธ")
                .HasColumnName("start_date");
            entity.Property(e => e.TimeEnd)
                .HasComment("เธเนเธงเธเนเธงเธฅเธฒเธชเธดเนเธเธชเธธเธเธเธญเธเนเธเธฃเธฏ (เนเธเธฃเธฏ 1)")
                .HasColumnType("time")
                .HasColumnName("time_end");
            entity.Property(e => e.TimeStart)
                .HasComment("เธเนเธงเธเนเธงเธฅเธฒเนเธฃเธดเนเธกเธเธญเธเนเธเธฃเธฏ (เนเธเธฃเธฏ 1)")
                .HasColumnType("time")
                .HasColumnName("time_start");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("fk_promo_user");
        });

        modelBuilder.Entity<PromotionRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PRIMARY");

            entity.ToTable("promotion_rules", tb => tb.HasComment("เนเธเธทเนเธญเธเนเธเนเธฅเธฐเธฃเธฒเธเธงเธฑเธฅเธเธญเธเนเธเนเธฅเธฐเนเธเธฃเนเธกเธเธฑเนเธ"));

            entity.HasIndex(e => e.CategoryId, "fk_rule_cat");

            entity.HasIndex(e => e.PromotionId, "idx_rule_promo");

            entity.Property(e => e.RuleId)
                .HasComment("PK")
                .HasColumnName("rule_id");
            entity.Property(e => e.CategoryId)
                .HasComment("FK -> product_categories (เธซเธกเธงเธเธเธตเนเนเธเธตเนเธขเธงเธเนเธญเธ)")
                .HasColumnName("category_id");
            entity.Property(e => e.NthItem)
                .HasComment("เธฅเธเนเธเธเธฒเธฐเธเธดเนเธเธเธตเน N (เนเธเนเธ 2 = เธซเธฅเธญเธเธเธตเน 2)")
                .HasColumnName("nth_item");
            entity.Property(e => e.PromotionId)
                .HasComment("FK -> promotions.promotion_id")
                .HasColumnName("promotion_id");
            entity.Property(e => e.RequiredQty)
                .HasDefaultValueSql("'1'")
                .HasComment("เธเธณเธเธงเธเธเธฑเนเธเธเนเธณเธเธตเนเธเนเธญเธเธกเธต/เธเธณเธเธงเธเธเธตเนเนเธเนเธฃเธฑเธ")
                .HasColumnName("required_qty");
            entity.Property(e => e.RuleKind)
                .HasComment("CONDITION=เนเธเธทเนเธญเธเนเธเธเธตเนเธเนเธญเธเธกเธต, REWARD=เธชเธดเนเธเธเธตเนเนเธเนเธฃเธฑเธ/เธเธนเธเธฅเธเธฃเธฒเธเธฒ")
                .HasColumnType("enum('CONDITION','REWARD')")
                .HasColumnName("rule_kind");
            entity.Property(e => e.TargetType)
                .HasComment("เนเธเนเธฒเธซเธกเธฒเธข: เธเนเธฒเธเธญเธฃเนเธ / เธชเธดเธเธเนเธฒเธเธทเนเธญ / เธชเธดเธเธเนเธฒเนเธเนเธฒ / เธขเธญเธเธฃเธงเธกเธเธดเธฅ")
                .HasColumnType("enum('COURT_FEE','SALE_ITEM','RENT_ITEM','BILL_TOTAL')")
                .HasColumnName("target_type");

            entity.HasOne(d => d.Category).WithMany(p => p.PromotionRules)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("fk_rule_cat");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionRules)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("fk_rule_promo");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PRIMARY");

            entity.ToTable("receipts", tb => tb.HasComment("เนเธเนเธชเธฃเนเธเธฃเธฑเธเนเธเธดเธ / เนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต"));

            entity.HasIndex(e => e.BookingId, "fk_rc_booking");

            entity.HasIndex(e => e.PaymentId, "fk_rc_payment");

            entity.HasIndex(e => e.IssuedBy, "fk_rc_user");

            entity.HasIndex(e => e.ReceiptNo, "uq_receipt_no").IsUnique();

            entity.Property(e => e.ReceiptId)
                .HasComment("PK")
                .HasColumnName("receipt_id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasComment("เธเธตเนเธญเธขเธนเนเธญเธญเธเนเธญเธเธชเธฒเธฃ")
                .HasColumnName("address");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasComment("เธขเธญเธเธฃเธงเธก")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId)
                .HasComment("FK -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(150)
                .HasComment("เธเธทเนเธญเธเธตเนเธญเธญเธเนเธญเธเธชเธฒเธฃ")
                .HasColumnName("customer_name");
            entity.Property(e => e.DocType)
                .HasDefaultValueSql("'RECEIPT'")
                .HasComment("เนเธเนเธชเธฃเนเธเธฃเธฑเธเนเธเธดเธ / เนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต")
                .HasColumnType("enum('RECEIPT','TAX_INVOICE')")
                .HasColumnName("doc_type");
            entity.Property(e => e.IssuedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธญเธญเธ")
                .HasColumnType("datetime")
                .HasColumnName("issued_at");
            entity.Property(e => e.IssuedBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธฑเธเธเธต)")
                .HasColumnName("issued_by");
            entity.Property(e => e.PaymentId)
                .HasComment("FK -> payments.payment_id")
                .HasColumnName("payment_id");
            entity.Property(e => e.ReceiptNo)
                .HasMaxLength(30)
                .HasComment("เนเธฅเธเธเธตเนเนเธเนเธชเธฃเนเธ/เนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต")
                .HasColumnName("receipt_no");
            entity.Property(e => e.TaxId)
                .HasMaxLength(13)
                .HasComment("เนเธฅเธเธเธฃเธฐเธเธณเธเธฑเธงเธเธนเนเนเธชเธตเธขเธ�เธฒเธฉเธต (เธเนเธฒเธเธญเนเธเธเธณเธเธฑเธเธ�เธฒเธฉเธต)")
                .HasColumnName("tax_id");
            entity.Property(e => e.VatAmount)
                .HasPrecision(10, 2)
                .HasComment("เธ�เธฒเธฉเธตเธกเธนเธฅเธเนเธฒเนเธเธดเนเธก")
                .HasColumnName("vat_amount");

            entity.HasOne(d => d.Booking).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rc_booking");

            entity.HasOne(d => d.IssuedByNavigation).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.IssuedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rc_user");

            entity.HasOne(d => d.Payment).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rc_payment");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.RefundId).HasName("PRIMARY");

            entity.ToTable("refunds", tb => tb.HasComment("เธเธฒเธฃเธเธทเธเนเธเธดเธเธฅเธนเธเธเนเธฒ"));

            entity.HasIndex(e => e.PaymentId, "fk_rf_payment");

            entity.HasIndex(e => e.ProcessedBy, "fk_rf_user");

            entity.HasIndex(e => e.BookingId, "idx_refund_booking");

            entity.Property(e => e.RefundId)
                .HasComment("PK")
                .HasColumnName("refund_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasComment("เธขเธญเธเธเธทเธเนเธเธดเธ")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId)
                .HasComment("FK -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธเธญเธเธทเธเนเธเธดเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentId)
                .HasComment("FK -> payments.payment_id")
                .HasColumnName("payment_id");
            entity.Property(e => e.ProcessedAt)
                .HasComment("เธงเธฑเธเธเธตเนเธเธณเนเธเธดเธเธเธฒเธฃ")
                .HasColumnType("datetime")
                .HasColumnName("processed_at");
            entity.Property(e => e.ProcessedBy)
                .HasComment("FK -> users (เธเนเธฒเธขเธเธฑเธเธเธต)")
                .HasColumnName("processed_by");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasComment("เนเธซเธเธธเธเธฅ เนเธเนเธ เธฅเธนเธเธเนเธฒเธขเธเนเธฅเธดเธเธเธฒเธกเนเธเธทเนเธญเธเนเธ")
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'REQUESTED'")
                .HasComment("เธชเธเธฒเธเธฐเธเธฒเธฃเธเธทเธเนเธเธดเธ")
                .HasColumnType("enum('REQUESTED','APPROVED','REFUNDED','REJECTED')")
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rf_booking");

            entity.HasOne(d => d.Payment).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rf_payment");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("fk_rf_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");

            entity.ToTable("roles", tb => tb.HasComment("เธเธเธเธฒเธเธเธนเนเนเธเนเธเธฒเธ (5 เธเธเธเธฒเธ)"));

            entity.HasIndex(e => e.RoleCode, "uq_roles_code").IsUnique();

            entity.Property(e => e.RoleId)
                .ValueGeneratedOnAdd()
                .HasComment("PK เธฃเธซเธฑเธชเธเธเธเธฒเธ")
                .HasColumnName("role_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasComment("เธเธณเธญเธเธดเธเธฒเธขเธเธเธเธฒเธ")
                .HasColumnName("description");
            entity.Property(e => e.RoleCode)
                .HasMaxLength(20)
                .HasComment("เธฃเธซเธฑเธชเธเธเธเธฒเธ: CUSTOMER, COURT_STAFF, SALES_STAFF, ACCOUNTING, ADMIN")
                .HasColumnName("role_code");
            entity.Property(e => e.RoleName)
                .HasMaxLength(100)
                .HasComment("เธเธทเนเธญเธเธเธเธฒเธ (เนเธชเธเธเธเธฅ)")
                .HasColumnName("role_name");

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("fk_rp_perm"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("fk_rp_role"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("role_permissions", tb => tb.HasComment("เธชเธดเธเธเธดเนเธเธญเธเนเธเนเธฅเธฐเธเธเธเธฒเธ (Admin เธเธณเธซเธเธเนเธเน)"));
                        j.HasIndex(new[] { "PermissionId" }, "fk_rp_perm");
                        j.IndexerProperty<byte>("RoleId")
                            .HasComment("FK -> roles.role_id")
                            .HasColumnName("role_id");
                        j.IndexerProperty<ushort>("PermissionId")
                            .HasComment("FK -> permissions.permission_id")
                            .HasColumnName("permission_id");
                    });
        });

        modelBuilder.Entity<SiteSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PRIMARY");

            entity.ToTable("site_settings", tb => tb.HasComment("เธเนเธฒเธเธฑเนเธเธฃเธฐเธเธ (QR เธฃเธฑเธเนเธเธดเธ, เธเธเธฃเธฐเนเธเธตเธขเธเธซเธเนเธฒ Home, เธเนเธขเธเธฒเธขเธขเธเนเธฅเธดเธ)"));

            entity.HasIndex(e => e.UpdatedBy, "fk_set_user");

            entity.Property(e => e.SettingKey)
                .HasMaxLength(50)
                .HasComment("PK เธเธทเนเธญเธเนเธฒเธเธฑเนเธ เนเธเนเธ payment_qr_url, house_rules, cancel_free_hours")
                .HasColumnName("setting_key");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasComment("เธเธณเธญเธเธดเธเธฒเธข")
                .HasColumnName("description");
            entity.Property(e => e.SettingValue)
                .HasComment("เธเนเธฒเธเธตเนเธเธฑเนเธ")
                .HasColumnType("text")
                .HasColumnName("setting_value");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasComment("FK -> users (Admin)")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SiteSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("fk_set_user");
        });

        modelBuilder.Entity<StockLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("stock_logs", tb => tb.HasComment("เธเธฃเธฐเธงเธฑเธเธดเธเธฒเธฃเนเธเธฅเธทเนเธญเธเนเธซเธงเธชเธเนเธญเธ"));

            entity.HasIndex(e => e.CreatedBy, "fk_stock_user");

            entity.HasIndex(e => new { e.ProductId, e.CreatedAt }, "idx_stock_product");

            entity.Property(e => e.LogId)
                .HasComment("PK")
                .HasColumnName("log_id");
            entity.Property(e => e.BookingId)
                .HasComment("เธญเนเธฒเธเธญเธดเธเธเธดเธฅ (เธเนเธฒเธกเธต) -> bookings.booking_id")
                .HasColumnName("booking_id");
            entity.Property(e => e.ChangeQty)
                .HasComment("เธเธณเธเธงเธเธเธตเนเนเธเธฅเธตเนเธขเธ (+เนเธเธดเนเธก / -เธฅเธ)")
                .HasColumnName("change_qty");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธงเธฅเธฒเธเธตเนเธเธฑเธเธเธถเธ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("FK -> users.user_id เธเธนเนเธเธณเธฃเธฒเธขเธเธฒเธฃ")
                .HasColumnName("created_by");
            entity.Property(e => e.Note)
                .HasMaxLength(255)
                .HasComment("เธซเธกเธฒเธขเนเธซเธเธธ")
                .HasColumnName("note");
            entity.Property(e => e.ProductId)
                .HasComment("FK -> products.product_id")
                .HasColumnName("product_id");
            entity.Property(e => e.Reason)
                .HasComment("เธชเธฒเนเธซเธเธธเธเธฒเธฃเนเธเธฅเธตเนเธขเธเธชเธเนเธญเธ")
                .HasColumnType("enum('DAILY_UPDATE','SOLD','RENT_OUT','RENT_RETURN','DAMAGED','LOST','RESTOCK','ADJUST')")
                .HasColumnName("reason");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockLogs)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_stock_user");

            entity.HasOne(d => d.Product).WithMany(p => p.StockLogs)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_stock_product");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.SlotId).HasName("PRIMARY");

            entity.ToTable("time_slots", tb => tb.HasComment("เธเนเธงเธเนเธงเธฅเธฒเธเธตเนเนเธเธดเธเนเธซเนเธเธญเธ (เธฃเธฒเธขเธเธฑเนเธงเนเธกเธ)"));

            entity.HasIndex(e => new { e.StartTime, e.EndTime }, "uq_slot_time").IsUnique();

            entity.Property(e => e.SlotId)
                .ValueGeneratedOnAdd()
                .HasComment("PK เธฃเธซเธฑเธชเธเนเธงเธเนเธงเธฅเธฒ")
                .HasColumnName("slot_id");
            entity.Property(e => e.EndTime)
                .HasComment("เนเธงเธฅเธฒเธชเธดเนเธเธชเธธเธ")
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1=เนเธเธดเธเนเธซเนเธเธญเธ")
                .HasColumnName("is_active");
            entity.Property(e => e.StartTime)
                .HasComment("เนเธงเธฅเธฒเนเธฃเธดเนเธก")
                .HasColumnType("time")
                .HasColumnName("start_time");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users", tb => tb.HasComment("เธเธนเนเนเธเนเธเธฒเธเธเธธเธเธเธเธเธฒเธ"));

            entity.HasIndex(e => e.RoleId, "idx_users_role");

            entity.HasIndex(e => e.Email, "uq_users_email").IsUnique();

            entity.Property(e => e.UserId)
                .HasComment("PK เธฃเธซเธฑเธชเธเธนเนเนเธเน")
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเธชเธกเธฑเธเธฃ")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasComment("เธญเธตเนเธกเธฅเธชเธณเธซเธฃเธฑเธ Login (เนเธกเนเธเนเธณ)")
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasComment("เธเธทเนเธญ-เธเธฒเธกเธชเธเธธเธฅ")
                .HasColumnName("full_name");
            entity.Property(e => e.HasBooked)
                .HasComment("เนเธเธขเธเธญเธเธชเธณเนเธฃเนเธเนเธฅเนเธงเธซเธฃเธทเธญเธขเธฑเธ (0=เธชเธกเธฒเธเธดเธเนเธซเธกเน เนเธเนเนเธเนเธเนเธเธฃเธฏ เธขเธญเธเธฃเธงเธก)")
                .HasColumnName("has_booked");
            entity.Property(e => e.LastLoginAt)
                .HasComment("เนเธงเธฅเธฒเนเธเนเธฒเธชเธนเนเธฃเธฐเธเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("last_login_at");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("เธฃเธซเธฑเธชเธเนเธฒเธเธเธตเนเนเธเนเธฒเธฃเธซเธฑเธชเนเธฅเนเธง (bcrypt/argon2)")
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasComment("เนเธเธญเธฃเนเนเธเธฃเธจเธฑเธเธเน")
                .HasColumnName("phone");
            entity.Property(e => e.RoleId)
                .HasComment("FK -> roles.role_id")
                .HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVE'")
                .HasComment("เธชเธเธฒเธเธฐเธเธฑเธเธเธต")
                .HasColumnType("enum('ACTIVE','INACTIVE','BANNED')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เธงเธฑเธเธเธตเนเนเธเนเนเธเธฅเนเธฒเธชเธธเธ")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_users_role");
        });

        modelBuilder.Entity<VDailySale>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_daily_sales");

            entity.Property(e => e.BookingsCount).HasColumnName("bookings_count");
            entity.Property(e => e.CourtRevenue)
                .HasPrecision(32, 2)
                .HasColumnName("court_revenue");
            entity.Property(e => e.NetRevenue)
                .HasPrecision(32, 2)
                .HasColumnName("net_revenue");
            entity.Property(e => e.ProductRevenue)
                .HasPrecision(32, 2)
                .HasColumnName("product_revenue");
            entity.Property(e => e.RentalRevenue)
                .HasPrecision(32, 2)
                .HasColumnName("rental_revenue");
            entity.Property(e => e.SalesDate).HasColumnName("sales_date");
            entity.Property(e => e.TotalDiscount)
                .HasPrecision(32, 2)
                .HasColumnName("total_discount");
        });

        modelBuilder.Entity<VPeakTime>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_peak_time");

            entity.Property(e => e.EndTime)
                .HasComment("เนเธงเธฅเธฒเธชเธดเนเธเธชเธธเธ")
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.StartTime)
                .HasComment("เนเธงเธฅเธฒเนเธฃเธดเนเธก")
                .HasColumnType("time")
                .HasColumnName("start_time");
            entity.Property(e => e.TotalBooked).HasColumnName("total_booked");
        });

        modelBuilder.Entity<WalkInQueue>(entity =>
        {
            entity.HasKey(e => e.QueueId).HasName("PRIMARY");

            entity.ToTable("walk_in_queues", tb => tb.HasComment("เธเธดเธงเธฅเธนเธเธเนเธฒ Walk-in"));

            entity.HasIndex(e => e.CourtId, "fk_walkin_court");

            entity.HasIndex(e => e.SlotId, "fk_walkin_slot");

            entity.HasIndex(e => e.HandledBy, "fk_walkin_user");

            entity.HasIndex(e => new { e.QueueDate, e.QueueNo }, "uq_walkin_no").IsUnique();

            entity.Property(e => e.QueueId)
                .HasComment("PK")
                .HasColumnName("queue_id");
            entity.Property(e => e.CourtId)
                .HasComment("FK -> courts.court_id (เธเธญเธฃเนเธเธเธตเนเธเธฑเธเนเธซเน)")
                .HasColumnName("court_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("เนเธงเธฅเธฒเนเธเนเธฒเธเธดเธง")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(150)
                .HasComment("เธเธทเนเธญเธฅเธนเธเธเนเธฒ Walk-in")
                .HasColumnName("customer_name");
            entity.Property(e => e.HandledBy)
                .HasComment("FK -> users.user_id (เธเนเธฒเธขเธเธนเนเธฅเธเธญเธฃเนเธ)")
                .HasColumnName("handled_by");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasComment("เนเธเธญเธฃเนเนเธเธฃ")
                .HasColumnName("phone");
            entity.Property(e => e.QueueDate)
                .HasComment("เธงเธฑเธเธเธตเน")
                .HasColumnName("queue_date");
            entity.Property(e => e.QueueNo)
                .HasComment("เธฅเธณเธเธฑเธเธเธดเธงเธเธญเธเธงเธฑเธ")
                .HasColumnName("queue_no");
            entity.Property(e => e.SlotId)
                .HasComment("FK -> time_slots.slot_id")
                .HasColumnName("slot_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'WAITING'")
                .HasComment("เธชเธเธฒเธเธฐเธเธดเธง")
                .HasColumnType("enum('WAITING','ASSIGNED','DONE','CANCELLED')")
                .HasColumnName("status");

            entity.HasOne(d => d.Court).WithMany(p => p.WalkInQueues)
                .HasForeignKey(d => d.CourtId)
                .HasConstraintName("fk_walkin_court");

            entity.HasOne(d => d.HandledByNavigation).WithMany(p => p.WalkInQueues)
                .HasForeignKey(d => d.HandledBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_walkin_user");

            entity.HasOne(d => d.Slot).WithMany(p => p.WalkInQueues)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("fk_walkin_slot");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
