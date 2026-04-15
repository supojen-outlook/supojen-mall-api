using Manian.Domain.Services;

namespace Manian.Infrastructure.Services;

/// <summary>
/// 雪花演算法 (Snowflake) ID 產生器
/// 分散式系統中唯一 ID 的生成方案，由 Twitter 開發
/// 產生的標準 ID 是 64 位元整數，結構如下：
/// ┌────────────────────────────────────────────────────────────┐
/// │ 1 bit 保留 │ 41 bits 時間戳 │ 5 bits 資料中心ID │ 5 bits 機器ID │ 12 bits 序號 │
/// └────────────────────────────────────────────────────────────┘
/// 
/// 此實作提供兩種格式：
/// - 標準 64-bit Long：完整的 Snowflake 結構（給資料庫使用）
/// - 精簡 53-bit Integer：給 JavaScript 前端使用的版本（Number 安全整數範圍）
/// 
/// 精簡版的結構調整為：
/// ┌────────────────────────────────────────────────┐
/// │ 1 bit 保留 │ 41 bits 時間戳 │ 2 bits 資料中心 │ 8 bits 機器 │ 10 bits 序號 │
/// └────────────────────────────────────────────────┘
/// 總計 62 bits，但確保值在 2^53-1 以內，相容前端 Number 型別
/// </summary>
public class Snowflake : IUniqueIdentifier
{
    // =========================================================================
    // 標準 Snowflake 常數（64-bit）
    // =========================================================================
    /// <summary>
    /// 自訂的起始時間戳記（紀元）
    /// 這是 Twitter 原始實作的起始時間：2010-11-04 09:42:54.657 UTC
    /// 為什麼要有這個？因為我們用 41 bits 儲存時間戳，最大可表示 69 年
    /// 透過減去這個固定值，可以讓 ID 的有效期限從 2010 年開始算，而不是 1970 年
    /// </summary>
    private const long Twepoch = 1288834974657L;

    /// <summary>
    /// 機器 ID 佔用的位元數（5 bits）
    /// 5 bits 最大可表示 32 (2^5) 台機器
    /// </summary>
    private const int WorkerIdBits = 5;
    
    /// <summary>
    /// 資料中心 ID 佔用的位元數（5 bits）
    /// 5 bits 最大可表示 32 (2^5) 個資料中心
    /// </summary>
    private const int DatacenterIdBits = 5;
    
    /// <summary>
    /// 序號佔用的位元數（12 bits）
    /// 12 bits 最大可表示 4096 (2^12) 個序號
    /// 也就是同一毫秒內最多可產生 4096 個 ID
    /// </summary>
    private const int SequenceBits = 12;
    
    /// <summary>
    /// 最大機器 ID 值（31）
    /// 計算方式：-1 左移 5 bits 再取互斥或（XOR）
    /// 結果就是低 5 bits 全為 1 的數值
    /// </summary>
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    
    /// <summary>
    /// 最大資料中心 ID 值（31）
    /// 同樣是低 5 bits 全為 1
    /// </summary>
    private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

    /// <summary>
    /// 機器 ID 在最終 ID 中的位移量（12 bits）
    /// 因為序號佔用最低的 12 bits，所以機器 ID 要往左移 12 位
    /// </summary>
    private const int WorkerIdShift = SequenceBits;
    
    /// <summary>
    /// 資料中心 ID 的位移量（17 bits）
    /// 機器 ID 佔了 5 bits，所以資料中心 ID 要再往左移 5 位
    /// 總位移 = 序號 12 + 機器ID 5 = 17
    /// </summary>
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
    
    /// <summary>
    /// 時間戳的位移量（22 bits）
    /// 序號(12) + 機器ID(5) + 資料中心ID(5) = 22
    /// 所以時間戳要放在最高的 41 bits，往左移 22 位
    /// </summary>
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;
    
