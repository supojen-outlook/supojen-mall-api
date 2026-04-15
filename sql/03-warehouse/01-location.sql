-- =============================================================================
-- locations：儲位表
-- 用途：定義倉庫內的所有實體位置，包含區域、儲位等
-- 設計考量：目前只有單一倉庫，最高層級為區域 (ZONE)，不再有 DEPOT 層級
-- 注意：資料量 < 1000 筆，索引以精簡為原則
-- =============================================================================
CREATE TABLE locations (
    -- 基礎資訊欄位 (Basic Information)
    -- -------------------------------------------------------------------------
    id              INT NOT NULL,                          -- 位置唯一識別碼 (Unique location ID)
    name            VARCHAR(255),                          -- 位置名稱，如：A區-貨架01 (Location name)
    location_number VARCHAR(255),                          -- 位置編號，用於條碼/RFID 掃描 (Location number for barcode scanning)
    
    -- 類型與功能欄位 (Type & Function Fields)
    -- -------------------------------------------------------------------------
    location_type   VARCHAR(50) NOT NULL,                  -- 位置性質：ZONE區域/BIN儲位/INTERNAL虛擬 (Location type)
    zone_type       VARCHAR(50),                           -- 區域功能：RECEIVING收貨/STORAGE儲存/PICKING揀貨/PACKING包裝/SHIPPING出貨/QA品檢/RETURNING退貨 (Zone function type)
    
    -- 層級結構欄位 (Hierarchical Structure)
    -- -------------------------------------------------------------------------
    parent_id       INT,                                   -- 上層位置 ID (Parent location ID)
    level           INT NOT NULL,                          -- 所在層級：1為區域，2為儲位 (Hierarchy level)
    path_cache      INT[],                                 -- 路徑 ID 陣列，如：'{1,5,8}' (Path IDs cache)
    path_text       VARCHAR(500),                          -- 路徑文字，如：'/A區/A01貨架' (Path text)
    
    -- 容量與數量欄位 (Capacity & Quantity Fields)
    -- -------------------------------------------------------------------------
    unit_of_measure_id INT NOT NULL DEFAULT 1,             -- 計量單位 ID，如：個、箱、托盤 (Unit of measure ID)
    max_quantity    INT,                                   -- 最大儲存數量 (Maximum quantity)
    
    -- 地址資訊 (Address Information)
    -- -------------------------------------------------------------------------
    address         VARCHAR(500),                          -- 實體地址（如果跨廠區才需要）(Physical address)
    
    -- 管理控制欄位 (Management Control)
    -- -------------------------------------------------------------------------
    sort_order      INT NOT NULL DEFAULT 0,                -- 排序順序，數字越小越前面 (Display order)
    status          VARCHAR(20) NOT NULL DEFAULT 'active', -- 狀態：active啟用/inactive停用/maintenance維護中 (Location status)
    
    -- 時間戳欄位 (Timestamp Fields)
    -- -------------------------------------------------------------------------
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),    -- 位置建立時間 (Creation timestamp)

    -- 約束條件 (Constraints)
    -- -------------------------------------------------------------------------

    -- 主鍵約束：位置唯一識別碼 (Primary key: unique location identifier)
    CONSTRAINT pk_locations PRIMARY KEY (id),                  
    
    -- 外鍵約束：刪除上層位置時，子位置 parent_id 設為 NULL (Foreign key: set NULL when parent is deleted)
    CONSTRAINT fk_locations_parent 
        FOREIGN KEY (parent_id) 
        REFERENCES locations(id) 
        ON DELETE SET NULL,                                    
    
    -- 外鍵約束：關聯到計量單位表 (Foreign key: references unit of measures)
    CONSTRAINT fk_locations_unit_of_measure 
        FOREIGN KEY (unit_of_measure_id) 
        REFERENCES unit_of_measures(id),                        
    
    -- 檢查約束：位置性質必須為指定值 (Check: location type must be ZONE, BIN, or INTERNAL)
    -- 如果想要完整一點的，能加入 'DEPOT' 代表倉點或店面
    CONSTRAINT ck_locations_type CHECK 
        (location_type IN ('ZONE', 'BIN', 'INTERNAL')),        
    
    -- 檢查約束：區域功能必須為指定值（若有的話）(Check: zone type must be valid if provided)
    CONSTRAINT ck_locations_zone_type CHECK 
        (zone_type IS NULL OR zone_type IN (
            'RECEIVING', 'STORAGE', 'PICKING', 'PACKING', 
            'SHIPPING', 'QA', 'RETURNING'
        )),                                                     
    
    -- 檢查約束：層級只能是 1 (區域) 或 2 (儲位) (Check: level must be 1 for zone, 2 for bin)
    CONSTRAINT ck_locations_level CHECK 
        (level IN (1, 2)),                                      
                  
    -- 檢查約束：最大數量不能為負 (Check: max quantity cannot be negative)
    CONSTRAINT ck_locations_max_quantity CHECK 
        (max_quantity IS NULL OR max_quantity >= 0),           
    
    -- 檢查約束：狀態必須為指定值 (Check: status must be active, inactive, or maintenance)
    CONSTRAINT ck_locations_status CHECK 
        (status IN ('active', 'inactive', 'maintenance'))      
);

