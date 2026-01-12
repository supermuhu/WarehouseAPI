-- ===================================
-- HỆ THỐNG QUẢN LÝ KHO 3D - SQL SERVER 2022
-- ===================================

-- Tạo Database
use master
go
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

-- 1.1. DỮ LIỆU TÀI KHOẢN MẪU
IF NOT EXISTS (SELECT 1 FROM accounts WHERE username = 'admin')
BEGIN
    INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
    VALUES ('admin', 'admin123', N'Quản trị hệ thống', 'admin@example.com', '0900000000', 'admin', 'active');
END

IF NOT EXISTS (SELECT 1 FROM accounts WHERE username = 'chukhoa')
BEGIN
    INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
    VALUES ('chukhoa', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Chủ kho', 'owner@example.com', '0900000001', 'warehouse_owner', 'active');
END

IF NOT EXISTS (SELECT 1 FROM accounts WHERE username = 'customer_a')
BEGIN
    INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
    VALUES ('customer_a', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Khách hàng A - Lan', 'customer_a@example.com', '0900000002', 'customer', 'active');
END

IF NOT EXISTS (SELECT 1 FROM accounts WHERE username = 'customer_b')
BEGIN
    INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
    VALUES ('customer_b', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Khách hàng B - Hùng', 'customer_b@example.com', '0900000003', 'customer', 'active');
END

IF NOT EXISTS (SELECT 1 FROM accounts WHERE username = 'customer_c')
BEGIN
    INSERT INTO accounts (username, password_hash, full_name, email, phone, role, status)
    VALUES ('customer_c', '$2a$11$BWNG5pW0X8ywfQyHsMYUBeEU8sm5kuQoDK40I.c7OA0CcvHskB..2', N'Khách hàng C - Mai', 'customer_c@example.com', '0900000004', 'customer', 'active');
END

GO

-- 2. BẢNG KHO
-- ===================================
IF OBJECT_ID('warehouses', 'U') IS NOT NULL DROP TABLE warehouses;
GO

CREATE TABLE warehouses (
    warehouse_id INT IDENTITY(1,1) PRIMARY KEY,
    owner_id INT NOT NULL,
    warehouse_name NVARCHAR(200) NOT NULL,
    address NVARCHAR(500) NULL,

    length DECIMAL(10,2) NOT NULL, -- mét
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    warehouse_type VARCHAR(20) NOT NULL CHECK (warehouse_type IN ('small', 'medium', 'large')),
    -- small: chỉ hàng bao
    -- medium: bao hoặc thùng
    -- large: tất cả loại
    allowed_item_types NVARCHAR(MAX), -- JSON string ['bag', 'box', 'pallet']
    is_rentable BIT NOT NULL DEFAULT 1,
    checkin_position_x DECIMAL(10,2) NULL,
    checkin_position_y DECIMAL(10,2) NULL,
    checkin_position_z DECIMAL(10,2) NULL,
    checkin_length DECIMAL(10,2) NULL,
    checkin_width DECIMAL(10,2) NULL,
    checkin_height DECIMAL(10,2) NULL,
    created_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'inactive')),
    CONSTRAINT FK_warehouses_owner FOREIGN KEY (owner_id) REFERENCES accounts(account_id)
);
GO

IF OBJECT_ID('warehouse_gates', 'U') IS NOT NULL DROP TABLE warehouse_gates;
GO

CREATE TABLE warehouse_gates (
    gate_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    gate_name NVARCHAR(200),
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    length DECIMAL(10,2) NULL,
    width DECIMAL(10,2) NULL,
    height DECIMAL(10,2) NULL,
    gate_type VARCHAR(20) NOT NULL CHECK (gate_type IN ('entry', 'exit')),
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_warehouse_gates_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id)
);
GO

-- ===================================
-- 2. KHO (2 kho: 1 lớn, 1 vừa)
-- ===================================

DECLARE @owner_id INT = (SELECT account_id FROM accounts WHERE username = 'chukhoa');