    /// <summary>
    /// 序號遮罩（4095）
    /// 用來確保序號不會超過 12 bits 的範圍
    /// 當序號 +1 後與此遮罩做 AND，就會自動歸零（環狀計數）
    /// </summary>
    private const long SequenceMask = -1L ^ (-1L << SequenceBits);

    // =========================================================================
    // 精簡 Snowflake 常數（53-bit）- 適用於 JavaScript Number
    // =========================================================================
    /// <summary>
    /// 精簡版機器 ID 佔用的位元數（8 bits）
    /// 8 bits 最大可表示 256 (2^8) 台機器
    /// </summary>
    private const int ShortWorkerIdBits = 8;

    /// <summary>
    /// 精簡版資料中心 ID 佔用的位元數（2 bits）
    /// 2 bits 最大可表示 4 (2^2) 個資料中心
    /// </summary>
    private const int ShortDatacenterIdBits = 2;

    /// <summary>
    /// 精簡版序號佔用的位元數（10 bits）
    /// 10 bits 最大可表示 1024 (2^10) 個序號
    /// 也就是同一毫秒內最多可產生 1024 個 ID
    /// </summary>
    private const int ShortSequenceBits = 10;

    /// <summary>
    /// 精簡版最大機器 ID 值（255）
    /// </summary>
    private const long ShortMaxWorkerId = -1L ^ (-1L << ShortWorkerIdBits);

    /// <summary>
    /// 精簡版最大資料中心 ID 值（3）
    /// </summary>
    private const long ShortMaxDatacenterId = -1L ^ (-1L << ShortDatacenterIdBits);

    /// <summary>
    /// 精簡版序號遮罩（1023）
    /// 用來確保序號不會超過 10 bits 的範圍
    /// </summary>
    private const long ShortSequenceMask = -1L ^ (-1L << ShortSequenceBits);

    /// <summary>
    /// 精簡版機器 ID 位移量（10 bits）
    /// 因為序號佔用最低的 10 bits，所以機器 ID 要往左移 10 位
    /// </summary>
    private const int ShortWorkerIdShift = ShortSequenceBits;

    /// <summary>
    /// 精簡版資料中心 ID 位移量（18 bits）
    /// 機器 ID 佔了 8 bits，所以資料中心 ID 要再往左移 8 位
    /// 總位移 = 序號 10 + 機器ID 8 = 18
    /// </summary>
    private const int ShortDatacenterIdShift = ShortSequenceBits + ShortWorkerIdBits;

    /// <summary>
    /// 精簡版時間戳位移量（20 bits）
    /// 序號(10) + 機器ID(8) + 資料中心ID(2) = 20
    /// 所以時間戳要放在最高的 41 bits，往左移 20 位
    /// </summary>
    private const int ShortTimestampLeftShift = ShortSequenceBits + ShortWorkerIdBits + ShortDatacenterIdBits;

    /// <summary>
    /// 上一次產生標準 ID 的時間戳（毫秒）
    /// 用來檢測時鐘回撥，以及判斷是否需要重置序號
    /// 初始化為 -1 確保第一次產生時會正常運作
    /// </summary>
    private long _lastTimestamp = -1L;

    /// <summary>
    /// 上一次產生精簡 ID 的時間戳（毫秒）
    /// </summary>
    private long _shortLastTimestamp = -1L;

    /// <summary>
    /// 標準序號（12 bits）
    /// internal set 允許單元測試時模擬序號狀態
    /// </summary>
    public long Sequence { get; internal set; }

    /// <summary>
    /// 精簡序號（10 bits）
    /// </summary>
    public long ShortSequence { get; internal set; }

