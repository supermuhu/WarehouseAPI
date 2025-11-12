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
    account_id INT IDENTITY(1,1) PRIMARY KEY,
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
    warehouse_id INT IDENTITY(1,1) PRIMARY KEY,
    owner_id INT NOT NULL,
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
    zone_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    zone_name NVARCHAR(200),
    customer_id INT, -- Khu vực riêng cho khách hàng cụ thể
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
    rack_id INT IDENTITY(1,1) PRIMARY KEY,
    zone_id INT NOT NULL,
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
    shelf_id INT IDENTITY(1,1) PRIMARY KEY,
    rack_id INT NOT NULL,
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
    pallet_id INT IDENTITY(1,1) PRIMARY KEY,
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
    location_id INT IDENTITY(1,1) PRIMARY KEY,
    pallet_id INT NOT NULL,
    zone_id INT NOT NULL,
    shelf_id INT, -- NULL nếu để dưới đất
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    stack_level INT DEFAULT 1 CHECK (stack_level <= 2), -- Tầng chồng (1 hoặc 2)
    stacked_on_pallet INT, -- ID pallet bên dưới nếu chồng
    is_ground BIT DEFAULT 0,
    assigned_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_pallet_locations_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id),
    CONSTRAINT FK_pallet_locations_zone FOREIGN KEY (zone_id) REFERENCES warehouse_zones(zone_id),
    CONSTRAINT FK_pallet_locations_shelf FOREIGN KEY (shelf_id) REFERENCES shelves(shelf_id),
    CONSTRAINT FK_pallet_locations_stacked FOREIGN KEY (stacked_on_pallet) REFERENCES pallets(pallet_id)
);
GO

-- ===================================
-- 8. BẢNG SẢN PHẨM (PRODUCTS)
-- ===================================
IF OBJECT_ID('products', 'U') IS NOT NULL DROP TABLE products;
GO

CREATE TABLE products (
    product_id INT IDENTITY(1,1) PRIMARY KEY,
    product_code VARCHAR(100) UNIQUE NOT NULL, -- Mã sản phẩm
    product_name NVARCHAR(300) NOT NULL, -- Tên sản phẩm
    description NVARCHAR(MAX), -- Mô tả sản phẩm
    unit NVARCHAR(50) NOT NULL, -- Đơn vị tính (kg, thùng, bao, cái, etc.)
    category NVARCHAR(100), -- Danh mục (điện tử, thực phẩm, vật liệu xây dựng, etc.)
    standard_length DECIMAL(10,2), -- Kích thước chuẩn (m)
    standard_width DECIMAL(10,2),
    standard_height DECIMAL(10,2),
    standard_weight DECIMAL(10,2), -- Trọng lượng chuẩn (kg)
    is_fragile BIT DEFAULT 0, -- Hàng dễ vỡ
    is_hazardous BIT DEFAULT 0, -- Hàng nguy hiểm
    storage_conditions NVARCHAR(MAX), -- Điều kiện bảo quản
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive'))
);
GO

-- Trigger cập nhật updated_at cho products
CREATE OR ALTER TRIGGER trg_products_updated_at
ON products
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE products
    SET updated_at = GETDATE()
    FROM products p
    INNER JOIN inserted i ON p.product_id = i.product_id;
END
GO

-- ===================================
-- 9. BẢNG HÀNG HÓA (ITEMS)
-- ===================================
IF OBJECT_ID('items', 'U') IS NOT NULL DROP TABLE items;
GO

CREATE TABLE items (
    item_id INT IDENTITY(1,1) PRIMARY KEY,
    qr_code VARCHAR(100) UNIQUE NOT NULL, -- QR Code cho từng hàng
    product_id INT NOT NULL, -- Loại sản phẩm
    customer_id INT NOT NULL, -- Chủ hàng
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
    batch_number NVARCHAR(100), -- Số lô
    manufacturing_date DATE, -- Ngày sản xuất
    expiry_date DATE, -- Ngày hết hạn
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_items_product FOREIGN KEY (product_id) REFERENCES products(product_id),
    CONSTRAINT FK_items_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 10. BẢNG PHÂN BỔ HÀNG TRÊN PALLET
-- ===================================
IF OBJECT_ID('item_allocations', 'U') IS NOT NULL DROP TABLE item_allocations;
GO

CREATE TABLE item_allocations (
    allocation_id INT IDENTITY(1,1) PRIMARY KEY,
    item_id INT NOT NULL,
    pallet_id INT NOT NULL,
    position_x DECIMAL(10,2), -- Vị trí tương đối trên pallet
    position_y DECIMAL(10,2),
    position_z DECIMAL(10,2),
    allocated_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_item_allocations_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_item_allocations_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id)
);
GO

-- ===================================
-- 11. BẢNG PHIẾU NHẬP KHO
-- ===================================
IF OBJECT_ID('inbound_receipts', 'U') IS NOT NULL DROP TABLE inbound_receipts;
GO

