namespace SimpleAutomaticStorageSystem.Stub.Models;

// 入庫要求
public record PutAwayBody(string ItemCode);

// JOB情報
public record JobBody(string JobId, string JobType, string ItemId);

// アラーム通知
public record AlarmBody(string AlarmCode, DateTime OccurredAt);

// 通信エラー
public record ErrorBody(string Error);