    /// <summary>
    /// 建構函式 - 初始化 Snowflake 產生器
    /// </summary>
    /// <param name="workerId">機器 ID，標準模式 0-31，精簡模式 0-255，預設為 1</param>
    /// <param name="datacenterId">資料中心 ID，標準模式 0-31，精簡模式 0-3，預設為 1</param>
    /// <exception cref="ArgumentException">當參數超出範圍時拋出</exception>
    public Snowflake(long workerId = 1, long datacenterId = 1)
    {
        // 驗證標準 Snowflake 的範圍
        if (workerId > MaxWorkerId || workerId < 0)
        {
            throw new ArgumentException($"標準模式 Worker ID 必須在 0 和 {MaxWorkerId} 之間");
        }
        if (datacenterId > MaxDatacenterId || datacenterId < 0)
        {
            throw new ArgumentException($"標準模式 Datacenter ID 必須在 0 和 {MaxDatacenterId} 之間");
        }

        // 驗證精簡 Snowflake 的範圍
        if (workerId > ShortMaxWorkerId || workerId < 0)
        {
            throw new ArgumentException($"精簡模式 Worker ID 必須在 0 和 {ShortMaxWorkerId} 之間");
        }
        if (datacenterId > ShortMaxDatacenterId || datacenterId < 0)
        {
            throw new ArgumentException($"精簡模式 Datacenter ID 必須在 0 和 {ShortMaxDatacenterId} 之間");
        }

        WorkerId = workerId;
        DatacenterId = datacenterId;
    }

    /// <summary>
    /// 機器 ID
    /// 唯讀，建構時決定後就不再變更
    /// </summary>
    public long WorkerId { get; }
    
    /// <summary>
    /// 資料中心 ID
    /// 唯讀，建構時決定後就不再變更
    /// </summary>
    public long DatacenterId { get; }

