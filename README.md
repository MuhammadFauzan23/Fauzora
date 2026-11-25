# Fauzora Marketplace

Fauzora adalah aplikasi marketplace berbasis ASP.NET Core MVC dengan .NET 10, Entity Framework Core, PostgreSQL, ASP.NET Core Identity, Bootstrap, Repository, dan Service.

## Teknologi

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core 10.0.14
- PostgreSQL dan Npgsql
- ASP.NET Core Identity
- Bootstrap 5
- Razor Views
- Repository + Service pattern

Standar keamanan dan keputusan penyimpanan data dijelaskan di [SECURITY.md](SECURITY.md). Data katalog tidak dienkripsi di level kolom; password di-hash oleh ASP.NET Core Identity, sedangkan HTTPS, cookie Data Protection, secret management, dan encryption pada infrastruktur digunakan sesuai kebutuhan.

Roadmap, pembagian peran, in-charge, acceptance criteria, dan Definition of Done tersedia di [ROADMAP.md](ROADMAP.md).

## Alamat Pengiriman

Customer mengelola alamat tujuan pengiriman dan seller mengelola alamat asal toko melalui halaman Profil. Pilihan wilayah menggunakan dropdown berantai Provinsi → Kota/Kabupaten → Kecamatan → Kelurahan/Desa yang memuat katalog wilayah secara online; koneksi internet diperlukan saat mengisi wilayah. Hanya ID wilayah yang diminta dari katalog, bukan nama penerima, nomor telepon, atau detail jalan. Checkout hanya dapat dilanjutkan jika kedua alamat lengkap untuk semua toko yang dipilih. Setiap pesanan seller menyimpan snapshot alamat saat checkout; perubahan profil tidak mengubah pesanan yang telah dibuat. Pesanan lama tanpa snapshot menampilkan alamat tidak tersedia dan tidak diisi menggunakan alamat profil terkini.

## Pembaruan Implementasi Saat Ini

- Alamat customer dan seller dikelola di Profil; wilayah dipilih berurutan melalui Select2 dan proxy server-side ke katalog wilayah.
- Checkout memvalidasi kelengkapan alamat customer dan semua seller yang produknya dipilih, lalu menyimpan snapshot alamat di setiap sub-order.
- Checkout multi-toko membuat satu sub-order per seller dalam satu transaksi; stok dikurangi bersyarat agar tidak menjadi negatif karena checkout bersamaan.
- Halaman detail pesanan membatasi alamat sesuai peran: customer melihat alamat tujuan dan asal, sedangkan seller melihat tujuan order tokonya.
- Admin mengelola akun, satu role per akun, dan kategori; akun non-Admin dapat dinonaktifkan tanpa menghapus data atau keanggotaannya, dan sesi akun yang dinonaktifkan ditolak pada request berikutnya.
- Seller dapat melihat order dan memutuskan return untuk tokonya sendiri; customer dapat melihat order miliknya, mengonfirmasi penerimaan, dan mengajukan return.
- Gambar produk dapat diunggah banyak sekaligus dan disimpan lokal di `wwwroot/uploads/products`; maksimal 15 gambar total per produk, termasuk cover. Edit produk dapat mempertahankan atau menghapus gambar lama lalu menambahkan upload baru. File baru diterima sebagai JPG/PNG/WEBP, maksimum 5 MB per file dan 50 MB per submit, lalu diproses menjadi WEBP.
- Gambar URL eksternal yang sudah ada tetap ditampilkan sampai dihapus, tetapi form tidak lagi menerima URL gambar baru. Urutan gambar lama tetap; upload baru ditambahkan berdasarkan urutan pemilihan file. Relasi database `product_images` yang ada digunakan, sehingga fitur ini tidak memerlukan migration baru.

Catatan audit: validasi alamat di server saat ini memastikan field wajib tidak kosong, tetapi belum membuktikan bahwa wilayah yang dikirim cocok dengan hierarki kode katalog. Katalog wilayah juga memerlukan koneksi layanan eksternal ketika cache belum terisi.

Penyimpanan gambar lokal berada di disk server dan file di bawah `wwwroot` dapat diakses publik seperti gambar katalog. Backup/restore harus mencakup folder upload. Jika aplikasi dijalankan pada beberapa instance atau container ephemeral, gunakan shared/object storage sebelum deployment agar gambar konsisten dan tidak hilang saat instance diganti.

