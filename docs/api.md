# API仕様書

### サーバーAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|401|403|404|409|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|出庫依頼状態|スマホ|GET|/api/v1/picking-orders|-|-|○|-|-|○|-|-|-|
|オンライン要求|自動倉庫|POST|/api/v1/racks/online|-|-|-|-|-|○|-|-|-|
|次出庫JOB問合せ|自動倉庫|GET|/api/v1/racks/jobs|-|-|☆|-|-|○|-|-|-|
|JOB作業開始報告|自動倉庫|POST|/api/v1/racks/jobs/{id}/initiate|-|-|-|○|-|○|○|○|-|
|JOB作業完了報告|自動倉庫|POST|/api/v1/racks/jobs/{id}/complete|-|-|-|○|-|○|○|○|-|
|取出し完了報告|自動倉庫|POST|/api/v1/racks/jobs/{id}/remove|-|-|-|○|-|○|○|○|-|
|入庫要求|自動倉庫|POST|/api/v1/racks/putaway-order|-|○|○|○|○|○|-|-|○|
|アラーム報告|自動倉庫|POST|/api/v1/racks/alarms|-|○|-|-|○|○|-|-|-|

☆・・・ボディ有無の両方が存在

**エラー対処（スマホ）**
- 401：認証設定異常、プログラム継続不可能
- 403：アクセス権限不正、プログラム継続不可能

**エラー対処（スタブ）**
- 422：業務違反、プログラム継続可能なエラー
- 400：API契約違反、プログラム継続不可能
- 401：認証設定異常、プログラム継続不可能
- 403：アクセス権限不正、プログラム継続不可能
- 404：JOB管理不正、プログラム継続不可能
- 409：状態同期異常、プログラム継続不可能

**サーバーは、例外等の内部エラーは500を返す**

### 自動倉庫スタブAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|401|403|404|409|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|次出庫JOB送信　　|サーバー|POST|/api/v1/next-picking-order　　　|-|○|-|-|○|-|-|-|○|

**エラー対処（サーバー）**
- 400：API契約違反、オフライン化＆JOB再割当試行
- 409：JOB受領に失敗（状態不一致）、オフライン化＆JOB再割当試行

**スタブ側は、リスナー例外発生時は500を返さない**
- スタブ：アプリケーション終了
- サーバー：タイムアウト

### 表の見方
- 作業名：業務作業
- 利用者：APIの利用者（送信者）
- メソッド：HTTPメソッド
- URL：APIの公開URL
- ｸｴﾘ：クエリパラメータの有無
- Req：HTTPリクエストボディの有無
- Res：HTTPレスポンスボディの有無（☆はありとなしの両方）
- 422~409：HTTPステータスコードの実装有無

***

## 未完了出庫依頼取得

### リクエスト

``` http
GET /api/v1/picking-orders
```

### レスポンス

#### 200 OK

取得成功：作業途中のJOB一覧
``` json
{
  "available": [
    {
      "itemCode": "I01",
      "itemName": "部品A"
    },
    {
      "itemCode": "I03",
      "itemName": "部品C",
      "stockCount": 2
    },
    {
      "itemCode": "I04",
      "itemName": "部品D",
      "stockCount": 6
    },
    {
      "itemCode": "I05",
      "itemName": "部品E",
      "stockCount": 11
    }
  ],
  "statuses": [
    {
      "jobId": "J20260616-50",
      "itemCode": "I01",
      "itemName": "部品A",
      "status": "WaitOut",
      "equipmentId": "AS01",
      "canCancel": false
    },
    {
      "jobId": "J20260616-51",
      "itemCode": "I01",
      "itemName": "部品A",
      "status": "Transferring",
      "equipmentId": "AS03",
      "canCancel": false
    },
    {
      "jobId": "J20260616-52",
      "itemCode": "I01",
      "itemName": "部品A",
      "status": "Transferring",
      "equipmentId": "AS04",
      "canCancel": true
    },
    {
      "jobId": "J20260616-54",
      "itemCode": "I01",
      "itemName": "部品A",
      "status": "Waiting",
      "equipmentId": null,
      "canCancel": true
    }
  ]
}
```

取得成功：在庫なし、履歴なし
``` json
{
  "available": [],
  "statuses": []
}
```

