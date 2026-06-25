# DB設計書

## テーブル定義

### jobs：JOBデータ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|CHAR(14)|○|PK|JOB ID|
|job_type_id|INT|○|-|JOB種別|
|job_status|INT|○|-|JOB状態|
|device_id|VARCHAR(10)|-|-|スマホID|
|item_code|VARCHAR(10)|○|FK|品種番号|
|item_id|VARCHAR(20)|-|FK|商品個別ID|
|equipment_id|VARCHAR(10)|-|FK|装置ID|
|created_at|DATETIME|○|-|作成日時|
|assigned_at|DATETIME|-|-|JOB割当日時（配信日時）|
|initiated_at|DATETIME|-|-|作業開始日時|
|completed_at|DATETIME|-|-|搬送完了日時|
|removed_at|DATETIME|-|-|商品取出し日時（出庫のみ）|
|closed_at|DATETIME|-|-|終了日時|

- JOBの詳細、状態を表すテーブル
- item_id、equipment_id は商品の割当を行うまでは NULL 状態となる
- assigned_at～removed_atは、状態遷移日時を表す
  - タイムアウト監視に利用する
- closed_at が設定されたJOBは履歴扱いとする

### items：商品在庫データ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|VARCHAR(20)|○|PK|商品個別ID|
|item_code|VARCHAR(10)|○|FK|品種番号|
|stock_status|INT|○|-|在庫状態|
|equipment_id|VARCHAR(10)|○|FK|在庫保持している装置ID|
|registered_at|DATETIME|○|-|登録日時（入庫日時）|
|picked_at|DATETIME|-|-|出庫日時|

- サーバー管理下にある商品の在庫状態を表すテーブル
- 商品は搬送開始で管理外とし、物理削除を行う
  - 商品が管理対象である間は、紐づく自動倉庫が必ず存在する
  - したがって、equipment_idはNOT NULLとする
- 紐づく装置がオフラインになってもデータは保持するが、再オンライン化までは利用しない
- 自動倉庫が再オンライン化した際は、当該装置の最新在庫情報を反映し、サーバー上の在庫情報を補正する
 
### item_types：品種マスタ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|code|VARCHAR(10)|○|PK|品種番号|
|name|NVARCHAR(50)|○|-|商品名|

- 商品の品種を管理するためのマスタ
- 商品在庫状態の商品個別IDを、品種単位で分類するために利用する

### equipments：自動倉庫設備データ

|カラム名|型|NOT NULL|キー|説明|
|:---|:---|:---:|:---:|:---|
|id|VARCHAR(10)|○|PK|設備ID|
|equipment_status|INT|○|-|オンライン状況|
|available_capacity|INT|○|-|空き容量|
|picking_job_id|CHAR(14)|-|FK|割り当てられた出庫JOB番号|
|putaway_job_id|CHAR(14)|-|FK|割り当てられた入庫JOB番号|

- サーバー管理下にある自動倉庫設備の状態を管理するテーブル
- 自動倉庫のオンライン状態および担当JOBを管理するために利用する
- picking_job_id は、当該設備に割り当てられている出庫JOBを表す
- putaway_job_id は、当該設備に割り当てられている入庫JOBを表す
  - 各JOB ID は、該当するJOBが存在しない場合は NULL とする
  - JOBが完了、キャンセル、異常終了した場合は NULL に戻す

## データ初期値

### equipments：自動倉庫設備データ

|id|equipment_status_id|picking_job_id|putaway_job_id|備考|
|:---|:---|:---|:---|:---|
|AS01|1|初期オフライン||||
|AS02|1|初期オフライン||||

### item_types：品種マスタ

|code|name|current_sequence|sequence_updated_at|
|:---|:---|:---|:---|
|I01|部品A|0|20000101T00:00:00|
|I02|部品B|0|20000101T00:00:00|
|I03|部品C|0|20000101T00:00:00|
|I04|部品D|0|20000101T00:00:00|
|I05|部品E|0|20000101T00:00:00|
|I06|部品F|0|20000101T00:00:00|
|I07|部品G|0|20000101T00:00:00|
|I08|部品H|0|20000101T00:00:00|

## ER図