## Hasil Pemeriksaan Lokal

- `dotnet build --no-restore`: berhasil.
- `dotnet list package --vulnerable`: tidak menemukan package rentan dari source NuGet yang dikonfigurasi.
- Smoke test route publik dan route terlindungi: halaman publik merespons, sedangkan Cart, Admin, Seller, dan Profil tanpa login diarahkan ke Login.
- Proxy wilayah yang memerlukan login mengembalikan 38 provinsi; rantai DKI Jakarta → kabupaten/kota → kecamatan → kelurahan berhasil diuji.
- Belum ada test project. Flow yang mengubah data (checkout, review, return, approval, CRUD produk) belum diuji end-to-end pada pemeriksaan ini.

## Menjalankan Aplikasi

Persyaratan:

- .NET 10 SDK
- PostgreSQL
- EF Core CLI 10.0.14

Atur koneksi database menggunakan User Secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fauzora_db;Username=postgres;Password=PASSWORD_ANDA"
dotnet user-secrets set "SeedUsers:AdminPassword" "PASSWORD_ADMIN_LOKAL"
dotnet user-secrets set "SeedUsers:SellerPassword" "PASSWORD_SELLER_LOKAL"
```

Jalankan aplikasi:

```powershell
dotnet restore
dotnet run
```

Buka alamat berikut di browser:

```text
http://localhost:5000
```

## Setup PostgreSQL di Rumah

Langkah berikut membuat database baru dari model dan migration yang ada. Jalankan di PowerShell pada folder project `C:\xampp\htdocs\Fauzora`.

### 1. Pastikan PostgreSQL siap

Pastikan service PostgreSQL aktif dan `psql` atau pgAdmin dapat digunakan. Buat database kosong, misalnya:

```sql
CREATE DATABASE fauzora_db;
```

Untuk penggunaan lokal, connection string dapat memakai user PostgreSQL lokal. Untuk production, gunakan user aplikasi dengan permission minimum, bukan superuser.

### 2. Konfigurasi User Secrets

Jangan menulis password PostgreSQL di `appsettings.json`. Jalankan:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=fauzora_db;Username=postgres;Password=PASSWORD_POSTGRES_ANDA"
dotnet user-secrets set "SeedUsers:AdminPassword" "PASSWORD_ADMIN_LOKAL"
dotnet user-secrets set "SeedUsers:SellerPassword" "PASSWORD_SELLER_LOKAL"
```

Periksa konfigurasi tanpa menampilkan nilai password ke source control:

```powershell
dotnet user-secrets list
```

### 3. Pastikan EF Core CLI tersedia

```powershell
dotnet tool install --global dotnet-ef --version 10.0.14
```

Jika sudah terpasang, gunakan:

```powershell
dotnet ef --version
```

### 4. Restore dan update schema

Untuk database baru, migration yang tersedia akan membuat tabel dan relasi:

```powershell
dotnet restore
dotnet ef database update
dotnet build --no-restore
```

`dotnet ef database update` menjalankan migration dari folder `Migrations/`. Aplikasi juga menjalankan `Database.MigrateAsync()` saat startup, sehingga schema akan diperbarui saat `dotnet run`; menjalankan command EF secara manual lebih mudah untuk memeriksa error migration.

### 5. Jalankan aplikasi dan verifikasi

```powershell
dotnet run
```

Buka `http://localhost:5000`, lalu verifikasi:

- login akun admin dan seller dari password seed;
- tabel `users`, `roles`, `products`, `orders`, dan tabel lain sudah muncul;
- kategori awal tersedia;
- seller dapat membuat produk;
- customer dapat melihat katalog, keranjang, dan order.

### 6. Jika ingin mengulang database dari awal

Perintah berikut menghapus database development beserta datanya. Jangan jalankan pada database penting:

```powershell
dotnet ef database drop --force
dotnet ef database update
```

### Troubleshooting PostgreSQL