INSERT INTO warehouses (owner_id, warehouse_name, address, length, width, height, warehouse_type, allowed_item_types, status, is_rentable)
VALUES 
    (@owner_id, N'Kho Trung Tâm - Quận 7', N'Quận 7, TP. Hồ Chí Minh', 50.00, 30.00, 8.00, 'large', '["bag", "box", "pallet"]', 'active', 1),
    (@owner_id, N'Kho Chi Nhánh - Bình Thạnh', N'Quận Bình Thạnh, TP. Hồ Chí Minh', 30.00, 20.00, 5.00, 'medium', '["bag", "box"]', 'active', 1);

UPDATE warehouses
SET checkin_position_x = 3.00,
    checkin_position_y = 0.00,
    checkin_position_z = 3.00,
    checkin_length = 1.50,
    checkin_width = 0.80,
    checkin_height = 1.00
WHERE warehouse_name = N'Kho Trung Tâm - Quận 7';

UPDATE warehouses
SET checkin_position_x = 2.50,
    checkin_position_y = 0.00,
    checkin_position_z = 3.00,
    checkin_length = 1.20,
    checkin_width = 0.70,
    checkin_height = 0.90
WHERE warehouse_name = N'Kho Chi Nhánh - Bình Thạnh';

GO

-- Hiển thị kho
SELECT warehouse_id, warehouse_name, warehouse_type, 
       CONCAT(length, 'm x ', width, 'm x ', height, 'm') AS dimensions
FROM warehouses;
GO

-- ===================================
-- TẠO CẤU TRÚC BẢNG ZONE/RACK/PALLET/HÀNG HÓA/PHIẾU
-- (dùng được khi tạo database mới từ đầu)
-- ===================================

-- Khu vực trong kho
CREATE TABLE warehouse_zones (
    zone_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    zone_name NVARCHAR(200),
    customer_id INT NULL,
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    zone_type VARCHAR(20) NOT NULL,
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_zones_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_zones_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id)
);
GO

CREATE INDEX IX_zones_customer ON warehouse_zones(customer_id);
CREATE INDEX IX_zones_warehouse ON warehouse_zones(warehouse_id);
GO

-- Kệ trong khu vực
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

CREATE INDEX IX_racks_zone ON racks(zone_id);
GO

-- Tầng kệ
CREATE TABLE shelves (
    shelf_id INT IDENTITY(1,1) PRIMARY KEY,
    rack_id INT NOT NULL,
    shelf_level INT NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    max_weight DECIMAL(10,2) NULL,
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_shelves_rack FOREIGN KEY (rack_id) REFERENCES racks(rack_id)
);
GO

CREATE INDEX IX_shelves_rack ON shelves(rack_id);
CREATE UNIQUE INDEX UQ_shelf_level ON shelves(rack_id, shelf_level);
GO

-- Mẫu pallet
CREATE TABLE pallet_templates (
    template_id INT IDENTITY(1,1) PRIMARY KEY,
    template_name NVARCHAR(200) NOT NULL,
    pallet_type NVARCHAR(50),
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    max_weight DECIMAL(10,2) NOT NULL,
    max_stack_height DECIMAL(10,2) NOT NULL,
    description NVARCHAR(MAX),
    is_active BIT DEFAULT 1,
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE()
);
GO

CREATE OR ALTER TRIGGER trg_pallet_templates_updated_at
ON pallet_templates
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE pallet_templates
    SET updated_at = GETDATE()
    FROM pallet_templates pt
    INNER JOIN inserted i ON pt.template_id = i.template_id;
END
GO

-- Pallet
CREATE TABLE pallets (
    pallet_id INT IDENTITY(1,1) PRIMARY KEY,
    barcode VARCHAR(100) NOT NULL UNIQUE,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL DEFAULT 0.15,
    max_weight DECIMAL(10,2) NOT NULL DEFAULT 1000,
    max_stack_height DECIMAL(10,2) NOT NULL DEFAULT 1.5,
    pallet_type NVARCHAR(50),
    created_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) NOT NULL DEFAULT 'available'
);
GO

