/*
    RVS 2025/26 - Web aplikacija za evidenciju učlanjivanja kandidata
    u sportski klub

    Skript je namenjen instalaciji na čistoj SQL Server/LocalDB instanci.
    Zbog zaštite postojećih podataka namerno prekida rad ako baza već postoji.
*/

USE master;
GO

IF DB_ID(N'RVS2026SportskiKlub') IS NOT NULL
BEGIN
    THROW 50001, N'Baza RVS2026SportskiKlub već postoji. Instalacioni skript nije izvršen da postojeći podaci ne bi bili obrisani.', 1;
END;
GO

CREATE DATABASE RVS2026SportskiKlub;
GO

USE RVS2026SportskiKlub;
GO

CREATE TABLE dbo.Kandidat
(
    JMBG CHAR(13) NOT NULL,
    Ime NVARCHAR(50) NOT NULL,
    Prezime NVARCHAR(50) NOT NULL,
    DatumRodjenja DATE NOT NULL,
    Pol NCHAR(1) NOT NULL,
    Drzavljanstvo NVARCHAR(50) NOT NULL,
    Adresa NVARCHAR(120) NOT NULL,
    KontaktTelefon NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) NULL,

    CONSTRAINT PK_Kandidat PRIMARY KEY (JMBG),
    CONSTRAINT CK_Kandidat_JMBG_Cifre CHECK (JMBG NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_Kandidat_Pol CHECK (Pol IN (N'M', N'Ž')),
    CONSTRAINT CK_Kandidat_DatumRodjenja CHECK (DatumRodjenja >= '19000101')
);
GO

CREATE TABLE dbo.SportskaDisciplina
(
    IDSportskeDiscipline INT IDENTITY(1,1) NOT NULL,
    Sifra NVARCHAR(10) NOT NULL,
    Naziv NVARCHAR(60) NOT NULL,
    Aktivna BIT NOT NULL CONSTRAINT DF_SportskaDisciplina_Aktivna DEFAULT (1),

    CONSTRAINT PK_SportskaDisciplina PRIMARY KEY (IDSportskeDiscipline),
    CONSTRAINT UQ_SportskaDisciplina_Sifra UNIQUE (Sifra),
    CONSTRAINT UQ_SportskaDisciplina_Naziv UNIQUE (Naziv)
);
GO

CREATE TABLE dbo.ZahtevZaUclanjenje
(
    IDZahteva INT IDENTITY(1,1) NOT NULL,
    BrojZahteva NVARCHAR(30) NOT NULL,
    JMBGKandidata CHAR(13) NOT NULL,
    IDSportskeDiscipline INT NOT NULL,
    DatumPodnosenja DATE NOT NULL CONSTRAINT DF_Zahtev_DatumPodnosenja DEFAULT (CONVERT(date, GETDATE())),
    Sezona CHAR(7) NOT NULL,
    MestoKluba NVARCHAR(60) NOT NULL,
    DatumSportskogPregleda DATE NOT NULL,
    RezultatTestaSposobnosti NVARCHAR(20) NOT NULL,
    StatusZahteva NVARCHAR(20) NOT NULL CONSTRAINT DF_Zahtev_Status DEFAULT (N'U obradi'),
    Napomena NVARCHAR(500) NULL,

    CONSTRAINT PK_ZahtevZaUclanjenje PRIMARY KEY (IDZahteva),
    CONSTRAINT UQ_ZahtevZaUclanjenje_Broj UNIQUE (BrojZahteva),
    CONSTRAINT FK_Zahtev_Kandidat FOREIGN KEY (JMBGKandidata)
        REFERENCES dbo.Kandidat(JMBG),
    CONSTRAINT FK_Zahtev_SportskaDisciplina FOREIGN KEY (IDSportskeDiscipline)
        REFERENCES dbo.SportskaDisciplina(IDSportskeDiscipline),
    CONSTRAINT CK_Zahtev_Sezona_Format CHECK
        (Sezona LIKE '[1-2][0-9][0-9][0-9]/[0-9][0-9]'),
    CONSTRAINT CK_Zahtev_RezultatTesta CHECK
        (RezultatTestaSposobnosti IN (N'Položen', N'Nije položen', N'Nije realizovan')),
    CONSTRAINT CK_Zahtev_Status CHECK
        (StatusZahteva IN (N'U obradi', N'Na proveri', N'Odobren', N'Odbijen'))
);
GO

