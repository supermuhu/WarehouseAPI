-- ===================================
-- XÓA TẤT CẢ DỮ LIỆU TRỪ BẢNG ACCOUNTS
-- Thứ tự xóa theo đúng chuẩn liên kết FK
-- ===================================

USE WarehouseAPI;
GO

-- Tắt kiểm tra FK tạm thời (optional, nhưng an toàn hơn)
-- EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'

-- ===================================
-- 1. XÓA CÁC BẢNG CON TRƯỚC (không có bảng nào tham chiếu đến)
-- ===================================

-- Xóa inbound_item_stack_units (tham chiếu đến inbound_items)
DELETE FROM inbound_item_stack_units;

-- Xóa item_location_history (tham chiếu đến items, pallets, pallet_locations, accounts)
DELETE FROM item_location_history;

-- Xóa item_allocations (tham chiếu đến items, pallets)
DELETE FROM item_allocations;

-- Xóa outbound_items (tham chiếu đến outbound_receipts, items)
DELETE FROM outbound_items;

-- Xóa outbound_pallet_picks (tham chiếu đến outbound_receipts, pallets, accounts)
DELETE FROM outbound_pallet_picks;

-- Xóa inbound_items (tham chiếu đến inbound_receipts, items, pallets)
DELETE FROM inbound_items;

-- ===================================
-- 2. XÓA CÁC BẢNG TRUNG GIAN
-- ===================================

-- Xóa pallet_locations (tham chiếu đến pallets, warehouse_zones, shelves)
DELETE FROM pallet_locations;

-- Xóa shelves (tham chiếu đến racks)
DELETE FROM shelves;

-- Xóa racks (tham chiếu đến warehouse_zones)
DELETE FROM racks;

-- Xóa items (tham chiếu đến products, accounts)
DELETE FROM items;

-- ===================================
-- 3. XÓA CÁC BẢNG RECEIPTS
-- ===================================

-- Xóa inbound_receipts (tham chiếu đến warehouses, warehouse_zones, accounts)
DELETE FROM inbound_receipts;

-- Xóa outbound_receipts (tham chiếu đến warehouses, accounts)
DELETE FROM outbound_receipts;

-- ===================================
-- 4. XÓA CÁC BẢNG CHA
-- ===================================

-- Xóa pallets
DELETE FROM pallets;

-- Xóa pallet_templates
DELETE FROM pallet_templates;

-- Xóa products (tham chiếu đến accounts)
DELETE FROM products;

-- Xóa checkin_stations (tham chiếu đến warehouse_gates)
DELETE FROM checkin_stations;

-- Xóa warehouse_gates (tham chiếu đến warehouses)
DELETE FROM warehouse_gates;

-- Xóa warehouse_zones (tham chiếu đến warehouses, accounts)
DELETE FROM warehouse_zones;

-- Xóa warehouses (tham chiếu đến accounts)
DELETE FROM warehouses;

-- ===================================
-- 5. RESET IDENTITY (Optional - đặt lại ID về 1)
-- ===================================

DBCC CHECKIDENT ('inbound_item_stack_units', RESEED, 0);
DBCC CHECKIDENT ('item_location_history', RESEED, 0);
DBCC CHECKIDENT ('item_allocations', RESEED, 0);
DBCC CHECKIDENT ('outbound_items', RESEED, 0);
DBCC CHECKIDENT ('outbound_pallet_picks', RESEED, 0);
DBCC CHECKIDENT ('inbound_items', RESEED, 0);
DBCC CHECKIDENT ('pallet_locations', RESEED, 0);
DBCC CHECKIDENT ('shelves', RESEED, 0);
DBCC CHECKIDENT ('racks', RESEED, 0);
DBCC CHECKIDENT ('items', RESEED, 0);
DBCC CHECKIDENT ('inbound_receipts', RESEED, 0);
DBCC CHECKIDENT ('outbound_receipts', RESEED, 0);
DBCC CHECKIDENT ('pallets', RESEED, 0);
DBCC CHECKIDENT ('pallet_templates', RESEED, 0);
DBCC CHECKIDENT ('products', RESEED, 0);
DBCC CHECKIDENT ('checkin_stations', RESEED, 0);
DBCC CHECKIDENT ('warehouse_gates', RESEED, 0);
DBCC CHECKIDENT ('warehouse_zones', RESEED, 0);
DBCC CHECKIDENT ('warehouses', RESEED, 0);

-- Bật lại kiểm tra FK (nếu đã tắt)
-- EXEC sp_MSforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'

PRINT N'Đã xóa tất cả dữ liệu trừ bảng accounts!';
GO