-- Vị trí pallet
CREATE TABLE pallet_locations (
    location_id INT IDENTITY(1,1) PRIMARY KEY,
    pallet_id INT NOT NULL,
    zone_id INT NOT NULL,
    shelf_id INT NULL,
    position_x DECIMAL(10,2) NOT NULL,
    position_y DECIMAL(10,2) NOT NULL,
    position_z DECIMAL(10,2) NOT NULL,
    rotation_y DECIMAL(10,4) NOT NULL DEFAULT 0,
    stack_level INT NOT NULL DEFAULT 1,
    stacked_on_pallet INT NULL,
    is_ground BIT NOT NULL DEFAULT 0,
    assigned_at DATETIME2 DEFAULT GETDATE(),
    location_code AS ('LOC-' + RIGHT('000000' + CAST([location_id] AS VARCHAR(6)), 6)) PERSISTED,
    CONSTRAINT FK_pallet_locations_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id),
    CONSTRAINT FK_pallet_locations_zone FOREIGN KEY (zone_id) REFERENCES warehouse_zones(zone_id),
    CONSTRAINT FK_pallet_locations_shelf FOREIGN KEY (shelf_id) REFERENCES shelves(shelf_id),
    CONSTRAINT FK_pallet_locations_stacked FOREIGN KEY (stacked_on_pallet) REFERENCES pallets(pallet_id)
);
GO

CREATE INDEX IX_pallet_locations_shelf ON pallet_locations(shelf_id);
CREATE INDEX IX_pallet_locations_zone ON pallet_locations(zone_id);
GO

-- Sản phẩm
CREATE TABLE products (
    product_id INT IDENTITY(1,1) PRIMARY KEY,
    product_code VARCHAR(100) NOT NULL UNIQUE,
    product_name NVARCHAR(300) NOT NULL,
    description NVARCHAR(MAX),
    unit NVARCHAR(50) NOT NULL,
    category NVARCHAR(100),
    standard_length DECIMAL(10,2),
    standard_width DECIMAL(10,2),
    standard_height DECIMAL(10,2),
    standard_weight DECIMAL(10,2),
    is_fragile BIT NOT NULL DEFAULT 0,
    is_hazardous BIT NOT NULL DEFAULT 0,
    is_non_stackable BIT NOT NULL DEFAULT 0,
    storage_conditions NVARCHAR(MAX),
    create_user INT NULL,
    created_at DATETIME2 DEFAULT GETDATE(),
    updated_at DATETIME2 DEFAULT GETDATE(),
    status VARCHAR(20) NOT NULL DEFAULT 'active',
    CONSTRAINT FK_products_create_user FOREIGN KEY (create_user) REFERENCES accounts(account_id)
);
GO

CREATE INDEX IX_products_category ON products(category);
GO

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

-- Hàng hóa (items)
CREATE TABLE items (
    item_id INT IDENTITY(1,1) PRIMARY KEY,
    qr_code VARCHAR(100) NOT NULL UNIQUE,
    product_id INT NOT NULL,
    customer_id INT NOT NULL,
    item_name NVARCHAR(300) NOT NULL,
    item_type VARCHAR(20) NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    weight DECIMAL(10,2),
    shape VARCHAR(20) NOT NULL DEFAULT 'rectangle',
    priority_level INT NOT NULL DEFAULT 5,
    is_heavy BIT NOT NULL DEFAULT 0,
    is_fragile BIT NOT NULL DEFAULT 0,
    is_non_stackable BIT NOT NULL DEFAULT 0,
    batch_number NVARCHAR(100),
    manufacturing_date DATE,
    expiry_date DATE,
    unit_price DECIMAL(18,2),
    total_amount DECIMAL(18,2),
    created_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_items_product FOREIGN KEY (product_id) REFERENCES products(product_id),
    CONSTRAINT FK_items_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id)
);
GO

CREATE INDEX IX_items_customer ON items(customer_id);
CREATE INDEX IX_items_product ON items(product_id);
GO

-- Phân bổ hàng trên pallet
CREATE TABLE item_allocations (
    allocation_id INT IDENTITY(1,1) PRIMARY KEY,
    item_id INT NOT NULL,
    pallet_id INT NOT NULL,
    position_x DECIMAL(10,2),
    position_y DECIMAL(10,2),
    position_z DECIMAL(10,2),
    allocated_at DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_item_allocations_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_item_allocations_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id)
);
CREATE INDEX IX_item_allocations_pallet ON item_allocations(pallet_id);
GO

