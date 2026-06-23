namespace SimpleAutomaticStorageSystem.Server.Shared;

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

    // クエリが異常
    public const string INVALID_QUERY = "INVALID_QUERY";

    // 未登録スマホからアクセスされた
    public const string UNREGISTERED_DEVICE = "UNREGISTERED_DEVICE";

    // 未登録自動倉庫からアクセスされた
    public const string UNREGISTERED_EQUIPMENT = "UNREGISTERED_EQUIPMENT";

    // 担当外端末からアクセスされた
    public const string ACCESS_DENIED_JOB = "ACCESS_DENIED_JOB";

    // JOB番号が不正である
    public const string JOB_NOT_FOUND = "JOB_NOT_FOUND";

    // 状態が一致していない
    public const string INVALID_STATUS = "INVALID_STATUS";

    // JOBを実行できない
    public const string CANNOT_DISPATCH = "CANNOT_DISPATCH";

    // その他のエラー
    public const string UNEXPECTED_ERROR = "UNEXPECTED_ERROR";

}
