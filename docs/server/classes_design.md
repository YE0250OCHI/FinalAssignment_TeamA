# クラス設計書

## クラス一覧


## クラス図

``` mermaid
classDiagram

namespace Infrastructures{
  class JobDispatcher
}

namespace Repository{
  class JobsRepository
  class ItemsRepository
  class EquipmentsRepository
}

namespace UseCase{
  class JobIssuer
  class JobAssigner
  class JobManager
  class JobViewer
  class InventoryViewer

  class IJobsRepository
  class IItemsRepository
  class IEquipmentsRepository

  class IJobDispatcher
}

namespace ViewController{
  class Index
  class History
  class Inventory
}

namespace ApiController{
  class OrdersApi
  class RacksApi
}

namespace BackgroundService{
  class Timeout
}

namespace Shared{
  class ClientValidator
}


Index --> JobIssuer
Index --> JobAssigner
Index --> JobViewer
Index --> ClientValidator

History --> JobViewer
History --> ClientValidator

Inventory --> InventoryViewer
Inventory --> ClientValidator

OrdersApi --> JobViewer
OrdersApi --> ClientValidator

RacksApi --> JobIssuer
RacksApi --> JobAssigner
RacksApi --> JobManager
RacksApi --> ClientValidator

Timeout --> JobManager

JobIssuer --> IJobsRepository
JobIssuer --> IItemsRepository
JobIssuer --> IEquipmentsRepository

JobAssigner --> IJobsRepository
JobAssigner --> IItemsRepository
JobAssigner --> IEquipmentsRepository
JobAssigner --> IJobDispatcher

JobManager --> IJobsRepository
JobManager --> IItemsRepository
JobManager --> IEquipmentsRepository

JobViewer --> IJobsRepository

InventoryViewer --> IItemsRepository

IJobsRepository <|-- JobsRepository
IItemsRepository <|-- ItemsRepository
IEquipmentsRepository <|-- EquipmentsRepository

IJobDispatcher <|-- JobDispatcher

```


## クラス詳細
