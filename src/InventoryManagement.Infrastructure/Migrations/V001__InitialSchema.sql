IF OBJECT_ID('Categories') IS NULL
CREATE TABLE Categories (Id uniqueidentifier NOT NULL PRIMARY KEY, Name nvarchar(150) NOT NULL, Description nvarchar(500) NULL, IsActive bit NOT NULL, CreatedAt datetimeoffset NOT NULL, UpdatedAt datetimeoffset NULL, CONSTRAINT UQ_Categories_Name UNIQUE (Name));
IF OBJECT_ID('Products') IS NULL
CREATE TABLE Products (Id uniqueidentifier NOT NULL PRIMARY KEY, Name nvarchar(150) NOT NULL, Description nvarchar(1000) NULL, Sku nvarchar(80) NOT NULL, Price decimal(18,2) NOT NULL, CategoryId uniqueidentifier NOT NULL, IsActive bit NOT NULL, CurrentStock int NOT NULL CONSTRAINT CK_Products_CurrentStock CHECK (CurrentStock >= 0), CreatedAt datetimeoffset NOT NULL, UpdatedAt datetimeoffset NULL, CONSTRAINT UQ_Products_Sku UNIQUE (Sku), CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id));
IF OBJECT_ID('InventoryMovements') IS NULL
CREATE TABLE InventoryMovements (Id uniqueidentifier NOT NULL PRIMARY KEY, ProductId uniqueidentifier NOT NULL, Type int NOT NULL, Quantity int NOT NULL CONSTRAINT CK_InventoryMovements_Quantity CHECK (Quantity > 0), Reason nvarchar(500) NULL, CreatedAt datetimeoffset NOT NULL, CONSTRAINT CK_InventoryMovements_Type CHECK (Type IN (1,2)), CONSTRAINT FK_InventoryMovements_Products FOREIGN KEY (ProductId) REFERENCES Products(Id));
IF NOT EXISTS (SELECT 1 FROM Categories)
INSERT INTO Categories (Id, Name, Description, IsActive, CreatedAt) VALUES ('11111111-1111-1111-1111-111111111111', 'General', 'Default category', 1, SYSUTCDATETIME());
