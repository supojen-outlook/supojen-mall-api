namespace Manian.Domain.ValueObjects;

/// <summary>
/// 商品規格值物件
/// 用途：定義商品的規格參數，用於商品篩選和比較
/// 設計考量：
/// - 使用值物件模式，確保規格資料的完整性
/// - 支援多種規格類型（數值、文字、選項等）
/// - 提供單位支援，方便規格比較
/// 
/// 使用場景：
/// - 商品詳細頁規格展示
/// - 商品篩選功能
/// - 商品比較功能
/// </summary>
public class Specification
{
    /// <summary>
    /// 規格鍵 ID
    /// 
    /// 用途：
    /// - 識別規格類型
    /// - 對應到系統中的規格定義
    /// 
    /// 範例：
    /// - "1"：重量規格
    /// - "2"：材質規格
    /// - "3"：尺寸規格
    /// </summary>
    public int KeyId { get; set; }

    /// <summary>
    /// 規格值 ID
    /// 
    /// 用途：
    /// - 識別具體的規格值
    /// - 對應到系統中的規格值定義
    /// 
    /// 範例：
    /// - "100"：1.2kg
    /// - "200"：陶瓷
    /// - "300"：25cm
    /// </summary>
    public int ValueId { get; set; }

    /// <summary>
    /// 規格名稱
    /// 
    /// 用途：
    /// - 顯示給使用者看的規格名稱
    /// - 用於規格篩選和比較
    /// 
    /// 範例：
    /// - "重量"
    /// - "材質"
    /// - "尺寸"
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 規格值
    /// 
    /// 用途：
    /// - 顯示給使用者看的規格值
    /// - 用於規格篩選和比較
    /// 
    /// 範例：
    /// - "1.2kg"
    /// - "陶瓷"
    /// - "25cm"
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 規格單位
    /// 
    /// 用途：
    /// - 提供規格值的單位
    /// - 用於規格比較
    /// 
    /// 預設值：
    /// - null（可選欄位）
    /// 
    /// 範例：
    /// - "kg"：重量單位
    /// - "cm"：長度單位
    /// - "ml"：容量單位
    /// </summary>
    public string? Unit { get; set; }

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
}