-- =============================================================================
-- 索引建立 (Indexes)
-- 用途：優化各類常見查詢的效能
-- 說明：資料量 < 1000 筆，只保留必要索引
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 索引 1：父位置查詢索引（必要）
-- 名稱：idx_locations_parent
-- 類型：B-tree
-- 欄位：parent_id
-- 用途：加速查詢某個區域下的所有儲位（最常用）
-- 範例：SELECT * FROM locations WHERE parent_id = 1 ORDER BY sort_order;
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_locations_parent 
    ON locations (parent_id);

-- -----------------------------------------------------------------------------
-- 索引 2：位置編號查詢索引（選擇性，有掃描需求才建）
-- 名稱：idx_locations_number
-- 類型：B-tree
-- 欄位：location_number
-- 用途：加速條碼掃描、精確查找位置
-- 範例：SELECT * FROM locations WHERE location_number = 'A01-01';
-- 說明：1000 筆資料，沒這個索引也只要掃 1000 筆，差別不大
-- -----------------------------------------------------------------------------
-- CREATE INDEX IF NOT EXISTS idx_locations_number 
--     ON locations (location_number) 
--     WHERE location_number IS NOT NULL;

-- =============================================================================
-- 觸發器 (Triggers)
-- 用途：自動維護層級相關欄位
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 觸發器函數：更新位置層級資訊
-- 名稱：fn_update_location_hierarchy
-- 用途：當新增或修改位置時，自動計算並填入 level、path_cache、path_text
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_update_location_hierarchy()
RETURNS TRIGGER AS $$
DECLARE
    parent_path_cache INT[];
    parent_path_text VARCHAR(500);
    parent_level INT;
BEGIN
    -- 情況 1：根節點 (parent_id IS NULL) - 區域層級
    IF NEW.parent_id IS NULL THEN
        NEW.level := 1;
        NEW.path_cache := ARRAY[NEW.id];
        NEW.path_text := '/' || NEW.name;
    
    -- 情況 2：子節點 (有 parent_id) - 儲位層級
    ELSE
        -- 從父節點繼承層級資訊
        SELECT 
            l.path_cache,
            l.path_text,
            l.level
        INTO 
            parent_path_cache,
            parent_path_text,
            parent_level
        FROM locations l
        WHERE l.id = NEW.parent_id;
        
        -- 如果父節點存在，組合新的路徑資訊
        IF parent_path_cache IS NOT NULL THEN
            NEW.level := parent_level + 1;
            NEW.path_cache := parent_path_cache || NEW.id;
            NEW.path_text := parent_path_text || '/' || NEW.name;
        ELSE
            -- 預防措施：如果父節點不存在（理論上不應發生）
            NEW.level := 1;
            NEW.path_cache := ARRAY[NEW.id];
            NEW.path_text := '/' || NEW.name;
        END IF;
    END IF;
    
    -- 檢查層級是否合理（目前只有兩層）
    IF NEW.level > 2 THEN
        RAISE EXCEPTION 'Location hierarchy cannot exceed 2 levels (ZONE -> BIN)';
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 觸發器：在新增或更新位置時自動維護層級資訊
-- -----------------------------------------------------------------------------
CREATE TRIGGER trg_locations_before_insert_update
    BEFORE INSERT OR UPDATE OF parent_id ON locations
    FOR EACH ROW
    EXECUTE FUNCTION fn_update_location_hierarchy();

-- =============================================================================
-- 範例資料 (Sample Data)
-- 用途：提供一組完整的倉庫位置資料
-- 說明：目前只有單一倉庫，最高層級為區域 (ZONE)
-- =============================================================================

/*
-- 位置層級說明：
-- 層級 1：區域 (ZONE) - 倉庫內的功能分區
-- 層級 2：儲位 (BIN) - 實際存放貨物的位置
*/