| Masalah | Solusi |
| --- | --- |
| `Connection refused` | Pastikan service PostgreSQL aktif dan port sesuai, biasanya `5432`. |
| `password authentication failed` | Periksa username/password pada User Secrets. |
| `database does not exist` | Buat `fauzora_db` melalui pgAdmin atau `CREATE DATABASE`. |
| `dotnet ef not found` | Install global tool `dotnet-ef` dan buka terminal baru. |
| Migration gagal karena schema lama | Backup data, cek migration yang sudah diterapkan, lalu buat migration baru; jangan menghapus migration lama sembarangan. |
| Port aplikasi dipakai | Hentikan proses Fauzora atau ubah `applicationUrl` di `Properties/launchSettings.json`. |

Aplikasi menjalankan `Database.MigrateAsync()` saat startup. Migration yang sudah tersedia akan diterapkan otomatis.

Jika perlu menjalankan migration secara manual:

```powershell
dotnet ef database update
```

Jangan menjalankan `dotnet run` dua kali bersamaan karena keduanya akan berebut port `5000`.

## Akun Default Lokal

Akun berikut dibuat otomatis oleh `Data/DbInitializer.cs` saat aplikasi pertama kali berjalan. Password seed dibaca dari User Secrets, bukan dari source code.

### Admin

```text
Email    : admin@fauzora.local
Password : sesuai nilai `SeedUsers:AdminPassword`
Role     : Admin
```

Admin dapat mengelola akun, role, dan kategori katalog. Admin tidak memiliki akses Seller/Customer, produk, pesanan, maupun return. Kategori yang masih digunakan produk tidak dapat dihapus. Role SuperAdmin lama dipindahkan ke Admin oleh migration.

### Seller

```text
Email    : seller@fauzora.local
Password : sesuai nilai `SeedUsers:SellerPassword`
Role     : Seller
Kota     : Jakarta
```

Seller dapat membuka dashboard seller, menambah produk, mengubah produk sendiri, dan menghapus produk sendiri.

Ganti password akun seed sebelum aplikasi digunakan bersama atau dipasang di production. Jangan menyimpan password database atau password production di repository.

## Role dan Tanggung Jawab User

### Customer

Customer adalah pengguna yang membeli produk. Customer dapat:

- Registrasi dan login.
- Melihat katalog dan detail produk.
- Menambahkan produk ke keranjang.
- Checkout demo.
- Melihat riwayat dan detail pesanan.
- Mengonfirmasi barang sudah diterima.
- Memberikan review setelah pesanan dikonfirmasi selesai.
- Mengajukan pengembalian ketika pesanan berstatus `Delivered` atau `Completed`.
- Mengubah nama lengkap, alamat pengiriman, dan foto profil dari halaman profil.
- Mengubah kota profil hanya setelah memiliki role Seller.
- Foto profil menerima JPG, PNG, atau WEBP dengan ukuran maksimal 2 MB.

### Seller

Seller adalah akun dengan role `Seller`, yang diberikan langsung oleh Admin atau melalui akun seed. Data pengajuan seller lama dipertahankan sebagai riwayat, tetapi tidak lagi menjadi jalur pemberian role. Seller dapat:

- Membuka Seller Dashboard.
- Menambah produk.
- Mengedit produk miliknya sendiri.
- Menghapus produk miliknya sendiri.
- Mengatur harga, stok, deskripsi, kategori, dan gambar produk.
- Melihat kota seller pada profil dan katalog.

Seller dapat membuka menu Pesanan Toko untuk melihat sub-order miliknya dan memilih status pesanan yang tersedia. Seller juga dapat menyetujui atau menolak pengajuan pengembalian untuk order tokonya. Admin tidak memiliki akses operasional order.
Produk dibuat dan dikelola hanya oleh Seller pemilik; Admin tidak dapat menambahkan produk atau memilih pemilik produk.
Seller Dashboard menampilkan total penjualan dan unit terjual dari order valid, jumlah pesanan yang perlu diproses/dalam pengiriman, serta grafik penjualan enam bulan terakhir. Order berstatus batal dan retur yang disetujui tidak dihitung dalam ringkasan penjualan.

### Admin

Admin memiliki akses manajemen akun dan kategori:

