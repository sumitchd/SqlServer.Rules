
CREATE PROCEDURE dbo.ExecInto
AS
set nocount on
DECLARE @SQL NVARCHAR(2048)
INSERT INTO dbo.TestTableSSDT
EXEC (@sql)

--SML021
