namespace SimpleAutomaticStorageSystem.Server.Shared;

// =========================
//   カスタム例外
// =========================

/// <summary>
/// 通信時のエラーコードの定義
/// </summary>
public static class HttpErrors
{
    // =========================
    //   業務エラー
    // =========================

    // 品種コードが不正
    public const string INVALID_ITEM_CODE = "INVALID_ITEM_CODE";

    // 在庫がない
    public const string OUT_OF_STOCK = "OUT_OF_STOCK";

    // 空き容量がない
    public const string NO_CAPACITY_AVAILABLE = "NO_CAPACITY_AVAILABLE";

    // =========================
    //   システムエラー
    // =========================

    // リクエストボディが異常
    public const string INVALID_REQUEST = "INVALID_REQUEST";

    // 不正なクエリ
    public const string INVALID_QUERY = "INVALID_QUERY";

    // 未登録端末からアクセスされた
    public const string UNREGISTERED_DEVICE = "UNREGISTERED_DEVICE";

    // JOBのアクセス権がない
    public const string ACCESS_DENIED_JOB = "ACCESS_DENIED_JOB";

    // JOB番号が存在しない
    public const string JOB_NOT_FOUND = "JOB_NOT_FOUND";

    // 遷移可能な状態ではない
    public const string INVALID_STATUS = "INVALID_STATUS";

    // JOBを実行できない
    public const string CANNOT_DISPATCH = "CANNOT_DISPATCH";

    // その他のエラー
    public const string UNEXPECTED_ERROR = "UNEXPECTED_ERROR";

}

// =========================
//   カスタム例外
// =========================

/// <summary>
/// APIエラーを表現する基底クラス
/// </summary>
/// <param name="statusCode">ステータスコード</param>
/// <param name="errorCode">JSON用エラーコード</param>
/// <param name="message">ログ用メッセージ</param>
public abstract class ApiException(int statusCode, string errorCode, string message) 
    : Exception(message)
{
    /// <summary>
    /// ステータスコード
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// エラーコード本体
    /// </summary>
    public string ErrorCode { get; } = errorCode;
}

/// <summary>
/// 品種コードが不正
/// </summary>
public sealed class InvalidItemCodeException() 
    : ApiException(
        StatusCodes.Status422UnprocessableEntity,
        HttpErrors.INVALID_ITEM_CODE,
        "品種コードが不正");

/// <summary>
/// 在庫がない
/// </summary>
public sealed class OutOfStockException() 
    : ApiException(
        StatusCodes.Status422UnprocessableEntity,
        HttpErrors.OUT_OF_STOCK,
        "出荷可能な在庫がない");

/// <summary>
/// 空き容量がない
/// </summary>
public sealed class NoCapacityAvailableException()
    : ApiException(
        StatusCodes.Status422UnprocessableEntity,
        HttpErrors.NO_CAPACITY_AVAILABLE,
        "空き容量不足");

/// <summary>
/// 不正なクエリ
/// </summary>
/// <param name="reason">不正理由</param>
public sealed class InvalidQueryException()
    : ApiException(
        StatusCodes.Status400BadRequest,
        HttpErrors.INVALID_QUERY,
        "不正なクエリ");

/// <summary>
/// 未登録スマホからアクセスされた
/// </summary>
public sealed class UnregisteredDeviceException()
    : ApiException(
        StatusCodes.Status401Unauthorized,
        HttpErrors.UNREGISTERED_DEVICE,
        "未登録端末からのアクセス");

/// <summary>
/// JOBのアクセス権がない
/// </summary>
public sealed class JobAccessDeniedException()
    : ApiException(
        StatusCodes.Status403Forbidden,
        HttpErrors.ACCESS_DENIED_JOB,
        "JOBアクセスのアクセス権がない");

/// <summary>
/// JOB番号が存在しない
/// </summary>
public sealed class JobNotFoundException()
    : ApiException(
        StatusCodes.Status404NotFound,
        HttpErrors.JOB_NOT_FOUND,
        "JOB番号が存在しない");

/// <summary>
/// 遷移可能な状態ではない
/// </summary>
public sealed class InvalidStatusException()
    : ApiException(
        StatusCodes.Status409Conflict,
        HttpErrors.INVALID_STATUS,
        "遷移可能な状態ではない");
