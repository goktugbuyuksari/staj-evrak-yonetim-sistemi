# Staj Evrak Doğrulama ve Yönetim Sistemi (Ek-16 / Ek-17)
**Kocaeli Üniversitesi Yazılım Mühendisliği Bölümü - Programlama Laboratuvarı III**

Bu proje, öğrencilerin staj başvuru evrakı olan **Ek-16 belgesini** sisteme yüklemesini, belgedeki bilgilerin **Nesne Yönelimli Programlama (NYP)** prensipleriyle tasarlanmış kural motoru tarafından otomatik olarak denetlenmesini, eksiklerin ve önerilerin raporlanmasını, onaylanan evrakların "Tamamlandı" statüsüne geçirilerek korunmasını ve yönetici (admin) tarafından merkezi olarak izlenmesini sağlayan tam teşekküllü bir web uygulamasıdır.

---

## 🚀 Hızlı Başlangıç (Projeyi Çalıştırma)

Projeyi çalıştırmak için terminalden sunucu projesinin bulunduğu dizine gidip `dotnet run` komutunu çalıştırın:

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