CREATE TABLE dbo.Dokumentacija
(
    IDDokumentacije INT IDENTITY(1,1) NOT NULL,
    IDZahteva INT NOT NULL,
    NazivDokumenta NVARCHAR(100) NOT NULL,
    Dostavljeno BIT NOT NULL CONSTRAINT DF_Dokumentacija_Dostavljeno DEFAULT (0),

    CONSTRAINT PK_Dokumentacija PRIMARY KEY (IDDokumentacije),
    CONSTRAINT UQ_Dokumentacija_Zahtev_Naziv UNIQUE (IDZahteva, NazivDokumenta),
    CONSTRAINT FK_Dokumentacija_Zahtev FOREIGN KEY (IDZahteva)
        REFERENCES dbo.ZahtevZaUclanjenje(IDZahteva) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.RoditeljStaratelj
(
    IDRoditeljaStaratelja INT IDENTITY(1,1) NOT NULL,
    IDZahteva INT NOT NULL,
    ImePrezime NVARCHAR(100) NOT NULL,
    JMBG CHAR(13) NOT NULL,
    Srodstvo NVARCHAR(40) NOT NULL,
    KontaktTelefon NVARCHAR(20) NOT NULL,
    Email NVARCHAR(100) NULL,

    CONSTRAINT PK_RoditeljStaratelj PRIMARY KEY (IDRoditeljaStaratelja),
    CONSTRAINT UQ_RoditeljStaratelj_Zahtev UNIQUE (IDZahteva),
    CONSTRAINT CK_RoditeljStaratelj_JMBG_Cifre CHECK (JMBG NOT LIKE '%[^0-9]%'),
    CONSTRAINT FK_RoditeljStaratelj_Zahtev FOREIGN KEY (IDZahteva)
        REFERENCES dbo.ZahtevZaUclanjenje(IDZahteva) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.IstorijaStatusaZahteva
(
    IDIstorije INT IDENTITY(1,1) NOT NULL,
    IDZahteva INT NOT NULL,
    StariStatus NVARCHAR(20) NULL,
    NoviStatus NVARCHAR(20) NOT NULL,
    DatumPromene DATETIME2(0) NOT NULL CONSTRAINT DF_Istorija_DatumPromene DEFAULT (SYSDATETIME()),
    KorisnickoIme NVARCHAR(50) NOT NULL,
    Napomena NVARCHAR(250) NULL,

    CONSTRAINT PK_IstorijaStatusaZahteva PRIMARY KEY (IDIstorije),
    CONSTRAINT FK_Istorija_Zahtev FOREIGN KEY (IDZahteva)
        REFERENCES dbo.ZahtevZaUclanjenje(IDZahteva) ON DELETE CASCADE,
    CONSTRAINT CK_Istorija_StariStatus CHECK
        (StariStatus IS NULL OR StariStatus IN (N'U obradi', N'Na proveri', N'Odobren', N'Odbijen')),
    CONSTRAINT CK_Istorija_NoviStatus CHECK
        (NoviStatus IN (N'U obradi', N'Na proveri', N'Odobren', N'Odbijen'))
);
GO

CREATE TABLE dbo.Korisnik
(
    IDKorisnika INT IDENTITY(1,1) NOT NULL,
    KorisnickoIme NVARCHAR(50) NOT NULL,
    Sifra NVARCHAR(100) NOT NULL,
    Ime NVARCHAR(50) NOT NULL,
    Prezime NVARCHAR(50) NOT NULL,
    Uloga NVARCHAR(30) NOT NULL,
    Aktivan BIT NOT NULL CONSTRAINT DF_Korisnik_Aktivan DEFAULT (1),

    CONSTRAINT PK_Korisnik PRIMARY KEY (IDKorisnika),
    CONSTRAINT UQ_Korisnik_KorisnickoIme UNIQUE (KorisnickoIme),
    CONSTRAINT CK_Korisnik_Uloga CHECK (Uloga IN (N'Administrator', N'Referent'))
);
GO

CREATE INDEX IX_Zahtev_Status ON dbo.ZahtevZaUclanjenje(StatusZahteva);
CREATE INDEX IX_Zahtev_DatumPodnosenja ON dbo.ZahtevZaUclanjenje(DatumPodnosenja);
CREATE INDEX IX_Zahtev_Disciplina ON dbo.ZahtevZaUclanjenje(IDSportskeDiscipline);
CREATE INDEX IX_Istorija_Zahtev_Datum ON dbo.IstorijaStatusaZahteva(IDZahteva, DatumPromene DESC);
GO

CREATE PROCEDURE dbo.PrijaviKorisnika
    @KorisnickoIme NVARCHAR(50),
    @Sifra NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        IDKorisnika,
        KorisnickoIme,
        Ime,
        Prezime,
        Uloga
    FROM dbo.Korisnik
    WHERE KorisnickoIme = @KorisnickoIme
      AND Sifra = @Sifra
      AND Aktivan = 1;
END;
GO

INSERT INTO dbo.SportskaDisciplina (Sifra, Naziv, Aktivna)
VALUES
    (N'KOS', N'Košarka', 1),
    (N'FUD', N'Fudbal', 1),
    (N'ODB', N'Odbojka', 1),
    (N'PLV', N'Plivanje', 1),
    (N'ATL', N'Atletika', 1),
    (N'TEN', N'Tenis', 1);
GO

INSERT INTO dbo.Korisnik (KorisnickoIme, Sifra, Ime, Prezime, Uloga, Aktivan)
VALUES
    (N'admin', N'admin123', N'Admin', N'Sistema', N'Administrator', 1),
    (N'referent', N'referent123', N'Milan', N'Petrović', N'Referent', 1);
GO

INSERT INTO dbo.Kandidat
    (JMBG, Ime, Prezime, DatumRodjenja, Pol, Drzavljanstvo, Adresa, KontaktTelefon, Email)
VALUES
    ('1206000123456', N'Nikola', N'Jovanović', '20000612', N'M', N'Srbija', N'Bulevar oslobođenja 10, Novi Sad', N'064111222', N'nikola.jovanovic@example.com'),
    ('1505010123456', N'Marko', N'Petrović', '20100115', N'M', N'Srbija', N'Cara Dušana 25, Zrenjanin', N'064333444', N'marko.petrovic@example.com'),
    ('2307995123456', N'Jelena', N'Marković', '19950723', N'Ž', N'Srbija', N'Kralja Petra 8, Zrenjanin', N'064555666', N'jelena.markovic@example.com');
GO

INSERT INTO dbo.ZahtevZaUclanjenje
    (BrojZahteva, JMBGKandidata, IDSportskeDiscipline, DatumPodnosenja, Sezona, MestoKluba,
     DatumSportskogPregleda, RezultatTestaSposobnosti, StatusZahteva, Napomena)
VALUES
    (N'ZSK-2026-000001', '1206000123456', 1, '20260801', '2026/27', N'Zrenjanin', DATEADD(MONTH, -2, CONVERT(date, GETDATE())), N'Položen', N'Odobren', N'Ispunjeni uslovi za učlanjivanje.'),
    (N'ZSK-2026-000002', '1505010123456', 2, '20260805', '2026/27', N'Zrenjanin', DATEADD(MONTH, -7, CONVERT(date, GETDATE())), N'Položen', N'Na proveri', N'Sportski pregled je potrebno obnoviti.'),
    (N'ZSK-2026-000003', '2307995123456', 4, '20260810', '2026/27', N'Zrenjanin', DATEADD(MONTH, -1, CONVERT(date, GETDATE())), N'Nije realizovan', N'U obradi', N'Čeka se realizacija testa sposobnosti.');
GO

INSERT INTO dbo.Dokumentacija (IDZahteva, NazivDokumenta, Dostavljeno)
VALUES
    (1, N'Fotografija kandidata', 1),
    (1, N'Dokaz identiteta', 1),
    (1, N'Potvrda o sportskom pregledu', 1),
    (1, N'Evidencija o položenom testu sposobnosti', 1),
    (1, N'Saglasnost roditelja/staratelja', 0),
    (1, N'Drugi dokument', 0),
    (2, N'Fotografija kandidata', 1),
    (2, N'Dokaz identiteta', 1),
    (2, N'Potvrda o sportskom pregledu', 1),
    (2, N'Evidencija o položenom testu sposobnosti', 1),
    (2, N'Saglasnost roditelja/staratelja', 1),
    (2, N'Drugi dokument', 0),
    (3, N'Fotografija kandidata', 1),
    (3, N'Dokaz identiteta', 1),
    (3, N'Potvrda o sportskom pregledu', 1),
    (3, N'Evidencija o položenom testu sposobnosti', 0),
    (3, N'Saglasnost roditelja/staratelja', 0),
    (3, N'Drugi dokument', 0);
GO

INSERT INTO dbo.RoditeljStaratelj
    (IDZahteva, ImePrezime, JMBG, Srodstvo, KontaktTelefon, Email)
VALUES
    (2, N'Milan Petrović', '0101980123456', N'Otac', N'064777888', N'milan.petrovic@example.com');
GO

INSERT INTO dbo.IstorijaStatusaZahteva
    (IDZahteva, StariStatus, NoviStatus, DatumPromene, KorisnickoIme, Napomena)
VALUES
    (1, NULL, N'U obradi', '20260801 09:00:00', N'admin', N'Zahtev je evidentiran.'),
    (1, N'U obradi', N'Na proveri', '20260801 09:15:00', N'admin', N'Pokrenuta je provera uslova.'),
    (1, N'Na proveri', N'Odobren', '20260801 09:30:00', N'admin', N'Poslovno pravilo je zadovoljeno.'),
    (2, NULL, N'U obradi', '20260805 10:00:00', N'referent', N'Zahtev je evidentiran.'),
    (2, N'U obradi', N'Na proveri', '20260805 10:10:00', N'referent', N'Pregled dokumentacije.'),
    (3, NULL, N'U obradi', '20260810 11:00:00', N'referent', N'Zahtev je evidentiran.');
GO
