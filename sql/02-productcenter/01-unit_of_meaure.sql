-- =========================================================================
-- unit_of_measures：計量單位表
-- 用途：定義商品的多種計量單位，支援不同包裝規格的換算
-- 設計考量：所有單位以 EA (單件) 為基準單位，conversion_to_base 定義與基準單位的轉換率
-- =========================================================================
CREATE TABLE unit_of_measures (
    id INTEGER NOT NULL,                  -- 唯一識別碼 (Unique identifier)
    code VARCHAR(20) NOT NULL,             -- 單位代碼：EA, BOX, CTN, PALLET... (Unit code)
    name VARCHAR(100) NOT NULL,            -- 單位名稱：Each, Box, Carton... (Unit name)
    description VARCHAR(255),              -- 單位描述 (Unit description)
    conversion_to_base INT NOT NULL,       -- 轉換率：此單位等於多少基準單位 (EA) (Conversion rate to base unit)
    created_at TIMESTAMPTZ DEFAULT NOW(),  -- 建立時間 (Creation timestamp)

    -- 主鍵約束 (Primary key constraint)
    CONSTRAINT pk_unit_of_measures PRIMARY KEY (id),

    -- 唯一約束：單位代碼不可重複 (Unique constraint for unit code)
    CONSTRAINT uk_unit_of_measures_code UNIQUE (code),

    -- 檢查約束：轉換率必須為正整數 (Check constraint for positive conversion rate)
    CONSTRAINT ck_unit_of_measures_conversion CHECK (conversion_to_base > 0)
);

-- =========================================================================
-- 初始化計量單位資料
-- 基準單位：EA (單件)，所有其他單位都定義與 EA 的轉換關係
-- =========================================================================
INSERT INTO unit_of_measures
(id, code, name, description, conversion_to_base) 
VALUES 
(1, 'EA',    '單件',    '單件，最小基準單位 (Base unit, minimum unit)', 1),
(2, 'BOX',   '箱',      '一箱，通常包含 24 EA (Box, typically contains 24 EA)', 24),
(3, 'CTN',   '大箱',    '大箱，通常包含 10 BOX = 240 EA (Carton, 10 boxes = 240 EA)', 240),
(4, 'PALLET','棧板',    '棧板，通常包含 20 CTN = 4800 EA (Pallet, 20 cartons = 4800 EA)', 4800),
(5, 'PK',    '包',      '一包，通常包含 6 EA (Pack, typically contains 6 EA)', 6),
(6, 'DOZ',   '打',      '一打，12 EA (Dozen, 12 EA)', 12),
(7, 'BAG',   '袋',      '袋裝，通常包含 50 EA (Bag, typically contains 50 EA)', 50),
(8, 'ROLL',  '卷',      '一卷，常用於布料或紙品，假設 100 EA (Roll, e.g., fabric or paper, assumed 100 EA)', 100),
(9, 'LTR',   '公升',    '公升，液體計量單位，假設 1 L = 1 EA 基準 (Liter, liquid, assumed 1 L = 1 base EA)', 1),
(10,'KG',    '公斤',    '公斤，重量計量單位，假設 1 KG = 1 EA 基準 (Kilogram, weight, assumed 1 KG = 1 base EA)', 1);