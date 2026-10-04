Spirometri XML İçe Aktarımı
BigSafeHealth Ödev 2 için hazırlanmıştır. Verilen sentetik NDD EasyOne XML dosyalarını okur, orders.csv üzerinden iş emri ve kiracı eşleştirmesini yapar ve sonuçları PostgreSQL'e kaydeder. Sonuçlar WPF masaüstü uygulaması veya ASP.NET Core MVC ekranından görüntülenebilir.
Teknolojiler ve proje yapısı
- C# / .NET 10
- Entity Framework Core 10 ve Npgsql PostgreSQL sağlayıcısı
- PostgreSQL 18 ve Row Level Security (RLS)
- WPF ve ASP.NET Core MVC
Komutlarda çalışma klasörü, case.slnx dosyasının bulunduğu solution köküdür.
Yol	Görevi
case/Spirometry.Importer.csproj	CSV/XML okuma, doğrulama, içe aktarma ve işlem günlükleri
Spirometry.Data/Spirometry.Data.csproj	Ortak DbContext, entity ve EF model yapılandırmaları
Spirometry.Viewer/Spirometry.Viewer.csproj	WPF sonuç ve ölçüm ekranı
Spirometry.Web/Spirometry.Web.csproj	MVC giriş, sonuç listesi ve detay ekranı
case/Data/orders.csv	İş emri, kiracı ve çalışan eşleştirmesi
case/Data/xml/	Altı örnek XML dosyası
case/Persistence/Migrations/	Şema ve RLS migration'ları
scripts/01_bootstrap.sql	İlk kurulum için veritabanı ve sınırlı hesaplar
scripts/02_schema_and_rls.sql	Migration'lardan üretilmiş tablo, ilişki ve RLS scripti


Ortak projeye taşınan bazı sınıfların namespace'i Spirometry.Importer.Persistence olarak korunmuştur. Bu sınıflar fiziksel olarak Spirometry.Data projesindedir. Migration'lar importer projesinde kalır.
Gereksinimler
- Tam solution ve WPF uygulaması için Windows ve .NET 10 SDK.
- Visual Studio kullanılıyorsa ASP.NET/web ve .NET masaüstü geliştirme iş yükleri.
- Çalışan PostgreSQL sunucusu. Geliştirme ortamında PostgreSQL 18 kullanıldı.
- Yerel bağlantı: localhost:5432, veritabanı: spirometry_db.
- PostgreSQL yönetim hesabı ve ilk kurulum için rol/veritabanı oluşturma yetkisi.
Bağlantı adresi, port ve veritabanı adı mevcut factory/controller kodlarında yerel demo için sabittir. Farklı bir ortamda importer factory'leri, WPF ResultReader, web AccountController ve WebDbContextFactory içindeki bağlantı ayarları birlikte güncellenmelidir. Parolalar kaynak koda yazılmaz.
1. PostgreSQL veritabanı ve hesapları
Bu bölüm yeni bir kurulum içindir. Mevcut veritabanını veya rolleri silmeyin; daha önce oluşturulduysa oluşturma adımlarını tekrarlamayın.
PowerShell'de PostgreSQL yönetim hesabıyla psql açın. Kurulum yolunuz farklıysa yolu değiştirin:
& "D:\PostgreSQL\bin\psql.exe" -h localhost -p 5432 -U postgres -d postgres -W
psql içinde aşağıdaki SQL'i çalıştırın. CREATE DATABASE komutunu bir transaction içine almayın:
CREATE DATABASE spirometry_db WITH ENCODING 'UTF8';

CREATE ROLE spirometry_importer
    LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
    NOINHERIT NOREPLICATION NOBYPASSRLS;

CREATE ROLE spirometry_osgb_a
    LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
    NOINHERIT NOREPLICATION NOBYPASSRLS;

CREATE ROLE spirometry_osgb_b
    LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE
    NOINHERIT NOREPLICATION NOBYPASSRLS;
