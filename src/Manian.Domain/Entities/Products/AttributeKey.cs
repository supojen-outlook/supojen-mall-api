using Xiao.Domain.Entities;

namespace Manian.Domain.Entities.Products;

/// <summary>
/// 屬性鍵實體
/// 用途：定義產品的各種屬性，如顏色、尺寸、材質等，用於商品規格管理
/// 設計考量：
/// - 支援銷售屬性（用於SKU生成）與非銷售屬性（用於商品描述）
/// - 提供 input_type 控制前端顯示元件
/// - 屬性值會儲存在 attribute_values 表中，與此表為一對多關係
/// </summary>
public class AttributeKey : IEntity
{
    /// <summary>
    /// 屬性唯一識別碼
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 屬性顯示名稱，如：顏色、尺寸
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 屬性詳細描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 是否為銷售屬性：TRUE用於SKU生成/FALSE僅為描述
    /// </summary>
    public bool ForSales { get; set; }

    /// <summary>
    /// 前端輸入類型：select下拉選單/text文字/number數字/checkbox複選框
    /// 預設值：select
    /// </summary>
    private string _inputType = "select";

    /// <summary>
    /// 前端輸入類型：select下拉選單/text文字/number數字/checkbox複選框
    /// 
    /// 驗證規則：
    /// - 只能接受 "select"、"text"、"number" 或 "checkbox" 四個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// attributeKey.InputType = "select";   // 正確
    /// attributeKey.InputType = "text";     // 正確
    /// attributeKey.InputType = "number";   // 正確
    /// attributeKey.InputType = "checkbox"; // 正確
    /// attributeKey.InputType = "radio";    // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "select"、"text"、"number" 或 "checkbox" 時拋出
    /// </exception>
    public string InputType
    {
        get => _inputType;
        set
        {
            if (value != "select" && value != "text" && value != "number" && value != "checkbox")
                throw new ArgumentException("InputType 必須是 'select'、'text'、'number' 或 'checkbox'");
            
            _inputType = value;
        }
    }

    /// <summary>
    /// 是否為必填屬性
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 單位，如：cm(公分)、g(公克)
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// 同層級間的排序順序，數字越小越前面
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 屬性狀態：active啟用，inactive停用
    /// 預設值：active
    /// </summary>
    private string _status = "active";

    /// <summary>
    /// 屬性狀態：active啟用，inactive停用
    /// 
    /// 驗證規則：
    /// - 只能接受 "active" 或 "inactive" 兩個值
    /// - 設定其他值會拋出 ArgumentException
    /// 
    /// 使用範例：
    /// <code>
    /// attributeKey.Status = "active";   // 正確
    /// attributeKey.Status = "inactive"; // 正確
    /// attributeKey.Status = "pending";  // 會拋出 ArgumentException
    /// </code>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// 當設定值不是 "active" 或 "inactive" 時拋出
    /// </exception>
    public string Status
    {
        get => _status;
        set
        {
            if (value != "active" && value != "inactive")
                throw new ArgumentException("Status 必須是 'active' 或 'inactive'");
            
            _status = value;
        }
    }

    /// <summary>
    /// 屬性建立時間
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
