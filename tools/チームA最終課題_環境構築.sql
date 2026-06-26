-- データベース
IF DB_ID('automatic_storage_db')  IS NULL
BEGIN
	EXEC('CREATE DATABASE automatic_storage_db')
END
GO

-- 接続
USE automatic_storage_db
GO

-- テーブル作成
CREATE TABLE dbo.equipments(
	id VARCHAR(10) PRIMARY KEY,
	equipment_status INT NOT NULL,
	available_capacity INT NOT NULL,
	picking_job_id VARCHAR(14) NULL,
	putaway_job_id VARCHAR(14) NULL
)
GO

CREATE TABLE dbo.item_types(
	code VARCHAR(10) PRIMARY KEY,
	name NVARCHAR(50) NOT NULL
)
GO

CREATE TABLE dbo.items(
	id VARCHAR(20) PRIMARY KEY,
	item_code VARCHAR(10) NOT NULL,
	stock_status INT NOT NULL,
	equipment_id VARCHAR(10) NOT NULL,
	registered_at DATETIME NOT NULL DEFAULT GETDATE(),
	picked_at DATETIME NULL,

	FOREIGN KEY (item_code) REFERENCES dbo.item_types(code),
	FOREIGN KEY (equipment_id) REFERENCES dbo.equipments(id)
)
GO

CREATE TABLE dbo.jobs(
	id VARCHAR(14) PRIMARY KEY,
    job_type INT NOT NULL,
    job_status INT NOT NULL,
    device_id VARCHAR(10) NULL,
    item_code VARCHAR(10) NOT NULL,
    item_id VARCHAR(20) NULL,
    equipment_id VARCHAR(10) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE(),
    assigned_at DATETIME NULL,
    initiated_at DATETIME NULL,
    completed_at DATETIME NULL,
    removed_at DATETIME NULL,
    closed_at DATETIME NULL,
	
	FOREIGN KEY (item_code) REFERENCES dbo.item_types(code),
	FOREIGN KEY (item_id) REFERENCES dbo.items(id),
	FOREIGN KEY (equipment_id) REFERENCES dbo.equipments(id)
)
GO

-- FKの追加
ALTER TABLE dbo.equipments
ADD CONSTRAINT FK_equipments_picking_job
FOREIGN KEY (picking_job_id)
REFERENCES dbo.jobs(id);

ALTER TABLE dbo.equipments
ADD CONSTRAINT FK_equipments_putaway_job
FOREIGN KEY (putaway_job_id)
REFERENCES dbo.jobs(id);
GO

-- マスターデータ追加
INSERT INTO dbo.item_types(code, name)
VALUES
	('I01', N'部品A'),
	('I02', N'部品B'),
	('I03', N'部品C'),
	('I04', N'部品D'),
	('I05', N'部品E'),
	('I06', N'部品F'),
	('I07', N'部品G'),
	('I08', N'部品H');
GO

INSERT INTO dbo.equipments(id, equipment_status, available_capacity)
VALUES
	('AS15', 0, 50),
	('AS24', 0, 60);
GO
