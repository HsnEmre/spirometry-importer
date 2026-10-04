
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Npgsql;
using Spirometry.Importer.Parsing;
using Spirometry.Importer.Persistence;
using Spirometry.Importer.Persistence.Entities;
using System.Text.Json;
using System.Xml;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");

string ordersFilePath = Path.Combine(dataDir, "orders.csv");

//xml ler tam yol
string xmlDir = Path.Combine(dataDir, "xml");

//is emirleri belirtilen yolda yoksa calisacak
if (!File.Exists(ordersFilePath))
{
    //bulunamayan dosyanin hata ciktisi
    Console.Error.WriteLine($"is emri dosyasi bulunamadi: {ordersFilePath}");
    //app basarisiz cikis
    Environment.ExitCode = 1;
    return;

}


//xmllerin tam yolunu tutacak
//string[] xmlFiles = Directory.EnumerateFiles(xmlDir, "*.xml")//yollari getirecek
//    .OrderBy(x => x, StringComparer.Ordinal)//siralicak
//    .ToArray();//yolla sira olucak

// Bulunan XML dosyalarının yollarını tutar.
string[] xmlFiles;

try
{
    // Klasördeki XML dosyalarını bulur ve dosya adına göre sıralar.
    xmlFiles = Directory
        .EnumerateFiles(xmlDir, "*.xml")
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
}
catch (Exception ex) when (
    ex is IOException or UnauthorizedAccessException)
{
    // Klasör yoksa veya erişilemiyorsa anlaşılır bir hata kodu seçer.
    string errorCode = ex switch
    {
        DirectoryNotFoundException => "XML_DIRECTORY_NOT_FOUND",
        UnauthorizedAccessException => "XML_DIRECTORY_ACCESS_DENIED",
        _ => "XML_DIRECTORY_READ_ERROR"
    };

    // Başlangıç hatasını konsola yazar.
    Console.Error.WriteLine($"XML klasoru okunamadi | Kod: {errorCode}");

    // İşlemin başarısız tamamlandığını bildirir.
    Environment.ExitCode = 1;

    // Dosyalar listelenemediği için aktarımı başlatmaz.
    return;
}

// Yanlış veya boş klasörün başarılı aktarım gibi görünmesini engeller.
if (xmlFiles.Length == 0)
{
    // Klasörde işlenecek dosya bulunmadığını bildirir.
    Console.Error.WriteLine("XML klasorunde islenecek dosya bulunamadi.");

    // İşlemi başarısız olarak işaretler.
    Environment.ExitCode = 1;

    // Veritabanına kayıt yapmadan çıkar.
    return;
}

//dosya adini yaz
Console.WriteLine($"is emri dosyasi: {Path.GetFileName(ordersFilePath)}");

//eleman sayisi
Console.WriteLine($"bulunan xml dosya sayisi: {xmlFiles.Length}");

//dosyalari gez
foreach (string fp in xmlFiles)
{
    //dosya adi yaz
    Console.WriteLine($"- {Path.GetFileName(fp)}");
}

