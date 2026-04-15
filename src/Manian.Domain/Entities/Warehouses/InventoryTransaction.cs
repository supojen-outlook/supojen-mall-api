using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Warehouses;

/// <summary>
/// 庫存交易實體
/// 用途：記錄所有庫存異動的明細，用於追蹤、稽核、對帳
/// 設計考量：
/// - 精簡版只記錄核心資訊，足夠追蹤問題但不複雜
/// - 交易數量為正數表示入庫，負數表示出庫
/// </summary>
public class InventoryTransaction : IEntity
{
    /// <summary>
    /// 交易記錄唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// SKU ID，關聯到 skus 表
    /// </summary>
    public int SkuId { get; set; }

    /// <summary>
    /// 交易類型：in入庫/out出庫/adjust調整
    /// 約束：必須為 'in'、'out' 或 'adjust'
    /// </summary>
    private string _transactionType = string.Empty;

    public string TransactionType
    {
        get => _transactionType;
        set
        {
            if (value != "IN" && value != "OUT" && value != "ADJUST")
                throw new ArgumentException("TransactionType 必須是 'IN'、'OUT' 或 'ADJUST'");
            _transactionType = value;
        }
    }

    /// <summary>
    /// 交易數量：正數入庫，負數出庫
    /// 約束：不能為 0
    /// </summary>
    private int _quantity;

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value == 0)
                throw new ArgumentException("Quantity 不能為 0");
            _quantity = value;
        }
    }

    /// <summary>
    /// 來源文檔類型：ORDER訂單/PURCHASE採購/ADJUST調整單
    /// 約束：必須為 'ORDER'、'PURCHASE' 或 'ADJUST'
    /// 約束：必須與 reference_id 同時為 NULL 或同時有值
    /// </summary>
    private string? _referenceType;

    public string? ReferenceType
    {
        get => _referenceType;
        set
        {
            if (value != null && value != "ORDER" && value != "PURCHASE" && value != "ADJUST")
                throw new ArgumentException("ReferenceType 必須是 'ORDER'、'PURCHASE' 或 'ADJUST'");
            _referenceType = value;
        }
    }

    /// <summary>
    /// 來源文檔 ID
    /// 約束：必須與 reference_type 同時為 NULL 或同時有值
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>
    /// 交易備註
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// 交易發生時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