CREATE TABLE inbound_receipts (
    receipt_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    customer_id INT NOT NULL,
    receipt_number VARCHAR(100) UNIQUE NOT NULL,
    total_items INT NOT NULL,
    total_pallets INT NOT NULL,
    inbound_date DATETIME2 DEFAULT GETDATE(),
    notes NVARCHAR(MAX),
    created_by INT NOT NULL,
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
    inbound_item_id INT IDENTITY(1,1) PRIMARY KEY,
    receipt_id INT NOT NULL,
    item_id INT NOT NULL,
    pallet_id INT NOT NULL,
    quantity INT DEFAULT 1,
    CONSTRAINT FK_inbound_items_receipt FOREIGN KEY (receipt_id) REFERENCES inbound_receipts(receipt_id),
    CONSTRAINT FK_inbound_items_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_inbound_items_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id)
);
GO

-- ===================================
-- 13. BẢNG PHIẾU XUẤT KHO
-- ===================================
IF OBJECT_ID('outbound_receipts', 'U') IS NOT NULL DROP TABLE outbound_receipts;
GO

CREATE TABLE outbound_receipts (
    receipt_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    customer_id INT NOT NULL,
    receipt_number VARCHAR(100) UNIQUE NOT NULL,
    total_items INT NOT NULL,
    outbound_date DATETIME2 DEFAULT GETDATE(),
    notes NVARCHAR(MAX),
    created_by INT NOT NULL,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'completed', 'cancelled')),
    CONSTRAINT FK_outbound_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_outbound_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id),
    CONSTRAINT FK_outbound_created_by FOREIGN KEY (created_by) REFERENCES accounts(account_id)
);
GO

-- ===================================
-- 14. CHI TIẾT PHIẾU XUẤT
-- ===================================
IF OBJECT_ID('outbound_items', 'U') IS NOT NULL DROP TABLE outbound_items;
GO

CREATE TABLE outbound_items (
    outbound_item_id INT IDENTITY(1,1) PRIMARY KEY,
    receipt_id INT NOT NULL,
    item_id INT NOT NULL,
    quantity INT DEFAULT 1,
    removed_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_outbound_items_receipt FOREIGN KEY (receipt_id) REFERENCES outbound_receipts(receipt_id),
    CONSTRAINT FK_outbound_items_item FOREIGN KEY (item_id) REFERENCES items(item_id)
);
GO

-- ===================================
-- 15. LỊCH SỬ VỊ TRÍ HÀNG (Audit log)
-- ===================================
IF OBJECT_ID('item_location_history', 'U') IS NOT NULL DROP TABLE item_location_history;
GO