``` mermaid
erDiagram
    jobs {
        CHAR(14) id PK
        INT job_type_id FK
        INT job_status_id FK
        VARCHAR(10) device_id FK
        VARCHAR(10) item_code FK
        VARCHAR(20) item_id FK
        VARCHAR(10) equipment_id FK
        DATETIME created_at
        DATETIME delivered_at
        DATETIME initiated_at
        DATETIME completed_at
        DATETIME removed_at
        DATETIME closed_at
    }

    items {
        VARCHAR(20) id PK
        VARCHAR(10) item_code FK
        INT stock_status_id FK
        VARCHAR(10) equipment_id FK
        DATETIME registered_at
    }

    equipments {
        VARCHAR(10) id PK
        INT status_id FK
        CHAR(14) picking_job_id FK
        CHAR(14) putaway_job_id FK
    }

    item_types {
        VARCHAR(10) code PK
        NVARCHAR(50) name
        INT current_sequence
        DATETIME sequence_updated_at
    }

    jobs }o--|| item_types : "要求品種"
    jobs }o--|| items : "割当商品"
    jobs }o--|| equipments : "割当設備"

    items }o--|| item_types : "品種"
    items }o--|| equipments : "保管設備"

    equipments |o--|| jobs : "割当中の出庫JOB"
    equipments |o--|| jobs : "割当中の入庫JOB"
```

## データ操作方針

### CRUD対応表

|処理|jobs|items|item_types|equipments|備考|
|:---|:---:|:---:|:---:|:---:|:---|
|出庫JOB登録|C|R|R|-|品種の存在と在庫有無を確認してJOBを作成する|
|出庫JOBキャンセル|U|-|-|-|JOBをキャンセル状態にする|
|プッシュ配信|U|R/U|R|R/U|JOBを自動倉庫へ配信する|
|プル配信|U|R/U|R|R/U|自動倉庫へJOBを割り当てて返却する|
|出庫作業開始報告|U|U|-|U|商品を出庫済みに更新し、設備をBusyへ変更する|
|出庫搬送完了報告|U|-|-|U|WaitOutへ遷移する|
|商品取出し報告|U|-|-|U|JOB完了、設備をAvailableへ戻す|
|入庫JOB登録|C|-|R|R/U|品種の存在を確認して入庫JOBを作成し、設備をBusyへ変更する|
|入庫作業開始報告|U|-|-|-|Putawayへ遷移する|
|入庫完了報告|U|C|R/U|U|商品個別IDを採番し、itemsへ商品を登録する。JOB完了、設備をAvailableへ戻す|
|自動倉庫オンライン要求|R/U|R/U|R/U|U|最新の在庫情報を反映し、設備をAvailableへ変更する。割当可能なJOBがあれば配信する|
|自動倉庫エラー報告|U|U/D|-|U|JOB状態・在庫・設備状態を更新する|
|タイムアウト監視|R/U|U|-|R/U|タイムアウトを検知したら、JOBを異常終了させる|
|商品在庫一覧取得|-|R|R|-|品種別在庫数を取得する|
|未完了JOB一覧取得|R|-|-|-|closed_at IS NULLのJOBを取得する|
|JOB履歴取得|R|-|-|-|closed_at IS NOT NULLのJOBを取得する|
|サーバー再起動時|R/U|-|-|U|すべての未完了JOBを異常終了させ、装置もすべてオフライン化|

**記号の意味**
- C : 作成
- R : 参照
- U : 更新
- D : 削除

### 採番ルール

|項目|対象|形式|採番単位|採番方法|採番例|備考|
|:---|:---|:---|:---|:---|:---|:---|
|JOB番号|jobs.id|JyyyyMMdd-NN|日単位|当日作成済みJOBの最大連番+1|J20260616-01|入出庫で共通の採番体系とする|
|商品ID|items.id|{item_types.code}-yyMMdd-NNN|品種・日単位|品種のcurrent_sequence+1|I01-260617-001|日付変更時はcurrent_sequenceを0にリセットする|

- JOB番号は、1日の出庫回数を60回以下と想定し、枝番2桁とする
- 商品IDは、物理的な保管量と1日当たりの出庫最大回数を考慮し、枝番3桁とする

### データの保持と削除

|対象|保持・削除方針|
|:---|:---|
|jobs|JOB履歴として保持する。完了・キャンセル・異常終了時は closed_at を設定し、クローズ済みとして扱う。|
|items|サーバー管理下にある在庫のみ保持する。出庫作業開始時に商品は管理対象外となるため、物理削除する。|
|equipments|設備マスタ兼状態データとして保持する。削除は行わず、状態更新のみ行う。|
|devices|操作端末マスタとして保持する。削除は行わない。|
|item_types|品種マスタおよび商品ID採番情報として保持する。削除は行わない。|
|各種状態マスタ|JOB状態、JOB種別、設備状態、在庫状態は固定マスタとして保持する。削除は行わない。|
|エラー情報|DBには保持しない。アプリケーションログに出力する。|

**補足**
- 紐づく装置がOfflineになっても items の在庫データは保持する
- ただし、Offline中の装置に紐づく在庫は引当対象外とする
- 自動倉庫の再オンライン化時に最新在庫情報を反映し、サーバー上の在庫情報を補正する

