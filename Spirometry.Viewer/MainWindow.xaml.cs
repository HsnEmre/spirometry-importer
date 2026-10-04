using Npgsql;
using Spirometry.Viewer.Models;
using Spirometry.Viewer.Services;
using System.Windows;
using System.Windows.Controls;

namespace Spirometry.Viewer
{
    public partial class MainWindow : Window
    {
        // Başarılı bağlantıdan sonra ölçümleri de aynı hesapla okuyacağız.
        private ResultReader? _reader;
        // Hızlı seçim değişikliklerinde eski sorgunun sonucu ekranı değiştirmesin.
        private int _measurementRequestId;
        public MainWindow()
        {
            // XAML dosyasındaki ekran elemanlarını oluşturur.
            InitializeComponent();
            // Üst tabloda test seçildiğinde ölçümlerini yükler.
            TestsGrid.SelectionChanged += TestsGrid_SelectionChanged;
            // Butona basıldığında testleri yükleyen metodu çalıştırır.
            LoadButton.Click += LoadButton_Click;

            // OSGB değiştiğinde önceki hesabın sonuçlarını temizler.
            TenantSelector.SelectionChanged += TenantSelector_SelectionChanged;

            // Parola değiştiğinde mevcut oturumu ve sonuçlarını temizler.
            PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
        }

        // Mevcut bağlantı bilgisini ve ekrandaki sonuçları temizler.
        private void ClearResults()
        {
            _measurementRequestId++;
            // Önceki hesabın okuyucusunu bırakır.
            _reader = null;

            // Üst tablodaki testleri temizler.
            TestsGrid.ItemsSource = null;

            // Alt tablodaki ölçümleri temizler.
            MeasurementsGrid.ItemsSource = null;
        }

        // Kullanıcı farklı bir OSGB seçtiğinde çalışır.
        private void TenantSelector_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            // Önceki OSGB'nin verilerini ekrandan kaldırır.
            ClearResults();

            // Önceki hesabın parolasını temizler.
            PasswordInput.Clear();

            // Yeni hesapla giriş yapılması gerektiğini bildirir.
            StatusText.Text = "Seçtiğiniz OSGB hesabının parolasını girin.";
        }

        // Kullanıcı parolayı değiştirdiğinde çalışır.
        private void PasswordInput_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            // Eski bağlantıyla işlem yapılmasını önler.
            ClearResults();

