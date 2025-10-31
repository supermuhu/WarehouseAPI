-- ===================================
-- HỆ THỐNG QUẢN LÝ KHO 3D - SQL SERVER 2022
-- ===================================

-- Tạo Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'WarehouseAPI')
BEGIN
    CREATE DATABASE WarehouseAPI;
END
GO

USE WarehouseAPI;
GO

-- ===================================
-- 1. BẢNG TÀI KHOẢN
-- ===================================
IF OBJECT_ID('accounts', 'U') IS NOT NULL DROP TABLE accounts;
GO

CREATE TABLE accounts (
    account_id VARCHAR(50) PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    full_name NVARCHAR(200),
    email VARCHAR(100),
    phone VARCHAR(20),
    role VARCHAR(20) NOT NULL CHECK (role IN ('admin', 'warehouse_owner', 'customer')),
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive'))
);
GO

-- Trigger cập nhật updated_at
CREATE OR ALTER TRIGGER trg_accounts_updated_at
ON accounts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE accounts
    SET updated_at = GETDATE()
    FROM accounts a
    INNER JOIN inserted i ON a.account_id = i.account_id;
END
GO

-- ===================================
-- 2. BẢNG KHO
-- ===================================
IF OBJECT_ID('warehouses', 'U') IS NOT NULL DROP TABLE warehouses;
GO

CREATE TABLE warehouses (
    warehouse_id VARCHAR(50) PRIMARY KEY,
    owner_id VARCHAR(50) NOT NULL,
    warehouse_name NVARCHAR(200) NOT NULL,
    length DECIMAL(10,2) NOT NULL, -- mét
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    warehouse_type VARCHAR(20) NOT NULL CHECK (warehouse_type IN ('small', 'medium', 'large')),
    -- small: chỉ hàng bao
    -- medium: bao hoặc thùng
    -- large: tất cả loại
    allowed_item_types NVARCHAR(MAX), -- JSON string ['bag', 'box', 'pallet']
    created_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive')),
    CONSTRAINT FK_warehouses_owner FOREIGN KEY (owner_id) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 3. BẢNG KHU VỰC TRONG KHO
-- ===================================
IF OBJECT_ID('warehouse_zones', 'U') IS NOT NULL DROP TABLE warehouse_zones;
GO

CREATE TABLE warehouse_zones (
    zone_id VARCHAR(50) PRIMARY KEY,
    warehouse_id VARCHAR(50) NOT NULL,
    zone_name NVARCHAR(200),
    customer_id VARCHAR(50), -- Khu vực riêng cho khách hàng cụ thể
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    zone_type VARCHAR(20) NOT NULL CHECK (zone_type IN ('ground', 'rack')), -- Đất hoặc kệ
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_zones_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_zones_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 4. BẢNG KỆ
-- ===================================
IF OBJECT_ID('racks', 'U') IS NOT NULL DROP TABLE racks;
GO

CREATE TABLE racks (
    rack_id VARCHAR(50) PRIMARY KEY,
    zone_id VARCHAR(50) NOT NULL,
    rack_name NVARCHAR(200),
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    max_shelves INT DEFAULT 5,
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_racks_zone FOREIGN KEY (zone_id) REFERENCES warehouse_zones(zone_id)
);
GO

-- ===================================
-- 5. BẢNG TẦNG KỆ
-- ===================================
IF OBJECT_ID('shelves', 'U') IS NOT NULL DROP TABLE shelves;
GO

CREATE TABLE shelves (
    shelf_id VARCHAR(50) PRIMARY KEY,
    rack_id VARCHAR(50) NOT NULL,
    shelf_level INT NOT NULL, -- Tầng 1, 2, 3...
    position_y DECIMAL(10,2) NOT NULL, -- Độ cao
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    max_weight DECIMAL(10,2), -- kg
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_shelves_rack FOREIGN KEY (rack_id) REFERENCES racks(rack_id),
    CONSTRAINT UQ_shelf_level UNIQUE (rack_id, shelf_level)
);
GO

-- ===================================
-- 6. BẢNG PALLET
-- ===================================
IF OBJECT_ID('pallets', 'U') IS NOT NULL DROP TABLE pallets;
GO

CREATE TABLE pallets (
    pallet_id VARCHAR(50) PRIMARY KEY,
    barcode VARCHAR(100) UNIQUE NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) DEFAULT 0.15, -- Chiều cao pallet chuẩn
    max_weight DECIMAL(10,2) DEFAULT 1000,
    max_stack_height DECIMAL(10,2) DEFAULT 1.5, -- Tối đa 1.5m
    created_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) DEFAULT 'available' CHECK (status IN ('available', 'in_use', 'maintenance'))
);
GO

