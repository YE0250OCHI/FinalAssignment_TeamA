# API仕様書

### サーバーAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|403|404|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|出庫依頼の作成|スマホ|POST|/api/v1/picking-orders|-|○|○|○|○|○|-|
|未完了出庫依頼取得|スマホ|GET|/api/v1/picking-orders|-|-|○|-|-|○|-|
|出庫依頼履歴取得|スマホ|GET|/api/v1/picking-orders/history|○|-|○|-|○|○|-|
|出庫依頼キャンセル|スマホ|POST|/api/v1/picking-orders/{id}/cancel|-|-|-|○|-|○|○|
|在庫一覧取得|スマホ|GET|/api/v1/items|-|-|○|-|-|○|-|
|出庫可能商品一覧取得|スマホ|GET|/api/v1/items/available|-|-|○|-|-|○|-|
|オンライン要求|自動倉庫|POST|/api/v1/racks/online|-|○|○|○|○|○|-|
|次出庫JOB問合せ|自動倉庫|POST|/api/v1/racks/job|-|-|☆|○|-|○|-|
|JOB作業開始報告|自動倉庫|POST|/api/v1/racks/job/{id}/initiate|-|-|☆|○|-|○|○|
|JOB作業完了報告|自動倉庫|POST|/api/v1/racks/job/{id}/complete|-|-|☆|○|-|○|○|
|取出し完了報告|自動倉庫|POST|/api/v1/racks/job/{id}/remove|-|-|-|○|-|○|○|
|入庫要求|自動倉庫|POST|/api/v1/racks/putaway-order|-|○|○|○|○|○|-|
|アラーム報告|自動倉庫|POST|/api/v1/racks/errors|-|○|-|-|○|○|-|

### 自動倉庫スタブAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|403|404|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|次出庫JOB送信　　|サーバー|POST|/api/v1/next-picking-order　　　|-|○|-|○|○|-|-|


**【表の見方】**
- 作業名：業務作業
- 利用者：APIの利用者（送信者）
- メソッド：HTTPメソッド
- URL：APIの公開URL
- ｸｴﾘ：クエリパラメータの有無
- Req：HTTPリクエストボディの有無
- Res：HTTPレスポンスボディの有無（☆はありとなしの両方）
- 422～404：エラーステータスコードの実装有無

※その他、内部エラーは共通で500を返す

***

## 出庫依頼の作成

### リクエスト

``` http
POST /api/v1/picking-orders
```

#### リクエストボディ

出庫したい商品ID
``` json
{
  "itemCode" : "I01"
}
```

### レスポンス

#### 201 Created

作成成功：採番されたJOB番号を返す
``` json
{
  "jobId" : "J20260616-01"
}
```

#### 422 Unprocessable Content

商品IDが不正
``` json
{
  "error" : "INVALID_PRODUCT_ID"
}
```

在庫がない
``` json
{
  "error" : "OUT_OF_STOCK"
}
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error" : "INVALID_REQUEST"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

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
  "count" : 6,
  "results" : [
    {
      "jobId" : "J20260616-49",
      "itemCode" : "I02",
      "itemName" : "部品B",
      "status" : "Recovering",
      "equipmentId" : null,
      "canCancel": true
    },
    {
      "jobId" : "J20260616-50",
      "itemCode" : "I01",
      "itemName" : "部品A",
      "status" : "WaitOut",
      "equipmentId" : "AS01",
      "canCancel": false
    },
    {
      "jobId" : "J20260616-51",
      "itemCode" : "I01",
      "itemName" : "部品A",
      "status" : "Working",
      "equipmentId" : "AS03",
      "canCancel": false
    },
    {
      "jobId" : "J20260616-52",
      "itemCode" : "I01",
      "itemName" : "部品A",
      "status" : "Waiting",
      "equipmentId" : "AS04",
      "canCancel": false
    },
    {
      "jobId" : "J20260616-53",
      "itemCode" : "I03",
      "itemName" : "部品C",
      "status" : "Waiting",
      "equipmentId" : "AS05",
      "canCancel": true
    },
    {
      "jobId" : "J20260616-54",
      "itemCode" : "I01",
      "itemName" : "部品A",
      "status" : "Waiting",
      "equipmentId" : null,
      "canCancel": true
    }
  ]
}
```