//beklenen hatalar
try
{
    var orderReader = new OrderCsvReader();//nesne

    var orders = orderReader.Read(ordersFilePath);//is emirlerini al

    Console.WriteLine($"okunan is emri sayisi: {orders.Count}");

    foreach (var item in orders)
    {
        Console.WriteLine($"{item.OrderId} -> {item.TenantId}");
    }


    //değişkenden import hesabının parolası okunacak
    string? importPassword = Environment.GetEnvironmentVariable("SPIROMETRY_IMPORT_PASSWORD");

    if (string.IsNullOrWhiteSpace(importPassword))
    {
        Console.Error.WriteLine("SPIROMETRY_IMPORT_PASSWORD ortam degiskeni ayarlanmamis.");

        Environment.ExitCode = 1;
        return;
    }

    //sınırlı import hesapla context
    var importDbFactory = new ImportDbContextFactory();

    try
    {
        await using var db = importDbFactory.Create(importPassword);
        var orderStore = new OrderStore();

        int savedCount = await orderStore.SaveAsync(db, orders);

        Console.WriteLine($"veri tabanına eklenen teni osgb/is emri kaydi : {savedCount}");
    }
    catch (Exception ex) when (
    ex is InvalidDataException or
    DbUpdateException or
    NpgsqlException or
    TimeoutException)
    {

        Console.Error.WriteLine("OSGB ve is emirleri kaydedilemedi");
        if (ex is InvalidDataException)
        {
            Console.Error.WriteLine(ex.Message);
        }

        PostgresException? postgresError = ex as PostgresException ?? ex.InnerException as PostgresException;

        //dbde hata kodu varsa göster
        if (postgresError is not null)
        {
            Console.Error.WriteLine($"psql hata kodu :{postgresError.SqlState}");

        }
        else
        {
            Console.Error.WriteLine($"hata turu:{ex.GetType().Name}");
        }

        Environment.ExitCode = 1;
        return;
    }


    #region revize
    //var ordersById = orders.ToDictionary(order => order.OrderId, StringComparer.Ordinal);
    //var xmlReader = new SpirometryXmlReader();
    //int successfulCount = 0;
    //int warningCount = 0;
    //int failedCount = 0;

    //foreach (string filePath in xmlFiles)
    //{
    //    string fileName = Path.GetFileName(filePath);
    //    try
    //    {
    //        var test = xmlReader.Read(filePath);

    //        if (!ordersById.TryGetValue(test.OrderId, out var matchedOrder))
    //        {
    //            throw new InvalidDataException("is emri csv eslestirmesinde bulunamadi");
    //        }

    //        bool hasWarnings = test.Warnings.Count > 0;

    //        if (hasWarnings)
    //        {
    //            warningCount++;
    //        }
    //        else
    //        {
    //            successfulCount++;
    //        }

    //        string status = hasWarnings ? "UYARILI" : "OKUNDU";

    //        Console.WriteLine(
    //        $"[{status}] {fileName} | " +
    //        $"Is emri: {test.OrderId} | " +
    //        $"OSGB: {matchedOrder.TenantId} | " +
    //        $"Olcum: {test.Measurements.Count}");

    //        foreach (string warning in test.Warnings)
    //        {
    //            Console.WriteLine($"  Uyari: {warning}");
    //        }
    //    }
    //    catch (Exception ex) when (
    //   ex is XmlException or
    //   InvalidDataException or
    //   IOException or
    //   UnauthorizedAccessException)
    //    {

    //        failedCount++;

    //        string errorCode = ex switch
    //        {
    //            XmlException => "INVALID_XML",
    //            InvalidDataException => "VALIDATION_ERROR",
    //            UnauthorizedAccessException => "FILE_ACCESS_DENIED",
    //            _ => "FILE_READ_ERROR"
    //        };

    //        Console.Error.WriteLine($"[HATA] {fileName} | {errorCode}");
    //        if (ex is InvalidDataException)
    //        {
    //            // Doğrulamadan neden geçilemediğini açıklar.
    //            Console.Error.WriteLine($"  Neden: {ex.Message}");
    //        }
    //    }
    //}

    //Console.WriteLine(
    //$"Ozet: {successfulCount} uyarısız, " +
    //$"{warningCount} uyarılı, {failedCount} hatalı.");

    //// En az bir dosya başarısızsa toplu işlemin kısmen başarısız olduğunu bildirir.
    //Environment.ExitCode = failedCount > 0 ? 1 : 0; 
    #endregion

    var ordersById = orders.ToDictionary(
        order => order.OrderId, StringComparer.Ordinal);

    Guid runId = Guid.NewGuid();

    var xmlReader = new SpirometryXmlReader();
    var testStore = new SpirometryTestStore();
    int successfulCount = 0;
    int warningCount = 0;
    int skippedCount = 0;
    int failedCount = 0;

    Console.WriteLine($"calistirma kimliği: {runId}");

    foreach (string filePath in xmlFiles)
    {
        string fileName = Path.GetFileName(filePath);
        string validationCode = "VALIDATION_ERROR";


        try
        {
            var test = xmlReader.Read(filePath);
            if (!ordersById.TryGetValue(test.OrderId, out var matchOrder))
            {
                validationCode = "ORDER_NOT_FOUND";

                throw new InvalidDataException("is emri csv eslestirmede bulunmadi");

            }

            await using var db = importDbFactory.Create(importPassword);
            var result = await testStore.SaveAsync(
                db, test, matchOrder, fileName, runId
               );

            // Veritabanına kaydedilen işlem sonucuna göre ilgili sayacı artırır.
            switch (result.Status)
            {
                // Uyarısız yeni test kaydedildi.
                case "Imported":
                    successfulCount++;
                    break;

                // Uyarıyla yeni test kaydedildi.
                case "ImportedWithWarnings":
                    warningCount++;
                    break;

                // Daha önce kaydedilmiş aynı test atlandı.
                case "Skipped":
                    skippedCount++;
                    break;

                // Aynı test kimliğiyle farklı içerik geldi.
                case "Failed":
                    failedCount++;
                    break;
            }

            Console.WriteLine(
            $"[{result.Status}] {fileName} | " +
            $"OSGB: {result.TenantId} | Kod: {result.Code}");


            foreach (string warning in result.Warnings)
            {
                // Her uyarıyı ayrı satırda gösterir.
                Console.WriteLine($"  Uyari: {warning}");
            }
        }
        catch (Exception ex) when (
         ex is XmlException or
         InvalidDataException or
         IOException or
         UnauthorizedAccessException or
         DbUpdateException or
         NpgsqlException or
         TimeoutException)
        {

            failedCount++;
            bool databaseFailure =
            ex is DbUpdateException or NpgsqlException or TimeoutException;


            string errorCode = ex switch
            {
                // XML yapısı bozuktur veya okuma kurallarına aykırıdır.
                XmlException => "INVALID_XML",

                // İş emri eşleştirmesi veya veri doğrulaması başarısızdır.
                InvalidDataException => validationCode,

                // Dosyaya erişim izni yoktur.
                UnauthorizedAccessException => "FILE_ACCESS_DENIED",

                // Dosya okunamamıştır.
                IOException => "FILE_READ_ERROR",

                // Veritabanı işlemi başarısızdır.
                _ => "DATABASE_ERROR"
            };

            Console.Error.WriteLine($"[Failed] {fileName} | Kod: {errorCode}");

            var failureLog = new ImportLogEntity
            {
                RunId = runId,
                FileName = fileName,
                TenantId = null,
                OrderId = null,
                Status = "Failed",
                Code = errorCode
            };

            bool logSaved = false;

            try
            {
                await using var logDb = importDbFactory.Create(importPassword);
                logDb.ImportLogs.Add(failureLog);
                await logDb.SaveChangesAsync();
                logSaved = true;
            }
            catch (Exception logException) when (
           logException is DbUpdateException or
           NpgsqlException or
           TimeoutException)
            {
                try
                {
                    string logDirectory = Path.Combine(
                    AppContext.BaseDirectory, "Logs");

                    Directory.CreateDirectory(logDirectory);

                    string logPath = Path.Combine(
                   logDirectory, "import-fallback.jsonl");
                    string json = JsonSerializer.Serialize(failureLog);

                    await File.AppendAllTextAsync(
                  logPath, json + Environment.NewLine);

                    logSaved = true;


                    Console.Error.WriteLine($"Hata logu dosyaya yazildi: {logPath}");
                }
                catch (Exception fileException) when (
                 fileException is IOException or UnauthorizedAccessException)
                {
                    // Logun kalıcı olarak saklanamadığını bildirir.
                    Console.Error.WriteLine(
                        "Hata logu ne veritabanina ne dosyaya yazilabildi.");
                }

            }

            if (databaseFailure || !logSaved)
            {
                // Kalan dosyaların işlenmediğini açıkça belirtir.
                Console.Error.WriteLine(
                    "Aktarim durduruldu; kalan dosyalar islenmedi.");

                // XML döngüsünü bitirir.
                break;
            }
        }

    }
    Console.WriteLine(
    $"Ozet: {successfulCount} yeni, " +
    $"{warningCount} uyarili yeni, " +
    $"{skippedCount} atlandi, {failedCount} hatali.");

    // Başarısız dosya varsa işlem sonucunu başarısız olarak bildirir.
    Environment.ExitCode = failedCount > 0 ? 1 : 0;

}
catch (Exception ex) when (
ex is IOException or
UnauthorizedAccessException or
InvalidDataException or
MalformedLineException
)
{
    Console.Error.WriteLine($"is emri dosyasi okunamadi {ex.GetType().Name}");

    if (ex is InvalidDataException) //dogrulana hatalar
    {
        Console.Error.WriteLine(ex.Message);
    }
    Environment.ExitCode = 1;
    return;

}

