CREATE OR REPLACE TYPE AUTO_TAGS_RECORD FORCE IS OBJECT (tag VARCHAR2(1000));
/
CREATE OR REPLACE TYPE AUTO_TAGS_TABLE IS TABLE OF AUTO_TAGS_RECORD;
/
CREATE OR REPLACE PACKAGE "tspkg_AutoTagger"
AS
	FUNCTION "fnSplitString"(string IN VARCHAR2, delimiter IN CHAR) RETURN AUTO_TAGS_TABLE;
	PROCEDURE "tsp_AssignTags" (tags IN VARCHAR2, schemaName IN VARCHAR2, recordId IN VARCHAR2);	
END "tspkg_AutoTagger";
/
CREATE OR REPLACE PACKAGE BODY "tspkg_AutoTagger"
AS
	FUNCTION "fnSplitString"(string IN VARCHAR2, delimiter IN CHAR)
	RETURN AUTO_TAGS_TABLE
	IS
			l_res_coll AUTO_TAGS_TABLE;
	BEGIN
		SELECT AUTO_TAGS_RECORD(REGEXP_SUBSTR(string,'[^' || delimiter || ']+', 1, LEVEL)) BULK COLLECT INTO l_res_coll FROM DUAL
  		CONNECT BY REGEXP_SUBSTR(string, '[^' || delimiter || ']+', 1, LEVEL) IS NOT NULL;
		RETURN l_res_coll;
	END;
	
	PROCEDURE "tsp_AssignTags" (tags IN VARCHAR2, schemaName IN VARCHAR2, recordId IN VARCHAR2)
	IS
	BEGIN
		EXECUTE IMMEDIATE 'INSERT INTO "' || schemaName || 'Tag" ("Id", "Name", "TypeId") '
			|| 'SELECT "tspkg_Utilities"."fn_CreateGuid", sp.tag, ''{D6FB4DE6-0809-41FE-A84F-6D245CBC5F32}'' FROM '
			|| 'TABLE("tspkg_AutoTagger"."fnSplitString"(''' || tags || ''', '';'')) sp '
			|| 'WHERE NOT EXISTS (SELECT * FROM "' || schemaName || 'Tag" lt '
			|| 'WHERE lt."Name" = sp.tag) and sp.tag IS NOT NULL';

		EXECUTE IMMEDIATE 'INSERT INTO "' || schemaName || 'InTag" ("Id", "TagId", "EntityId") '
			|| 'SELECT "tspkg_Utilities"."fn_CreateGuid", lt."Id", ''' || recordId || ''' '
			|| 'FROM "' || schemaName || 'Tag" lt '
			|| 'WHERE EXISTS ('
			||	'SELECT sp.tag FROM TABLE("tspkg_AutoTagger"."fnSplitString"(''' || tags || ''', '';'')) sp '
			||	'WHERE sp.tag = lt."Name"'
			|| ') AND NOT EXISTS ('
			||	'SELECT * FROM "' || schemaName || 'InTag" lit '
			||	'WHERE lit."TagId" = lt."Id" AND lit."EntityId" = ''' || recordId || ''' )';
	END;
END "tspkg_AutoTagger";