Her hesap için ayrı bir parola belirleyin. Aşağıdakiler SQL değil, psql komutlarıdır; pgAdmin Query Tool'da çalıştırılmaz:
\password spirometry_importer
\password spirometry_osgb_a
\password spirometry_osgb_b
\c spirometry_db
Ardından bağlantı ve şema erişimini verin:
GRANT CONNECT ON DATABASE spirometry_db
TO spirometry_importer, spirometry_osgb_a, spirometry_osgb_b;

GRANT USAGE ON SCHEMA public
TO spirometry_importer, spirometry_osgb_a, spirometry_osgb_b;
psql'den \q ile çıkın. PostgreSQL kimlik doğrulaması bu hesaplar için parola doğrulaması kullanmalıdır; parolasız trust yapılandırması bu demonun giriş varsayımına uygun değildir.
2. Derleme ve migration
Solution kökünde:
dotnet restore .\case.slnx
dotnet build .\case.slnx
Visual Studio'da case.slnx dosyasını açın. Tools > NuGet Package Manager > Package Manager Console üzerinden aşağıdakileri çalıştırın. Bu komutlar normal PowerShell için değildir:
$migrationSecret = Read-Host "PostgreSQL postgres parolasi" -AsSecureString
$env:SPIROMETRY_MIGRATION_PASSWORD = [System.Net.NetworkCredential]::new("", $migrationSecret).Password
$migrationSecret.Dispose()
Remove-Variable migrationSecret
Update-Database -Context SpirometryDbContext -Project Spirometry.Importer -StartupProject Spirometry.Importer
Remove-Item Env:SPIROMETRY_MIGRATION_PASSWORD -ErrorAction SilentlyContinue
PMC komutlarını sırayla, tek tek çalıştırın. Migration hata verse de en sondaki ortam değişkeni temizleme komutunu çalıştırın.
Mevcut migration'lar uygulanır; kurulumu yapmak için yeniden Add-Migration çalıştırılmaz. InitialCreate tabloları, AddRowLevelSecurity erişim yetkilerini ve RLS politikalarını oluşturur. RLS migration'ından önce yukarıdaki üç rol oluşturulmuş olmalıdır. Importer factory'lerinde migration assembly'si Spirometry.Importer olarak tanımlıdır.
Alternatif: SQL dosyalarıyla veritabanı kurulumu
Bu yol, yukarıdaki manuel veritabanı/rol oluşturma ve Update-Database adımlarının alternatifidir. İki kurulumu arka arkaya uygulamanız gerekmez. Mevcut çalışan veritabanınız varsa 01_bootstrap.sql dosyasını yeniden çalıştırmayın; dosya yalnızca ilk kurulum içindir ve mevcut nesneleri silmez veya atlamaz.
GitHub deposunu klonlayıp case.slnx dosyasının bulunduğu klasörde PowerShell açın. Komutlardaki psql yolunu kendi PostgreSQL kurulumunuza göre değiştirin.
A. Veritabanını ve hesapları oluşturun
& "D:\PostgreSQL\bin\psql.exe" -X -h localhost -p 5432 -U postgres -d postgres -W -v ON_ERROR_STOP=1 -f ".\scripts\01_bootstrap.sql"
Bu adım başarılı olmadan sonraki adıma geçmeyin. CREATE DATABASE transaction içinde çalışmadığından komuta --single-transaction eklemeyin. Hata halinde önceki başarılı komutlar geri alınmaz; mevcut durumu kontrol ederek ilerleyin.
B. Hesap parolalarını belirleyin
& "D:\PostgreSQL\bin\psql.exe" -X -h localhost -p 5432 -U postgres -d spirometry_db -W
Açılan psql ekranında aşağıdaki komutları tek tek çalıştırın. Bunları normal PowerShell veya pgAdmin Query Tool'a yapıştırmayın:
\password spirometry_importer
\password spirometry_osgb_a
\password spirometry_osgb_b
\q
Her hesap için ayrı parola belirleyin. Gerçek parolaları SQL dosyalarına veya GitHub deposuna eklemeyin.
C. Şema ve RLS scriptini uygulayın
PowerShell'e döndükten sonra hedef veritabanına uygulayın:
& "D:\PostgreSQL\bin\psql.exe" -X -h localhost -p 5432 -U postgres -d spirometry_db -W -v ON_ERROR_STOP=1 -f ".\scripts\02_schema_and_rls.sql"
Bu dosya mevcut EF Core migration'larından Script-Migration -Idempotent ile üretilmiştir. Tabloları, indeksleri, ilişkileri, hesap yetkilerini ve RLS politikalarını oluşturur. __EFMigrationsHistory üzerinden uygulanmış migration'ları atlar; elle değiştirilmiş bir şemayı otomatik onarmaz. Standart public şeması/search_path yapılandırması varsayılır.
Scriptte örnek test verisi veya hesap parolası yoktur. Kurulumdan sonra aşağıdaki importer adımıyla örnek verileri yükleyin. Bu SQL kurulum yolu dosya üzerinden incelenmiştir; temiz bir PostgreSQL ortamında ayrıca uçtan uca denenmemiştir.
Yeni migration eklendiğinde scripti güncellemek için Visual Studio Package Manager Console'da migration parolası ortam değişkenini tanımlayın ve şu komutu çalıştırın (çıktı yolu solution kökündeki scripts klasörü olmalıdır):
Script-Migration -Idempotent -Context SpirometryDbContext -Project Spirometry.Importer -StartupProject Spirometry.Importer -Output "C:\PROJE_KOKU\scripts\02_schema_and_rls.sql"
C:\PROJE_KOKU örnek yolunu kendi solution klasörünüzle değiştirin. Bu komut yalnızca dosya üretir; veritabanına uygulamaz. Sonrasında SPIROMETRY_MIGRATION_PASSWORD ortam değişkenini kaldırın.
3. XML içe aktarma
Solution kökünde normal PowerShell açın:
$importSecret = Read-Host "spirometry_importer parolasi" -AsSecureString
try {
    $env:SPIROMETRY_IMPORT_PASSWORD = [System.Net.NetworkCredential]::new("", $importSecret).Password

    dotnet run --project .\case\Spirometry.Importer.csproj

    Write-Host "Importer cikis kodu: $LASTEXITCODE"
}
finally {
    Remove-Item Env:SPIROMETRY_IMPORT_PASSWORD -ErrorAction SilentlyContinue
    $importSecret.Dispose()
    Remove-Variable importSecret
}
CSV ve XML dosyaları derleme sırasında uygulamanın çıktı klasöründeki Data klasörüne kopyalanır. Uygulama AppContext.BaseDirectory üzerinden bu kopyaları okur. Kaynak dosyalar case/Data altında tutulmalıdır.
En az bir dosya başarısızsa çıkış kodu 1, tüm dosyalar başarılı/uyarılı/tekrar nedeniyle atlanmışsa 0 olur. Örnek sette kasıtlı olarak bozuk XML bulunduğundan normal örnek çalıştırmada 1 beklenir; bu, uygulamanın kontrolsüz çöktüğü anlamına gelmez.
4. Masaüstü ekranı
dotnet run --project .\Spirometry.Viewer\Spirometry.Viewer.csproj
OSGB A veya B seçilir ve ilgili PostgreSQL hesabının parolası girilir. Sonuçları Getir düğmesi testleri listeler; test seçilince alt tabloda ölçümler gösterilir. OSGB değiştiğinde önceki hesabın sonuçları temizlenir. Burada postgres veya importer parolası kullanılmaz.
5. Web ekranı
İlk kullanımda yerel HTTPS sertifikasını güvenilir hale getirin, ardından web uygulamasını başlatın:
dotnet dev-certs https --trust
dotnet run --project .\Spirometry.Web\Spirometry.Web.csproj --launch-profile https
Konsolda yazan HTTPS adresini açıp /Account/Login yoluna gidin. Port, web projesinin Properties/launchSettings.json dosyasına bağlıdır. Visual Studio'da da Spirometry.Web başlangıç projesi ve HTTPS profili seçilebilir.
- OSGB A: spirometry_osgb_a, kiracı T-01.
- OSGB B: spirometry_osgb_b, kiracı T-02.
- Doğru parola ile sonuç listesi açılır; Detay düğmesi ölçümleri gösterir.
- Çıkış POST ve antiforgery doğrulaması ile yapılır.
Web girişinde seçilen hesapla yeni PostgreSQL bağlantısı açılarak kimlik bilgileri doğrulanır. Tarayıcıdaki korumalı giriş çerezi sadece rastgele oturum kimliğini taşır. Kullanıcı adı, kiracı ve yeniden bağlantı için gereken parola sunucu belleğinde 20 dakika tutulur. Çerez ve bellek kaydının süresi otomatik uzatılmaz. Çıkışta bellek kaydı ve çerez kaldırılır; uygulama yeniden başlarsa tekrar giriş gerekir.
Sonuç controller'ı [Authorize] ile korunur. Her istekte bellekteki oturumun mevcut olduğu kontrol edilir. Test ve ölçüm sorguları ilgili OSGB hesabıyla çalışır. Başka kiracıya ait veya hiç bulunmayan test kimliği için aynı 404 cevabı verilir. Sonuç cevaplarında önbellekleme kapatılmıştır.
Tasarım kararları ve varsayımlar
Kiracı eşleştirmesi ve veri modeli
Kiracı XML'den alınmaz. XML'deki OrderID, doğrulanmış CSV kaydı ve veritabanındaki iş emri üzerinden kiracıya bağlanır. Cihazın hasta kimliği ile CSV'deki çalışan referansı ayrı tutulur. Mevcut iş emrinin kiracısı, çalışanı veya tarihi CSV ile çelişirse sessiz güncelleme yapılmaz.
Tablo	Amaç
tenant	Kiracı kimliği ve adı
orders	İş emri, kiracı, çalışan referansı ve tarih
spirometry_tests	Test bilgileri, içerik özeti, kaynak dosya ve uyarılar
spirometry_measurements	Teste ait ölçümler ve isteğe bağlı referans değerleri
import_logs	Çalıştırma kimliği, dosya, sonuç durumu ve hata/uyarı kodları