- Mencari akun dan melihat role/status.
- Mengatur satu role: Customer, Seller, Admin, atau tanpa role.
- Menonaktifkan/mengaktifkan akun non-Admin tanpa menghapus data atau role; akun Admin nonaktif juga dapat diaktifkan kembali, tetapi akun Admin aktif tidak dapat dinonaktifkan dari halaman ini.
- Mengelola akun lain, tetapi tidak dapat mengubah role/status dirinya sendiri atau menurunkan Admin terakhir.
- Menambah, mengubah, dan menghapus kategori yang belum digunakan produk.

Admin tidak dapat memiliki role Customer atau Seller secara bersamaan. Promosi menjadi Admin menghapus role tersebut dan memastikan akun aktif. Perubahan akses membatalkan cookie yang role-claim-nya sudah usang.

## Riwayat Pengajuan Seller

Tabel `seller_applications` tetap menyimpan record historis. Pengajuan dan approval tidak lagi tersedia; Admin memberikan role Seller langsung melalui halaman Kelola Akun & Role.

## Flow Produk

```text
Seller login
    |
Seller Dashboard / Produk Saya
    |
Tambah Produk
    |
ProductController memeriksa role
    |
ProductService menyimpan SellerId dari user yang login
    |
Produk tampil di katalog jika aktif dan stok lebih dari 0
```

Seller tidak dapat mengubah atau menghapus produk seller lain. Admin tidak memiliki akses CRUD produk.

## Flow Pesanan Customer

```text
Customer pilih produk
    |
Keranjang
    |
Checkout demo dan stok berkurang dalam satu transaksi
    |
Satu sub-order dibuat untuk setiap seller
    |
Pembayaran demo dianggap berhasil; status menjadi Processing
    |
Seller memperbarui status pengiriman secara manual
    |
Catatan: aplikasi belum memaksa urutan transisi; seller saat ini dapat memilih status terdaftar apa pun
    |
Customer memilih per sub-order:
    +-- Pesanan Diterima --> Completed --> Customer dapat memberi review
    |
    +-- Ajukan Pengembalian --> Requested --> Seller memeriksa pengajuan
```

Jika customer tidak memberikan pilihan selama tujuh hari setelah pesanan `Delivered`, sistem otomatis mengubahnya menjadi `Completed`. Pesanan yang sedang memiliki pengajuan pengembalian `Requested` tidak ikut di-auto-complete.

## Flow Pengembalian

1. Customer membuka detail pesanan berstatus `Delivered`.
2. Customer memilih `Ajukan Pengembalian`.
3. Customer mengisi alasan pengembalian.
4. Status menjadi `Requested`.
5. Seller pemilik sub-order dapat memeriksa pengajuan dan menyetujui/menolaknya.
6. Keputusan menyimpan status dan catatan; refund pembayaran tetap manual.

Modul ini mencatat status dan catatan pengajuan. Refund pembayaran asli belum terhubung ke payment gateway karena checkout masih bersifat demo.

## Arsitektur MVC

Alur request secara umum:

```text
Browser
  -> Controller
  -> Service
  -> Repository / AppDbContext
  -> PostgreSQL
  -> View Razor
```

| Folder | Tanggung jawab |
| --- | --- |
| `Controllers` | Menerima request, authorization, validasi alur, dan redirect/view |
| `Services` | Aturan bisnis seperti manajemen akun, checkout, review, dan return |
| `Repositories` | Query database dan operasi persistence yang dipakai service |
| `Models` | Entity database dan konstanta status |
| `ViewModels` | Model form dan data tampilan |
| `Data` | `AppDbContext` dan seed awal (`DbInitializer`) |
| `Views` | Halaman Razor untuk customer, seller, dan admin |
| `wwwroot` | CSS dan JavaScript frontend |
| `Migrations` | Riwayat perubahan schema database |

## Database dan Migration

Migration fitur penting:

```text
20261124070413_AddSellerApplications
20261125043949_AddProfileAddressesAndOrderSnapshots
20261125094544_AdminAccountManagement
```

Tabel utama yang digunakan aplikasi:

- `users`
- `roles`
- `user_roles`
- `categories`
- `products`
- `product_images`
- `cart_items`
- `orders`
- `order_items`
- `product_reviews`
- `seller_applications`

Jika model database berubah, buat migration baru:

```powershell
dotnet ef migrations add NamaPerubahan
dotnet ef database update
```

