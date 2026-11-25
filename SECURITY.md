# Standar Cybersecurity Fauzora

## Kebijakan Penyimpanan Data

- Data bisnis seperti nama produk, deskripsi, harga, stok, kategori, kota, dan status pesanan disimpan sebagai data biasa agar dapat dicari, diurutkan, dan ditampilkan oleh aplikasi.
- Password tidak pernah disimpan dalam bentuk asli. ASP.NET Core Identity menyimpannya sebagai `PasswordHash` menggunakan password hasher dengan salt dan mekanisme hash bawaan Identity.
- Password hash tidak dapat didekripsi. Saat login, password yang dikirim dibandingkan dengan hash menggunakan `PasswordSignInAsync`.
- Cookie login dilindungi ASP.NET Core Data Protection. Cookie bukan tempat menyimpan password atau data sensitif tambahan.
- ID pada URL halaman detail/edit produk dan detail/return pesanan diproteksi backend dengan ASP.NET Core Data Protection; database tetap menyimpan ID integer asli.
- Alamat customer dan seller merupakan data pribadi. Alamat hanya dapat dikelola dari profil pemilik; snapshot tujuan ditampilkan kepada customer pemilik dan seller pesanan, sedangkan snapshot asal ditampilkan kepada customer pesanan. Admin tidak memiliki akses ke modul order.
- Snapshot alamat pada pesanan tidak dibaca dari profil saat ini dan tidak di-backfill untuk pesanan lama. Perubahan alamat profil hanya berlaku untuk checkout berikutnya.
- Checkout saat ini hanya memvalidasi bahwa komponen alamat wajib tidak kosong. Server belum memverifikasi teks wilayah terhadap kode dan hubungan parent-child katalog wilayah; jangan menganggap alamat tervalidasi secara geografis.
- Connection string, password seed, dan secret deployment harus berada di User Secrets, environment variable, atau secret manager production. Jangan commit secret ke repository.

## Kapan Enkripsi Digunakan

Enkripsi digunakan untuk perlindungan pada batas yang tepat:

1. HTTPS untuk melindungi data saat transit antara browser dan server.
2. ASP.NET Core Data Protection untuk cookie authentication dan token internal.
	Token ID halaman hanya dikirim ke browser sebagai opaque token; encryption key tidak pernah dikirim ke frontend.
3. Enkripsi database/disk dikelola pada infrastruktur production, misalnya encrypted volume, backup encryption, dan TLS PostgreSQL.
4. Field-level encryption hanya ditambahkan jika ada data rahasia yang memang perlu dibaca kembali, misalnya token integrasi atau data pembayaran. Field tersebut harus memiliki kebutuhan akses, rotasi key, dan prosedur recovery yang jelas.

Jangan mengenkripsi semua kolom database secara otomatis. Hal itu menyulitkan pencarian, indexing, reporting, migration, dan debugging tanpa memberi manfaat yang sebanding untuk data katalog biasa.

## Kontrol Aplikasi

- Semua endpoint POST memakai antiforgery token.
- Endpoint pengelolaan dilindungi role dan pemeriksaan ownership.
- Login memakai cookie `HttpOnly`, `SameSite=Lax`, dan `Secure` pada production.
- Lockout login dinonaktifkan. Percobaan password salah tidak dibatasi oleh lockout; rate limiting login perlu ditambahkan sebelum deployment production.
- Security headers dasar aktif: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, dan `Permissions-Policy`.
- Rich text dari CKEditor disanitasi server-side sebelum disimpan dan sebelum dirender sebagai HTML.
- Upload gambar produk menerima JPG/JPEG, PNG, dan WEBP; maksimum 5 MB per gambar, 50 MB per request, serta 15 gambar total per produk. Server memvalidasi format hasil decode dan dimensi, mengonversi gambar menjadi WEBP, membuat nama acak, dan menyimpan hanya di `wwwroot/uploads/products`. File upload disajikan publik sesuai fungsi katalog; jangan simpan data privat pada gambar produk.
- Penghapusan gambar lokal dilakukan setelah perubahan relasi database berhasil dan hanya untuk path di direktori upload produk. Karena database dan filesystem tidak memiliki transaksi atomik bersama, kegagalan cleanup dapat meninggalkan file yatim; log aplikasi perlu dipantau dan direktori dapat dibersihkan melalui prosedur terkontrol.
- Penyimpanan upload bersifat lokal per instance. Backup harus mencakup file upload; deployment multi-instance/container ephemeral memerlukan shared/object storage atau strategi sinkronisasi.
- Upload foto profil lama masih memakai validasi ekstensi/ukuran dan nama acak, tetapi belum memeriksa signature/decode konten; perbaikan tersebut masih perlu dilakukan terpisah.
- Password reset menggunakan token Identity. Namun, implementasi saat ini menaruh link reset di `TempData` lalu merendernya pada halaman konfirmasi yang dapat dibuka publik. Pesan generik tidak menghilangkan kebocoran ini. Fitur reset password adalah **blocker production** sampai link dikirim melalui kanal privat terverifikasi dan tidak pernah ditampilkan kepada pemohon anonim.
- Tidak ditemukan rate limiting umum. Login tidak memakai lockout dan endpoint sensitif lain, termasuk permintaan reset password, juga belum dibatasi rate.