CREATE TABLE item_location_history (
    history_id INT IDENTITY(1,1) PRIMARY KEY,
    item_id INT NOT NULL,
    pallet_id INT,
    location_id INT,
    action_type VARCHAR(20) NOT NULL CHECK (action_type IN ('inbound', 'outbound', 'move', 'restack')),
    action_date DATETIME2 DEFAULT GETDATE(),
    performed_by INT NOT NULL,
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
CREATE NONCLUSTERED INDEX IX_products_category ON products(category);
CREATE NONCLUSTERED INDEX IX_items_product ON items(product_id);
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
    i.product_id,
    prod.product_name,
    prod.product_code,
    prod.unit,
    prod.category,
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
    i.batch_number,
    i.manufacturing_date,
    i.expiry_date,
    ir.inbound_date,
    CASE 
        WHEN pl.is_ground = 1 THEN 'ground'
        WHEN s.shelf_level = 1 THEN 'bottom_shelf'
        WHEN s.shelf_level <= 3 THEN 'middle_shelf'
        ELSE 'top_shelf'
    END as storage_position
FROM items i
INNER JOIN products prod ON i.product_id = prod.product_id
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
    @warehouse_id INT = NULL
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
    @warehouse_id INT,
    @customer_id INT,
    @created_by INT,
    @total_items INT,
    @total_pallets INT,
    @notes NVARCHAR(MAX) = NULL,
    @receipt_id INT OUTPUT,
    @receipt_number VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        -- Tạo receipt_number
        DECLARE @temp_id VARCHAR(50) = CAST(NEWID() AS VARCHAR(50));
        SET @receipt_number = 'IN-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + RIGHT(@temp_id, 8);
        
        INSERT INTO inbound_receipts (
            warehouse_id, customer_id, receipt_number,
            total_items, total_pallets, inbound_date, notes, created_by, status
        )
        VALUES (
            @warehouse_id, @customer_id, @receipt_number,
            @total_items, @total_pallets, GETDATE(), @notes, @created_by, 'completed'
        );
        
        SET @receipt_id = SCOPE_IDENTITY();
        
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
    @warehouse_id INT,
    @customer_id INT,
    @created_by INT,
    @total_items INT,
    @notes NVARCHAR(MAX) = NULL,
    @receipt_id INT OUTPUT,
    @receipt_number VARCHAR(100) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @temp_id VARCHAR(50) = CAST(NEWID() AS VARCHAR(50));
        SET @receipt_number = 'OUT-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + RIGHT(@temp_id, 8);
        
        INSERT INTO outbound_receipts (
            warehouse_id, customer_id, receipt_number,
            total_items, outbound_date, notes, created_by, status
        )
        VALUES (
            @warehouse_id, @customer_id, @receipt_number,
            @total_items, GETDATE(), @notes, @created_by, 'completed'
        );
        
        SET @receipt_id = SCOPE_IDENTITY();
        
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

USE WarehouseAPI;
GO

-- ===================================
-- 1. TÀI KHOẢN (1 Admin, 1 Owner, 3 Customers)
-- ===================================

-- Admin
INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
VALUES 
    ('admin', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Nguyễn Văn Admin', 'admin@warehouseapi.vn', '0901234567', 'admin', 'active');

-- Owner
INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
VALUES 
    ('chukhoa', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Trần Minh Khoa', 'khoa@warehouseapi.vn', '0912345678', 'warehouse_owner', 'active');

-- Customers
INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
VALUES 
    ('customer_a', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Nguyễn Thị Lan', 'lan.nguyen@example.com', '0923456789', 'customer', 'active'),
    ('customer_b', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Lê Văn Hùng', 'hung.le@example.com', '0934567890', 'customer', 'active'),
    ('customer_c', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Phạm Thị Mai', 'mai.pham@example.com', '0945678901', 'customer', 'active');

GO

-- Hiển thị tài khoản đã tạo
SELECT account_id, username, full_name, email, role FROM accounts;
GO

-- ===================================
-- 2. KHO (2 kho: 1 lớn, 1 vừa)
-- ===================================

DECLARE @owner_id INT = (SELECT account_id FROM accounts WHERE username = 'chukhoa');

INSERT INTO warehouses (owner_id, warehouse_name, length, width, height, warehouse_type, allowed_item_types, status)
VALUES 
    (@owner_id, N'Kho Trung Tâm - Quận 7', 50.00, 30.00, 8.00, 'large', '["bag", "box", "pallet"]', 'active'),
    (@owner_id, N'Kho Chi Nhánh - Bình Thạnh', 30.00, 20.00, 5.00, 'medium', '["bag", "box"]', 'active');

GO

-- Hiển thị kho
SELECT warehouse_id, warehouse_name, warehouse_type, 
       CONCAT(length, 'm x ', width, 'm x ', height, 'm') AS dimensions
FROM warehouses;
GO

-- ===================================
-- 3. KHU VỰC TRONG KHO
-- ===================================

DECLARE @warehouse1_id INT = (SELECT TOP 1 warehouse_id FROM warehouses WHERE warehouse_name LIKE N'%Quận 7%');
DECLARE @warehouse2_id INT = (SELECT TOP 1 warehouse_id FROM warehouses WHERE warehouse_name LIKE N'%Bình Thạnh%');
DECLARE @customer_a_id INT = (SELECT account_id FROM accounts WHERE username = 'customer_a');
DECLARE @customer_b_id INT = (SELECT account_id FROM accounts WHERE username = 'customer_b');
DECLARE @customer_c_id INT = (SELECT account_id FROM accounts WHERE username = 'customer_c');

-- Kho 1: Chia 3 khu cho 3 khách hàng
INSERT INTO warehouse_zones (warehouse_id, zone_name, customer_id, position_x, position_y, position_z, length, width, height, zone_type)
VALUES 
    (@warehouse1_id, N'Khu A - Khách Lan', @customer_a_id, 5.0, 0.0, 5.0, 15.0, 12.0, 8.0, 'ground'),
    (@warehouse1_id, N'Khu B - Khách Hùng', @customer_b_id, 25.0, 0.0, 5.0, 15.0, 12.0, 8.0, 'ground'),
    (@warehouse1_id, N'Khu C - Khách Mai', @customer_c_id, 45.0, 0.0, 5.0, 15.0, 12.0, 8.0, 'rack');

-- Kho 2: Chia 2 khu
INSERT INTO warehouse_zones (warehouse_id, zone_name, customer_id, position_x, position_y, position_z, length, width, height, zone_type)
VALUES 
    (@warehouse2_id, N'Khu D - Khách Lan', @customer_a_id, 5.0, 0.0, 5.0, 12.0, 10.0, 5.0, 'ground'),
    (@warehouse2_id, N'Khu E - Khách Hùng', @customer_b_id, 20.0, 0.0, 5.0, 12.0, 10.0, 5.0, 'rack');

GO

-- Hiển thị khu vực
SELECT z.zone_id, w.warehouse_name, z.zone_name, a.full_name AS customer_name, z.zone_type
FROM warehouse_zones z
JOIN warehouses w ON z.warehouse_id = w.warehouse_id
LEFT JOIN accounts a ON z.customer_id = a.account_id;
GO

-- ===================================
-- 4. KỆ (Racks) - Chỉ tạo cho zone type 'rack'
-- ===================================

DECLARE @zone_c_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu C%');
DECLARE @zone_e_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu E%');

-- Kệ cho Khu C
INSERT INTO racks (zone_id, rack_name, position_x, position_y, position_z, length, width, height, max_shelves)
VALUES 
    (@zone_c_id, N'Kệ C1', 46.0, 0.0, 6.0, 4.0, 1.5, 6.0, 4),
    (@zone_c_id, N'Kệ C2', 51.0, 0.0, 6.0, 4.0, 1.5, 6.0, 4),
    (@zone_c_id, N'Kệ C3', 56.0, 0.0, 6.0, 4.0, 1.5, 6.0, 4);

-- Kệ cho Khu E
INSERT INTO racks (zone_id, rack_name, position_x, position_y, position_z, length, width, height, max_shelves)
VALUES 
    (@zone_e_id, N'Kệ E1', 21.0, 0.0, 6.0, 4.0, 1.5, 4.0, 3),
    (@zone_e_id, N'Kệ E2', 26.0, 0.0, 6.0, 4.0, 1.5, 4.0, 3);

GO

-- ===================================
-- 5. TẦNG KỆ (Shelves)
-- ===================================

DECLARE @rack_c1_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ C1');
DECLARE @rack_c2_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ C2');
DECLARE @rack_c3_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ C3');
DECLARE @rack_e1_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ E1');
DECLARE @rack_e2_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ E2');

-- Tầng cho Kệ C1 (4 tầng)
INSERT INTO shelves (rack_id, shelf_level, position_y, length, width, max_weight)
VALUES 
    (@rack_c1_id, 1, 0.5, 4.0, 1.5, 500),
    (@rack_c1_id, 2, 2.0, 4.0, 1.5, 400),
    (@rack_c1_id, 3, 3.5, 4.0, 1.5, 300),
    (@rack_c1_id, 4, 5.0, 4.0, 1.5, 200);

-- Tầng cho Kệ C2
INSERT INTO shelves (rack_id, shelf_level, position_y, length, width, max_weight)
VALUES 
    (@rack_c2_id, 1, 0.5, 4.0, 1.5, 500),
    (@rack_c2_id, 2, 2.0, 4.0, 1.5, 400),
    (@rack_c2_id, 3, 3.5, 4.0, 1.5, 300),
    (@rack_c2_id, 4, 5.0, 4.0, 1.5, 200);

-- Tầng cho Kệ C3
INSERT INTO shelves (rack_id, shelf_level, position_y, length, width, max_weight)
VALUES 
    (@rack_c3_id, 1, 0.5, 4.0, 1.5, 500),
    (@rack_c3_id, 2, 2.0, 4.0, 1.5, 400),
    (@rack_c3_id, 3, 3.5, 4.0, 1.5, 300),
    (@rack_c3_id, 4, 5.0, 4.0, 1.5, 200);

-- Tầng cho Kệ E1 (3 tầng)
INSERT INTO shelves (rack_id, shelf_level, position_y, length, width, max_weight)
VALUES 
    (@rack_e1_id, 1, 0.5, 4.0, 1.5, 400),
    (@rack_e1_id, 2, 2.0, 4.0, 1.5, 300),
    (@rack_e1_id, 3, 3.5, 4.0, 1.5, 200);

-- Tầng cho Kệ E2
INSERT INTO shelves (rack_id, shelf_level, position_y, length, width, max_weight)
VALUES 
    (@rack_e2_id, 1, 0.5, 4.0, 1.5, 400),
    (@rack_e2_id, 2, 2.0, 4.0, 1.5, 300),
    (@rack_e2_id, 3, 3.5, 4.0, 1.5, 200);

GO

-- ===================================
-- 6. PALLET (10 pallets)
-- ===================================

INSERT INTO pallets (barcode, length, width, height, max_weight, max_stack_height, status)
VALUES 
    ('PLT-000001', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000002', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000003', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000004', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000005', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000006', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000007', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000008', 1.20, 1.00, 0.15, 1000, 1.5, 'in_use'),
    ('PLT-000009', 1.20, 1.00, 0.15, 1000, 1.5, 'available'),
    ('PLT-000010', 1.20, 1.00, 0.15, 1000, 1.5, 'available');

GO

-- ===================================
-- 7. VỊ TRÍ PALLET
-- ===================================

DECLARE @zone_a_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu A%');
DECLARE @zone_b_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu B%');
DECLARE @zone_c_id_loc INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu C%');
DECLARE @zone_d_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu D%');

DECLARE @shelf_c1_1 INT = (SELECT shelf_id FROM shelves WHERE rack_id = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ C1') AND shelf_level = 1);
DECLARE @shelf_c1_2 INT = (SELECT shelf_id FROM shelves WHERE rack_id = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ C1') AND shelf_level = 2);
DECLARE @shelf_c2_1 INT = (SELECT shelf_id FROM shelves WHERE rack_id = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ C2') AND shelf_level = 1);

-- Pallet trên đất (ground) - Khu A
INSERT INTO pallet_locations (pallet_id, zone_id, shelf_id, position_x, position_y, position_z, stack_level, is_ground)
VALUES 
    (1, @zone_a_id, NULL, 7.0, 0.0, 7.0, 1, 1),
    (2, @zone_a_id, NULL, 9.5, 0.0, 7.0, 1, 1),
    (3, @zone_a_id, NULL, 12.0, 0.0, 7.0, 1, 1);

-- Pallet trên đất - Khu B
INSERT INTO pallet_locations (pallet_id, zone_id, shelf_id, position_x, position_y, position_z, stack_level, is_ground)
VALUES 
    (4, @zone_b_id, NULL, 27.0, 0.0, 7.0, 1, 1),
    (5, @zone_b_id, NULL, 29.5, 0.0, 7.0, 1, 1);

-- Pallet trên kệ - Khu C
INSERT INTO pallet_locations (pallet_id, zone_id, shelf_id, position_x, position_y, position_z, stack_level, is_ground)
VALUES 
    (6, @zone_c_id_loc, @shelf_c1_1, 46.5, 0.5, 6.5, 1, 0),
    (7, @zone_c_id_loc, @shelf_c1_2, 46.5, 2.0, 6.5, 1, 0),
    (8, @zone_c_id_loc, @shelf_c2_1, 51.5, 0.5, 6.5, 1, 0);

GO

-- ===================================
-- 8. SẢN PHẨM (Products)
-- ===================================

INSERT INTO products (product_code, product_name, description, unit, category, standard_length, standard_width, standard_height, standard_weight, is_fragile, is_hazardous, storage_conditions, status)
VALUES 
    -- Điện tử
    ('PROD-ELEC-001', N'Đồ điện tử Samsung', N'Các sản phẩm điện tử Samsung bao gồm điện thoại, tablet, phụ kiện', N'Thùng', N'Điện tử', 0.50, 0.40, 0.35, 18.5, 1, 0, N'Tránh ẩm, nhiệt độ phòng', 'active'),
    ('PROD-ELEC-002', N'Linh kiện máy tính', N'Linh kiện máy tính như RAM, ổ cứng, card màn hình', N'Thùng', N'Điện tử', 0.45, 0.40, 0.30, 14.0, 1, 0, N'Tránh ẩm, tĩnh điện', 'active'),
    ('PROD-ELEC-003', N'Đèn LED', N'Đèn LED chiếu sáng các loại', N'Thùng', N'Điện tử', 0.55, 0.40, 0.30, 12.0, 1, 0, N'Tránh va đập', 'active'),
    ('PROD-ELEC-004', N'Thiết bị điện', N'Thiết bị điện công nghiệp', N'Thùng', N'Điện tử', 0.50, 0.45, 0.35, 16.5, 0, 0, N'Khô ráo', 'active'),
    
    -- Thực phẩm
    ('PROD-FOOD-001', N'Gạo ST25', N'Gạo ST25 cao cấp', N'Bao 50kg', N'Thực phẩm', 0.80, 0.50, 0.20, 50.0, 0, 0, N'Khô ráo, thoáng mát', 'active'),
    ('PROD-FOOD-002', N'Thực phẩm đóng hộp', N'Thực phẩm đóng hộp các loại', N'Thùng', N'Thực phẩm', 0.50, 0.40, 0.35, 18.0, 0, 0, N'Nơi khô ráo, tránh ánh sáng', 'active'),
    
    -- Vật liệu xây dựng
    ('PROD-CONST-001', N'Xi măng', N'Xi măng PCB40', N'Bao 50kg', N'Vật liệu xây dựng', 0.75, 0.48, 0.18, 50.0, 0, 0, N'Tránh ẩm', 'active'),
    ('PROD-CONST-002', N'Cát xây dựng', N'Cát xây dựng đã sàng', N'Bao 40kg', N'Vật liệu xây dựng', 0.70, 0.45, 0.20, 40.0, 0, 0, N'Bảo quản khô', 'active'),
    ('PROD-CONST-003', N'Phân bón', N'Phân bón NPK', N'Bao 40kg', N'Vật liệu xây dựng', 0.70, 0.45, 0.18, 40.0, 0, 0, N'Khô ráo, thoáng mát', 'active'),
    ('PROD-CONST-004', N'Vật liệu xây dựng tổng hợp', N'Các loại vật liệu xây dựng khác', N'Thùng', N'Vật liệu xây dựng', 0.70, 0.55, 0.40, 35.0, 0, 0, N'Bình thường', 'active'),
    ('PROD-CONST-005', N'Dụng cụ cơ khí', N'Dụng cụ và thiết bị cơ khí', N'Thùng', N'Vật liệu xây dựng', 0.60, 0.50, 0.45, 25.0, 0, 0, N'Khô ráo', 'active'),
    
    -- Thời trang
    ('PROD-FASH-001', N'Quần áo xuất khẩu', N'Quần áo may mặc xuất khẩu', N'Thùng', N'Thời trang', 0.60, 0.45, 0.40, 12.0, 0, 0, N'Khô ráo, thoáng mát', 'active'),
    ('PROD-FASH-002', N'Giày thể thao', N'Giày thể thao các loại', N'Thùng', N'Thời trang', 0.55, 0.40, 0.35, 15.0, 0, 0, N'Tránh ẩm', 'active'),
    
    -- Mỹ phẩm
    ('PROD-COSM-001', N'Mỹ phẩm cao cấp', N'Mỹ phẩm chăm sóc da cao cấp', N'Thùng', N'Mỹ phẩm', 0.40, 0.35, 0.30, 8.5, 1, 0, N'Nhiệt độ phòng, tránh ánh sáng', 'active'),
    
    -- Đồ chơi
    ('PROD-TOY-001', N'Đồ chơi trẻ em', N'Đồ chơi an toàn cho trẻ em', N'Thùng', N'Đồ chơi', 0.50, 0.45, 0.40, 10.0, 0, 0, N'Khô ráo', 'active'),
    
    -- Văn phòng phẩm
    ('PROD-STAT-001', N'Sách giáo khoa', N'Sách giáo khoa các cấp', N'Thùng', N'Văn phòng phẩm', 0.50, 0.40, 0.35, 20.0, 0, 0, N'Tránh ẩm', 'active'),
    ('PROD-STAT-002', N'Văn phòng phẩm', N'Văn phòng phẩm văn phòng', N'Thùng', N'Văn phòng phẩm', 0.45, 0.35, 0.30, 9.0, 0, 0, N'Bình thường', 'active'),
    
    -- Y tế & Dược phẩm
    ('PROD-MED-001', N'Thiết bị y tế', N'Thiết bị y tế chuyên dụng', N'Thùng', N'Y tế', 0.40, 0.35, 0.28, 7.5, 1, 0, N'Nhiệt độ 15-25°C, tránh ẩm', 'active'),
    ('PROD-MED-002', N'Dược phẩm', N'Dược phẩm và thuốc men', N'Thùng', N'Dược phẩm', 0.38, 0.32, 0.25, 6.0, 1, 0, N'Nhiệt độ 2-8°C, tránh ánh sáng', 'active'),
    
    -- Đồ gia dụng
    ('PROD-HOME-001', N'Đồ gia dụng', N'Đồ gia dụng các loại', N'Thùng', N'Gia dụng', 0.55, 0.45, 0.40, 13.5, 0, 0, N'Bình thường', 'active');

GO

-- ===================================
-- 9. HÀNG HÓA (Items)
-- ===================================

DECLARE @customer_a_id_item INT = (SELECT account_id FROM accounts WHERE username = 'customer_a');
DECLARE @customer_b_id_item INT = (SELECT account_id FROM accounts WHERE username = 'customer_b');
DECLARE @customer_c_id_item INT = (SELECT account_id FROM accounts WHERE username = 'customer_c');

-- Hàng của khách Lan (Customer A) - 8 items
INSERT INTO items (qr_code, product_id, customer_id, item_name, item_type, length, width, height, weight, shape, priority_level, is_heavy, is_fragile, batch_number, manufacturing_date, expiry_date)
VALUES 
    ('QR-A001', (SELECT product_id FROM products WHERE product_code = 'PROD-ELEC-001'), @customer_a_id_item, N'Thùng đồ điện tử Samsung', 'box', 0.50, 0.40, 0.35, 18.5, 'rectangle', 2, 0, 1, 'BATCH-2025-001', '2025-10-01', NULL),
    ('QR-A002', (SELECT product_id FROM products WHERE product_code = 'PROD-FASH-001'), @customer_a_id_item, N'Thùng quần áo xuất khẩu', 'box', 0.60, 0.45, 0.40, 12.0, 'rectangle', 5, 0, 0, 'BATCH-2025-002', '2025-09-15', NULL),
    ('QR-A003', (SELECT product_id FROM products WHERE product_code = 'PROD-FOOD-001'), @customer_a_id_item, N'Bao gạo ST25 50kg', 'bag', 0.80, 0.50, 0.20, 50.0, 'rectangle', 7, 1, 0, 'BATCH-2025-003', '2025-09-01', '2026-09-01'),
    ('QR-A004', (SELECT product_id FROM products WHERE product_code = 'PROD-COSM-001'), @customer_a_id_item, N'Thùng mỹ phẩm cao cấp', 'box', 0.40, 0.35, 0.30, 8.5, 'rectangle', 1, 0, 1, 'BATCH-2025-004', '2025-08-01', '2027-08-01'),
    ('QR-A005', (SELECT product_id FROM products WHERE product_code = 'PROD-FASH-002'), @customer_a_id_item, N'Thùng giày thể thao', 'box', 0.55, 0.40, 0.35, 15.0, 'rectangle', 3, 0, 0, 'BATCH-2025-005', '2025-09-20', NULL),
    ('QR-A006', (SELECT product_id FROM products WHERE product_code = 'PROD-CONST-003'), @customer_a_id_item, N'Bao phân bón 40kg', 'bag', 0.70, 0.45, 0.18, 40.0, 'rectangle', 9, 1, 0, 'BATCH-2025-006', '2025-07-15', '2026-07-15'),
    ('QR-A007', (SELECT product_id FROM products WHERE product_code = 'PROD-TOY-001'), @customer_a_id_item, N'Thùng đồ chơi trẻ em', 'box', 0.50, 0.45, 0.40, 10.0, 'rectangle', 4, 0, 0, 'BATCH-2025-007', '2025-09-10', NULL),
    ('QR-A008', (SELECT product_id FROM products WHERE product_code = 'PROD-ELEC-002'), @customer_a_id_item, N'Thùng linh kiện máy tính', 'box', 0.45, 0.40, 0.30, 14.0, 'rectangle', 2, 0, 1, 'BATCH-2025-008', '2025-08-25', NULL);

-- Hàng của khách Hùng (Customer B) - 6 items
INSERT INTO items (qr_code, product_id, customer_id, item_name, item_type, length, width, height, weight, shape, priority_level, is_heavy, is_fragile, batch_number, manufacturing_date)
VALUES 
    ('QR-B001', (SELECT product_id FROM products WHERE product_code = 'PROD-CONST-005'), @customer_b_id_item, N'Thùng dụng cụ cơ khí', 'box', 0.60, 0.50, 0.45, 25.0, 'rectangle', 6, 0, 0, 'BATCH-2025-009', '2025-09-01'),
    ('QR-B002', (SELECT product_id FROM products WHERE product_code = 'PROD-CONST-001'), @customer_b_id_item, N'Bao xi măng 50kg', 'bag', 0.75, 0.48, 0.18, 50.0, 'rectangle', 10, 1, 0, 'BATCH-2025-010', '2025-08-15'),
    ('QR-B003', (SELECT product_id FROM products WHERE product_code = 'PROD-CONST-004'), @customer_b_id_item, N'Thùng vật liệu xây dựng', 'box', 0.70, 0.55, 0.40, 35.0, 'rectangle', 8, 1, 0, 'BATCH-2025-011', '2025-08-20'),
    ('QR-B004', (SELECT product_id FROM products WHERE product_code = 'PROD-ELEC-004'), @customer_b_id_item, N'Thùng thiết bị điện', 'box', 0.50, 0.45, 0.35, 16.5, 'rectangle', 4, 0, 0, 'BATCH-2025-012', '2025-09-05'),
    ('QR-B005', (SELECT product_id FROM products WHERE product_code = 'PROD-ELEC-003'), @customer_b_id_item, N'Thùng đèn LED', 'box', 0.55, 0.40, 0.30, 12.0, 'rectangle', 3, 0, 1, 'BATCH-2025-013', '2025-08-30'),
    ('QR-B006', (SELECT product_id FROM products WHERE product_code = 'PROD-CONST-002'), @customer_b_id_item, N'Bao cát xây dựng 40kg', 'bag', 0.70, 0.45, 0.20, 40.0, 'rectangle', 9, 1, 0, 'BATCH-2025-014', '2025-07-25');

-- Hàng của khách Mai (Customer C) - 6 items
INSERT INTO items (qr_code, product_id, customer_id, item_name, item_type, length, width, height, weight, shape, priority_level, is_heavy, is_fragile, batch_number, manufacturing_date, expiry_date)
VALUES 
    ('QR-C001', (SELECT product_id FROM products WHERE product_code = 'PROD-STAT-001'), @customer_c_id_item, N'Thùng sách giáo khoa', 'box', 0.50, 0.40, 0.35, 20.0, 'rectangle', 5, 0, 0, 'BATCH-2025-015', '2025-06-01', NULL),
    ('QR-C002', (SELECT product_id FROM products WHERE product_code = 'PROD-STAT-002'), @customer_c_id_item, N'Thùng văn phòng phẩm', 'box', 0.45, 0.35, 0.30, 9.0, 'rectangle', 2, 0, 0, 'BATCH-2025-016', '2025-07-10', NULL),
    ('QR-C003', (SELECT product_id FROM products WHERE product_code = 'PROD-MED-001'), @customer_c_id_item, N'Thùng thiết bị y tế', 'box', 0.40, 0.35, 0.28, 7.5, 'rectangle', 1, 0, 1, 'BATCH-2025-017', '2025-08-01', '2030-08-01'),
    ('QR-C004', (SELECT product_id FROM products WHERE product_code = 'PROD-MED-002'), @customer_c_id_item, N'Thùng dược phẩm', 'box', 0.38, 0.32, 0.25, 6.0, 'rectangle', 1, 0, 1, 'BATCH-2025-018', '2025-09-01', '2027-09-01'),
    ('QR-C005', (SELECT product_id FROM products WHERE product_code = 'PROD-HOME-001'), @customer_c_id_item, N'Thùng đồ gia dụng', 'box', 0.55, 0.45, 0.40, 13.5, 'rectangle', 6, 0, 0, 'BATCH-2025-019', '2025-08-10', NULL),
    ('QR-C006', (SELECT product_id FROM products WHERE product_code = 'PROD-FOOD-002'), @customer_c_id_item, N'Thùng thực phẩm đóng hộp', 'box', 0.50, 0.40, 0.35, 18.0, 'rectangle', 4, 0, 0, 'BATCH-2025-020', '2025-07-01', '2027-07-01');

GO

-- ===================================
-- 10. PHÂN BỔ HÀNG TRÊN PALLET
-- ===================================

-- Pallet 1: 3 hàng của khách A
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (1, 1, 0.0, 0.15, 0.0),   -- QR-A001
    (2, 1, 0.6, 0.15, 0.0),   -- QR-A002
    (3, 1, 0.0, 0.15, 0.5);   -- QR-A003

-- Pallet 2: 3 hàng của khách A
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (4, 2, 0.0, 0.15, 0.0),   -- QR-A004
    (5, 2, 0.45, 0.15, 0.0),  -- QR-A005
    (6, 2, 0.0, 0.15, 0.5);   -- QR-A006

-- Pallet 3: 2 hàng của khách A
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (7, 3, 0.0, 0.15, 0.0),   -- QR-A007
    (8, 3, 0.55, 0.15, 0.0);  -- QR-A008

-- Pallet 4: 3 hàng của khách B
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (9, 4, 0.0, 0.15, 0.0),   -- QR-B001
    (10, 4, 0.65, 0.15, 0.0), -- QR-B002
    (11, 4, 0.0, 0.15, 0.55); -- QR-B003

-- Pallet 5: 3 hàng của khách B
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (12, 5, 0.0, 0.15, 0.0),  -- QR-B004
    (13, 5, 0.55, 0.15, 0.0), -- QR-B005
    (14, 5, 0.0, 0.15, 0.45); -- QR-B006

-- Pallet 6: 2 hàng của khách C
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (15, 6, 0.0, 0.15, 0.0),  -- QR-C001
    (16, 6, 0.55, 0.15, 0.0); -- QR-C002

-- Pallet 7: 2 hàng của khách C
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (17, 7, 0.0, 0.15, 0.0),  -- QR-C003
    (18, 7, 0.45, 0.15, 0.0); -- QR-C004

-- Pallet 8: 2 hàng của khách C
INSERT INTO item_allocations (item_id, pallet_id, position_x, position_y, position_z)
VALUES 
    (19, 8, 0.0, 0.15, 0.0),  -- QR-C005
    (20, 8, 0.60, 0.15, 0.0); -- QR-C006

GO

-- ===================================
-- 11. PHIẾU NHẬP KHO (3 phiếu)
-- ===================================

DECLARE @admin_id INT = (SELECT account_id FROM accounts WHERE username = 'admin');
DECLARE @warehouse1 INT = (SELECT TOP 1 warehouse_id FROM warehouses WHERE warehouse_name LIKE N'%Quận 7%');
DECLARE @warehouse2 INT = (SELECT TOP 1 warehouse_id FROM warehouses WHERE warehouse_name LIKE N'%Bình Thạnh%');
DECLARE @cust_a INT = (SELECT account_id FROM accounts WHERE username = 'customer_a');
DECLARE @cust_b INT = (SELECT account_id FROM accounts WHERE username = 'customer_b');
DECLARE @cust_c INT = (SELECT account_id FROM accounts WHERE username = 'customer_c');

-- Phiếu nhập 1: Khách A
INSERT INTO inbound_receipts (warehouse_id, customer_id, receipt_number, total_items, total_pallets, inbound_date, notes, created_by, status)
VALUES 
    (@warehouse1, @cust_a, 'IN-20251001-001', 8, 3, '2025-10-01 08:30:00', N'Nhập hàng tháng 10 - Khách Lan', @admin_id, 'completed');

-- Phiếu nhập 2: Khách B
INSERT INTO inbound_receipts (warehouse_id, customer_id, receipt_number, total_items, total_pallets, inbound_date, notes, created_by, status)
VALUES 
    (@warehouse1, @cust_b, 'IN-20251015-002', 6, 2, '2025-10-15 10:15:00', N'Nhập vật liệu xây dựng - Khách Hùng', @admin_id, 'completed');

-- Phiếu nhập 3: Khách C
INSERT INTO inbound_receipts (warehouse_id, customer_id, receipt_number, total_items, total_pallets, inbound_date, notes, created_by, status)
VALUES 
    (@warehouse1, @cust_c, 'IN-20251020-003', 6, 3, '2025-10-20 14:45:00', N'Nhập hàng y tế và văn phòng phẩm - Khách Mai', @admin_id, 'completed');

GO

-- ===================================
-- 12. CHI TIẾT PHIẾU NHẬP
-- ===================================

-- Chi tiết phiếu 1 (Khách A - items 1-8)
INSERT INTO inbound_items (receipt_id, item_id, pallet_id, quantity)
VALUES 
    (1, 1, 1, 1), (1, 2, 1, 1), (1, 3, 1, 1),
    (1, 4, 2, 1), (1, 5, 2, 1), (1, 6, 2, 1),
    (1, 7, 3, 1), (1, 8, 3, 1);

-- Chi tiết phiếu 2 (Khách B - items 9-14)
INSERT INTO inbound_items (receipt_id, item_id, pallet_id, quantity)
VALUES 
    (2, 9, 4, 1), (2, 10, 4, 1), (2, 11, 4, 1),
    (2, 12, 5, 1)

PRINT 'Database WarehouseAPI đã được tạo thành công cho SQL Server 2022!';
GO