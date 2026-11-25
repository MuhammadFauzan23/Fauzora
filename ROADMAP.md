# Fauzora Roadmap

Dokumen ini menjadi urutan kerja pengembangan Fauzora. Setiap item memiliki `in-charge` agar tanggung jawabnya jelas.

## Peran dan In-Charge

| Peran | Tanggung jawab utama |
| --- | --- |
| Product Owner | Menentukan prioritas fitur, aturan marketplace, dan acceptance criteria |
| Backend Developer | Controller, service, repository, EF Core, migration, authorization, dan API/domain logic |
| Frontend Developer | Razor View, CSS, JavaScript, responsive layout, accessibility, dan UX |
| Database/DevOps | PostgreSQL, backup, secret/configuration, deployment, monitoring, dan restore |
| QA/Security | Test alur customer/seller/admin, authorization, validation, XSS, CSRF, dan regression |
| Customer | Registrasi, melihat katalog, keranjang, checkout, order, return, dan review |
| Seller | Mengajukan toko, mengelola produk milik sendiri, stok, harga, dan deskripsi |
| Admin | Manajemen akun dan role; seller mengelola operasional toko dan pesanan sendiri |

Untuk proyek personal, satu orang boleh memegang beberapa peran. Minimal setiap perubahan harus memiliki satu owner teknis dan satu pemeriksa.

## Status Roadmap

Status yang digunakan: `Done`, `In Progress`, `Next`, dan `Backlog`.

### Phase 0 - Fondasi dan Reproducible Setup

| Status | Pekerjaan | In-charge | Hasil selesai |
| --- | --- | --- | --- |
| Done | MVC, repository, service, Identity, PostgreSQL | Backend Developer | Aplikasi dapat dijalankan lokal |
| Done | User Secrets untuk connection string dan seed password | Database/DevOps | Tidak ada credential lokal di source |
| Done | Migration EF Core dan auto-migrate saat startup | Backend Developer | Schema dapat dibuat dari migration |
| Done | Security headers, antiforgery, cookie protection | QA/Security | Baseline keamanan aplikasi tersedia |
| Next | Membuat database PostgreSQL rumah dari migration bersih | Database/DevOps | Database lokal berhasil di-update |
| Next | Menyimpan versi SDK, package, dan langkah setup | Database/DevOps | Developer lain dapat mengulang setup |

### Phase 1 - Stabilitas Core Marketplace

| Status | Pekerjaan | In-charge | Acceptance criteria |
| --- | --- | --- | --- |
| Done | Catalog, kategori, detail, related products | Backend + Frontend | Produk aktif dan stok tersedia tampil benar |
| Done | Cart dan checkout demo | Backend Developer | Stok tidak menjadi minus pada checkout bersamaan |
| Done | Order status dan auto-complete 7 hari | Backend Developer | Delivered otomatis menjadi Completed sesuai aturan |
| Done | Review dan rating | Backend + QA | Review hanya dapat dibuat sesuai aturan order |
| Next | Functional test otomatis/manual untuk customer, seller, dan admin | QA/Security | Skenario utama memiliki bukti test; saat ini belum ada test project |
| Next | Error handling dan logging terstruktur | Backend + DevOps | Error tidak membocorkan secret atau data sensitif |

### Phase 2 - Seller dan Operasional Admin

| Status | Pekerjaan | In-charge | Acceptance criteria |
| --- | --- | --- | --- |
| Retired | Pengajuan seller dan approval admin | Backend + Admin | Record lama dipertahankan sebagai riwayat; role Seller diberikan dari manajemen akun |
| Done | Admin mengelola akun dan role | Backend + Admin | Admin eksklusif dari Customer/Seller; status non-Admin dapat dinonaktifkan; sesi lama ditolak |
| Done | Product editor dengan CKEditor dan preview gambar | Frontend Developer | Format aman dan preview sesuai detail |
| Done | Seller mengelola order tokonya sendiri | Backend Developer | Query dan keputusan return dibatasi ke seller pemilik; transisi status masih perlu pekerjaan terpisah |
| Done | Split order multi-seller | Backend Developer | Satu checkout menghasilkan satu sub-order per toko dalam transaksi atomik dan snapshot alamat |
| Done | Dashboard laporan penjualan seller | Backend + Frontend | Total dihitung dari data order valid; order batal dan retur disetujui dikecualikan |

