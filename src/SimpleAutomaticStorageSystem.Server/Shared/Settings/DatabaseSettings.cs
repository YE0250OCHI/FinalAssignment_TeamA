namespace SimpleAutomaticStorageSystem.Server.Shared.Settings;

public class DatabaseSettings
{
    /// <summary>
    /// DB接続文字列
    /// </summary>
    public required string DefaultConnection {  get; init; }
}
