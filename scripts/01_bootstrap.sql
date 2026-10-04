-- Yalnýzca ilk kurulum içindir.
-- postgres veritabanýna yönetim hesabýyla baðlanarak çalýþtýrýn.
-- Transaction içine almayýn; CREATE DATABASE bunu desteklemez.
-- Veritabaný ve hesaplar zaten mevcutsa bu dosyayý çalýþtýrmayýn.

-- Ödev veritabanýný oluþturur.
CREATE DATABASE spirometry_db
    WITH ENCODING 'UTF8';

-- Tüm kiracýlar için aktarým yapacak sýnýrlý hesabý oluþturur.
CREATE ROLE spirometry_importer
    LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOINHERIT
    NOREPLICATION
    NOBYPASSRLS;

-- T-01 kiracýsýnýn sonuçlarýný okuyacak hesabý oluþturur.
CREATE ROLE spirometry_osgb_a
    LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOINHERIT
    NOREPLICATION
    NOBYPASSRLS;

-- T-02 kiracýsýnýn sonuçlarýný okuyacak hesabý oluþturur.
CREATE ROLE spirometry_osgb_b
    LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOINHERIT
    NOREPLICATION
    NOBYPASSRLS;

-- Parolalar bu dosyada tutulmaz.
-- Hesap parolalarýný psql içindeki \password komutuyla belirleyin.
-- Tablo yetkileri ve RLS, 02_schema_and_rls.sql ile uygulanýr.