-- Lịch sử di chuyển / xuất kho của từng mặt hàng
IF OBJECT_ID('item_location_history', 'U') IS NULL
BEGIN
    CREATE TABLE item_location_history (
        history_id   INT IDENTITY(1,1) PRIMARY KEY,
        item_id      INT NOT NULL,
        pallet_id    INT NULL,
        location_id  INT NULL,
        action_type  VARCHAR(20) NOT NULL,
        action_date  DATETIME2 DEFAULT GETDATE(),
        performed_by INT NOT NULL,
        notes        NVARCHAR(MAX) NULL,
        CONSTRAINT FK_item_location_history_item      FOREIGN KEY (item_id)     REFERENCES items(item_id),
        CONSTRAINT FK_item_location_history_pallet    FOREIGN KEY (pallet_id)   REFERENCES pallets(pallet_id),
        CONSTRAINT FK_item_location_history_location  FOREIGN KEY (location_id) REFERENCES pallet_locations(location_id),
        CONSTRAINT FK_item_location_history_account   FOREIGN KEY (performed_by) REFERENCES accounts(account_id)
    );
END;
GO

-- Phiếu nhập kho
CREATE TABLE inbound_receipts (
    receipt_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    zone_id INT NULL,
    customer_id INT NOT NULL,
    receipt_number VARCHAR(100) NOT NULL UNIQUE,
    total_items INT NOT NULL,
    total_pallets INT NOT NULL,
    inbound_date DATETIME2 DEFAULT GETDATE(),
    notes NVARCHAR(MAX),
    created_by INT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    auto_stack_template VARCHAR(50) NULL,
    stack_mode VARCHAR(20) NOT NULL DEFAULT 'auto' CHECK (stack_mode IN ('auto', 'manual')),
    CONSTRAINT FK_inbound_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_inbound_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id),
    CONSTRAINT FK_inbound_created_by FOREIGN KEY (created_by) REFERENCES accounts(account_id)
);
GO


CREATE INDEX IX_inbound_customer ON inbound_receipts(customer_id);
CREATE INDEX IX_inbound_date ON inbound_receipts(inbound_date);
CREATE INDEX IX_inbound_warehouse ON inbound_receipts(warehouse_id);
GO

-- Chi tiết phiếu nhập
CREATE TABLE inbound_items (
    inbound_item_id INT IDENTITY(1,1) PRIMARY KEY,
    receipt_id INT NOT NULL,
    item_id INT NOT NULL,
    pallet_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    stack_mode VARCHAR(20) NOT NULL DEFAULT 'auto' CHECK (stack_mode IN ('auto', 'manual')),
    CONSTRAINT FK_inbound_items_receipt FOREIGN KEY (receipt_id) REFERENCES inbound_receipts(receipt_id),
    CONSTRAINT FK_inbound_items_item FOREIGN KEY (item_id) REFERENCES items(item_id),
    CONSTRAINT FK_inbound_items_pallet FOREIGN KEY (pallet_id) REFERENCES pallets(pallet_id)
);
GO

CREATE TABLE inbound_item_stack_units (
    layout_id INT IDENTITY(1,1) PRIMARY KEY,
    inbound_item_id INT NOT NULL,
    unit_index INT NOT NULL,
    local_x DECIMAL(10,2) NOT NULL,
    local_y DECIMAL(10,2) NOT NULL,
    local_z DECIMAL(10,2) NOT NULL,
    length DECIMAL(10,2) NOT NULL,
    width DECIMAL(10,2) NOT NULL,
    height DECIMAL(10,2) NOT NULL,
    rotation_y DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_inbound_item_stack_units_inbound_item FOREIGN KEY (inbound_item_id) REFERENCES inbound_items(inbound_item_id)
);
GO

CREATE INDEX IX_inbound_item_stack_units_inbound_item ON inbound_item_stack_units(inbound_item_id);
GO

-- Phiếu xuất kho
CREATE TABLE outbound_receipts (
    receipt_id INT IDENTITY(1,1) PRIMARY KEY,
    warehouse_id INT NOT NULL,
    customer_id INT NOT NULL,
    receipt_number VARCHAR(100) NOT NULL UNIQUE,
    total_items INT NOT NULL,
    outbound_date DATETIME2 DEFAULT GETDATE(),
    completed_date DATETIME2 NULL,
    notes NVARCHAR(MAX),
    created_by INT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    CONSTRAINT FK_outbound_warehouse FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    CONSTRAINT FK_outbound_customer FOREIGN KEY (customer_id) REFERENCES accounts(account_id),
    CONSTRAINT FK_outbound_created_by FOREIGN KEY (created_by) REFERENCES accounts(account_id)
);
GO

