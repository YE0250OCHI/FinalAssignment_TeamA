DELETE FROM [dbo].[jobs];

UPDATE [dbo].[items]
SET [picked_at] = NULL, [stock_status] = 0;

SELECT TOP (1000) [id]
      ,[item_code]
      ,[stock_status]
      ,[equipment_id]
      ,[registered_at]
      ,[picked_at]
FROM [automatic_storage_db].[dbo].[items]
ORDER BY [registered_at] ASC