### Phase 3 - Keamanan dan Production Readiness

| Status | Pekerjaan | In-charge | Acceptance criteria |
| --- | --- | --- | --- |
| Done | Password hashing, secret management, Data Protection | QA/Security | Password tidak pernah disimpan plaintext |
| Done | Proteksi token ID halaman produk/pesanan | Backend Developer | ID URL tidak dikirim sebagai angka langsung |
| Next | Email verification dan password reset aman untuk production | Backend + DevOps | Reset dikirim melalui email privat; endpoint publik tidak pernah menampilkan link/token reset |
| Next | Pembatasan transisi status order | Backend + QA/Security | Seller/admin hanya dapat melakukan transisi status yang sah; penerimaan customer tidak dapat disimulasikan seller |
| Next | Validasi wilayah alamat di server | Backend Developer | Nilai wilayah cocok dengan katalog dan hubungan parent-child tervalidasi |
| Next | Rate limiting login dan endpoint sensitif | Backend + Security | Abuse login, reset password, dan request berulang dibatasi |
| Next | Database least privilege dan encrypted backup | Database/DevOps | App user bukan PostgreSQL superuser |
| Next | Dependency scan dan security review | QA/Security | Tidak ada advisory high severity yang belum ditangani |
| Next | CI build, migration check, dan test | DevOps + QA | Pull request gagal jika build/test gagal |
| Next | Validasi konten upload foto profil | Backend + Security | Signature/MIME dan ukuran divalidasi; file tidak dapat dieksekusi sebagai konten aktif |
| Next | Pembatasan akses detail produk nonaktif | Backend Developer | Produk nonaktif tidak dapat dibuka publik kecuali ada alur operasional eksplisit |
| Next | Kebijakan batas waktu return | Product Owner + Backend | Batas pengajuan diputuskan dan ditegakkan server-side |
| Next | Penanganan credential sementara yang terekspos | Database/DevOps | Artifact lokal diverifikasi tidak dibagikan/di-commit dan credential dirotasi jika masih berlaku |

### Phase 4 - Pembayaran dan Pengalaman Pengguna

| Status | Pekerjaan | In-charge | Acceptance criteria |
| --- | --- | --- | --- |
| Backlog | Payment gateway | Backend + DevOps | Webhook tervalidasi dan idempotent |
| Backlog | Refund otomatis | Backend + Finance | Refund memiliki audit trail |
| Backlog | Upload gambar produk ke object storage | DevOps + Frontend | Validasi MIME, ukuran, dan akses file aman |
| Backlog | Notification email/in-app | Backend + Frontend | Customer dan seller menerima status penting |
| Backlog | Search dan filtering lanjutan | Backend + Frontend | Query terindeks dan tetap responsif |

## Definition of Done

Sebuah pekerjaan dianggap selesai jika:

- Acceptance criteria terpenuhi.
- Authorization dan ownership sudah diperiksa.
- Input tervalidasi di server.
- Tidak ada secret atau password di source control/log.
- Migration dibuat jika schema berubah.
- `dotnet build --no-restore` berhasil.
- Test relevan dijalankan atau gap test dicatat.
- README/SECURITY diperbarui jika alurnya berubah.
- Perubahan ditinjau oleh owner kedua untuk fitur sensitif.

## Alur Kerja Perubahan Model Database

1. Ubah model atau konfigurasi `Data/AppDbContext.cs`.
2. Jalankan `dotnet ef migrations add NamaPerubahan`.
3. Review file migration sebelum diterapkan.
4. Jalankan `dotnet ef database update` pada database development.
5. Jalankan aplikasi dan verifikasi alur yang terdampak.
6. Commit model dan migration bersama-sama.
7. Pada staging/production, backup database sebelum migration.
8. Terapkan migration melalui proses deployment yang terdokumentasi.

Jangan menghapus migration lama yang sudah dipakai database bersama. Buat migration baru untuk perubahan berikutnya.