-- Chi tiết phiếu xuất kho
CREATE TABLE outbound_items (
    outbound_item_id INT IDENTITY(1,1) PRIMARY KEY,
    receipt_id       INT NOT NULL,
    item_id          INT NOT NULL,
    quantity         INT NULL DEFAULT 1,
    removed_at       DATETIME NULL DEFAULT GETDATE(),
    CONSTRAINT FK_outbound_items_receipt FOREIGN KEY (receipt_id) REFERENCES outbound_receipts(receipt_id),
    CONSTRAINT FK_outbound_items_item    FOREIGN KEY (item_id)    REFERENCES items(item_id)
);
GO

-- Trạng thái pallet đã được lấy cho phiếu xuất
CREATE TABLE outbound_pallet_picks (
    pick_id    INT IDENTITY(1,1) PRIMARY KEY,
    receipt_id INT NOT NULL,
    pallet_id  INT NOT NULL,
    picked_at  DATETIME2 DEFAULT GETDATE(),
    picked_by  INT NOT NULL,
    notes      NVARCHAR(MAX) NULL,
    CONSTRAINT FK_outbound_pallet_picks_receipt FOREIGN KEY (receipt_id) REFERENCES outbound_receipts(receipt_id),
    CONSTRAINT FK_outbound_pallet_picks_pallet  FOREIGN KEY (pallet_id)  REFERENCES pallets(pallet_id),
    CONSTRAINT FK_outbound_pallet_picks_account FOREIGN KEY (picked_by)  REFERENCES accounts(account_id)
);
GO

CREATE UNIQUE INDEX UX_outbound_pallet_picks_receipt_pallet
    ON outbound_pallet_picks(receipt_id, pallet_id);
GO

-- Lấy ID kho để seed cổng
DECLARE @warehouse1_id INT = (SELECT TOP 1 warehouse_id FROM warehouses WHERE warehouse_name LIKE N'%Quận 7%');
DECLARE @warehouse2_id INT = (SELECT TOP 1 warehouse_id FROM warehouses WHERE warehouse_name LIKE N'%Bình Thạnh%');

INSERT INTO warehouse_gates (warehouse_id, gate_name, position_x, position_y, position_z, length, width, height, gate_type)
VALUES 
    (@warehouse1_id, N'Cổng ra 1', 1.50, 0.00, 1.50, 2.50, 0.30, 2.40, 'exit'),
    (@warehouse1_id, N'Cổng ra 2', 48.50, 0.00, 1.50, 3.00, 0.30, 2.40, 'exit'),
    (@warehouse2_id, N'Cổng ra 1', 3.00, 0.00, 1.50, 2.00, 0.25, 2.20, 'exit');

DECLARE @customer_a_id INT = (SELECT account_id FROM accounts WHERE username = 'customer_a');
DECLARE @customer_b_id INT = (SELECT account_id FROM accounts WHERE username = 'customer_b');

DECLARE @customer_c_id INT = (SELECT account_id FROM accounts WHERE username = 'customer_c');

-- Kho 1: Chia 3 khu cho 3 khách hàng, khoảng cách giữa Khu A - B - C ~ 2m
INSERT INTO warehouse_zones (warehouse_id, zone_name, customer_id, position_x, position_y, position_z, length, width, height, zone_type)
VALUES 
    (@warehouse1_id, N'Khu A - Khách Lan', @customer_a_id, 5.0, 0.0, 5.0, 15.0, 12.0, 8.0, 'ground'),
    -- Khu B bắt đầu sau Khu A 2m: A từ x=5..20, B từ x=22..37
    (@warehouse1_id, N'Khu B - Khách Hùng', @customer_b_id, 22.0, 0.0, 5.0, 15.0, 12.0, 8.0, 'ground'),
    -- Khu B Rack cùng khoảng x với Khu B ground
    (@warehouse1_id, N'Khu B Rack - Khách Hùng', @customer_b_id, 22.0, 0.0, 19.0, 15.0, 8.0, 8.0, 'rack'),
    -- Khu C bắt đầu sau Khu B 2m: B từ x=22..37, C từ x=39..50
    (@warehouse1_id, N'Khu C - Khách Mai', @customer_c_id, 39.0, 0.0, 5.0, 11.0, 12.0, 8.0, 'rack');