tenant tablosundaki kimlik sütununun mevcut adı büyük harfle "Id" şeklindedir; diğer tablolarda snake_case kullanılmıştır. Migration'lar ve politikalar bu gerçek adlarla uyumludur.
Test–iş emri ve ölçüm–test ilişkilerinde kiracı kimliğini de içeren birleşik foreign key kullanılır. Böylece farklı kiracılara ait kayıtların birbirine bağlanması engellenir. Bir test, ölçümleri ve başarılı işlem günlüğü aynı SaveChangesAsync çağrısıyla atomik kaydedilir.
RLS ve hesaplar
Beş tabloda da ENABLE ROW LEVEL SECURITY ve FORCE ROW LEVEL SECURITY uygulanmıştır.
Hesap	Erişim
postgres	Yalnızca yönetim ve migration; uygulama sorgularında kullanılmaz
spirometry_importer	Güvenilir toplu aktarım hesabı; tüm kiracılarda SELECT ve INSERT
spirometry_osgb_a	SELECT; yalnızca T-01 satırları
spirometry_osgb_b	SELECT; yalnızca T-02 satırları


OSGB hesaplarına yazma, superuser veya BYPASSRLS yetkisi verilmez; birbirlerinin rollerine üye yapılmazlar. RLS politikaları hesaplara özel tanımlıdır. Kullanıcının ekrandaki OSGB seçimi tek başına yetki sağlamaz; o hesaba ait parola doğrulanmalıdır. FORCE RLS, superuser veya BYPASSRLS hesabını sınırlandırmaz; izolasyon kontrolleri sınırlı hesaplarla yapılır.
Kiracısı belirlenemeyen hata günlüklerinde tenant_id boş bırakılır; bu kayıtlar OSGB hesaplarına gösterilmez. Mevcut importer hata yakalama akışı exception ile oluşan hata günlüklerini merkezi kayıt olarak, kiracı/iş emri alanları boş şekilde tutar.
Doğrulama ve tekrar çalıştırma
- CSV başlıkları ve alan sırası sabittir. Zorunlu alanlar, tarih biçimi ve tekrarlayan iş emri kimlikleri kontrol edilir. Hatalı CSV tüm aktarımı durdurur; güvenilir kiracı eşleştirmesi olmadan devam edilmez.
- XML okuyucu verilen basitleştirilmiş örnek biçimini destekler: namespace içermeyen ndd kökü, tek hasta/aralık/test ve FVC test türü. Tam üretici XML şemasının bütün sürümlerini desteklediği varsayılmaz.
- DTD yasaktır, dış kaynak çözümleme kapalıdır ve belge büyüklüğü 1.000.000 karakterle sınırlandırılır.
- Sayılar kültürden bağımsız ondalık noktayla okunur. Bozuk sayı reddedilir; eksik isteğe bağlı sayı null kalır, sıfır üretilmez.
- FVC, FEV1, FEV1_FVC ve PEF beklenir. En az bir geçerli ölçüm varsa eksik kodlar uyarı olarak saklanır; hiç ölçüm yoksa test reddedilir. Aynı ölçüm kodunun tekrarı reddedilir.
- Test tarihi yyyy-MM-dd'T'HH:mm:ss biçimindedir. Kaynakta saat dilimi olmadığı için timestamp without time zone olarak korunur; UTC'ye dönüştürülmez. Aktarım ve günlük zamanları UTC tutulur.
- Test kimliği kiracı, iş emri, cihaz hasta kimliği, test tarihi ve test türü birleşimidir. Veritabanında bu birleşim için unique constraint/index bulunur.
- Ölçümler koduna göre sıralanır ve sayılar ortak biçime dönüştürülerek SHA-256 içerik özeti hesaplanır. Dosya adı ve XML boşlukları bu özete dahil değildir.
- Aynı test kimliği ve aynı içerik Skipped / DUPLICATE_TEST olur. Aynı kimlikle farklı içerik Failed / TEST_CONTENT_CONFLICT olur; eski sonuç ezilmez.
Hata kaydı
Her XML için sonuç import_logs tablosuna yazılır ve aynı çalıştırmanın kayıtları run_id ile ilişkilendirilir. Hatalı XML veya doğrulama hatasında diğer dosyalar işlenmeye devam eder. Veritabanı hatasında veya hata günlüğü kalıcı olarak yazılamadığında kalan aktarım durdurulur.
XML hata günlüğü veritabanına yazılamazsa uygulamanın çıktı klasörü altında Logs/import-fallback.jsonl dosyasına yazılması denenir. Bu kayıtlar otomatik olarak veritabanına geri aktarılmaz. Eksik klasör, CSV okuma sorunu, eksik parola ve başlangıçtaki iş emri kayıt hataları şu an konsola bildirilir; bu başlangıç hataları için kalıcı günlük mekanizması yoktur.
Örnek sonuçlar ve kontroller
Boş test tablolarıyla ilk içe aktarmada beklenen sonuç:
Dosya	Durum	Kod
test_001.xml	Imported	IMPORT_OK
test_002.xml	Imported	IMPORT_OK
test_003.xml	Imported	IMPORT_OK
test_004.xml	Skipped	DUPLICATE_TEST
test_005.xml	ImportedWithWarnings	MISSING_MEASUREMENTS
test_006.xml	Failed	INVALID_XML


