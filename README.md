# Internship Project

Bu proje, ASP.NET MVC mimarisi ile geliştirilen bir kullanıcı yönetim sistemi ve harita modülünü bir araya getiren tam işlevli bir web uygulamasıdır. Uygulama hem veri tabanı işlemleri (CRUD) hem de etkileşimli harita işlemlerini desteklemektedir.

## 🚀 Özellikler

### 👤 Kullanıcı Yönetimi

- Kullanıcı **kayıt olma (register)** ve **giriş yapma (login)** sistemi
- Şifreler **SHA256** algoritmasıyla güvenli şekilde hashlenir
- Rol bazlı yetkilendirme (Admin / Normal Kullanıcı)
- Kullanıcıları listeleme, güncelleme ve silme
- Admin paneli üzerinden kullanıcı ekleme

### 📝 Form Yönetimi

- Bootstrap destekli başvuru formu
- Form verilerinin veritabanına kaydedilmesi
- Kayıtlı verilerin listelenmesi
- Verilerin güncellenmesi ve silinmesi (soft delete)
- Verilerin Excel formatında dışa aktarılması (Excel Export)
- Sayfalama ve filtreleme (Pagination & Filtering)

### 🗺️ Harita Modülü (OpenLayers)

- OpenStreetMap tabanlı interaktif harita
- Harita tipi seçimi (Yol Haritası, Uydu, Arazi)
- Enlem-boylam ile konuma gitme
- Şehir adı ile **Geocoding** (Nominatim API kullanılarak)
- Nokta, çizgi, poligon, daire gibi çizim araçları
- Çizimleri GeoJSON formatında indirme
- Çizimleri veritabanına WKT formatında kaydetme (`GEOMETRY` tipi)
- Veritabanından kayıtlı çizimleri haritada görüntüleme

## 🛠️ Kullanılan Teknolojiler

| Katman         | Teknoloji |
|----------------|-----------|
| Backend        | ASP.NET MVC, C# |
| Frontend       | HTML, CSS, JavaScript, Bootstrap, OpenLayers |
| Veritabanı     | SQL Server (GEOMETRY tipi destekli) |
| Veri Formatları| JSON, WKT, GeoJSON |
| Harita API     | OpenStreetMap, Nominatim |
| Güvenlik       | SHA256 Hash |

## 🖥️ Ekran Görüntüleri

> Kullanıcı Girişi, Kayıt Sayfası, Listeleme, Harita Üzerinde Çizim, Excel'e Aktarım, Yetkilendirme Paneli