-- ===================================
-- 7. BẢNG VỊ TRÍ PALLET (Trên kệ hoặc dưới đất)
-- ===================================
IF OBJECT_ID('pallet_locations', 'U') IS NOT NULL DROP TABLE pallet_locations;
GO

CREATE TABLE pallet_locations (
    location_id VARCHAR(50) PRIMARY KEY,
    pallet_id VARCHAR(50) NOT NULL,
    zone_id VARCHAR(50) NOT NULL,
    shelf_id VARCHAR(50), -- NULL nếu để dưới đất
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    stack_level INT DEFAULT 1 CHECK (stack_level <= 2), -- Tầng chồng (1 hoặc 2)
    stacked_on_pallet VARCHAR(50), -- ID pallet bên dưới nếu chồng
    is_ground BIT DEFAULT 0,
    assigned_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_pallet_locations_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id),
    CONSTRAINT FK_pallet_locations_zone FOREIGN KEY (zone_id) REFERENCES warehouse_zones(zone_id),
    CONSTRAINT FK_pallet_locations_shelf FOREIGN KEY (shelf_id) REFERENCES shelves(shelf_id),
    CONSTRAINT FK_pallet_locations_stacked FOREIGN KEY (stacked_on_pallet) REFERENCES pallets(pallet_id)
);
GO

-- ===================================
-- 8. BẢNG HÀNG HÓA
-- ===================================
IF OBJECT_ID('items', 'U') IS NOT NULL DROP TABLE items;
GO

CREATE TABLE items (
    item_id VARCHAR(50) PRIMARY KEY,
    qr_code VARCHAR(100) UNIQUE NOT NULL, -- QR Code cho từng hàng
    customer_id VARCHAR(50) NOT NULL, -- Chủ hàng
    item_name NVARCHAR(300) NOT NULL,
    item_type VARCHAR(20) NOT NULL CHECK (item_type IN ('bag', 'box', 'custom')), -- Bao, thùng, tùy chỉnh
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    weight DECIMAL(10,2), -- kg
    shape VARCHAR(20) DEFAULT 'rectangle' CHECK (shape IN ('rectangle', 'square', 'cylinder', 'irregular')),
    priority_level INT DEFAULT 5 CHECK (priority_level BETWEEN 1 AND 10), -- 1-10, số nhỏ = ưu tiên cao (hay lấy)
    is_heavy BIT DEFAULT 0,
    is_fragile BIT DEFAULT 0,
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_items_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 9. BẢNG PHÂN BỔ HÀNG TRÊN PALLET
-- ===================================
IF OBJECT_ID('item_allocations', 'U') IS NOT NULL DROP TABLE item_allocations;
GO

CREATE TABLE item_allocations (
    allocation_id VARCHAR(50) PRIMARY KEY,
    item_id VARCHAR(50) NOT NULL,
    pallet_id VARCHAR(50) NOT NULL,
    position_x DECIMAL(10,2), -- Vị trí tương đối trên pallet
    position_y DECIMAL(10,2),
    position_z DECIMAL(10,2),
    allocated_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_item_allocations_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_item_allocations_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id)
);
GO

-- ===================================
-- 10. BẢNG PHIẾU NHẬP KHO
-- ===================================
IF OBJECT_ID('inbound_receipts', 'U') IS NOT NULL DROP TABLE inbound_receipts;
GO

CREATE TABLE inbound_receipts (
    receipt_id VARCHAR(50) PRIMARY KEY,
    warehouse_id VARCHAR(50) NOT NULL,
    customer_id VARCHAR(50) NOT NULL,
    receipt_number VARCHAR(100) UNIQUE NOT NULL,
    total_items INT NOT NULL,
    total_pallets INT NOT NULL,
    inbound_date DATETIME2 DEFAULT GETDATE(),
    notes NVARCHAR(MAX),
    created_by VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'completed', 'cancelled')),
    CONSTRAINT FK_inbound_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_inbound_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id),
    CONSTRAINT FK_inbound_created_by FOREIGN KEY (created_by) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 11. CHI TIẾT PHIẾU NHẬP
