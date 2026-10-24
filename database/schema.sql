-- Reference copy of the two tables from the assessment brief, with the creation order fixed:
-- [Product] must exist before [Item] because Item.ProductId references it.
--
-- The application creates these tables itself through EF Core migrations
-- (src/Infrastructure/Data/Migrations), which produce the same columns, types and constraints.
-- Use this script only if you want to create them by hand in an empty database.

CREATE TABLE [dbo].[Product]
(
    [Id]          INT            NOT NULL PRIMARY KEY IDENTITY (1,1),
    [ProductName] NVARCHAR(255)  NOT NULL,
    [CreatedBy]   NVARCHAR(100)  NOT NULL,
    [CreatedOn]   DATETIME       NOT NULL,
    [ModifiedBy]  NVARCHAR(100)  NULL,
    [ModifiedOn]  DATETIME       NULL
);

CREATE TABLE [dbo].[Item]
(
    [Id]        INT NOT NULL PRIMARY KEY IDENTITY (1,1),
    [ProductId] INT NOT NULL FOREIGN KEY REFERENCES [dbo].[Product]([Id]),
    [Quantity]  INT NOT NULL
);

-- SQL Server does not index foreign keys automatically; items are always read by product.
CREATE INDEX [IX_Item_ProductId] ON [dbo].[Item]([ProductId]);