Özet: 3 yeni, 1 uyarılı yeni, 1 atlandı, 1 hatalı. test_004 ilk testin tekrarıdır. test_005 FEV1 ve FEV1_FVC içermez. test_006 bozuk XML'dir.
Yeniden çalıştırmada doğrulanan özet: 0 yeni, 0 uyarılı yeni, 5 atlandı, 1 hatalı. Günlükler her çalıştırmada artar; test ve ölçümler çoğalmaz.
Yönetim hesabıyla kontrol sorguları ve beklenen toplamlar:
SELECT COUNT(*) AS test_sayisi FROM public.spirometry_tests;
-- Beklenen: 4

SELECT COUNT(*) AS olcum_sayisi FROM public.spirometry_measurements;
-- Beklenen: 14

SELECT run_id, file_name, status, code
FROM public.import_logs
ORDER BY created_at_utc, file_name;
RLS'yi uygulama dışında kontrol etmek için doğrudan sınırlı hesapla bağlanın:
& "D:\PostgreSQL\bin\psql.exe" -h localhost -p 5432 -U spirometry_osgb_a -d spirometry_db -W
SELECT current_user;
SELECT order_id, tenant_id FROM public.spirometry_tests ORDER BY order_id;
-- OSGB A: O-1001/T-01 ve O-1003/T-01