-- ===================================
IF OBJECT_ID('inbound_items', 'U') IS NOT NULL DROP TABLE inbound_items;
GO

CREATE TABLE inbound_items (
    inbound_item_id VARCHAR(50) PRIMARY KEY,
    receipt_id VARCHAR(50) NOT NULL,
    item_id VARCHAR(50) NOT NULL,
    pallet_id VARCHAR(50) NOT NULL,
    quantity INT DEFAULT 1,
    CONSTRAINT FK_inbound_items_receipt FOREIGN KEY (receipt_id) REFERENCES inbound_receipts(receipt_id),
    CONSTRAINT FK_inbound_items_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_inbound_items_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id)
);
GO

-- ===================================
-- 12. BẢNG PHIẾU XUẤT KHO
-- ===================================
IF OBJECT_ID('outbound_receipts', 'U') IS NOT NULL DROP TABLE outbound_receipts;
GO

CREATE TABLE outbound_receipts (
    receipt_id VARCHAR(50) PRIMARY KEY,
    warehouse_id VARCHAR(50) NOT NULL,
    customer_id VARCHAR(50) NOT NULL,
    receipt_number VARCHAR(100) UNIQUE NOT NULL,
    total_items INT NOT NULL,
    outbound_date DATETIME2 DEFAULT GETDATE(),
    notes NVARCHAR(MAX),
    created_by VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'completed', 'cancelled')),
    CONSTRAINT FK_outbound_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_outbound_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id),
    CONSTRAINT FK_outbound_created_by FOREIGN KEY (created_by) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 13. CHI TIẾT PHIẾU XUẤT
-- ===================================
IF OBJECT_ID('outbound_items', 'U') IS NOT NULL DROP TABLE outbound_items;
GO

CREATE TABLE outbound_items (
    outbound_item_id VARCHAR(50) PRIMARY KEY,
    receipt_id VARCHAR(50) NOT NULL,
    item_id VARCHAR(50) NOT NULL,
    quantity INT DEFAULT 1,
    removed_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_outbound_items_receipt FOREIGN KEY (receipt_id) REFERENCES outbound_receipts(receipt_id),
    CONSTRAINT FK_outbound_items_item FOREIGN KEY (item_id) REFERENCES items(item_id)
);
GO

-- ===================================
-- 14. LỊCH SỬ VỊ TRÍ HÀNG (Audit log)
-- ===================================
IF OBJECT_ID('item_location_history', 'U') IS NOT NULL DROP TABLE item_location_history;
GO

CREATE TABLE item_location_history (
    history_id VARCHAR(50) PRIMARY KEY,
    item_id VARCHAR(50) NOT NULL,
    pallet_id VARCHAR(50),
    location_id VARCHAR(50),
    action_type VARCHAR(20) NOT NULL CHECK (action_type IN ('inbound', 'outbound', 'move', 'restack')),
    action_date DATETIME2 DEFAULT GETDATE(),
    performed_by VARCHAR(50) NOT NULL,
    notes NVARCHAR(MAX),
    CONSTRAINT FK_history_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_history_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id),
    CONSTRAINT FK_history_location FOREIGN KEY (location_id) REFERENCES pallet_locations(location_id),
    CONSTRAINT FK_history_performed_by FOREIGN KEY (performed_by) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- INDEXES ĐỂ TỐI ƯU TRUY VẤN
-- ===================================

CREATE NONCLUSTERED INDEX IX_warehouses_owner ON warehouses(owner_id);
CREATE NONCLUSTERED INDEX IX_zones_warehouse ON warehouse_zones(warehouse_id);
CREATE NONCLUSTERED INDEX IX_zones_customer ON warehouse_zones(customer_id);
CREATE NONCLUSTERED INDEX IX_racks_zone ON racks(zone_id);
CREATE NONCLUSTERED INDEX IX_shelves_rack ON shelves(rack_id);
CREATE NONCLUSTERED INDEX IX_pallet_locations_zone ON pallet_locations(zone_id);
CREATE NONCLUSTERED INDEX IX_pallet_locations_shelf ON pallet_locations(shelf_id);
CREATE NONCLUSTERED INDEX IX_items_customer ON items(customer_id);
CREATE NONCLUSTERED INDEX IX_item_allocations_pallet ON item_allocations(pallet_id);
CREATE NONCLUSTERED INDEX IX_inbound_warehouse ON inbound_receipts(warehouse_id);
CREATE NONCLUSTERED INDEX IX_inbound_customer ON inbound_receipts(customer_id);
CREATE NONCLUSTERED INDEX IX_inbound_date ON inbound_receipts(inbound_date);
CREATE NONCLUSTERED INDEX IX_outbound_warehouse ON outbound_receipts(warehouse_id);
CREATE NONCLUSTERED INDEX IX_outbound_customer ON outbound_receipts(customer_id);
GO