            // Yeniden bağlantı kurulmasını ister.
            StatusText.Text = "Sonuçları getirmek için butona basın.";
        }

        // Butona basıldığında ekranı dondurmadan testleri yükler.
        private async void LoadButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Yeni giriş denemesinden önce eski sonuçları temizler.
            ClearResults();

            // Seçili OSGB'nin Tag alanındaki veritabanı hesabını alır.
            string? username =
                (TenantSelector.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            // Hesap seçilmemişse işlemi başlatmaz.
            if (string.IsNullOrWhiteSpace(username))
            {
                StatusText.Text = "Önce bir OSGB seçin.";
                return;
            }

            // Parola boşsa kullanıcıya açıklama gösterir.
            if (string.IsNullOrWhiteSpace(PasswordInput.Password))
            {
                StatusText.Text = "Seçtiğiniz OSGB hesabının parolasını girin.";
                return;
            }

            // Sorgu sürerken tekrar yükleme yapılmasını engeller.
            LoadButton.IsEnabled = false;

            // Sorgu sürerken hesabın değiştirilmesini engeller.
            TenantSelector.IsEnabled = false;

            // Sorgu sürerken parolanın değiştirilmesini engeller.
            PasswordInput.IsEnabled = false;

            // İşlemin başladığını ekranda gösterir.
            StatusText.Text = "Sonuçlar yükleniyor...";

            try
            {
                // Seçilen sınırlı veritabanı hesabıyla okuyucu oluşturur.
                var reader = new ResultReader(
                    username,
                    PasswordInput.Password);

                // Bu hesabın görebildiği testleri veritabanından getirir.
                var tests = await reader.GetTestsAsync();

                // Başarılı okuyucuyu ölçüm sorguları için saklar.
                _reader = reader;

                // Gelen testleri üst tabloya bağlar.
                TestsGrid.ItemsSource = tests;

                // Kullanıcıya yüklenen kayıt sayısını gösterir.
                StatusText.Text = $"{tests.Count} test listelendi.";
            }
            catch (PostgresException ex)
            {
                // PostgreSQL'in hata koduna göre anlaşılır mesaj gösterir.
                StatusText.Text = ex.SqlState switch
                {
                    // Kullanıcı adı veya parola doğrulanamadı.
                    "28P01" => "Kullanıcı adı veya parola hatalı.",

                    // Hesabın giriş yapmasına izin verilmedi.
                    "28000" => "Bu hesapla bağlantı kurulmasına izin verilmedi.",

                    // Hesabın sorgu için gerekli yetkisi yok.
                    "42501" => "Bu hesabın verileri okuma yetkisi yok.",

                    // Diğer veritabanı hatalarında yalnızca kodu gösterir.
                    _ => $"Veritabanı işlemi başarısız. Kod: {ex.SqlState}"
                };
            }
            catch (NpgsqlException)
            {
                // Bağlantı bilgilerini veya parolayı ekrana dökmez.
                StatusText.Text =
                    "PostgreSQL bağlantısı kurulamadı veya kesildi.";
            }
            catch (TimeoutException)
            {
                // İşlem zamanında tamamlanamadığında bilgi verir.
                StatusText.Text = "Veritabanı işlemi zaman aşımına uğradı.";
            }
            finally
            {
                // Başarılı veya başarısız her durumda butonu açar.
                LoadButton.IsEnabled = true;

                // OSGB seçimini yeniden açar.
                TenantSelector.IsEnabled = true;

                // Parola girişini yeniden açar.
                PasswordInput.IsEnabled = true;
            }
        }


        #region helper
        // Üst tablodaki seçili test değiştiğinde çalışır.
        private async void TestsGrid_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            // Bu seçime ait sorgunun numarasını oluşturur.
            int requestId = ++_measurementRequestId;

            // Yeni sonuç gelene kadar önceki testin ölçümlerini kaldırır.
            MeasurementsGrid.ItemsSource = null;

            // Bu işlemde kullanılacak mevcut okuyucuyu alır.
            ResultReader? reader = _reader;

            // Bağlantı yoksa veya seçili satır bir test değilse işlemi bitirir.
            if (reader is null ||
                TestsGrid.SelectedItem is not TestSummary selectedTest)
            {
                return;
            }

            // Seçilen testin ölçümlerinin yüklendiğini bildirir.
            StatusText.Text = "Seçilen testin ölçümleri yükleniyor...";

            try
            {
                // Seçilen testin ölçümlerini aynı OSGB hesabıyla getirir.
                var measurements =
                    await reader.GetMeasurementsAsync(selectedTest.Id);

                // Beklerken başka test veya hesap seçildiyse bu cevabı kullanmaz.
                if (requestId != _measurementRequestId ||
                    !ReferenceEquals(reader, _reader))
                {
                    return;
                }

                // Ölçümleri alt tabloya bağlar.
                MeasurementsGrid.ItemsSource = measurements;

                // Seçilen iş emrini ve ölçüm sayısını gösterir.
                StatusText.Text =
                    $"İş emri: {selectedTest.OrderId} | " +
                    $"{measurements.Count} ölçüm listelendi.";
            }
            catch (Exception ex) when (
                ex is NpgsqlException or TimeoutException)
            {
                // Eski bir sorgunun hatası yeni seçimin durumunu değiştirmesin.
                if (requestId != _measurementRequestId ||
                    !ReferenceEquals(reader, _reader))
                {
                    return;
                }

                // Bağlantı ayrıntılarını göstermeden hata bilgisini verir.
                StatusText.Text = ex switch
                {
                    PostgresException postgresError =>
                        $"Ölçümler okunamadı. Kod: {postgresError.SqlState}",

                    TimeoutException =>
                        "Ölçüm sorgusu zaman aşımına uğradı.",

                    _ => "Ölçümler okunurken veritabanı bağlantısı kesildi."
                };
            }
        }
        #endregion
    }
}