**statuses.status凡例**
|状態|説明|備考|
|:---|:---|:---|
|Waiting|準備中|キャンセル可能|
|Transferring|搬送中||
|WaitOut|取出待ち||

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

***

## オンライン要求

### リクエスト

``` http
POST /api/v1/racks/online
```

#### リクエストボディ

オンライン要求
``` json
なし
```

### レスポンス

### 204 No Content

オンライン受理
``` json
なし
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

## 次出庫JOB問合せ

### リクエスト

``` http
POST /api/v1/racks/jobs
```

### レスポンス

#### 200 OK

出庫JOBが存在している
``` json
{
  "jobId": "J20260616-01",
  "jobType": "PICKING",
  "itemId": "I01-260616-004"
}
```

#### 204 No Content

出庫JOBがない
``` json
なし
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

## JOB作業開始報告

### リクエスト

``` http
POST /api/v1/racks/jobs/{id}/initiate
```

id : JOB番号（例：J20260616-01）

### レスポンス

#### 204 No Content

遷移成功
``` json
なし
```

#### 422 Unprocessable Content

開始遷移に失敗（開始ができない）
``` json
{
  "error": "INVALID_STATUS"
}
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

#### 403 Forbidden

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error": "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error": "JOB_NOT_FOUND"
}
```

## JOB作業完了報告

### リクエスト

``` http
POST /api/v1/racks/jobs/{id}/complete
```

id : JOB番号（例：J20260616-01）

### レスポンス

#### 204 No Content

遷移成功
``` json
なし
```

#### 422 Unprocessable Content

完了遷移に失敗（完了ができない）
``` json
{
  "error": "INVALID_STATUS"
}
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

#### 403 Forbidden

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error": "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error": "JOB_NOT_FOUND"
}
```

## 取出し完了報告

### リクエスト

``` http
POST /api/v1/racks/jobs/{id}/remove
```

id : JOB番号（例：J20260616-01）

### レスポンス

#### 204 No Content

遷移成功
``` json
なし
```

#### 422 Unprocessable Content

取出し完了遷移に失敗（取出しができない）
``` json
{
  "error": "INVALID_STATUS"
}
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

#### 403 Forbidden

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error": "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error": "JOB_NOT_FOUND"
}
```

## 入庫要求

### リクエスト

``` http
POST /api/v1/racks/putaway-order
```

入庫したい品種
``` json
{
  "itemCode": "I04"
}
```

### レスポンス

#### 201 Created

作成成功：入庫JOBデータを返却
``` json
{
  "jobId": "J20260616-22",
  "jobType": "PUTAWAY",
  "itemId": "I04-260616-037"
}
```

#### 422 Unprocessable Content

品種コードが不正
``` json
{
  "error": "INVALID_ITEM_CODE"
}
```

棚の空き容量がない
``` json
{
  "error": "NO_CAPACITY_AVAILABLE"
}
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error": "INVALID_REQUEST"
}
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

#### 409 Conflict

状態不一致（入庫JOBが実行中に送られた）
``` json
{
  "error": "INVALID_STATUS"
}
```

## アラーム報告

### リクエスト

``` http
POST /api/v1/racks/alarms
```

アラーム詳細（エラーの内容のこと）
``` json
{
  "alarmCode": "EMERGENCY_OFF",
  "occurredAt": "2026-06-16T15:30:00"
}
```

### レスポンス

#### 204 No Content

受領
``` json
なし
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error": "INVALID_REQUEST"
}
```

#### 401 Unauthorized

未登録の端末からの要求
``` json
{
  "error": "UNREGISTERED_DEVICE"
}
```

## その他汎用エラー

### サーバー -> スマホ・自動倉庫

#### 500 Internal Server Error

その他の予期しないエラー
``` json
{
  "error": "UNEXPECTED_ERROR"
}
```

***

## 次出庫JOB送信　　

### リクエスト

``` http
POST /api/v1/next-picking-order　　　
```

出庫JOBデータ
``` json
{
  "jobId": "J20260616-01",
  "jobType": "PICKING",
  "itemId": "I01-260616-004"
}
```

### レスポンス

#### 204 No Content

受領
``` json
なし
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error": "INVALID_REQUEST"
}
```

#### 409 Conflict

指令を受け付けられない
``` json
{
  "error": "CANNOT_DISPATCH"
}
```