INSERT INTO locations (id, name, location_number, location_type, zone_type, parent_id, unit_of_measure_id, max_quantity, sort_order, status) VALUES
    -- 層級 1：區域 (ZONES)
    (1, '收貨區', 'Z-RECV', 'ZONE', 'RECEIVING', NULL, 1, NULL, 100, 'active'),
    (2, '儲存區 A', 'Z-STOR-A', 'ZONE', 'STORAGE', NULL, 1, NULL, 200, 'active'),
    (3, '儲存區 B', 'Z-STOR-B', 'ZONE', 'STORAGE', NULL, 1, NULL, 210, 'active'),
    (4, '揀貨區', 'Z-PICK', 'ZONE', 'PICKING', NULL, 1, NULL, 300, 'active'),
    (5, '包裝區', 'Z-PACK', 'ZONE', 'PACKING', NULL, 1, NULL, 400, 'active'),
    (6, '出貨區', 'Z-SHIP', 'ZONE', 'SHIPPING', NULL, 1, NULL, 500, 'active'),
    (7, '品檢區', 'Z-QA', 'ZONE', 'QA', NULL, 1, NULL, 600, 'active'),
    (8, '退貨處理區', 'Z-RET', 'ZONE', 'RETURNING', NULL, 1, NULL, 700, 'active'),

    -- 層級 2：儲位 (BINS) - 儲存區 A 下的儲位
    (101, 'A區-貨架01-層01', 'A01-01', 'BIN', NULL, 2, 1, 1000, 1010, 'active'),
    (102, 'A區-貨架01-層02', 'A01-02', 'BIN', NULL, 2, 1, 1000, 1020, 'active'),
    (103, 'A區-貨架01-層03', 'A01-03', 'BIN', NULL, 2, 1, 1000, 1030, 'active'),
    (104, 'A區-貨架02-層01', 'A02-01', 'BIN', NULL, 2, 1, 1000, 1040, 'active'),
    (105, 'A區-貨架02-層02', 'A02-02', 'BIN', NULL, 2, 1, 1000, 1050, 'active'),
    
    -- 層級 2：儲位 (BINS) - 儲存區 B 下的儲位
    (201, 'B區-貨架01-層01', 'B01-01', 'BIN', NULL, 3, 1, 2000, 2010, 'active'),
    (202, 'B區-貨架01-層02', 'B01-02', 'BIN', NULL, 3, 1, 2000, 2020, 'active'),
    (203, 'B區-貨架01-層03', 'B01-03', 'BIN', NULL, 3, 1, 2000, 2030, 'active'),
    (204, 'B區-貨架02-層01', 'B02-01', 'BIN', NULL, 3, 1, 2000, 2040, 'maintenance'),  -- 維護中
    (205, 'B區-貨架02-層02', 'B02-02', 'BIN', NULL, 3, 1, 2000, 2050, 'inactive'),      -- 停用
    
    -- 層級 2：儲位 (BINS) - 揀貨區下的儲位（動線揀貨）
    (401, '揀貨動線-01', 'PICK-01', 'BIN', NULL, 4, 1, 500, 4010, 'active'),
    (402, '揀貨動線-02', 'PICK-02', 'BIN', NULL, 4, 1, 500, 4020, 'active');

-- =============================================================================
-- 驗證範例 (Validation Queries)
-- =============================================================================

/*
-- 1. 查看某個區域下的所有儲位（最常用）
SELECT * FROM locations WHERE parent_id = 2 ORDER BY sort_order;

-- 2. 查詢所有可用的儲存區儲位
SELECT * FROM locations 
WHERE zone_type = 'STORAGE' 
  AND location_type = 'BIN' 
  AND status = 'active'
ORDER BY path_text;

-- 3. 使用條碼掃描查詢位置（如果有索引）
SELECT * FROM locations WHERE location_number = 'A01-01';

-- 4. 查看所有區域（層級 1）
SELECT id, name, location_type, zone_type, status
FROM locations 
WHERE level = 1 
ORDER BY sort_order;

-- 5. 查詢特定功能的區域
SELECT * FROM locations 
WHERE zone_type IN ('RECEIVING', 'SHIPPING') 
  AND level = 1;
*/

-- =============================================================================
-- 註解說明 (Comments)
-- =============================================================================

COMMENT ON TABLE locations IS '儲位表，定義倉庫內的所有實體位置（區域和儲位）';
COMMENT ON COLUMN locations.id IS '位置唯一識別碼，主鍵';
COMMENT ON COLUMN locations.name IS '位置名稱，用於顯示';
COMMENT ON COLUMN locations.location_number IS '位置編號，用於條碼掃描';
COMMENT ON COLUMN locations.location_type IS '位置性質：ZONE區域/BIN儲位/INTERNAL虛擬';
COMMENT ON COLUMN locations.zone_type IS '區域功能：RECEIVING收貨/STORAGE儲存/PICKING揀貨/PACKING包裝/SHIPPING出貨/QA品檢/RETURNING退貨';
COMMENT ON COLUMN locations.parent_id IS '上層位置 ID，NULL 表示根區域';
COMMENT ON COLUMN locations.level IS '所在層級：1區域/2儲位，由觸發器自動維護';
COMMENT ON COLUMN locations.path_cache IS '從根到目前節點的所有 ID 陣列，由觸發器自動維護';
COMMENT ON COLUMN locations.path_text IS '從根到目前節點的路徑文字，由觸發器自動維護';
COMMENT ON COLUMN locations.unit_of_measure_id IS '計量單位 ID，如：個、箱、托盤';
COMMENT ON COLUMN locations.max_quantity IS '最大儲存數量';
COMMENT ON COLUMN locations.address IS '實體地址（如果跨廠區才需要）';
COMMENT ON COLUMN locations.sort_order IS '排序順序，數字越小越前面';
COMMENT ON COLUMN locations.status IS '狀態：active啟用/inactive停用/maintenance維護中';
COMMENT ON COLUMN locations.created_at IS '位置建立時間';