-- Kho 2: Chia 2 khu
INSERT INTO warehouse_zones (warehouse_id, zone_name, customer_id, position_x, position_y, position_z, length, width, height, zone_type)
VALUES 
    (@warehouse2_id, N'Khu D - Khách Lan', @customer_a_id, 5.0, 0.0, 5.0, 12.0, 10.0, 5.0, 'ground'),
    (@warehouse2_id, N'Khu E - Khách Hùng', @customer_b_id, 19.0, 0.0, 5.0, 12.0, 10.0, 5.0, 'rack');

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
DECLARE @zone_b_rack_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu B Rack%');
DECLARE @zone_e_id INT = (SELECT zone_id FROM warehouse_zones WHERE zone_name LIKE N'%Khu E%');

-- Kệ cho Khu C
INSERT INTO racks (zone_id, rack_name, position_x, position_y, position_z, length, width, height, max_shelves)
VALUES 
    (@zone_c_id, N'Kệ C1', 46.0, 0.0, 6.0, 4.0, 1.5, 6.0, 4),
    (@zone_c_id, N'Kệ C2', 51.0, 0.0, 6.0, 4.0, 1.5, 6.0, 4),
    (@zone_c_id, N'Kệ C3', 56.0, 0.0, 6.0, 4.0, 1.5, 6.0, 4);

-- Kệ cho Khu B Rack - Khách Hùng
INSERT INTO racks (zone_id, rack_name, position_x, position_y, position_z, length, width, height, max_shelves)
VALUES 
    (@zone_b_rack_id, N'Kệ B1', 26.0, 0.0, 21.0, 4.0, 1.5, 6.0, 3),
    (@zone_b_rack_id, N'Kệ B2', 31.0, 0.0, 21.0, 4.0, 1.5, 6.0, 3);

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
DECLARE @rack_b1_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ B1');
DECLARE @rack_b2_id INT = (SELECT rack_id FROM racks WHERE rack_name = N'Kệ B2');
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

-- Tầng cho Kệ B1/B2 (Khu B Rack - Khách Hùng)
INSERT INTO shelves (rack_id, shelf_level, position_y, length, width, max_weight)
VALUES 
    (@rack_b1_id, 1, 0.5, 4.0, 1.5, 500),
    (@rack_b1_id, 2, 2.0, 4.0, 1.5, 400),
    (@rack_b1_id, 3, 3.5, 4.0, 1.5, 300),
    (@rack_b2_id, 1, 0.5, 4.0, 1.5, 500),
    (@rack_b2_id, 2, 2.0, 4.0, 1.5, 400),
    (@rack_b2_id, 3, 3.5, 4.0, 1.5, 300);

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
-- 6. PALLET TEMPLATES (Mẫu pallet có sẵn)
-- ===================================

