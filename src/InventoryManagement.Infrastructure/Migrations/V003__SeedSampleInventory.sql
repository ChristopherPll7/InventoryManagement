DECLARE @createdAt datetimeoffset = SYSUTCDATETIME();

-- Reuse the baseline category and any existing categories with these names.
INSERT INTO dbo.Categories (Id, Name, Description, IsActive, CreatedAt)
SELECT NEWID(), seed.Name, seed.Description, 1, @createdAt
FROM (VALUES
    (N'General', N'Default category'),
    (N'Electronics', N'Electronic devices and accessories'),
    (N'Office Supplies', N'Everyday office materials')
) AS seed(Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories category WHERE category.Name = seed.Name);

IF EXISTS (SELECT 1 FROM dbo.Categories WHERE Name IN (N'General', N'Electronics', N'Office Supplies') AND IsActive = 0)
    THROW 51012, 'Sample products require active categories.', 1;

DECLARE @products TABLE (
    Id uniqueidentifier NOT NULL,
    Name nvarchar(150) NOT NULL,
    Sku nvarchar(80) NOT NULL,
    Price decimal(18,2) NOT NULL,
    CategoryName nvarchar(150) NOT NULL,
    Quantity int NOT NULL
);

INSERT INTO @products (Id, Name, Sku, Price, CategoryName, Quantity)
VALUES
    (NEWID(), N'Storage Box', N'SAMPLE-BOX-001', 12.50, N'General', 10),
    (NEWID(), N'USB Keyboard', N'SAMPLE-KEYBOARD-001', 25.00, N'Electronics', 20),
    (NEWID(), N'Notebook', N'SAMPLE-NOTEBOOK-001', 3.75, N'Office Supplies', 30);

-- Categories exist before products; the migration transaction also covers stock and history.
INSERT INTO dbo.Products (Id, Name, Description, Sku, Price, CategoryId, IsActive, CurrentStock, CreatedAt)
SELECT seed.Id, seed.Name, N'Sample inventory product', seed.Sku, seed.Price, category.Id, 1, seed.Quantity, @createdAt
FROM @products seed
JOIN dbo.Categories category ON category.Name = seed.CategoryName;

-- Type 1 is Entry. Each initial balance has a matching immutable movement.
INSERT INTO dbo.InventoryMovements (Id, ProductId, Type, Quantity, Reason, CreatedAt)
SELECT NEWID(), Id, 1, Quantity, N'Initial sample inventory', @createdAt
FROM @products;
