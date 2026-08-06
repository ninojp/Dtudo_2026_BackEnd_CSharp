:setvar MyAnimesDatabase ""
:setvar IdentityDatabase ""
:setvar ApiMyAnimesPrincipal ""
:setvar BackupPrincipal ""

SET NOCOUNT ON;

IF ISNULL(CONVERT(int, SERVERPROPERTY('IsIntegratedSecurityOnly')), 0) <> 1
    THROW 51000, 'A instancia SQL deve aceitar somente Windows Authentication.', 1;

IF DB_ID(N'$(MyAnimesDatabase)') IS NULL
    THROW 51001, 'O banco MyAnimes da baseline nao existe.', 1;

IF DB_ID(N'$(IdentityDatabase)') IS NULL
    THROW 51002, 'O banco Identity da baseline nao existe.', 1;

DECLARE @apiLogin sysname = N'$(ApiMyAnimesPrincipal)';
DECLARE @backupLogin sysname = N'$(BackupPrincipal)';
DECLARE @databaseName sysname;
DECLARE @databaseSql nvarchar(max);

IF SUSER_ID(@apiLogin) IS NULL
BEGIN
    SET @databaseSql = N'CREATE LOGIN ' + QUOTENAME(@apiLogin) + N' FROM WINDOWS;';
    EXEC sys.sp_executesql @databaseSql;
END;

IF SUSER_ID(@backupLogin) IS NULL
BEGIN
    SET @databaseSql = N'CREATE LOGIN ' + QUOTENAME(@backupLogin) + N' FROM WINDOWS;';
    EXEC sys.sp_executesql @databaseSql;
END;

SET @databaseName = N'$(MyAnimesDatabase)';
SET @databaseSql = N'
USE ' + QUOTENAME(@databaseName) + N';
IF USER_ID(N''' + REPLACE(@apiLogin, '''', '''''') + N''') IS NULL
    CREATE USER ' + QUOTENAME(@apiLogin) + N' FOR LOGIN ' + QUOTENAME(@apiLogin) + N';
IF USER_ID(N''' + REPLACE(@apiLogin, '''', '''''') + N''') IS NOT NULL
BEGIN
    ALTER ROLE [db_datareader] ADD MEMBER ' + QUOTENAME(@apiLogin) + N';
    ALTER ROLE [db_datawriter] ADD MEMBER ' + QUOTENAME(@apiLogin) + N';
END;
IF USER_ID(N''' + REPLACE(@backupLogin, '''', '''''') + N''') IS NULL
    CREATE USER ' + QUOTENAME(@backupLogin) + N' FOR LOGIN ' + QUOTENAME(@backupLogin) + N';
IF USER_ID(N''' + REPLACE(@backupLogin, '''', '''''') + N''') IS NOT NULL
    ALTER ROLE [db_backupoperator] ADD MEMBER ' + QUOTENAME(@backupLogin) + N';';
EXEC sys.sp_executesql @databaseSql;

SET @databaseName = N'$(IdentityDatabase)';
SET @databaseSql = N'
USE ' + QUOTENAME(@databaseName) + N';
IF USER_ID(N''' + REPLACE(@backupLogin, '''', '''''') + N''') IS NULL
    CREATE USER ' + QUOTENAME(@backupLogin) + N' FOR LOGIN ' + QUOTENAME(@backupLogin) + N';
IF USER_ID(N''' + REPLACE(@backupLogin, '''', '''''') + N''') IS NOT NULL
    ALTER ROLE [db_backupoperator] ADD MEMBER ' + QUOTENAME(@backupLogin) + N';';
EXEC sys.sp_executesql @databaseSql;
