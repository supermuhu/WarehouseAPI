using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WarehouseAPI.Data;

public partial class WarehouseApiContext : DbContext
{
    public WarehouseApiContext()
    {
    }

    public WarehouseApiContext(DbContextOptions<WarehouseApiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<InboundItem> InboundItems { get; set; }

    public virtual DbSet<InboundItemStackUnit> InboundItemStackUnits { get; set; }

    public virtual DbSet<InboundReceipt> InboundReceipts { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemAllocation> ItemAllocations { get; set; }

    public virtual DbSet<ItemLocationHistory> ItemLocationHistories { get; set; }

    public virtual DbSet<OutboundItem> OutboundItems { get; set; }

    public virtual DbSet<OutboundReceipt> OutboundReceipts { get; set; }

    public virtual DbSet<OutboundPalletPick> OutboundPalletPicks { get; set; }

    public virtual DbSet<Pallet> Pallets { get; set; }

    public virtual DbSet<PalletLocation> PalletLocations { get; set; }

    public virtual DbSet<PalletTemplate> PalletTemplates { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Rack> Racks { get; set; }

    public virtual DbSet<Shelf> Shelves { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseGate> WarehouseGates { get; set; }

    public virtual DbSet<WarehouseZone> WarehouseZones { get; set; }

    public virtual DbSet<ZoneLayoutConfig> ZoneLayoutConfigs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Warehouse");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__accounts__46A222CD7562CB50");

            entity.ToTable("accounts", tb => tb.HasTrigger("trg_accounts_updated_at"));

            entity.HasIndex(e => e.Username, "UQ__accounts__F3DBC572E1399352").IsUnique();

            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(200)
                .HasColumnName("full_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("username");
        });

        modelBuilder.Entity<InboundItem>(entity =>
        {
            entity.HasKey(e => e.InboundItemId).HasName("PK__inbound___9EF59407F37961F6");

            entity.ToTable("inbound_items");

            entity.Property(e => e.InboundItemId).HasColumnName("inbound_item_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.StackMode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("auto")
                .HasColumnName("stack_mode");

            entity.HasOne(d => d.Item).WithMany(p => p.InboundItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_items_item");

            entity.HasOne(d => d.Pallet).WithMany(p => p.InboundItems)
                .HasForeignKey(d => d.PalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_items_pallet");

            entity.HasOne(d => d.Receipt).WithMany(p => p.InboundItems)
                .HasForeignKey(d => d.ReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_items_receipt");
        });

        modelBuilder.Entity<InboundItemStackUnit>(entity =>
        {
            entity.HasKey(e => e.LayoutId).HasName("PK__inbound___255F1E9AF5BC5F10");

            entity.ToTable("inbound_item_stack_units");

            entity.HasIndex(e => e.InboundItemId, "IX_inbound_item_stack_units_inbound_item");

            entity.Property(e => e.LayoutId).HasColumnName("layout_id");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.InboundItemId).HasColumnName("inbound_item_id");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.LocalX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("local_x");
            entity.Property(e => e.LocalY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("local_y");
            entity.Property(e => e.LocalZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("local_z");
            entity.Property(e => e.RotationY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("rotation_y");
            entity.Property(e => e.UnitIndex).HasColumnName("unit_index");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");

            entity.HasOne(d => d.InboundItem).WithMany(p => p.InboundItemStackUnits)
                .HasForeignKey(d => d.InboundItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_item_stack_units_inbound_item");
        });

        modelBuilder.Entity<InboundReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__inbound___91F52C1FAEF78D6E");

            entity.ToTable("inbound_receipts");

            entity.HasIndex(e => e.CustomerId, "IX_inbound_customer");

            entity.HasIndex(e => e.InboundDate, "IX_inbound_date");

            entity.HasIndex(e => e.WarehouseId, "IX_inbound_warehouse");

            entity.HasIndex(e => e.ReceiptNumber, "UQ__inbound___89FE4B75250BBC8D").IsUnique();

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.AutoStackTemplate)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("auto_stack_template");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.InboundDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("inbound_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("receipt_number");
            entity.Property(e => e.StackMode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("auto")
                .HasColumnName("stack_mode");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TotalItems).HasColumnName("total_items");
            entity.Property(e => e.TotalPallets).HasColumnName("total_pallets");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InboundReceiptCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_created_by");

            entity.HasOne(d => d.Customer).WithMany(p => p.InboundReceiptCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_customer");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.InboundReceipts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_inbound_warehouse");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__items__52020FDD3CF3D9B7");

            entity.ToTable("items");

            entity.HasIndex(e => e.CustomerId, "IX_items_customer");

            entity.HasIndex(e => e.ProductId, "IX_items_product");

            entity.HasIndex(e => e.QrCode, "UQ__items__E2FB88894EAFA35B").IsUnique();

            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.BatchNumber)
                .HasMaxLength(100)
                .HasColumnName("batch_number");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.IsFragile).HasColumnName("is_fragile");
            entity.Property(e => e.IsHeavy).HasColumnName("is_heavy");
            entity.Property(e => e.IsNonStackable).HasColumnName("is_non_stackable");
            entity.Property(e => e.ItemName)
                .HasMaxLength(300)
                .HasColumnName("item_name");
            entity.Property(e => e.ItemType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("item_type");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.ManufacturingDate).HasColumnName("manufacturing_date");
            entity.Property(e => e.PriorityLevel)
                .HasDefaultValue(5)
                .HasColumnName("priority_level");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QrCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("qr_code");
            entity.Property(e => e.Shape)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("rectangle")
                .HasColumnName("shape");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("weight");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");

            entity.HasOne(d => d.Customer).WithMany(p => p.Items)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_items_customer");

            entity.HasOne(d => d.Product).WithMany(p => p.Items)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_items_product");
        });

        modelBuilder.Entity<ItemAllocation>(entity =>
        {
            entity.HasKey(e => e.AllocationId).HasName("PK__item_all__5DFAFF30C8A7A347");

            entity.ToTable("item_allocations");

            entity.HasIndex(e => e.PalletId, "IX_item_allocations_pallet");

            entity.Property(e => e.AllocationId).HasColumnName("allocation_id");
            entity.Property(e => e.AllocatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("allocated_at");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.PositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.PositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_z");

            entity.HasOne(d => d.Item).WithMany(p => p.ItemAllocations)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_item_allocations_item");

            entity.HasOne(d => d.Pallet).WithMany(p => p.ItemAllocations)
                .HasForeignKey(d => d.PalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_item_allocations_pallet");
        });

        modelBuilder.Entity<ItemLocationHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__item_loc__096AA2E9F359E640");

            entity.ToTable("item_location_history");

            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ActionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("action_date");
            entity.Property(e => e.ActionType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("action_type");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.PerformedBy).HasColumnName("performed_by");

            entity.HasOne(d => d.Item).WithMany(p => p.ItemLocationHistories)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_history_item");

            entity.HasOne(d => d.Location).WithMany(p => p.ItemLocationHistories)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_history_location");

            entity.HasOne(d => d.Pallet).WithMany(p => p.ItemLocationHistories)
                .HasForeignKey(d => d.PalletId)
                .HasConstraintName("FK_history_pallet");

            entity.HasOne(d => d.PerformedByNavigation).WithMany(p => p.ItemLocationHistories)
                .HasForeignKey(d => d.PerformedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_history_performed_by");
        });

        modelBuilder.Entity<ZoneLayoutConfig>(entity =>
        {
            entity.HasKey(e => e.ConfigId).HasName("PK_zone_layout_configs");

            entity.ToTable("zone_layout_configs");

            entity.Property(e => e.ConfigId).HasColumnName("config_id");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.BlockWidth).HasColumnType("decimal(10, 2)").HasColumnName("block_width");
            entity.Property(e => e.BlockDepth).HasColumnType("decimal(10, 2)").HasColumnName("block_depth");
            entity.Property(e => e.FirstBlockWidth).HasColumnType("decimal(10, 2)").HasColumnName("first_block_width");
            entity.Property(e => e.FirstBlockDepth).HasColumnType("decimal(10, 2)").HasColumnName("first_block_depth");
            entity.Property(e => e.HorizontalAisleWidth).HasColumnType("decimal(10, 2)").HasColumnName("horizontal_aisle_width");
            entity.Property(e => e.VerticalAisleWidth).HasColumnType("decimal(10, 2)").HasColumnName("vertical_aisle_width");
            entity.Property(e => e.StartOffsetX).HasColumnType("decimal(10, 2)").HasColumnName("start_offset_x");
            entity.Property(e => e.StartOffsetZ).HasColumnType("decimal(10, 2)").HasColumnName("start_offset_z");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Zone).WithMany(p => p.ZoneLayoutConfigs)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_zone_layout_configs_zone");
        });

        modelBuilder.Entity<OutboundItem>(entity =>
        {
            entity.HasKey(e => e.OutboundItemId).HasName("PK__outbound__3EEE316F6F6C59B8");

            entity.ToTable("outbound_items");

            entity.Property(e => e.OutboundItemId).HasColumnName("outbound_item_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.RemovedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("removed_at");

            entity.HasOne(d => d.Item).WithMany(p => p.OutboundItems)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_items_item");

            entity.HasOne(d => d.Receipt).WithMany(p => p.OutboundItems)
                .HasForeignKey(d => d.ReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_items_receipt");
        });

        modelBuilder.Entity<OutboundReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__outbound__91F52C1F1DE336F0");

            entity.ToTable("outbound_receipts");

            entity.HasIndex(e => e.ReceiptNumber, "UQ__outbound__89FE4B7555714852").IsUnique();

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OutboundDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("outbound_date");
            entity.Property(e => e.CompletedDate).HasColumnName("completed_date");
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("receipt_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.TotalItems).HasColumnName("total_items");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.OutboundReceiptCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_created_by");

            entity.HasOne(d => d.Customer).WithMany(p => p.OutboundReceiptCustomers)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_customer");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.OutboundReceipts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_warehouse");
        });

        modelBuilder.Entity<OutboundPalletPick>(entity =>
        {
            entity.HasKey(e => e.PickId).HasName("PK__outbound_pallet_picks");

            entity.ToTable("outbound_pallet_picks");

            entity.HasIndex(e => new { e.ReceiptId, e.PalletId }, "UX_outbound_pallet_picks_receipt_pallet")
                .IsUnique();

            entity.Property(e => e.PickId).HasColumnName("pick_id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.PickedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("picked_at");
            entity.Property(e => e.PickedBy).HasColumnName("picked_by");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(d => d.Receipt).WithMany(p => p.OutboundPalletPicks)
                .HasForeignKey(d => d.ReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_pallet_picks_receipt");

            entity.HasOne(d => d.Pallet).WithMany(p => p.OutboundPalletPicks)
                .HasForeignKey(d => d.PalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_pallet_picks_pallet");

            entity.HasOne(d => d.PickedByNavigation).WithMany(p => p.OutboundPalletPicks)
                .HasForeignKey(d => d.PickedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_outbound_pallet_picks_account");
        });

        modelBuilder.Entity<Pallet>(entity =>
        {
            entity.HasKey(e => e.PalletId).HasName("PK__pallets__99AF89598AEB4823");

            entity.ToTable("pallets");

            entity.HasIndex(e => e.Barcode, "UQ__pallets__C16E36F86E8C19BD").IsUnique();

            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.Barcode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("barcode");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Height)
                .HasDefaultValue(0.15m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.MaxStackHeight)
                .HasDefaultValue(1.5m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_stack_height");
            entity.Property(e => e.MaxWeight)
                .HasDefaultValue(1000m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_weight");
            entity.Property(e => e.PalletType)
                .HasMaxLength(50)
                .HasColumnName("pallet_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("available")
                .HasColumnName("status");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");
        });

        modelBuilder.Entity<PalletLocation>(entity =>
        {
            entity.HasKey(e => e.LocationId).HasName("PK__pallet_l__771831EAFBD835A2");

            entity.ToTable("pallet_locations");

            entity.HasIndex(e => e.ShelfId, "IX_pallet_locations_shelf");

            entity.HasIndex(e => e.ZoneId, "IX_pallet_locations_zone");

            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("assigned_at");
            entity.Property(e => e.IsGround).HasColumnName("is_ground");
            entity.Property(e => e.LocationCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComputedColumnSql("('LOC-'+right('000000'+CONVERT([varchar](6),[location_id]),(6)))", true)
                .HasColumnName("location_code");
            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.PositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.PositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_z");
            entity.Property(e => e.RotationY)
                .HasColumnType("decimal(10, 4)")
                .HasDefaultValue(0m)
                .HasColumnName("rotation_y");
            entity.Property(e => e.ShelfId).HasColumnName("shelf_id");
            entity.Property(e => e.StackLevel)
                .HasDefaultValue(1)
                .HasColumnName("stack_level");
            entity.Property(e => e.StackedOnPallet).HasColumnName("stacked_on_pallet");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");

            entity.HasOne(d => d.Pallet).WithMany(p => p.PalletLocationPallets)
                .HasForeignKey(d => d.PalletId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pallet_locations_pallet");

            entity.HasOne(d => d.Shelf).WithMany(p => p.PalletLocations)
                .HasForeignKey(d => d.ShelfId)
                .HasConstraintName("FK_pallet_locations_shelf");

            entity.HasOne(d => d.StackedOnPalletNavigation).WithMany(p => p.PalletLocationStackedOnPalletNavigations)
                .HasForeignKey(d => d.StackedOnPallet)
                .HasConstraintName("FK_pallet_locations_stacked");

            entity.HasOne(d => d.Zone).WithMany(p => p.PalletLocations)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_pallet_locations_zone");
        });

        modelBuilder.Entity<PalletTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId).HasName("PK__pallet_t__BE44E079852F501A");

            entity.ToTable("pallet_templates", tb => tb.HasTrigger("trg_pallet_templates_updated_at"));

            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.MaxStackHeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_stack_height");
            entity.Property(e => e.MaxWeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_weight");
            entity.Property(e => e.PalletType)
                .HasMaxLength(50)
                .HasColumnName("pallet_type");
            entity.Property(e => e.TemplateName)
                .HasMaxLength(200)
                .HasColumnName("template_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__products__47027DF597445F67");

            entity.ToTable("products", tb => tb.HasTrigger("trg_products_updated_at"));

            entity.HasIndex(e => e.Category, "IX_products_category");

            entity.HasIndex(e => e.ProductCode, "UQ__products__AE1A8CC418C1A66B").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreateUser).HasColumnName("create_user");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFragile).HasColumnName("is_fragile");
            entity.Property(e => e.IsHazardous).HasColumnName("is_hazardous");
            entity.Property(e => e.IsNonStackable).HasColumnName("is_non_stackable");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("product_code");
            entity.Property(e => e.ProductName)
                .HasMaxLength(300)
                .HasColumnName("product_name");
            entity.Property(e => e.StandardHeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("standard_height");
            entity.Property(e => e.StandardLength)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("standard_length");
            entity.Property(e => e.StandardWeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("standard_weight");
            entity.Property(e => e.StandardWidth)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("standard_width");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.StorageConditions).HasColumnName("storage_conditions");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreateUserNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.CreateUser)
                .HasConstraintName("FK_products_create_user");
        });

        modelBuilder.Entity<Rack>(entity =>
        {
            entity.HasKey(e => e.RackId).HasName("PK__racks__6E237EB3E10A3D40");

            entity.ToTable("racks");

            entity.HasIndex(e => e.ZoneId, "IX_racks_zone");

            entity.Property(e => e.RackId).HasColumnName("rack_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.MaxShelves)
                .HasDefaultValue(5)
                .HasColumnName("max_shelves");
            entity.Property(e => e.PositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.PositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_z");
            entity.Property(e => e.RotationY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("rotation_y");
            entity.Property(e => e.RackName)
                .HasMaxLength(200)
                .HasColumnName("rack_name");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");

            entity.HasOne(d => d.Zone).WithMany(p => p.Racks)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_racks_zone");
        });

        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.HasKey(e => e.ShelfId).HasName("PK__shelves__E33A5B7CA815306B");

            entity.ToTable("shelves");

            entity.HasIndex(e => e.RackId, "IX_shelves_rack");

            entity.HasIndex(e => new { e.RackId, e.ShelfLevel }, "UQ_shelf_level").IsUnique();

            entity.Property(e => e.ShelfId).HasColumnName("shelf_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.MaxWeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("max_weight");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.RackId).HasColumnName("rack_id");
            entity.Property(e => e.ShelfLevel).HasColumnName("shelf_level");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");

            entity.HasOne(d => d.Rack).WithMany(p => p.Shelves)
                .HasForeignKey(d => d.RackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_shelves_rack");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__warehous__734FE6BF1E954094");

            entity.ToTable("warehouses");

            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.AllowedItemTypes).HasColumnName("allowed_item_types");
            entity.Property(e => e.CheckinHeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_height");
            entity.Property(e => e.CheckinLength)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_length");
            entity.Property(e => e.CheckinPositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_position_x");
            entity.Property(e => e.CheckinPositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_position_y");
            entity.Property(e => e.CheckinPositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_position_z");
            entity.Property(e => e.CheckinWidth)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_width");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.WarehouseName)
                .HasMaxLength(200)
                .HasColumnName("warehouse_name");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.IsRentable)
                .HasColumnName("is_rentable")
                .HasDefaultValue(true);
            entity.Property(e => e.WarehouseType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("warehouse_type");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");

            entity.HasOne(d => d.Owner).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_warehouses_owner");
        });

        modelBuilder.Entity<WarehouseGate>(entity =>
        {
            entity.HasKey(e => e.GateId).HasName("PK__warehous__8CB119222B213D7A");

            entity.ToTable("warehouse_gates");

            entity.Property(e => e.GateId).HasColumnName("gate_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.GateName)
                .HasMaxLength(200)
                .HasColumnName("gate_name");
            entity.Property(e => e.GateType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("gate_type");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.RotationY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("rotation_y");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.PositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.PositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_z");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseGates)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_warehouse_gates_warehouse");
        });

        modelBuilder.Entity<WarehouseZone>(entity =>
        {
            entity.HasKey(e => e.ZoneId).HasName("PK__warehous__80B401DF06F07784");

            entity.ToTable("warehouse_zones");

            entity.HasIndex(e => e.CustomerId, "IX_zones_customer");

            entity.HasIndex(e => e.WarehouseId, "IX_zones_warehouse");

            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.PositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.PositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_z");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");
            entity.Property(e => e.ZoneName)
                .HasMaxLength(200)
                .HasColumnName("zone_name");
            entity.Property(e => e.ZoneType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("zone_type");

            entity.HasOne(d => d.Customer).WithMany(p => p.WarehouseZones)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_zones_customer");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseZones)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_zones_warehouse");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