INSERT INTO pallet_templates (
    template_name, pallet_type, length, width, height, 
    max_weight, max_stack_height, description, is_active
)
VALUES 
    -- Pallet tiêu chuẩn Châu Âu
    (N'Pallet tiêu chuẩn 1200x800mm', 'Standard', 1.20, 0.80, 0.15, 1000, 1.5, 
     N'Pallet tiêu chuẩn Châu Âu, kích thước 1200x800mm, phù hợp cho hầu hết các loại hàng hóa', 1),
    
    -- Pallet tiêu chuẩn Mỹ
    (N'Pallet tiêu chuẩn 1219x1016mm', 'Standard', 1.219, 1.016, 0.15, 1200, 1.5,
     N'Pallet tiêu chuẩn Mỹ, kích thước 1219x1016mm (48x40 inch)', 1),
    
    -- Pallet nhỏ
    (N'Pallet nhỏ 1000x600mm', 'Small', 1.00, 0.60, 0.15, 500, 1.2,
     N'Pallet nhỏ, phù hợp cho hàng hóa có kích thước nhỏ hoặc trọng lượng nhẹ', 1),
    
    -- Pallet lớn
    (N'Pallet lớn 1400x1200mm', 'Large', 1.40, 1.20, 0.15, 1500, 1.8,
     N'Pallet lớn, phù hợp cho hàng hóa có kích thước lớn hoặc trọng lượng nặng', 1),
    
    -- Pallet siêu nhỏ
    (N'Pallet siêu nhỏ 800x600mm', 'Extra Small', 0.80, 0.60, 0.12, 300, 1.0,
     N'Pallet siêu nhỏ, phù hợp cho hàng hóa nhẹ và nhỏ gọn', 1),
    
    -- Pallet chuyên dụng cho container
    (N'Pallet container 1200x1000mm', 'Container', 1.20, 1.00, 0.15, 1200, 1.5,
     N'Pallet chuyên dụng cho container, kích thước tối ưu cho vận chuyển', 1);

GO

-- ===================================
-- 8. SẢN PHẨM (Products)
-- ===================================

DECLARE @admin_id_product INT = (SELECT account_id FROM accounts WHERE username = 'admin');