-- ===================================
-- VIEWS HỖ TRỢ TÌM KIẾM
-- ===================================

-- View: Thông tin đầy đủ về pallet và hàng hóa
IF OBJECT_ID('v_pallet_inventory', 'V') IS NOT NULL DROP VIEW v_pallet_inventory;
GO

CREATE VIEW v_pallet_inventory AS
SELECT 
    p.pallet_id,
    p.barcode,
    pl.zone_id,
    z.zone_name,
    z.customer_id,
    acc.full_name as customer_name,
    pl.shelf_id,
    pl.position_x,
    pl.position_y,
    pl.position_z,
    pl.is_ground,
    pl.stack_level,
    COUNT(ia.item_id) as item_count,
    MIN(ir.inbound_date) as earliest_inbound_date,
    SUM(i.weight) as total_weight
FROM pallets p
LEFT JOIN pallet_locations pl ON p.pallet_id = pl.pallet_id
LEFT JOIN warehouse_zones z ON pl.zone_id = z.zone_id
LEFT JOIN accounts acc ON z.customer_id = acc.account_id
LEFT JOIN item_allocations ia ON p.pallet_id = ia.pallet_id
LEFT JOIN items i ON ia.item_id = i.item_id
LEFT JOIN inbound_items ii ON i.item_id = ii.item_id
LEFT JOIN inbound_receipts ir ON ii.receipt_id = ir.receipt_id
WHERE p.status = 'in_use'
GROUP BY 
    p.pallet_id, p.barcode, pl.zone_id, z.zone_name, 
    z.customer_id, acc.full_name, pl.shelf_id,
    pl.position_x, pl.position_y, pl.position_z,
    pl.is_ground, pl.stack_level;
GO

-- View: Tìm kiếm hàng theo ưu tiên
IF OBJECT_ID('v_item_priority_search', 'V') IS NOT NULL DROP VIEW v_item_priority_search;
GO

CREATE VIEW v_item_priority_search AS
SELECT 
    i.item_id,
    i.qr_code,
    i.item_name,
    i.customer_id,
    acc.full_name as customer_name,
    ia.pallet_id,
    p.barcode as pallet_barcode,
    pl.zone_id,
    z.zone_name,
    pl.shelf_id,
    s.shelf_level,
    pl.position_x,
    pl.position_y,
    pl.position_z,
    i.priority_level,
    i.is_heavy,
    i.weight,
    ir.inbound_date,
    CASE 
        WHEN pl.is_ground = 1 THEN 'ground'
        WHEN s.shelf_level = 1 THEN 'bottom_shelf'
        WHEN s.shelf_level <= 3 THEN 'middle_shelf'
        ELSE 'top_shelf'
    END as storage_position
FROM items i
INNER JOIN accounts acc ON i.customer_id = acc.account_id
LEFT JOIN item_allocations ia ON i.item_id = ia.item_id
LEFT JOIN pallets p ON ia.pallet_id = p.pallet_id
LEFT JOIN pallet_locations pl ON p.pallet_id = pl.pallet_id
LEFT JOIN shelves s ON pl.shelf_id = s.shelf_id
LEFT JOIN warehouse_zones z ON pl.zone_id = z.zone_id
LEFT JOIN inbound_items ii ON i.item_id = ii.item_id
LEFT JOIN inbound_receipts ir ON ii.receipt_id = ir.receipt_id;
GO

-- ===================================
-- STORED PROCEDURES
-- ===================================