取得成功：履歴がないとき
``` json
{
  "count" : 0,
  "results" : []
}
```

**status補足**
- Waiting：作業開始前
- Working：出庫作業中
- WaitOut：商品取出待ち
- Recovering：異常発生により復旧・再割当処理中

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## 出庫依頼履歴取得

### リクエスト

``` http
GET /api/v1/picking-orders/history
```

#### クエリパラメータ

|項目|必須|値パラメータ名|パラメータ値|記載例|
|:---|:---:|:---|:---|:---|
|終了日フィルタ（開始）|-|from|検索の始め|from=2026-06-16|
|終了日フィルタ（開始）|-|to|検索の終わり|to=2026-06-16|
|並び順（最新順）|※|sort|latest|from=latest|
|並び順（登録順）|※|sort|oldest|sort=oldest|

※並び順は、どちらかを必ず指定すること

URL例
``` http
GET /api/v1/picking-orders/history?sort=latest&from=2026-06-16&to=2026-06-16
```

### レスポンス

#### 200 OK

取得成功：終了した出庫JOBの一覧
``` json
{
  "count" : 3,
  "results" : [
    {
      "jobId" : "J20260616-48",
      "itemCode" : "I02",
      "itemName" : "部品B",
      "status" : "Aborted",
      "closedAt" : "2026-06-16T17:01:00"
    },
    {
      "jobId" : "J20260616-51",
      "itemCode" : "I01",
      "itemName" : "部品A",
      "status" : "Canceled",
      "closedAt" : "2026-06-16T16:58:00"
    },
    {
      "jobId" : "J20260616-44",
      "itemCode" : "I01",
      "itemName" : "部品A",
      "status" : "Completed",
      "closedAt" : "2026-06-16T16:55:00"
    }
  ]
}
```

取得成功：履歴がないとき
``` json
{
  "count" : 0,
  "results" : []
}
```

**status補足**
- Completed：完了（正常終了）
- Canceled：キャンセル済み
- Aborted：異常終了

#### 400 Bad Request

クエリパラメータ異常
``` json
{
  "error" : "INVALID_QUERY"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## 出庫依頼キャンセル

### リクエスト

``` http
POST /api/v1/picking-orders/{id}/cancel
```

### レスポンス

#### 204 No Content

キャンセル遷移に成功
``` json
なし
```

#### 422 Unprocessable Content

キャンセル遷移に失敗（キャンセルができない）
``` json
{
  "error" : "INVALID_STATUS"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error" : "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error" : "JOB_NOT_FOUND"
}
```

## 在庫一覧取得

### リクエスト

``` http
GET /api/v1/items
```

### レスポンス

### 200 OK

取得成功：商品マスタに登録されている商品の在庫状況
``` json
[
  {
    "itemCode" : "I01",
    "itemName" : "部品A",
    "stockCount" : 10,
    "availableCount" : 6
  },
  {
    "itemCode" : "I02",
    "itemName" : "部品B",
    "stockCount" : 0,
    "availableCount" : 0
  },
  {
    "itemCode" : "I03",
    "itemName" : "部品C",
    "stockCount" : 7,
    "availableCount" : 7
  },
  {
    "itemCode" : "I04",
    "itemName" : "部品D",
    "stockCount" : 12,
    "availableCount" : 10
  },
  {
    "itemCode" : "I05",
    "itemName" : "部品E",
    "stockCount" : 0,
    "availableCount" : 0
  }
]
```

**補足**
- stockCount：総在庫数（物理的に保管されている在庫数）
- availableCount：出庫可能な個数（JOBに予約分を除いた個数）

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## 出庫可能商品一覧取得

### リクエスト

``` http
GET /api/v1/items/available
```

### レスポンス

### 200 OK

取得成功：現在出庫可能な商品の一覧（在庫あり品のみ）
``` json
[
  {
    "itemCode" : "I01",
    "itemName" : "部品A",
    "stockCount" : 10,
    "availableCount" : 6
  },
  {
    "itemCode" : "I03",
    "itemName" : "部品C",
    "stockCount" : 7,
    "availableCount" : 7
  },
  {
    "itemCode" : "I04",
    "itemName" : "部品D",
    "stockCount" : 12,
    "availableCount" : 10
  }
]
```

取得成功：出庫可能な商品が存在しない場合
``` json
[]
```

**補足**
- stockCount：総在庫数（物理的に保管されている在庫数）
- availableCount：出庫可能な個数（JOB予約分を除いた個数）

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## オンライン要求

### リクエスト

``` http
POST /api/v1/racks/online
```

#### リクエストボディ

オンライン試行時の装置情報（装置ID、空き容量、在庫情報）
``` json
{
  "equipmentId" : "AS01",
  "availableCapacity" : 47,
  "stocks" : [
    {
      "itemId" : "I01-260616-001"
    },
    {
      "itemId" : "I01-260616-021"
    },
    {
      "itemId" : "I01-260616-043"
    }
  ]
}
```

在庫なしの場合
``` json
{
  "equipmentId" : "AS01",
  "availableCapacity" : 50,
  "stocks" : []
}
```

### レスポンス

### 200 OK

オンライン受理：適用可能なJOBがあったときは、JOBを返却
``` json
{
  "jobId" : "J20260616-44",
  "jobType" : "PICKING",
  "itemId" : "I01-260616-023"
}
```

### 204 No Content

オンライン受理：適用可能なJOBないとき
``` json
なし
```

#### 422 Unprocessable Content

在庫情報がおかしい（完了JOBに同一IDが存在する）
``` json
{
  "error" : "DUPLICATE_STOCK"
}
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error" : "INVALID_REQUEST"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## 次出庫JOB問合せ

