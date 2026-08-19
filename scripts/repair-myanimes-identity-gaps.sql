SET XACT_ABORT ON;
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

BEGIN TRANSACTION;

DECLARE @Targets TABLE
(
    Id int NOT NULL PRIMARY KEY,
    Titulo nvarchar(200) NOT NULL
);

INSERT INTO @Targets (Id, Titulo)
VALUES
    (3333, N'Tokyo 24-ku'),
    (3334, N'Trillion Game'),
    (4333, N'Tsue to Tsurugi no Wistoria'),
    (4334, N'Toji no Miko'),
    (4335, N'Tonari no Youkai-san'),
    (4336, N'Fractale'),
    (4337, N'Make My Day'),
    (4338, N'Ji Yao Lu: Qicheng Pian'),
    (4339, N'Gensoumaden Saiyuuki'),
    (4340, N'Dolls'' Frontline');

IF EXISTS
(
    SELECT 1
    FROM @Targets AS target
    LEFT JOIN dbo.MyAnimes AS myAnime WITH (UPDLOCK, HOLDLOCK)
        ON myAnime.Id = target.Id
        AND myAnime.Titulo = target.Titulo
    WHERE myAnime.Id IS NULL
)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51000, 'Um dos dez IDs nao possui o titulo esperado. Nada foi alterado.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.MyAnimes AS myAnime WITH (UPDLOCK, HOLDLOCK)
    WHERE myAnime.Id > 2333
      AND NOT EXISTS (SELECT 1 FROM @Targets AS target WHERE target.Id = myAnime.Id)
)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51001, 'Existe outro MyAnime acima do ID 2333. Nada foi alterado.', 1;
END;

DECLARE @UnlinkedAnimes int;
DECLARE @DeletedRows int;

UPDATE anime
SET MyAnimeID = 0
FROM dbo.Animes AS anime
INNER JOIN @Targets AS target ON target.Id = anime.MyAnimeID;

SET @UnlinkedAnimes = @@ROWCOUNT;

DELETE myAnime
FROM dbo.MyAnimes AS myAnime
INNER JOIN @Targets AS target
    ON target.Id = myAnime.Id
    AND target.Titulo = myAnime.Titulo;

SET @DeletedRows = @@ROWCOUNT;

IF @DeletedRows <> 10
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51002, 'A quantidade de colecoes removidas nao foi dez. Nada foi confirmado.', 1;
END;

DBCC CHECKIDENT ('dbo.MyAnimes', RESEED, 2333) WITH NO_INFOMSGS;

COMMIT TRANSACTION;

SELECT
    @DeletedRows AS DeletedRows,
    @UnlinkedAnimes AS UnlinkedAnimes,
    IDENT_CURRENT('dbo.MyAnimes') AS CurrentIdentity,
    MAX(Id) AS MaxMyAnimeId
FROM dbo.MyAnimes;