-- SP: Tìm kiếm pallet theo khách hàng với sắp xếp ưu tiên
CREATE OR ALTER PROCEDURE sp_search_pallets_by_customer
    @customer_name NVARCHAR(200) = NULL,
    @warehouse_id VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        p.pallet_id,
        p.barcode,
        acc.full_name as customer_name,
        COUNT(ia.item_id) as item_count,
        MIN(ir.inbound_date) as earliest_date,
        pl.position_x,
        pl.position_y,
        pl.position_z,
        z.zone_name,
        DATEDIFF(DAY, MIN(ir.inbound_date), GETDATE()) as days_in_storage
    FROM pallets p
    LEFT JOIN pallet_locations pl ON p.pallet_id = pl.pallet_id
    LEFT JOIN warehouse_zones z ON pl.zone_id = z.zone_id
    LEFT JOIN accounts acc ON z.customer_id = acc.account_id
    LEFT JOIN item_allocations ia ON p.pallet_id = ia.pallet_id
    LEFT JOIN inbound_items ii ON ia.item_id = ii.item_id
    LEFT JOIN inbound_receipts ir ON ii.receipt_id = ir.receipt_id
    WHERE p.status = 'in_use'
        AND (@customer_name IS NULL OR acc.full_name LIKE '%' + @customer_name + '%')
        AND (@warehouse_id IS NULL OR z.warehouse_id = @warehouse_id)
    GROUP BY 
        p.pallet_id, p.barcode, acc.full_name,
        pl.position_x, pl.position_y, pl.position_z, z.zone_name
    ORDER BY 
        MIN(ir.inbound_date) ASC,  -- Hàng cũ lên trước
        COUNT(ia.item_id) ASC;     -- Pallet ít hàng lên trước
END
GO

-- SP: Tạo phiếu nhập kho
CREATE OR ALTER PROCEDURE sp_create_inbound_receipt
    @warehouse_id VARCHAR(50),
    @customer_id VARCHAR(50),
    @created_by VARCHAR(50),
    @total_items INT,
    @total_pallets INT,
    @notes NVARCHAR(MAX) = NULL,
    @receipt_id VARCHAR(50) OUTPUT,
    @receipt_number VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Tạo receipt_id và receipt_number
        SET @receipt_id = NEWID();
        SET @receipt_number = 'IN-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + RIGHT(@receipt_id, 8);
        
        INSERT INTO inbound_receipts (
            receipt_id, warehouse_id, customer_id, receipt_number,
            total_items, total_pallets, inbound_date, notes, created_by, status
        )
        VALUES (
            @receipt_id, @warehouse_id, @customer_id, @receipt_number,
            @total_items, @total_pallets, GETDATE(), @notes, @created_by, 'completed'
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- SP: Tạo phiếu xuất kho
CREATE OR ALTER PROCEDURE sp_create_outbound_receipt
    @warehouse_id VARCHAR(50),
    @customer_id VARCHAR(50),
    @created_by VARCHAR(50),
    @total_items INT,
    @notes NVARCHAR(MAX) = NULL,
    @receipt_id VARCHAR(50) OUTPUT,
    @receipt_number VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        SET @receipt_id = NEWID();
        SET @receipt_number = 'OUT-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + RIGHT(@receipt_id, 8);
        
        INSERT INTO outbound_receipts (
            receipt_id, warehouse_id, customer_id, receipt_number,
            total_items, outbound_date, notes, created_by, status
        )
        VALUES (
            @receipt_id, @warehouse_id, @customer_id, @receipt_number,
            @total_items, GETDATE(), @notes, @created_by, 'completed'
        );
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ===================================
-- DỮ LIỆU MẪU
-- ===================================

-- Tạo tài khoản mẫu
INSERT INTO accounts (account_id, username, password_hash, full_name, email, role)
VALUES 
    ('acc001', 'admin', 'hashed_password', N'Quản trị viên', 'admin@warehouse.com', 'admin'),
    ('acc002', 'owner1', 'hashed_password', N'Chủ kho A', 'owner1@warehouse.com', 'warehouse_owner'),
    ('acc003', 'customer1', 'hashed_password', N'Nguyễn Văn A', 'customer1@example.com', 'customer'),
    ('acc004', 'customer2', 'hashed_password', N'Trần Thị B', 'customer2@example.com', 'customer');
GO

PRINT 'Database WarehouseAPI đã được tạo thành công cho SQL Server 2022!';
GO