### リクエスト

``` http
POST /api/v1/racks/job
```

### レスポンス

#### 200 OK

出庫JOBが存在している
``` json
{
  "jobId" : "J20260616-01",
  "jobType" : "PICKING",
  "itemId" : "I01-260616-004"
}
```

#### 204 No Content

出庫JOBがない
``` json
なし
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## JOB作業開始報告

### リクエスト

``` http
POST /api/v1/racks/job/{id}/initiate
```

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
  "error" : "INVALID_STATUS"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error" : "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error" : "JOB_NOT_FOUND"
}
```

## JOB作業完了報告

### リクエスト

``` http
POST /api/v1/racks/job/{id}/complete
```

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
  "error" : "INVALID_STATUS"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error" : "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error" : "JOB_NOT_FOUND"
}
```

## 取出し完了報告

### リクエスト

``` http
POST /api/v1/racks/job/{id}/remove
```

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
  "error" : "INVALID_STATUS"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

JOBの発行元と、キャンセル送信者が異なる
``` json
{
  "error" : "ACCESS_DENIED_JOB"
}
```

#### 404 Not Found

指定されたJOBが存在しない（idが間違っている）
``` json
{
  "error" : "JOB_NOT_FOUND"
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
  "itemCode" : "I04"
}
```

### レスポンス

#### 201 Created

作成成功：入庫JOBデータを返却
``` json
{
  "jobId" : "J20260616-22",
  "jobType" : "PUTAWAY",
  "itemId" : "I04-260616-037"
}
```

#### 422 Unprocessable Content

商品IDが不正
``` json
{
  "error" : "INVALID_PRODUCT_ID"
}
```

空き容量がない
``` json
{
  "error" : "OUT_OF_STOCK"
}
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error" : "INVALID_REQUEST"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
}
```

## アラーム報告

### リクエスト

``` http
POST /api/v1/racks/errors
```

アラーム詳細（エラーの内容のこと）
``` json
{
  "alarmCode" : "EMERGENCY_OFF",
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
  "error" : "INVALID_REQUEST"
}
```

#### 403 Forbidden

未登録の端末からの要求
``` json
{
  "error" : "UNREGISTERED_DEVICE"
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
  "jobId" : "J20260616-01",
  "jobType" : "PICKING",
  "itemId" : "I01-260616-004"
}
```

### レスポンス

#### 204 No Content

受領
``` json
なし
```

#### 422 Unprocessable Content

拒否（対応不可）
``` json
{
  "error" : "CANNOT_DISPATCH"
}
```

#### 400 Bad Request

リクエスト形式が不正
``` json
{
  "error" : "INVALID_REQUEST"
}
```




