# コンソール設計資料2

## タスク

|No.|タスク|周期|役割|
|---:|:---|:---|:---|
|1.|メイン|常時|コンソールの更新や異常発生時の画面変更などを行う|
|2.|出庫要求待機|イベント|サーバーからの出庫要求を受信し、<br>JOB情報を出庫バッファに格納する|
|3.|出庫指示問合せ|5秒|サーバーに出庫指示の有無を問合せ、<br>JOB情報を出庫バッファに格納する|
|4.|入庫操作待機|イベント|コンソールからの入庫操作をサーバーに送信し、<br>返ってきたJOB情報を入庫バッファに格納する|
|5.|出庫処理|常時監視|出庫バッファ内のJOBを実行する|
|6.|入庫処理|常時監視|入庫バッファ内のJOBを実行する|

<img width="1368" height="726" alt="image" src="https://github.com/user-attachments/assets/698db184-57ed-4e5d-bb59-292d6e523320" />

## フロー処理

### アプリケーション起動

1. アプリケーション起動
2. HttpClient生成
3. Logger生成
4. Repository生成
5. 起動ログ記録
6. メインループ開始
7. オンライン通知送信
8. コンソール表示
9. 通信ログ記録
10. レスポンス受信
    1. 通信正常
       1. 通信ログ記録
       2. 各タスク起動
    2. 通信異常 (400系)
       1. 通信ログ記録
       2. 復帰不可異常処理へ遷移
    3. 通信異常 (500・タイムアウト)
       1. 通信ログ記録
       2. 復帰可能異常へ遷移
11. メインコンソール表示
12. 入力処理
13. 異常発生時
    1. 異常ログ記録
    2. アラーム報告送信
    3. 通信ログ記録
    4. 異常種別確認
       1. 復帰可能異常
          - キー入力待機
            1. 復帰入力
               - オンライン通知フローに遷移
            2. 終了入力
               1. 終了ログ記録
               2. プログラム終了
       2. 復帰不可異常
          1. 終了ログ記録
          2. プログラム終了

### 出庫要求待機

1. 出庫要求待機ループ
2. Httpリスナー起動
3. 通信ログ記録
4. リクエストあり
   1. 通信ログ記録
   2. 出庫処理ループの状態確認
      1. 処理中
         1. 対応不可レスポンス送信
         2. 通信ログ記録
         3. フローの先頭へ戻る
      2. 待機中
         - フロー継続
   3. JOB番号重複確認
      1. 重複あり
         1. 対応不可レスポンス送信
         2. 通信ログ記録
         3. フローの先頭へ戻る
      2. 重複なし
         - フロー継続
   4. 在庫確認
      1. 在庫なし
         1. 対応不可レスポンス送信
         2. 通信ログ記録
         3. フローの先頭へ戻る
      2. 在庫あり
         1. 対応可能レスポンス送信
         2. 通信ログ記録
         3. JOB情報を出庫バッファに保存
5. フローの先頭へ戻る

### 出庫指示問合せ

1. 出庫指示問合せループ
2. 出庫処理ループの状態確認
   1. 処理あり
      - フローの先頭へ戻る
   2. 処理なし
      - フロー継続
3. 問合せ送信
4. 通信ログ記録
5. レスポンス受信
   1. 200受信
      1. 通信ログ記録
      2. JOB番号重複確認
         1. 重複あり
            - フローの先頭へ戻る
         2. 重複なし
            - フロー継続
      3. 在庫確認
         1. 在庫なし
            - フローの先頭へ戻る
         2. 在庫あり
            - JOB情報を出庫バッファに保存
   2. 204受信
      - 通信ログ記録
   3. 異常レスポンス
      1. 通信ログ記録
      2. 復帰不可異常状態へ遷移
6. 5秒待機
7. フローの先頭へ戻る

### 入庫操作待機

1. 入庫操作受付ループ
2. 入庫処理ループの状態確認
  1. 処理あり
     - フローの先頭へ戻る
  2. 処理なし
     - フロー継続
3. 処理入力待機
4. 品種番号入力確認
5. 空き容量確認
   1. 空きなし
      1. エラーメッセージ表示
      2. フローの先頭へ戻る 
   2. 空きあり
      1. フロー継続
6. 品種番号をサーバーに送信
7. 通信ログ記録
   1. 201受信
      1. 通信ログ記録
      2. OB情報を入庫バッファに保存
   2. 422受信
      1. 通信ログ記録
      2. エラーメッセージを表示
8. フローの先頭へ戻る

### 出庫処理

1. 出庫処理ループ
2. 出庫バッファ確認
   1. JOBなし
      - フローの先頭へ戻る
   2. JOBあり
      - フロー継続
3. 状態を処理中に切り替え
4. 在庫データ削除
5. JOB開始報告送信
6. 通信ログ記録
7. レスポンス受信
   1. レスポンス異常
      1. 通信ログ記録
      2. 復帰不可異常へ遷移
   2. レスポンス正常
      1. 通信ログ記録
      2. フロー継続
8. JOB番号表示
9. 出庫完了待機
   1. 非常停止入力
      1. 処理をすべて中断
      2. 入出庫ログ記録
      3. 復帰可能異常へ遷移
   2. 出庫完了入力
      - 入出庫ログ記録
10. JOB完了報告送信
11. 通信ログ記録
   1. レスポンス異常
      1. 通信ログ記録
      2. 復帰不可異常へ遷移
   2. レスポンス正常
      - 通信ログ記録
12. 取出完了待機
   1. 非常停止入力
      1. 処理をすべて中断
      3. 復帰可能異常へ遷移
   2. 取出完了入力
      - フロー継続
