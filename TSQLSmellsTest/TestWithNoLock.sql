CREATE PROCEDURE dbo.TestWithNoLock
AS
SET nocount on 
SELECT idcol FROM dbo.TestTableSSDT WITH(NOLOCK)
GO
-- SML003
