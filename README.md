# Staj Evrak Doğrulama ve Yönetim Sistemi (Ek-16 / Ek-17)
**Kocaeli Üniversitesi Yazılım Mühendisliği Bölümü - Programlama Laboratuvarı III**

Bu proje, öğrencilerin staj başvuru evrakı olan **Ek-16 belgesini** sisteme yüklemesini, belgedeki bilgilerin **Nesne Yönelimli Programlama (NYP)** prensipleriyle tasarlanmış kural motoru tarafından otomatik olarak denetlenmesini, eksiklerin ve önerilerin raporlanmasını, onaylanan evrakların "Tamamlandı" statüsüne geçirilerek korunmasını ve yönetici (admin) tarafından merkezi olarak izlenmesini sağlayan tam teşekküllü bir web uygulamasıdır.

---

## 🚀 Hızlı Başlangıç (Projeyi Çalıştırma)

Projeyi çalıştırmak için terminalden sunucu projesinin bulunduğu dizine gidip `dotnet run` komutunu çalıştırınız:

```bash
cd StajTakipSistemi.Server
dotnet run
```

Sunucu başladığında tarayıcınızdan şu adrese gidiniz:
👉 **http://localhost:5180**

*(Not: Port ayarı `Properties/launchSettings.json` içerisinde tanımlıdır).*

---

## 🔑 Hazır Kullanıcı Hesapları (Giriş Bilgileri)

Sistem ilk açıldığında otomatik olarak SQLite veri tabanını oluşturur ve aşağıdaki test hesaplarını yükler:

| Rol | Kullanıcı Adı / E-posta | Şifre | Yetkiler |
| :--- | :--- | :--- | :--- |
| **Öğrenci** | `ogrenci` veya `ogrenci@ogr.kocaeli.edu.tr` | `Ogrenci123!` | Ek-16 yükleme, kural raporu inceleme, gün seçimi, Google form URL testi |
| **Yönetici (Admin)** | `admin` veya `admin@kocaeli.edu.tr` | `Admin123!` | Test veri seti (CSV) yükleme, tüm başvurular, tamamlananlar sekmesi, loglar, kilit açma |

*Giriş ekranında yer alan **"Öğrenci Girişi"** ve **"Admin Girişi"** butonlarına tek tıkla basarak formu anında doldurabilirsiniz.*

---

## 🎯 Vize Sunumu Sırasında Jüriye Gösterilebilecek Demo Adımları

1. **Oturum Açma ve 5 Hatalı Girişte Kilitleme (Doküman 4.1):**
   * Giriş ekranında rastgele yanlış bir şifreyi 5 kez deneyin.
   * 5. denemeden sonra sistemin *"5 kez üst üste hatalı giriş yaptığınız için hesabınız güvenlik gerekçesiyle kilitlenmiştir"* uyarısı verdiğini ve kilitlendiğini gösterin.
   * Admin hesabıyla giriş yapıp **"Güvenlik & Kilitli Hesaplar"** sekmesinden kilitlenen hesabı ve hatalı deneme loglarını gösterin, tek tıkla kilidi açın.
2. **Yetkilendirme Katmanı ve 403 Koruması (Doküman 4.2):**
   * Öğrenci olarak giriş yapıldığında Admin paneline erişimin engellendiğini ve yetkisiz isteklerin HTTP 403 ile yakalandığını gösterin.
3. **Evrak Format Kontrolü (Doküman 4.4):**
   * "Evrak Yükle" modalında `.zip` veya `.txt` gibi geçersiz bir dosya seçmeye çalıştığınızda sistemin anında kırmızı uyarı pop-up'ı ile dosyayı reddettiğini gösterin.
   * Yalnızca `.pdf`, `.docx`, `.doc`, `.png`, `.jpg` formatlarına izin verildiğini doğrulayın.
4. **Resmi Tatiller Düşülerek Staj Gün Hesabı (Doküman 6.3):**
   * Staj tarih aralığını resmi tatile (örn: 15 Temmuz veya 29 Ekim) denk gelecek şekilde seçin.
   * Beyan edilen günü 20 yapın. Kural motorunun resmi tatili düşerek net günü 19 hesapladığını ve *"1 gün uyuşmazlık var"* uyarısını raporda gösterin.
5. **30 Gün Teslim Kuralı ve Önerilen Tarih (Doküman 6.3):**
   * Belge teslim tarihini staj başlangıcından 10 gün öncesine ayarlayın.
   * Sistemin kuralı ihlal saydığını ve *"Önerilen En Geç Teslim Tarihi: [Tarih]"* bilgisini dinamik olarak ürettiğini gösterin.
6. **Mühendis Sayısı < 5 Uyarısı (Doküman 6.5):**
   * Mühendis sayısını 3 yapın. Arayüzde anında sarı renkli uyarı kutucuğunun açıldığını ve *"Mühendisin Mezun Olduğu Bölüm"* sorusunu zorunlu kıldığını gösterin.
7. **Öğrenci Veri Seti Çapraz Karşılaştırması (Doküman 4.3):**
   * Belgedeki Ad veya TC'yi bilerek farklı girin. Kural motorunun üniversite veri tabanındaki orijinal kayıtla uyuşmazlığı tespit ettiğini ve raporda kırmızı uyarı olarak sunduğunu gösterin.
