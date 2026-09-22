IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'IX_Products_CategoryId_IsActive')
    CREATE INDEX IX_Products_CategoryId_IsActive ON dbo.Products(CategoryId, IsActive);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.InventoryMovements') AND name = 'IX_InventoryMovements_ProductId_CreatedAt_Id')
    CREATE INDEX IX_InventoryMovements_ProductId_CreatedAt_Id
        ON dbo.InventoryMovements(ProductId, CreatedAt DESC, Id DESC) INCLUDE (Type, Quantity, Reason);