13. 取出完了報告送信
14. 通信ログ記録
   1. レスポンス異常
      1. 通信ログ記録
      2. 復帰不可異常へ遷移
   2. レスポンス正常
      - 通信ログ記録
15. 出庫動作完了
16. フローの先頭へ戻る

### 入庫処理

1. 入庫処理ループ
2. 入庫バッファ確認
   1. JOBなし
      - フローの先頭へ戻る
   2. JOBあり
      - フロー継続
3. 状態を処理中に切り替え
4. JOB開始報告送信
5. 通信ログ記録
6. レスポンス受信
   1. レスポンス異常
      1. 通信ログ記録
      2. 復帰不可異常へ遷移
   2. レスポンス正常
      1. 通信ログ記録
      2. フロー継続
7. JOB番号表示
8. 入庫完了待機
   1. 非常停止入力
      1. 処理をすべて中断
      2. 入庫ログ記録
      3. 復帰可能異常へ遷移
   2. 入庫完了入力
      - 入出庫ログ記録
9. JOB完了報告送信
10. 通信ログ記録
   1. レスポンス異常
      1. 通信ログ記録
      2. 復帰不可異常へ遷移
   2. レスポンス正常
      - 通信ログ記録
11. 在庫データ登録
12. 入庫動作完了
13. フローの先頭へ戻る

## クラス

|クラス|メゾット|戻り値|説明|
|:---|:---|:---|:---|
|Program|||メインループ、HTTPクライアント設定、ロガー設定、SQLRepository設定|
||Main|void|プログラム起動処理と各タスクの呼び出し|
||Online|void|オンライン通知|
||Alarm|void|異常処理|
|WaitPickingReq|||出庫要求待機のタスク|
||WaitPicking|void|出庫要求待機|
||Unavailable|void|対応不可レスポンス処理|
|PollingPicking|||出庫指示問合せのタスク|
||Polling|void|出庫指示問合せ|
||AddJob|void|出庫処理バッファへのデータ格納|
|WaitStoringReq|||入庫要求待機のタスク|
||WaitStoring|void|入庫要求待機|
|PickingTask|||出庫処理のタスク|
||Picking|void|出庫処理|
|StoringTask|||入庫処理のタスク|
||Storing|void|入庫処理|
|ApiResponceHandler|||APIレスポンス判定|
||Checker|bool|APIレスポンス判定|
|ConsoleInput|||コンソール入力|
||InputAction|bool|動作入力|
||InputString|string?|文字入力|
|ISqlRepository|||在庫データアクセス用のインターフェース|
||GetInventory()|`Task<List<Inventory>>`||
||SearchInventory(string itemId)|`Task<int>`|| 
||UpdateInventory(string itemId)|`Task`||
||RemoveInventory(string itemId)|`Task`||
|SqlRepository|||在庫データ取得・更新|
||GetInventory|`Task<List<Inventory>>`|在庫一覧を取得する|
||SearchInventory(string itemId)|`Task<int>`|在庫確認を行う|
||RemoveInventory(string itemId)|`Task`|在庫テーブルへ出庫操作を行う|
||UpdateInventory(string itemId)|`Task`|在庫テーブルへ入庫操作を行う|
|OnlineBody|||オンライン通知要求用のレコード|
||AvailableCapacity|`int`|空き容量|
||Stocks|`List<Inventory>`|在庫テーブル内の商品個別ID|
|PutAwayBody|||入庫要求用のレコード|
||ItemCode|`string`|品種番号|
|JobBody|||JOB情報受け取り用のレコード|
||JobId|`string`|JOB番号|
||JobType|`string`|入出庫方向|
||ItemId|`string`|商品個別ID|
|AlarmBody|||異常報告用のレコード|
||AlarmCode|`string`|アラームの内容|
||OccurredAt|`DateTime`|発生日時|
|ErrorBody|||エラーメッセージ用のレコード|
||Error|`string`|API通信のエラーメッセージ|
|Inventory|||在庫情報の格納|
||ItemId|`string`|商品個別ID|
|PickingJob|||出庫処理バッファ|
||JobId|`string`|JOB番号|
||JobType|`string`|入出庫方向|
||ItemId|`string`|商品個別ID|
|StoringJob|||入庫処理バッファ|
||JobId|`string`|JOB番号|
||JobType|`string`|入出庫方向|
||ItemId|`string`|商品個別ID|
|SystemState|||状態フラグ|
||IsPicking|`bool`|出庫処理状態|
||IsStoring|`bool`|入庫処理状態|
||Emergency|`int`|異常状態|

Program
- HttpCrient生成
- Logger生成
- Repository生成
- メインループ開始

Task
- WaitPickingReq
- PollingPicking
- WaitStoringReq
- PickingTask
- StoringTask
- ApiResponceHandler
- ConsoleInput

Repositories
- ISqlRepository
  - GetInventory()
  - SearchInventory()
  - RemoveInventory()
  - UpdateInventory()
- SqlRepository
  - GetInventory
  - SearchInventory
  - RemoveInventory
  - UpdateInventory

Models
- OnlineBody
  - AvailableCapacity
  - Stocks
- PutAwayBody
  - ItemCode
- JobBody
  - JobId
  - JobType
  - ItemId
- AlarmBody
  - AlarmCode
  - OccurredAt
- ErrorBody
  - Error
- Inventory
  - ItemId
- PickingJob
  - JobId
  - JobType
  - ItemId
- StoringJob
  - JobId
  - JobType
  - ItemId
- SystemState
  - IsPicking
  - IsStoring
  - Emergency


## エラー処理

<img width="1687" height="425" alt="image" src="https://github.com/user-attachments/assets/5f305b7d-c258-452e-8147-204455586da4" />

