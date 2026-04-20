namespace Manian.Infrastructure.Settings;

/// <summary>
/// 藍新金流服務設定類別
/// </summary>
public class NewebPaySettings
{
    /// <summary>
    /// 藍新金流提供的 MerchantID
    /// </summary>
    public string MerchantID { get; set; }

    /// <summary>
    /// 藍新金流提供的 HashKey
    /// </summary>
    public string HashKey { get; set; }

    /// <summary>
    /// 藍新金流提供的 HashIV
    /// </summary>
    public string HashIV { get; set; }
}