8. **Belge Değişmezliği (Doküman 5):**
   * Tüm kuralları sağlayan bir belge yükleyin. Belgenin statüsü **"Tamamlandı"** olur.
   * Öğrenci ekranında bu belgenin üzerinde **"Kilitli"** ibaresi belirdiğini ve silme/güncelleme butonlarının korumaya alındığını gösterin.
9. **Admin Test Veri Seti Yönetimi (Doküman 4.3):**
   * Admin panelinde **"Test Veri Seti Yönetimi"** sekmesine gelin. Proje ana dizinindeki `test_ogrenci_veriseti.csv` dosyasını yükleyin. 12 sütunlu öğrenci listesinin başarıyla yüklendiğini gösterin.
10. **Tamamlanan Belgeler Sekmesi (Doküman 4.7):**
    * Admin panelindeki özel **"Tamamlanan Belgeler Sekmesi"**ne gelin. Burada yalnızca onaylanmış evrakların listelendiğini ve Admin'e özel yetkilerin (revize etme, silme ve Final için Ek-17 üretme hazırlığı) çalıştığını gösterin.

---

## 🏛️ Nesne Yönelimli Programlama (NYP) Mimarisi

Dersin en önemli değerlendirme kriteri olan 4 temel prensip:

1. **Kapsülleme (Encapsulation):**
   * `User.cs` sınıfında şifre hash'i, tuz (salt), hatalı deneme sayaçları ve kilit durumu `private/protected` alanlarda tutulur.
   * `InternshipDocument.cs` sınıfında statü değişimleri ve öğrenciye karşı değişmezlik kontrolleri metotlar aracılığıyla korunur.
2. **Soyutlama (Abstraction):**
   * `IValidationRule` arayüzü ve `BaseValidator` soyut sınıfı. Kural motoru kuralların iç detaylarından tamamen bağımsızdır.
   * `IHolidayService`, `IAuthService`, `IDatasetImportService` servis sözleşmeleri.
3. **Kalıtım (Inheritance):**
   * `User` $\rightarrow$ `StudentUser` ve `AdminUser`.
   * `BaseValidator` $\rightarrow$ `IdentityValidator` $\rightarrow$ `TcIdentityValidator` ve `ForeignIdentityValidator`.
   * `AddressFormatValidator`, `ContactInfoValidator`, `AcademicTermValidator`, `SubmissionDateValidator`, `InternshipWorkDaysValidator`, `EmployerApprovalValidator`, `EngineerCountValidator`, `DatasetMatchValidator`.
4. **Çok Biçimlilik (Polymorphism):**
   * `DocumentValidationEngine` içerisindeki `List<IValidationRule>` koleksiyonunda farklı kurallar tek tip referans üzerinden döngüyle çalıştırılır (`rule.Evaluate(...)`).
   * TC kimlik algoritması ve Yabancı kimlik algoritması `IdentityValidator` üzerinden polimorfik olarak yürütülür.

---

## 📁 Proje Dosya Yapısı

```
StajTakipSistemi/
├── StajTakipSistemi.Server/          # ASP.NET Core 8 Web API
│   ├── Controllers/                  # Auth, Documents, Admin Controller sınıfları
│   ├── Data/                         # EF Core AppDbContext & DbInitializer
│   ├── Models/Domain/                # NYP Varlık sınıfları (User, StudentUser, AdminUser, vb.)
│   ├── Models/DTOs/                  # Veri Transfer Nesneleri (DTOs)
│   ├── Services/OOP_RuleEngine/      # Soyutlama, Kalıtım ve Polimorfizm kural motoru
│   ├── Services/                     # AuthService, HolidayService, DatasetImportService
│   ├── wwwroot/                      # Vue 3 & Tailwind CSS Reaktif Web Arayüzü
│   │   ├── index.html                # Tek Sayfa Uygulaması (SPA)
│   │   └── app.js                    # İstemci tarafı durum yönetimi ve API entegrasyonu
│   ├── test_ogrenci_veriseti.csv     # 12 Sütunlu test öğrenci veri seti
│   ├── Program.cs                    # Sunucu konfigürasyonu ve servis kayıtları
│   └── appsettings.json              # SQLite bağlantı ve JWT yapılandırması
├── Documentation/
│   ├── IEEE_Staj_Takip_Sistemi_Raporu.md  # IEEE Formatında En Az 4 Sayfalık Proje Raporu
│   └── UML_ve_Class_Diyagramlari.md       # Use Case, Akış, ER ve Class Diyagramları
└── README.md                         # Bu rehber dosyası
```

---

## 📄 IEEE Proje Raporu ve Diyagramlar

Vize teslimi için hazırlanan dokümanlar:
* **IEEE Raporu:** [`Documentation/IEEE_Staj_Takip_Sistemi_Raporu.md`](Documentation/IEEE_Staj_Takip_Sistemi_Raporu.md)
* **Diyagramlar Dokümanı:** [`Documentation/UML_ve_Class_Diyagramlari.md`](Documentation/UML_ve_Class_Diyagramlari.md)
