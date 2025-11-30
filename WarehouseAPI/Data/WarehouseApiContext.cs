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

    public virtual DbSet<InboundReceipt> InboundReceipts { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemAllocation> ItemAllocations { get; set; }

    public virtual DbSet<ItemLocationHistory> ItemLocationHistories { get; set; }

    public virtual DbSet<OutboundItem> OutboundItems { get; set; }

    public virtual DbSet<OutboundReceipt> OutboundReceipts { get; set; }

    public virtual DbSet<Pallet> Pallets { get; set; }

    public virtual DbSet<PalletLocation> PalletLocations { get; set; }

    public virtual DbSet<PalletTemplate> PalletTemplates { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Rack> Racks { get; set; }

    public virtual DbSet<Shelf> Shelves { get; set; }

    public virtual DbSet<VItemPrioritySearch> VItemPrioritySearches { get; set; }

    public virtual DbSet<VPalletInventory> VPalletInventories { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseZone> WarehouseZones { get; set; }

    public virtual DbSet<WarehouseGate> WarehouseGates { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:Warehouse");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__accounts__46A222CDE693C5CB");

            entity.ToTable("accounts", tb => tb.HasTrigger("trg_accounts_updated_at"));

            entity.HasIndex(e => e.Username, "UQ__accounts__F3DBC572E87AA4B2").IsUnique();

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
            entity.HasKey(e => e.InboundItemId).HasName("PK__inbound___9EF5940755AF0F77");

            entity.ToTable("inbound_items");

            entity.Property(e => e.InboundItemId).HasColumnName("inbound_item_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.PalletId).HasColumnName("pallet_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");

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

        modelBuilder.Entity<InboundReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK__inbound___91F52C1F21FAAFF4");

            entity.ToTable("inbound_receipts");

            entity.HasIndex(e => e.CustomerId, "IX_inbound_customer");

            entity.HasIndex(e => e.InboundDate, "IX_inbound_date");

            entity.HasIndex(e => e.WarehouseId, "IX_inbound_warehouse");

            entity.HasIndex(e => e.ReceiptNumber, "UQ__inbound___89FE4B755253F0FE").IsUnique();

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
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
            entity.HasKey(e => e.ItemId).HasName("PK__items__52020FDD9DE29772");

            entity.ToTable("items");

            entity.HasIndex(e => e.CustomerId, "IX_items_customer");

            entity.HasIndex(e => e.ProductId, "IX_items_product");

            entity.HasIndex(e => e.QrCode, "UQ__items__E2FB8889F47DF4F5").IsUnique();

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
            entity.Property(e => e.IsFragile)
                .HasDefaultValue(false)
                .HasColumnName("is_fragile");
            entity.Property(e => e.IsHeavy)
                .HasDefaultValue(false)
                .HasColumnName("is_heavy");
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
            entity.HasKey(e => e.AllocationId).HasName("PK__item_all__5DFAFF30405937CB");

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
            entity.HasKey(e => e.HistoryId).HasName("PK__item_loc__096AA2E9D37318DD");

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

        modelBuilder.Entity<OutboundItem>(entity =>
        {
            entity.HasKey(e => e.OutboundItemId).HasName("PK__outbound__3EEE316F0A40863E");

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
            entity.HasKey(e => e.ReceiptId).HasName("PK__outbound__91F52C1F8116E217");

            entity.ToTable("outbound_receipts");

            entity.HasIndex(e => e.CustomerId, "IX_outbound_customer");

            entity.HasIndex(e => e.WarehouseId, "IX_outbound_warehouse");

            entity.HasIndex(e => e.ReceiptNumber, "UQ__outbound__89FE4B75F97E06BD").IsUnique();

            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OutboundDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("outbound_date");
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

        modelBuilder.Entity<Pallet>(entity =>
        {
            entity.HasKey(e => e.PalletId).HasName("PK__pallets__99AF8959406046A8");

            entity.ToTable("pallets");

            entity.HasIndex(e => e.Barcode, "UQ__pallets__C16E36F869AA7C4F").IsUnique();

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
            entity.HasKey(e => e.LocationId).HasName("PK__pallet_l__771831EAA97ED087");

            entity.ToTable("pallet_locations");

            entity.HasIndex(e => e.ShelfId, "IX_pallet_locations_shelf");

            entity.HasIndex(e => e.ZoneId, "IX_pallet_locations_zone");

            entity.Property(e => e.LocationId).HasColumnName("location_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("assigned_at");
            entity.Property(e => e.IsGround)
                .HasDefaultValue(false)
                .HasColumnName("is_ground");
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
            entity.Property(e => e.ShelfId).HasColumnName("shelf_id");
            entity.Property(e => e.StackLevel)
                .HasDefaultValue(1)
                .HasColumnName("stack_level");
            entity.Property(e => e.StackedOnPallet).HasColumnName("stacked_on_pallet");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.LocationCode)
                .HasMaxLength(50)
                .HasColumnName("location_code")
                .HasComputedColumnSql("('LOC-' + RIGHT('000000' + CAST([location_id] AS VARCHAR(6)), 6))", stored: true);

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
            entity.HasKey(e => e.TemplateId).HasName("PK__pallet_t__BE44E079D6400CB4");

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
            entity.HasKey(e => e.ProductId).HasName("PK__products__47027DF59585481C");

            entity.ToTable("products", tb => tb.HasTrigger("trg_products_updated_at"));

            entity.HasIndex(e => e.Category, "IX_products_category");

            entity.HasIndex(e => e.ProductCode, "UQ__products__AE1A8CC4146D2D07").IsUnique();

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CreateUser).HasColumnName("create_user");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsFragile)
                .HasDefaultValue(false)
                .HasColumnName("is_fragile");
            entity.Property(e => e.IsHazardous)
                .HasDefaultValue(false)
                .HasColumnName("is_hazardous");
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
            entity.HasKey(e => e.RackId).HasName("PK__racks__6E237EB3B6A3BC58");

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
            entity.HasKey(e => e.ShelfId).HasName("PK__shelves__E33A5B7C11ED560F");

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

        modelBuilder.Entity<VItemPrioritySearch>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_item_priority_search");

            entity.Property(e => e.BatchNumber)
                .HasMaxLength(100)
                .HasColumnName("batch_number");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customer_name");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.InboundDate).HasColumnName("inbound_date");
            entity.Property(e => e.IsHeavy).HasColumnName("is_heavy");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemName)
                .HasMaxLength(300)
                .HasColumnName("item_name");
            entity.Property(e => e.ManufacturingDate).HasColumnName("manufacturing_date");
            entity.Property(e => e.PalletBarcode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("pallet_barcode");
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
            entity.Property(e => e.PriorityLevel).HasColumnName("priority_level");
            entity.Property(e => e.ProductCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("product_code");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ProductName)
                .HasMaxLength(300)
                .HasColumnName("product_name");
            entity.Property(e => e.QrCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("qr_code");
            entity.Property(e => e.ShelfId).HasColumnName("shelf_id");
            entity.Property(e => e.ShelfLevel).HasColumnName("shelf_level");
            entity.Property(e => e.StoragePosition)
                .HasMaxLength(12)
                .IsUnicode(false)
                .HasColumnName("storage_position");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("weight");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.ZoneName)
                .HasMaxLength(200)
                .HasColumnName("zone_name");
        });

        modelBuilder.Entity<VPalletInventory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_pallet_inventory");

            entity.Property(e => e.Barcode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("barcode");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customer_name");
            entity.Property(e => e.EarliestInboundDate).HasColumnName("earliest_inbound_date");
            entity.Property(e => e.IsGround).HasColumnName("is_ground");
            entity.Property(e => e.ItemCount).HasColumnName("item_count");
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
            entity.Property(e => e.ShelfId).HasColumnName("shelf_id");
            entity.Property(e => e.StackLevel).HasColumnName("stack_level");
            entity.Property(e => e.TotalWeight)
                .HasColumnType("decimal(38, 2)")
                .HasColumnName("total_weight");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");
            entity.Property(e => e.ZoneName)
                .HasMaxLength(200)
                .HasColumnName("zone_name");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("PK__warehous__734FE6BF6C791EA0");

            entity.ToTable("warehouses");

            entity.HasIndex(e => e.OwnerId, "IX_warehouses_owner");

            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.AllowedItemTypes).HasColumnName("allowed_item_types");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.CheckinPositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_position_x");
            entity.Property(e => e.CheckinPositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_position_y");
            entity.Property(e => e.CheckinPositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_position_z");
            entity.Property(e => e.CheckinLength)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_length");
            entity.Property(e => e.CheckinWidth)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_width");
            entity.Property(e => e.CheckinHeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("checkin_height");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.WarehouseName)
                .HasMaxLength(200)
                .HasColumnName("warehouse_name");
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
            entity.HasKey(e => e.GateId);

            entity.ToTable("warehouse_gates");

            entity.Property(e => e.GateId).HasColumnName("gate_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.GateName)
                .HasMaxLength(200)
                .HasColumnName("gate_name");
            entity.Property(e => e.PositionX)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_x");
            entity.Property(e => e.PositionY)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_y");
            entity.Property(e => e.PositionZ)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("position_z");
            entity.Property(e => e.Length)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("length");
            entity.Property(e => e.Width)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("width");
            entity.Property(e => e.Height)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("height");
            entity.Property(e => e.GateType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("gate_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseGates)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_warehouse_gates_warehouse");
        });

        modelBuilder.Entity<WarehouseZone>(entity =>
        {
            entity.HasKey(e => e.ZoneId).HasName("PK__warehous__80B401DF586F8FF1");

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
