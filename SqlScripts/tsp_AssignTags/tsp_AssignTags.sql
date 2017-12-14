IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[fnSplitString]'))
BEGIN
	DROP FUNCTION [dbo].[fnSplitString];
END;
GO
CREATE FUNCTION [dbo].[fnSplitString] 
( 
    @string NVARCHAR(MAX), 
    @delimiter CHAR(1) 
) 
RETURNS @output TABLE(splitdata NVARCHAR(MAX) 
) 
BEGIN 
    DECLARE @start INT, @end INT 
    SELECT @start = 1, @end = CHARINDEX(@delimiter, @string) 
    WHILE @start < LEN(@string) + 1 BEGIN 
        IF @end = 0  
            SET @end = LEN(@string) + 1
       
        INSERT INTO @output (splitdata)  
        VALUES(SUBSTRING(@string, @start, @end - @start)) 
        SET @start = @end + 1 
        SET @end = CHARINDEX(@delimiter, @string, @start)
        
    END 
    RETURN 
END
GO
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tsp_AssignTags]'))
BEGIN
	DROP PROCEDURE [dbo].[tsp_AssignTags];
END;
GO
CREATE PROCEDURE [dbo].[tsp_AssignTags]
	@tags nvarchar(max),
	@schemaName nvarchar(50),
	@recordId uniqueidentifier
AS
BEGIN
	SET NOCOUNT ON
	IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(@schemaName + 'Tag'))
		AND EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(@schemaName + 'InTag'))
	BEGIN
		declare @handle nvarchar(max) = 'insert into '+ @schemaName +'Tag (Id, Name, TypeId) ' +
		'select NEWID(), sp.splitdata, ''{D6FB4DE6-0809-41FE-A84F-6D245CBC5F32}'' ' +
		'from dbo.[fnSplitString]('''+ @tags +''', '';'') sp  ' +
		'where not exists ( ' +
			'select * from '+@schemaName+'Tag lt ' +
			'where lt.Name = sp.splitdata ' +
		') and splitdata != '''' ';
		exec sp_executesql @handle

		declare @handle2 NVARCHAR(MAX) = 'insert into ' + @schemaName + 'InTag (Id, TagId, EntityId) '
			+ 'select NEWID(), lt.Id, ''' + CAST(@recordId as nvarchar(50)) + ''' '
			+ 'from ' + @schemaName + 'Tag lt '
			+ 'where exists ('
			+	'select 1, sp.splitdata '
			+	'from dbo.[fnSplitString](''' + @tags + ''', '';'') sp '
			+	'where sp.splitdata = lt.Name'
			+') and not exists ('
			+	'select 1'
			+	'from ' + @schemaName + 'InTag lit '
			+	'where lit.TagId = lt.id and lit.EntityId = ''' + CAST(@recordId as nvarchar(50)) + ''' '
			+')';

		exec sp_executesql @handle2
	END;
END
GO
