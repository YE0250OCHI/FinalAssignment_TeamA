# API仕様書

### サーバーAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|403|404|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|出庫依頼の作成|スマホ|POST|/api/v1/picking-orders|-|○|○|○|○|○|-|
|未完了出庫依頼取得|スマホ|GET|/api/v1/picking-orders|-|-|○|-|-|○|-|
|出庫依頼履歴取得|スマホ|GET|/api/v1/picking-orders/history|○|-|○|-|○|○|-|
|出庫依頼キャンセル|スマホ|POST|/api/v1/picking-orders/{id}/cancel|-|-|-|○|-|○|○|
|商品一覧取得|スマホ|GET|/api/v1/picking-orders/items|-|-|○|-|-|○|-|
|オンライン要求|自動倉庫|POST|/api/v1/racks/online|-|○|○|○|○|○|-|
|次出庫JOB問合せ|自動倉庫|POST|/api/v1/racks/job|-|-|☆|○|○|○|-|
|JOB作業開始報告|自動倉庫|POST|/api/v1/racks/job/{id}/initiate|-|-|☆|○|-|○|○|
|JOB作業完了報告|自動倉庫|POST|/api/v1/racks/job/{id}/complete|-|-|☆|○|-|○|○|
|取出し完了報告|自動倉庫|POST|/api/v1/racks/job/{id}/remove|-|-|-|○|-|○|○|
|入庫要求|自動倉庫|POST|/api/v1/racks/putaway-order|-|○|○|○|○|○|-|
|エラー報告|自動倉庫|POST|/api/v1/racks/errors|-|○|-|-|○|○|-|

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
|並び順（最新順）|sort|※|latest|from=latest|
|並び順（登録順）|sort|※|oldest|sort=oldest|

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






## 商品一覧取得

### リクエスト

``` http
GET /api/v1/picking-orders/items
```

### レスポンス






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
    { "itemId" : "I01-260616-001" },
    { "itemId" : "I01-260616-021" },
    { "itemId" : "I01-260616-043" }
  ]
}
```

### レスポンス






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
  "itemCode" : "I01",
  "itemId" : "I01-260616-004",
  "equipmentId" : "AS01"
}
```

#### 204 No Content



## JOB作業開始報告

### リクエスト

``` http
POST /api/v1/racks/job/{id}/initiate
```

### レスポンス






## JOB作業完了報告

### リクエスト

``` http
POST /api/v1/racks/job/{id}/complete
```

### レスポンス






## 取出し完了報告

### リクエスト

``` http
POST /api/v1/racks/job/{id}/remove
```

### レスポンス






## 入庫要求

### リクエスト

``` http
POST /api/v1/racks/putaway-order
```

入庫したい品種
``` json
{
  "equipmentId" : "AS01",
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
  "itemCode" : "I04",
  "itemId" : "I04-260616-037",
  "equipmentId" : "AS01"
}
```


## エラー報告

### リクエスト

``` http
POST /api/v1/racks/errors
```

装置に発生したエラー（アラーム）
``` json
{
  "equipmentId" : "AS01",
  "alarmCode" : "EMERGENCY_OFF",
  "occurredAt": "2026-06-16T15:30:00Z"
}
```

### レスポンス






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
  "itemCode" : "I01",
  "itemId" : "I01-260616-004",
  "equipmentId" : "AS01"
}
```

### レスポンス