SELECT COUNT(*) FROM public.spirometry_tests WHERE tenant_id = 'T-02';
-- OSGB A icin beklenen: 0

SELECT COUNT(*) FROM public.spirometry_measurements;
-- OSGB A icin beklenen: 8
B hesabıyla bağlandığında O-1002/T-02 ve O-1004/T-02, toplam 6 ölçüm beklenir; T-01 filtresi sıfır satır döndürmelidir.
Geliştirme sırasında manuel olarak importer çıktıları ve DB günlükleri incelendi; tekrar aktarım, WPF sonuç/ölçüm gösterimi, web giriş/çıkış ve detay erişimi kontrol edildi. Web'de A'nın detay adresi B hesabıyla açıldığında 404 alındığı, çıkıştan sonra detay adresinin girişe yönlendiği doğrulandı. Otomatik test paketi eklenmedi. Temiz bir makinede uçtan uca yeniden kurulum henüz ayrıca doğrulanmadı.
Kapsam ve sınırlamalar
- Çözüm verilen sentetik örnek seti ve yerel demo içindir. Klinik değerlendirme veya tıbbi yorum üretmez.
- İki kiracının hesap/politika eşleştirmesi sabittir. Kiracı yönetimi veya kullanıcı kayıt ekranı yoktur.
- Web'de DB parolası bağlantı kurmak için sunucu belleğinde tutulur. Bu tercih yerel demo içindir; üretimde son kullanıcı kimliği ile DB erişimi ayrı tasarlanmalıdır. Dağıtık oturum, MFA ve giriş denemesi hız sınırlaması eklenmemiştir.
- Web ve masaüstü hesapları salt okunurdur; XML yükleme/import ekranı yoktur. Aktarım console uygulamasından yapılır.
- Paralel importer süreçlerinin unique constraint yarışında özel yeniden deneme yoktur. Çift kayıt DB tarafından engellenir; yarış hatası veritabanı hatası olarak aktarımı durdurabilir.
- Dosya işleme sıralıdır; büyük veri hacmi için toplu aktarım, sayfalama ve performans çalışması yapılmadı.
- Başlangıç hatalarının kalıcı kaydı, otomatik testler ve farklı üretici XML sürümleri sonraki geliştirme alanlarıdır.
Yapay zekâ kullanım notu
Projeyi geliştirirken ChatGPT’yi teknik fikir alışverişi, kod örnekleri oluşturma, hata çözümleme ve dokümantasyon desteği için kullandım. Özellikle PostgreSQL RLS, XML doğrulama ve EF Core yapılandırmalarında önerilerden yararlandım.
Kodları kendim yazarak ilerledim; ChatGPT’yi yanımda danışabileceğim kıdemli bir geliştirici veya yazılım mimarı gibi kullandım. Tasarım seçeneklerini, karşılaştığım hataların nedenlerini ve çözüm önerilerini birlikte değerlendirdim. Önerilen kod örneklerini projeye uyarladım; içe aktarma, tekrar kayıt kontrolü ve kiracılar arası erişim senaryolarını kendi ortamımda çalıştırarak kontrol ettim.
Teslim içeriği
Teslim GitHub deposu üzerinden yapılır. Depoda case.slnx, dört projenin kaynak kodları, migration'lar, scripts klasöründeki iki SQL dosyası, sentetik CSV/XML örnekleri ve bu README bulunmalıdır. .vs, bin, obj, yerel loglar ve parola içeren dosyalar depoya eklenmemelidir. Bu README'de gerçek parola bulunmaz; kurulumu yapan kişi kendi parolalarını belirler. Depo özel ise değerlendirecek kişiye erişim verilmelidir.