## Struktur Folder Penting

```text
Controllers/
  AccountController.cs
  AdminController.cs
  ProductController.cs
  SellerController.cs
Data/
  AppDbContext.cs
  DbInitializer.cs
Models/
  SellerApplication.cs
  SellerApplicationStatus.cs
Services/
Views/
    Admin/Index.cshtml
```

## Split Order dan Manajemen Pesanan Seller

Satu checkout dapat berisi produk dari beberapa toko. Aplikasi membuat satu sub-order per seller dengan nomor, total, status, dan proses pengembalian masing-masing. Sub-order dari checkout yang sama berbagi ID checkout agar customer dapat mengenali kelompoknya.

Seller hanya dapat melihat sub-order miliknya dan dapat memproses pengajuan pengembalian order tokonya yang berstatus `Requested`. Seller saat ini dapat memilih semua status pesanan yang dikenal aplikasi; aturan transisi belum dipaksakan. Customer mengonfirmasi penerimaan atau mengajukan pengembalian untuk masing-masing sub-order. Data pesanan lama tetap tersedia untuk customer, tetapi pesanan lama yang belum mempunyai seller eksplisit tidak ditampilkan dalam pengelolaan seller.

Checkout dan pembayaran masih demo. Fitur ini belum menghitung ongkir per toko, belum mengintegrasikan kurir/payment gateway, dan belum melakukan refund otomatis.

## Batasan Versi Saat Ini

Fitur keranjang mendukung pemilihan barang per item maupun per toko. Checkout membuat sub-order terpisah untuk setiap seller yang dipilih. Batasan dan gap akses/operasional yang masih ada:

- Pembayaran masih demo; payment gateway, konfirmasi pembayaran, dan webhook belum tersedia.
- Alamat customer dan asal seller disimpan di Profil dan disalin menjadi snapshot order saat checkout. Ongkir per toko, integrasi kurir, nomor resi, dan pelacakan pengiriman belum tersedia; status pengiriman hanya pembaruan manual.
- Refund yang disetujui seller masih diproses manual dan belum terhubung ke payment gateway.
- Email verification belum tersedia. Reset password memakai token Identity, tetapi implementasi saat ini menampilkan link reset kepada pemohon anonim di halaman konfirmasi; ini blocker keamanan dan tidak aman untuk production sampai link dikirim secara privat melalui email.
- Notifikasi email/in-app untuk status pesanan belum tersedia.
- Transisi status order belum dibatasi. Seller dapat memilih semua status terdaftar tanpa validasi urutan transisi.
- Pengajuan return tidak memiliki batas waktu setelah order `Delivered`/`Completed`.
- Detail produk publik masih dapat menampilkan produk nonaktif jika ID produk yang valid diketahui, meski katalog dan pembelian menyaring produk nonaktif.
- Registrasi tidak memeriksa hasil `AddToRoleAsync`; jika pemberian role gagal, akun dapat terbuat/login tanpa izin Customer yang dibutuhkan.
- Saat penambahan cart gagal, redirect menggunakan ID numerik meskipun halaman detail produk mengharapkan ID opaque; halaman tujuan dapat berakhir 404.
- Foto profil hanya divalidasi dari ekstensi nama file dan ukuran, bukan signature/MIME isi file.
- Belum ada rate limiting umum. Batas percobaan login Identity aktif, tetapi bukan pengganti rate limiting.
- Validasi alamat checkout memeriksa kelengkapan teks, bukan kecocokan nilai/hierarki wilayah terhadap katalog server.
- Order lama yang belum memiliki seller eksplisit tetap dapat diakses customer, tetapi tidak muncul pada manajemen order seller.
- Belum ada proyek test otomatis. Build dan smoke test route/wilayah berhasil pada pemeriksaan lokal, tetapi skenario end-to-end customer/seller/admin, concurrency, dan security masih perlu dijalankan sebelum production.
- Audit lokal menemukan file sementara verifikasi berisi connection string database dengan password non-placeholder di bawah `%muhammad.fauzan%/AppData/Local/Temp/fauzora-verify/`. Status tracking belum diverifikasi; pastikan file tidak dibagikan/di-commit, hapus salinan yang tidak diperlukan, dan rotasi credential bila masih berlaku.