## Temuan Audit yang Belum Ditutup

Status berikut berdasarkan pemeriksaan kode dan smoke test lokal; belum dianggap sebagai penetration test.

1. **Kritis — kebocoran link reset password.** Pengunjung anonim dapat mengirim alamat email korban lalu menerima link reset di halaman konfirmasi. Jangan deploy fitur ini sebelum diperbaiki dan diuji.
2. **Tinggi — credential database di artifact sementara lokal.** Audit menemukan file `%muhammad.fauzan%/AppData/Local/Temp/fauzora-verify/appsettings.json` dengan password connection string non-placeholder. Status tracking dan validitas credential belum diverifikasi. Pastikan artifact tidak dibagikan/di-commit, hapus bila tidak diperlukan, dan rotasi credential jika masih berlaku. Nilai secret tidak dicatat di dokumen ini.
3. **Tinggi — perubahan status order tidak memiliki transition guard.** Seller dibatasi pada order miliknya, tetapi dapat menetapkan semua status yang valid, termasuk `Completed`; ini dapat melewati konfirmasi customer dan membuka eligibility review.
4. **Sedang — hasil operasi Identity saat registrasi tidak diperiksa konsisten.** Registrasi tidak memeriksa hasil penambahan role Customer, sehingga akun dapat dibuat/login tanpa akses customer yang diharapkan.
5. **Sedang — akses detail produk nonaktif.** Produk nonaktif tidak muncul di katalog/pembelian, tetapi detail publik masih dapat dibuka jika ID proteksi valid diketahui.
6. **Sedang — validasi alamat dan return belum lengkap.** Nilai wilayah tidak dicek ke katalog di server dan pengajuan return tidak mempunyai cutoff waktu.
7. **Rendah — redirect gagal menambah cart.** Aksi menambah cart memakai ID numerik saat redirect ke detail yang mengharapkan ID opaque; kegagalan stok dapat berujung halaman 404.
8. **Rendah/operasional — verifikasi foto profil dan pengujian.** File divalidasi berdasarkan ekstensi, bukan signature konten. Belum ada test project; skenario end-to-end dan concurrency belum memiliki bukti otomatis.

## Batas Akses yang Sudah Diterapkan

- Katalog/detail produk bersifat publik; operasi CRUD produk memerlukan role Seller dan seller hanya dapat mengelola produk miliknya.
- Cart, checkout, review, serta aksi customer pada order dibatasi ke role Customer. Data cart dan order customer di-scope berdasarkan user yang login.
- Dashboard dan order seller memerlukan role Seller. Query order dan keputusan return menggunakan SellerId dari user login.
- Manajemen akun memerlukan role Admin. Admin hanya dapat mengatur satu role per akun dan status aktif akun non-Admin; Admin tidak dapat mengubah dirinya sendiri atau menurunkan Admin terakhir. Promosi Admin menghapus role Customer/Seller.
- Status akun dan role database diperiksa ulang pada validasi cookie setiap request; akun nonaktif dan cookie dengan role usang ditolak.
- Profil memerlukan login dan hanya memperbarui user saat ini. Field alamat customer/seller ditulis sesuai role user; field kota profil hanya dapat diubah Seller.
- Aksi perubahan memakai POST dengan antiforgery token; ID produk/order tertentu memakai opaque Data Protection token. Token opaque bukan pengganti pemeriksaan ownership.
- Akun nonaktif tetap menyimpan data dan role; reaktivasi mengembalikan akses sesuai role yang tersimpan. Data tidak dihapus saat status berubah.

## Standar Operasional Production

- Jalankan hanya melalui HTTPS dan aktifkan HSTS.
- Gunakan database user khusus aplikasi dengan permission minimum; jangan gunakan superuser PostgreSQL.
- Simpan secret di secret manager dan rotasi jika pernah terekspos.
- Enkripsi backup database dan uji proses restore secara berkala.
- Batasi akses database ke network aplikasi, bukan internet publik.
- Aktifkan logging terstruktur tanpa password, token, connection string, atau data pribadi yang tidak diperlukan.
- Patch .NET, package NuGet, PostgreSQL, dan server secara berkala.
- Tinjau dependency dan jalankan vulnerability scan sebelum deployment.
- Pisahkan environment development, staging, dan production.
- Sediakan prosedur incident response, backup recovery, penghapusan data, dan audit akses.

## Verifikasi Sebelum Release

- `dotnet build --no-restore`
- `dotnet list package --vulnerable`
- Pastikan `ConnectionStrings:DefaultConnection` dan password seed tidak ada di source control.
- Pastikan production menggunakan HTTPS dan cookie Secure.
- Uji login, lockout, reset password, authorization role, antiforgery, upload gambar produk (termasuk signature, batas ukuran/jumlah, dan ownership), serta sanitasi rich text.