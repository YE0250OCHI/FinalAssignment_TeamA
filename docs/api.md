## 概要

### サーバーAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|403|404|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|出庫依頼の作成|スマホ|POST|/api/v1/picking-orders|-|○|-|○|○|○|-|
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

※その他、内部エラーは共通で500を返す

### 自動倉庫スタブAPI一覧

|作業名|利用者|メソッド|URL|ｸｴﾘ|Req|Res|422|400|403|404|
|:---|:---|:---|:---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
|次出庫JOB送信　　|サーバー|POST|/api/v1/next-picking-order　　　|-|○|-|○|○|-|-|

※その他、内部エラーは共通で500を返す

### 表の見方

- 作業名：業務作業
- 利用者：APIの利用者（送信者）
- メソッド：HTTPメソッド
- URL：APIの公開URL
- ｸｴﾘ：クエリパラメータの有無
- Req：HTTPリクエストボディの有無
- Res：HTTPレスポンスボディの有無（☆はありとなしの両方）
- 422～404：エラーステータスコードの実装有無

## 出庫依頼

aaa