INSERT INTO products (product_code, product_name, description, unit, category, standard_length, standard_width, standard_height, standard_weight, is_fragile, is_hazardous, storage_conditions, create_user, status)
VALUES 
    -- Điện tử
    ('PROD-ELEC-001', N'Đồ điện tử Samsung', N'Các sản phẩm điện tử Samsung bao gồm điện thoại, tablet, phụ kiện', N'Thùng', N'Điện tử', 0.50, 0.40, 0.35, 18.5, 1, 0, N'Tránh ẩm, nhiệt độ phòng', @admin_id_product, 'active'),
    ('PROD-ELEC-002', N'Linh kiện máy tính', N'Linh kiện máy tính như RAM, ổ cứng, card màn hình', N'Thùng', N'Điện tử', 0.45, 0.40, 0.30, 14.0, 1, 0, N'Tránh ẩm, tĩnh điện', @admin_id_product, 'active'),
    ('PROD-ELEC-003', N'Đèn LED', N'Đèn LED chiếu sáng các loại', N'Thùng', N'Điện tử', 0.55, 0.40, 0.30, 12.0, 1, 0, N'Tránh va đập', @admin_id_product, 'active'),
    ('PROD-ELEC-004', N'Thiết bị điện', N'Thiết bị điện công nghiệp', N'Thùng', N'Điện tử', 0.50, 0.45, 0.35, 16.5, 0, 0, N'Khô ráo', @admin_id_product, 'active'),
    
    -- Thực phẩm
    ('PROD-FOOD-001', N'Gạo ST25', N'Gạo ST25 cao cấp', N'Bao 50kg', N'Thực phẩm', 0.80, 0.50, 0.20, 50.0, 0, 0, N'Khô ráo, thoáng mát', @admin_id_product, 'active'),
    ('PROD-FOOD-002', N'Thực phẩm đóng hộp', N'Thực phẩm đóng hộp các loại', N'Thùng', N'Thực phẩm', 0.50, 0.40, 0.35, 18.0, 0, 0, N'Nơi khô ráo, tránh ánh sáng', @admin_id_product, 'active'),
    
    -- Vật liệu xây dựng
    ('PROD-CONST-001', N'Xi măng', N'Xi măng PCB40', N'Bao 50kg', N'Vật liệu xây dựng', 0.75, 0.48, 0.18, 50.0, 0, 0, N'Tránh ẩm', @admin_id_product, 'active'),
    ('PROD-CONST-002', N'Cát xây dựng', N'Cát xây dựng đã sàng', N'Bao 40kg', N'Vật liệu xây dựng', 0.70, 0.45, 0.20, 40.0, 0, 0, N'Bảo quản khô', @admin_id_product, 'active'),
    ('PROD-CONST-003', N'Phân bón', N'Phân bón NPK', N'Bao 40kg', N'Vật liệu xây dựng', 0.70, 0.45, 0.18, 40.0, 0, 0, N'Khô ráo, thoáng mát', @admin_id_product, 'active'),
    ('PROD-CONST-004', N'Vật liệu xây dựng tổng hợp', N'Các loại vật liệu xây dựng khác', N'Thùng', N'Vật liệu xây dựng', 0.70, 0.55, 0.40, 35.0, 0, 0, N'Bình thường', @admin_id_product, 'active'),
    ('PROD-CONST-005', N'Dụng cụ cơ khí', N'Dụng cụ và thiết bị cơ khí', N'Thùng', N'Vật liệu xây dựng', 0.60, 0.50, 0.45, 25.0, 0, 0, N'Khô ráo', @admin_id_product, 'active'),
    
    -- Thời trang
    ('PROD-FASH-001', N'Quần áo xuất khẩu', N'Quần áo may mặc xuất khẩu', N'Thùng', N'Thời trang', 0.60, 0.45, 0.40, 12.0, 0, 0, N'Khô ráo, thoáng mát', @admin_id_product, 'active'),
    ('PROD-FASH-002', N'Giày thể thao', N'Giày thể thao các loại', N'Thùng', N'Thời trang', 0.55, 0.40, 0.35, 15.0, 0, 0, N'Tránh ẩm', @admin_id_product, 'active'),
    
    -- Mỹ phẩm
    ('PROD-COSM-001', N'Mỹ phẩm cao cấp', N'Mỹ phẩm chăm sóc da cao cấp', N'Thùng', N'Mỹ phẩm', 0.40, 0.35, 0.30, 8.5, 1, 0, N'Nhiệt độ phòng, tránh ánh sáng', @admin_id_product, 'active'),
    
    -- Đồ chơi
    ('PROD-TOY-001', N'Đồ chơi trẻ em', N'Đồ chơi an toàn cho trẻ em', N'Thùng', N'Đồ chơi', 0.50, 0.45, 0.40, 10.0, 0, 0, N'Khô ráo', @admin_id_product, 'active'),
    
    -- Văn phòng phẩm
    ('PROD-STAT-001', N'Sách giáo khoa', N'Sách giáo khoa các cấp', N'Thùng', N'Văn phòng phẩm', 0.50, 0.40, 0.35, 20.0, 0, 0, N'Tránh ẩm', @admin_id_product, 'active'),
    ('PROD-STAT-002', N'Văn phòng phẩm', N'Văn phòng phẩm văn phòng', N'Thùng', N'Văn phòng phẩm', 0.45, 0.35, 0.30, 9.0, 0, 0, N'Bình thường', @admin_id_product, 'active'),
    
    -- Y tế & Dược phẩm
    ('PROD-MED-001', N'Thiết bị y tế', N'Thiết bị y tế chuyên dụng', N'Thùng', N'Y tế', 0.40, 0.35, 0.28, 7.5, 1, 0, N'Nhiệt độ 15-25°C, tránh ẩm', @admin_id_product, 'active'),
    ('PROD-MED-002', N'Dược phẩm', N'Dược phẩm và thuốc men', N'Thùng', N'Dược phẩm', 0.38, 0.32, 0.25, 6.0, 1, 0, N'Nhiệt độ 2-8°C, tránh ánh sáng', @admin_id_product, 'active'),
    
    -- Đồ gia dụng
    ('PROD-HOME-001', N'Đồ gia dụng', N'Đồ gia dụng các loại', N'Thùng', N'Gia dụng', 0.55, 0.45, 0.40, 13.5, 0, 0, N'Bình thường', @admin_id_product, 'active');

GO

-- ===================================
-- 9. HÀNG HÓA (Items)
-- ===================================

DECLARE @customer_a_id_item INT = (SELECT account_id FROM accounts WHERE username = 'customer_a');
DECLARE @customer_b_id_item INT = (SELECT account_id FROM accounts WHERE username = 'customer_b');
DECLARE @customer_c_id_item INT = (SELECT account_id FROM accounts WHERE username = 'customer_c');

GO

-- ===================================
-- 10. PHÂN BỔ HÀNG TRÊN PALLET
-- ===================================

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

GO

-- ===================================
-- 12. CHI TIẾT PHIẾU NHẬP
-- ===================================

PRINT 'Database WarehouseAPI đã được tạo thành công cho SQL Server 2022!';
GO