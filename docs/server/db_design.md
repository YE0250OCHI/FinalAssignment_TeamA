# DB設計書

## テーブル定義

### jobs：JOBデータ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|CHAR(14)|○|PK|JOB ID|
|job_type_id|INT|○|FK|JOB種別|
|job_status_id|INT|○|FK|JOB状態|
|device_id|VARCHAR(10)|○|FK|スマホID|
|item_code|VARCHAR(10)|○|FK|品種番号|
|item_id|VARCHAR(20)|-|FK|商品個別ID|
|equipment_id|VARCHAR(10)|-|FK|装置ID|
|created_at|DATETIME|○|-|作成日時|
|delivered_at|DATETIME|-|-|JOB配信日時|
|initiated_at|DATETIME|-|-|作業開始日時|
|completed_at|DATETIME|-|-|搬送完了日時|
|removed_at|DATETIME|-|-|商品取出し日時（出庫のみ）|
|closed_at|DATETIME|-|-|終了日時|

- JOBの詳細、状態を表すテーブル
- item_id、equipment_idは商品の割当を行うまではNULL状態となる
- delivered_at～removed_atは、各状態の開始日時を表す
  - タイムアウト監視に利用する
  - 再割当等により状態が戻る場合はNULLに戻す
- closed_atがNULLではない場合、そのJOBはクローズしたものとみなす

### items：商品在庫データ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|VARCHAR(20)|○|PK|商品個別ID|
|item_code|VARCHAR(10)|○|FK|品種番号|
|stock_status_id|INT|○|FK|在庫状態|
|equipment_id|VARCHAR(10)|○|FK|在庫保持している装置ID|
|stored_at|DATETIME|-|-|入庫日時|
|shipped_at|DATETIME|-|-|出庫日時|

- サーバー管理下にある商品の在庫状態を表すテーブル
- 商品は搬送開始で管理外とし、物理削除を行う
  - 商品が管理対象である間は、紐づく自動倉庫が必ず存在するので、装置IDはNOT NULLとする
 
### equipments：自動倉庫設備データ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|VARCHAR(10)|○|PK|設備ID|
|equipment_status_id|INT|○|FK|設備状態|

- サーバー管理下にある自動倉庫設備の状態を管理するテーブル
- 自動倉庫のオンライン状態および作業可否の管理に利用する

### devices：スマートフォン端末マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|VARCHAR(10)|○|PK|スマホID|

- 操作用端末のマスタ
- JOBの依頼元端末を識別するために利用する
- 操作用端末の状態は管理しない

### item_types：品種マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|code|VARCHAR(10)|○|PK|品種番号|
|name|NVARCHAR(50)|○|-|商品名|

- 商品の品種を表すマスタ
- 商品在庫状態の商品個別IDを、品種単位で分類するために利用する

### job_status : JOB状態マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|INT|○|PK|状態キー|
|name|NVARCHAR(15)|○|-|状態名|

- JOB状態を表すマスタ
- JOBの進捗状況を管理・識別するために利用する

### job_types : JOB種別マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|INT|○|PK|状態キー|
|name|NVARCHAR(15)|○|-|状態名|

- JOB種別を表すマスタ
- JOBの種別を管理・識別するために利用する

### equipment_status : 自動倉庫設備状態マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|INT|○|PK|状態キー|
|name|NVARCHAR(15)|○|-|状態名|

- 自動倉庫設備の状態を表すマスタ
- 自動倉庫設備の状態を管理・識別するために利用する

### stock_status : 商品在庫状態マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|INT|○|PK|状態キー|
|name|NVARCHAR(15)|○|-|状態名|

- 商品在庫の状態を表すマスタ
- 商品在庫の状態を管理・識別するために利用する

## データ初期値

### equipments：自動倉庫設備データ

|id|equipment_status_id|備考|
|:---|:---|:------|
|AS01|1|初期オフライン|
|AS02|1|初期オフライン|

### devices：スマートフォン端末マスタ

|id|備考|
|:---|:------|
|SP01||
|SP02||

### item_types：品種マスタ

|code|name|備考|
|:---|:---|:------|
|I01|部品A||
|I02|部品B||
|I03|部品C||
|I04|部品D||
|I05|部品E||
|I06|部品F||
|I07|部品G||
|I08|部品H||

### job_status : JOB状態マスタ

|id|name|備考|
|:---|:---|:------|
|1|Unassigned|JOBが作成されたが、商品が割当られていない|
|2|Assigned|JOBに商品が割当られた|
|3|Delivered|自動倉庫にJOBが配信された|
|4|Picking|自動倉庫が出庫を始めた|
|5|WaitOut|出庫が完了したが、商品が取り出されていない|
|6|Putaway|自動倉庫が入庫を始めた|
|7|Completed|JOBが完了した|
|8|Canceled|JOBがキャンセルされた|
|9|Interrupted|JOB実行中の異常により、処理を中断している|
|10|Pending|商品の再割当を待っている|
|11|Aborted|再割当可能な商品が存在せず、JOBが成立しなくなった|

### job_types : JOB種別マスタ

|id|name|備考|
|:---|:---|:------|
|1|Picking|出庫JOB|
|2|Putaway|入庫JOB|

### equipment_status : 自動倉庫設備状態マスタ

|id|name|備考|
|:---|:---|:------|
|1|Offline|装置の生存が確認できない|
|2|Available|装置がJOB受付可能|
|3|Busy|装置がJOBを実行している|

### stock_status : 商品在庫状態マスタ

|id|name|備考|
|:---|:---|:------|
|1|Stored|商品が棚に保管されている|
|2|Reserved|商品がJOBに割り当てられて、作業開始を待っている|




