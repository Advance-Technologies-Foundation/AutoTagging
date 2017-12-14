DECLARE
  TableExists NUMBER;
BEGIN
  SELECT COUNT(*) INTO TableExists FROM TAB t WHERE t.tname = 'SysAzureCogInfo';
  IF TableExists < 1 THEN
  EXECUTE IMMEDIATE 'CREATE TABLE "SysAzureCogInfo" (' ||
      '"Id"               VARCHAR2(38 BYTE)  NOT NULL,' ||
      '"CreatedOn"        TIMESTAMP(6)       DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP),' ||
      '"CreatedById"      VARCHAR2(38 BYTE),' ||
      '"ApiKey"           VARCHAR2(50 CHAR) DEFAULT '''','||
      'CONSTRAINT "PK_Id_AzureCogInfo" PRIMARY KEY ("Id") ' ||
          'USING INDEX TABLESPACE USERS STORAGE (INITIAL 64 K ' ||
              'NEXT 1 M MAXEXTENTS UNLIMITED))';
   END IF; 
END;
/