    /// <summary>
    /// 產生標準 64-bit Snowflake ID
    /// 結構：1 bit 保留 + 41 bits 時間戳 + 5 bits 資料中心 + 5 bits 機器 + 12 bits 序號
    /// </summary>
    /// <returns>64-bit 長整數 ID（永遠為正數）</returns>
    /// <exception cref="InvalidOperationException">當時鐘回撥時拋出</exception>
    public long NextLong()
    {
        // 使用 lock 確保執行緒安全
        // 因為多執行緒同時呼叫時，序號計算必須是原子操作
        lock (this)
        {
            // 取得目前時間戳（毫秒）
            var timestamp = TimeGen();

            // 檢查時鐘是否回撥
            // 如果目前時間比上次產生 ID 的時間還早，表示系統時間被調回去了
            // 這種情況下不能產生 ID，因為可能導致 ID 重複
            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException(
                    $"時鐘回撥，拒絕生成 ID。{_lastTimestamp - timestamp} 毫秒");
            }

            // 如果是在同一毫秒內
            if (_lastTimestamp == timestamp)
            {
                // 序號加 1，並用遮罩確保不超過 4095
                // 例如：目前序號 4095，+1 後變成 4096
                // 4096 & 4095 = 0，自動歸零
                Sequence = (Sequence + 1) & SequenceMask;
                
                // 如果序號歸零（表示這一毫秒已經產生 4096 個 ID，超過上限）
                if (Sequence == 0)
                {
                    // 等待到下一毫秒
                    timestamp = TilNextMillis(_lastTimestamp);
                }
            }
            else
            {
                // 不同毫秒，序號重置為 0
                Sequence = 0L;
            }

            // 記錄這次的時間戳，供下次使用
            _lastTimestamp = timestamp;

            // 組合 ID 並回傳
            // 這是標準的 64 位元 Snowflake ID
            return ((timestamp - Twepoch) << TimestampLeftShift) |
                   (DatacenterId << DatacenterIdShift) |
                   (WorkerId << WorkerIdShift) |
                   Sequence;
        }
    }

    /// <summary>
    /// 產生精簡 53-bit Snowflake ID（以 32-bit int 回傳）
    /// 設計給 JavaScript 前端使用，確保在 Number 安全整數範圍內（2^53 - 1）
    /// 結構：1 bit 保留 + 41 bits 時間戳 + 2 bits 資料中心 + 8 bits 機器 + 10 bits 序號
    /// </summary>
    /// <returns>32-bit 整數 ID（實際上是 53-bit 但取低 31 bits 確保相容 INT）</returns>
    /// <exception cref="InvalidOperationException">當時鐘回撥時拋出</exception>
    public int NextInt()
    {
        lock (this)
        {
            var timestamp = TimeGen();

            if (timestamp < _shortLastTimestamp)
            {
                throw new InvalidOperationException(
                    $"時鐘回撥，拒絕生成 ID。{_shortLastTimestamp - timestamp} 毫秒");
            }

            if (_shortLastTimestamp == timestamp)
            {
                ShortSequence = (ShortSequence + 1) & ShortSequenceMask;
                if (ShortSequence == 0)
                {
                    timestamp = TilNextShortMillis(_shortLastTimestamp);
                }
            }
            else
            {
                ShortSequence = 0L;
            }

            _shortLastTimestamp = timestamp;

            // 組合精簡版 ID
            var id = ((timestamp - Twepoch) << ShortTimestampLeftShift) |
                     (DatacenterId << ShortDatacenterIdShift) |
                     (WorkerId << ShortWorkerIdShift) |
                     ShortSequence;

            // 確保在 32-bit 範圍內（取低 31-bit + 符號位）
            // 這樣做是為了相容資料庫 INT 型別
            return (int)(id & 0x7FFFFFFF);  // 0x7FFFFFFF = 2^31 - 1
        }
    }

    /// <summary>
    /// 產生精簡 53-bit Snowflake ID（以 long 回傳完整值）
    /// 如果需要保留完整的 53-bit 精度，可以使用這個方法
    /// 但注意：這個值可能超過 JavaScript Number 安全整數範圍
    /// </summary>
    /// <returns>53-bit 長整數 ID（實際上是 62-bit，但值不超過 2^53）</returns>
    public long NextShortLong()
    {
        lock (this)
        {
            var timestamp = TimeGen();

            if (timestamp < _shortLastTimestamp)
            {
                throw new InvalidOperationException(
                    $"時鐘回撥，拒絕生成 ID。{_shortLastTimestamp - timestamp} 毫秒");
            }

            if (_shortLastTimestamp == timestamp)
            {
                ShortSequence = (ShortSequence + 1) & ShortSequenceMask;
                if (ShortSequence == 0)
                {
                    timestamp = TilNextShortMillis(_shortLastTimestamp);
                }
            }
            else
            {
                ShortSequence = 0L;
            }

            _shortLastTimestamp = timestamp;

            // 回傳完整的 62-bit 值，但保證不超過 2^53 - 1
            return ((timestamp - Twepoch) << ShortTimestampLeftShift) |
                   (DatacenterId << ShortDatacenterIdShift) |
                   (WorkerId << ShortWorkerIdShift) |
                   ShortSequence;
        }
    }

    /// <summary>
    /// 等待到下一毫秒（標準模式）
    /// 當同一毫秒內產生的 ID 數量超過 4096 時呼叫
    /// </summary>
    /// <param name="lastTimestamp">上一次的時間戳</param>
    /// <returns>下一毫秒的時間戳</returns>
    private long TilNextMillis(long lastTimestamp)
    {
        var timestamp = TimeGen();
        while (timestamp <= lastTimestamp)
        {
            timestamp = TimeGen();
        }
        return timestamp;
    }

    /// <summary>
    /// 等待到下一毫秒（精簡模式）
    /// 當同一毫秒內產生的 ID 數量超過 1024 時呼叫
    /// </summary>
    private long TilNextShortMillis(long lastTimestamp)
    {
        var timestamp = TimeGen();
        while (timestamp <= lastTimestamp)
        {
            timestamp = TimeGen();
        }
        return timestamp;
    }

    /// <summary>
    /// 取得目前 UTC 時間的毫秒時間戳
    /// </summary>
    /// <returns>從 1970-01-01 到現在的毫秒數</returns>
    private static long TimeGen()
    {
        // 使用 DateTimeOffset.UtcNow 確保取得 UTC 時間，不受時區影響
        // ToUnixTimeMilliseconds() 直接回傳毫秒時間戳
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}