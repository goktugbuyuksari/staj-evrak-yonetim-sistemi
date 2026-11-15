const { createApp, ref, computed, onMounted } = Vue;

createApp({
  setup() {
    // State
    const currentUser = ref(null);
    const token = ref(localStorage.getItem('token') || '');
    const loading = ref(false);
    const loginError = ref('');
    const authTab = ref('login'); // 'login' veya 'register'
    const registerSuccess = ref('');
    const registerError = ref('');

    const loginForm = ref({
      username: 'ogrenci',
      password: 'Ogrenci123!'
    });

    // Hoca Soru-Cevap 4. Madde: Kullanıcılar hesaplarını kendileri oluşturabilmelidir
    const registerForm = ref({
      username: '',
      password: '',
      email: '',
      fullName: '',
      studentNumber: '',
      tcNo: '',
      nationality: 'TC',
      department: 'Yazılım Mühendisliği',
      phoneNumber: '',
      address: ''
    });

    // Öğrenci Dokümanları
    const documents = ref([]);
    const showUploadModal = ref(false);
    const selectedReportDoc = ref(null);
    const parsedReport = ref({ results: [] });
    const popupAlert = ref(null);
    const fileValidationError = ref('');
    const googleFormUrlInput = ref('https://docs.google.com/forms/d/e/1FAIpQLSc-demo/viewform');

    // Evrak Yükleme Form Verisi (Doküman Kural ve Kısıtlarına Göre Varsayılanlar)
    const uploadForm = ref({
      studentFullName: 'Göktuğ Büyüksarı',
      studentTcNo: '10000000146',
      nationality: 'TC',
      studentNumber: '210202001',
      department: 'Yazılım Mühendisliği',
      phoneNumber: '+90 532 111 22 33',
      email: 'ogrenci@ogr.kocaeli.edu.tr',
      address: 'Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli',
      faxNumber: '',
      academicYear: '2025-2026 Bahar',
      submissionDate: new Date().toISOString().substring(0, 10),
      startDate: '2026-07-01',
      endDate: '2026-07-28',
      declaredDaysCount: 20,
      companyName: 'Aselsan A.Ş.',
      companyAddress: 'ODTÜ Teknokent Ankara',
      engineerCount: 4,
      engineerGraduationDepartment: 'Yazılım Mühendisliği',
      hasStudentSignature: false,
      hasAdvisorSignature: false,
      hasEmployerSignatureAndStamp: true,
      googleFormUrl: ''
    });

    const selectedFile = ref(null);
    const selectedDaysArray = ref(['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma']);

    // Admin State
    const adminTab = ref('documents');
    const adminDocuments = ref([]);
    const completedDocuments = ref([]);
    const datasetRecords = ref([]);
    const adminLogs = ref({ failedLogins: [], systemLogs: [], lockedUsers: [] });
    const adminSearch = ref('');
    const adminStatusFilter = ref('');
    const datasetSearch = ref('');

    // Tarih 30 Gün Kuralı Hesaplayıcı
    const submissionDaysDiff = computed(() => {
      if (!uploadForm.value.startDate || !uploadForm.value.submissionDate) return 0;
      const start = new Date(uploadForm.value.startDate);
      const sub = new Date(uploadForm.value.submissionDate);
      const diffTime = start - sub;
      return Math.round(diffTime / (1000 * 60 * 60 * 24));
    });

    const suggestedSubDate = computed(() => {
      if (!uploadForm.value.startDate) return '';
      const start = new Date(uploadForm.value.startDate);
      start.setDate(start.getDate() - 30);
      return start.toLocaleDateString('tr-TR');
    });

    // Helper: API İsteği Gönderme
    const apiFetch = async (url, options = {}) => {
      const headers = options.headers || {};
      if (token.value) {
        headers['Authorization'] = `Bearer ${token.value}`;
      }
      options.headers = headers;

      try {
        const response = await fetch(url, options);

        // 403 Forbidden: Yetkilendirme Katmanı (Doküman 4.2: Yetkisiz istekler 403 döner ve login sayfasına yönlendirilir)
        if (response.status === 403) {
          popupAlert.value = {
            isError: true,
            title: '403 YETKİSİZ ERİŞİM',
            message: 'Bu sayfaya veya işleme erişim yetkiniz bulunmamaktadır! Güvenlik kuralları gereği oturum sonlandırılıyor.'
          };
          logout();
          throw new Error('403 Yetkisiz Erişim');
        }

        if (response.status === 401) {
          logout();
          throw new Error('Oturum süresi doldu.');
        }

        return response;
      } catch (err) {
        console.error('API Error:', err);
        throw err;
      }
    };

    // Oturum Açma
    const login = async () => {
      loading.value = true;
      loginError.value = '';

      try {
        const res = await fetch('/api/auth/login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(loginForm.value)
        });

        const data = await res.json();
        if (!res.ok) {
          loginError.value = data.message || 'Giriş yapılamadı.';
          return;
        }

        token.value = data.token;
        localStorage.setItem('token', data.token);
        currentUser.value = {
          id: data.userId,
          username: data.username,
          fullName: data.fullName,
          role: data.role
        };

        await loadUserProfile();

        if (currentUser.value.role === 'ROLE_ADMIN') {
          await loadAdminData();
        } else {
          await loadStudentDocuments();
        }
      } catch (err) {
        loginError.value = 'Sunucu bağlantı hatası oluştu.';
      } finally {
        loading.value = false;
      }
    };

    // Hoca Soru-Cevap 4. Madde: Kullanıcı Kayıt Olma Fonksiyonu
    const register = async () => {
      loading.value = true;
      registerError.value = '';
      registerSuccess.value = '';

      try {
        const res = await fetch('/api/auth/register', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(registerForm.value)
        });

        const data = await res.json();
        if (!res.ok) {
          registerError.value = data.message || 'Kayıt oluşturulamadı.';
          return;
        }

        registerSuccess.value = data.message || 'Kayıt başarılı! Şimdi giriş yapabilirsiniz.';
        // Formu temizle ve giriş sekmesine yönlendir
        loginForm.value.username = registerForm.value.username;
        loginForm.value.password = registerForm.value.password;
        setTimeout(() => {
          authTab.value = 'login';
        }, 1500);
      } catch (err) {
        registerError.value = 'Kayıt sırasında sunucu hatası oluştu.';
      } finally {
        loading.value = false;
      }
    };

    // Profil Yükleme
    const loadUserProfile = async () => {
      try {
        const res = await apiFetch('/api/auth/me');
        if (res.ok) {
          const profile = await res.json();
          currentUser.value = { ...currentUser.value, ...profile };
          // Form alanlarını öğrencinin kendi profiliyle ön doldur
          if (profile.role === 'ROLE_STUDENT') {
            uploadForm.value.studentFullName = profile.fullName;
            uploadForm.value.studentTcNo = profile.tcNo || '';
            uploadForm.value.studentNumber = profile.studentNumber || '';
            uploadForm.value.nationality = profile.nationality || 'TC';
            uploadForm.value.department = profile.department || 'Yazılım Mühendisliği';
            uploadForm.value.phoneNumber = profile.phoneNumber || '+90 532 111 22 33';
            uploadForm.value.email = profile.email || '';
            uploadForm.value.address = profile.residentialAddress || 'Atatürk Mah. Cumhuriyet Cad. No: 12 Daire: 4 İzmit / Kocaeli';
          }
        }
      } catch (err) { }
    };

    // Çıkış Yap
    const logout = async () => {
      try {
        if (token.value) {
          await apiFetch('/api/auth/logout', { method: 'POST' });
        }
      } catch (e) { }

      token.value = '';
      currentUser.value = null;
      localStorage.removeItem('token');
      documents.value = [];
      adminDocuments.value = [];
    };

    const fillLogin = (u, p) => {
      loginForm.value.username = u;
      loginForm.value.password = p;
    };

    // Öğrenci Belgelerini Yükle
    const loadStudentDocuments = async () => {
      try {
        const res = await apiFetch('/api/documents/my-documents');
        if (res.ok) {
          documents.value = await res.json();
        }
      } catch (e) { }
    };

    // Dosya Seçimi ve Format Kontrolü (Doküman Bölüm 4.4)
    const onFileSelected = (e) => {
      const file = e.target.files[0];
      fileValidationError.value = '';
      if (!file) {
        selectedFile.value = null;
        return;
      }

      const allowed = ['.pdf', '.doc', '.docx', '.jpg', '.jpeg', '.png'];
      const ext = '.' + file.name.split('.').pop().toLowerCase();

      if (!allowed.includes(ext)) {
        fileValidationError.value = `HATA: Seçilen dosya formatı (${ext}) geçersizdir! Yalnızca Word (.doc, .docx), PDF, JPEG ve PNG kabul edilmektedir.`;
        popupAlert.value = {
          isError: true,
          title: 'GEÇERSİZ DOSYA FORMATI!',
          message: `Kabul edilmeyen dosya formatı: ${ext}\nDoküman Kuralı (Bölüm 4.4): Yalnızca Word (.doc/.docx), PDF, JPEG, PNG kabul edilir. Diğer formatlar reddedilir.`
        };
        e.target.value = '';
        selectedFile.value = null;
        return;
      }

      selectedFile.value = file;
    };

    // Evrak Gönderme ve Kural Motorunu Tetikleme
    const submitUpload = async () => {
      loading.value = true;
      try {
        uploadForm.value.selectedDaysOfWeek = selectedDaysArray.value.join(',');
        uploadForm.value.googleFormUrl = googleFormUrlInput.value;

        const formData = new FormData();
        if (selectedFile.value) {
          formData.append('file', selectedFile.value);
        }
        formData.append('documentData', JSON.stringify(uploadForm.value));

        const res = await apiFetch('/api/documents/upload', {
          method: 'POST',
          body: formData
        });

        const data = await res.json();
        if (!res.ok) {
          popupAlert.value = {
            isError: true,
            title: 'Yükleme Başarısız',
            message: data.message || 'Evrak yüklenemedi.'
          };
          return;
        }

        showUploadModal.value = false;
        await loadStudentDocuments();

        // Raporu göster
        viewReport(data.document);
      } catch (e) {
        popupAlert.value = {
          isError: true,
          title: 'Hata',
          message: 'Evrak yükleme sırasında bir hata oluştu.'
        };
      } finally {
        loading.value = false;
      }
    };

    // Rapor Modalini Aç
    const viewReport = (doc) => {
      selectedReportDoc.value = doc;
      try {
        parsedReport.value = JSON.parse(doc.validationReportJson || '{}');
      } catch (e) {
        parsedReport.value = { results: [] };
      }
    };

    // Öğrenci Belge Silme (Tamamlandı statüsünde ise backend korumalıdır)
    const deleteDocument = async (id) => {
      if (!confirm('Bu evrakı silmek istediğinize emin misiniz?')) return;
      try {
        const res = await apiFetch(`/api/documents/${id}`, { method: 'DELETE' });
        const data = await res.json();
        if (res.ok) {
          await loadStudentDocuments();
        } else {
          popupAlert.value = {
            isError: true,
            title: 'Silme Engellendi',
            message: data.message
          };
        }
      } catch (e) { }
    };

    // Google Form URL Doğrulama (Doküman Bölüm 4.6)
    const verifyGoogleFormUrl = async (url) => {
      try {
        const res = await apiFetch('/api/documents/check-google-form', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ url })
        });
        const data = await res.json();
        popupAlert.value = {
          isError: !data.isValid,
          title: data.title,
          message: data.message
        };
      } catch (e) { }
    };

    // Admin Verilerini Yükle
    const loadAdminData = async () => {
      await loadAdminDocuments();
      await loadCompletedDocuments();
      await loadDatasetRecords();
      await loadAdminLogs();
    };

    const loadAdminDocuments = async () => {
      try {
        const res = await apiFetch('/api/admin/documents');
        if (res.ok) adminDocuments.value = await res.json();
      } catch (e) { }
    };

    const loadCompletedDocuments = async () => {
      try {
        const res = await apiFetch('/api/admin/documents/completed');
        if (res.ok) completedDocuments.value = await res.json();
      } catch (e) { }
    };

    const loadDatasetRecords = async () => {
      try {
        const res = await apiFetch('/api/admin/dataset');
        if (res.ok) datasetRecords.value = await res.json();
      } catch (e) { }
    };

    const loadAdminLogs = async () => {
      try {
        const res = await apiFetch('/api/admin/logs');
        if (res.ok) adminLogs.value = await res.json();
      } catch (e) { }
    };

    // Admin Statü Değiştirme
    const changeStatusAdmin = async (id, status) => {
      try {
        const res = await apiFetch(`/api/admin/documents/${id}/status`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ status })
        });
        if (res.ok) {
          await loadAdminDocuments();
          await loadCompletedDocuments();
        }
      } catch (e) { }
    };

    // Admin Belge Silme
    const deleteAdminDoc = async (id) => {
      if (!confirm('Yönetici yetkisiyle bu belgeyi kalıcı olarak silmek istiyor musunuz?')) return;
      try {
        const res = await apiFetch(`/api/admin/documents/${id}`, { method: 'DELETE' });
        if (res.ok) {
          await loadAdminDocuments();
          await loadCompletedDocuments();
        }
      } catch (e) { }
    };

    // Admin Test Veri Seti CSV Yükleme
    const uploadDatasetFile = async (e) => {
      const file = e.target.files[0];
      if (!file) return;

      const formData = new FormData();
      formData.append('file', file);

      try {
        const res = await apiFetch('/api/admin/dataset/upload', {
          method: 'POST',
          body: formData
        });

        const data = await res.json();
        if (res.ok) {
          popupAlert.value = {
            isError: false,
            title: 'Veri Seti Yüklendi',
            message: `Başarıyla ${data.addedRecords} yeni öğrenci eklendi, ${data.updatedRecords} kayıt güncellendi.`
          };
          await loadDatasetRecords();
        } else {
          popupAlert.value = {
            isError: true,
            title: 'Veri Seti Hatası',
            message: data.message || (data.errors && data.errors.join('\n'))
          };
        }
      } catch (err) { }
    };

    // Kilitli Kullanıcı Kilidini Aç
    const unlockUser = async (userId) => {
      try {
        const res = await apiFetch(`/api/admin/users/${userId}/unlock`, { method: 'POST' });
        if (res.ok) {
          popupAlert.value = {
            isError: false,
            title: 'Kilit Açıldı',
            message: 'Kullanıcı hesabı kilidi başarıyla kaldırıldı.'
          };
          await loadAdminLogs();
        }
      } catch (e) { }
    };

    const generateEk17Demo = () => {
      popupAlert.value = {
        isError: false,
        title: 'Ek-17 Belgesi Oluşturma (Final Etabı)',
        message: 'Tamamlanan başvurulardan otomatik Ek-17 üretim şablonu başarıyla hazırlandı.\n(Final sunumu için PDF/Word çıktısı ve Google Form senkronizasyonu tam entegre çalışacaktır).'
      };
    };

    // Filtreler
    const filteredAdminDocs = computed(() => {
      return adminDocuments.value.filter(d => {
        const matchStatus = !adminStatusFilter.value || d.status === adminStatusFilter.value;
        const s = adminSearch.value.toLowerCase();
        const matchSearch = !s || (
          (d.studentFullName && d.studentFullName.toLowerCase().includes(s)) ||
          (d.studentNumber && d.studentNumber.toLowerCase().includes(s)) ||
          (d.studentTcNo && d.studentTcNo.includes(s)) ||
          (d.companyName && d.companyName.toLowerCase().includes(s))
        );
        return matchStatus && matchSearch;
      });
    });

    const filteredDataset = computed(() => {
      if (!datasetSearch.value) return datasetRecords.value;
      const s = datasetSearch.value.toLowerCase();
      return datasetRecords.value.filter(r =>
        (r.fullName && r.fullName.toLowerCase().includes(s)) ||
        (r.studentNumber && r.studentNumber.toLowerCase().includes(s)) ||
        (r.tcNo && r.tcNo.includes(s))
      );
    });

    // Görsel Yardımcılar
    const getStatusBadgeClass = (status) => {
      switch (status) {
        case 'Tamamlandı': return 'bg-purple-100 text-purple-800 border border-purple-200';
        case 'Onaylandı': return 'bg-emerald-100 text-emerald-800 border border-emerald-200';
        case 'Hatalı / Eksik': return 'bg-red-100 text-red-800 border border-red-200';
        case 'İnceleniyor': return 'bg-indigo-100 text-indigo-800 border border-indigo-200';
        default: return 'bg-blue-100 text-blue-800 border border-blue-200';
      }
    };

    const getFileIcon = (ext) => {
      if (!ext) return 'fa-regular fa-file';
      ext = ext.toLowerCase();
      if (ext.includes('pdf')) return 'fa-regular fa-file-pdf text-red-500';
      if (ext.includes('doc')) return 'fa-regular fa-file-word text-blue-500';
      if (ext.includes('png') || ext.includes('jpg')) return 'fa-regular fa-file-image text-emerald-500';
      return 'fa-regular fa-file';
    };

    const formatDate = (dateStr) => {
      if (!dateStr) return '';
      try {
        const d = new Date(dateStr);
        return d.toLocaleDateString('tr-TR');
      } catch (e) {
        return dateStr;
      }
    };

    // İlk Yükleme
    onMounted(async () => {
      if (token.value) {
        await loadUserProfile();
        if (currentUser.value?.role === 'ROLE_ADMIN') {
          await loadAdminData();
        } else if (currentUser.value?.role === 'ROLE_STUDENT') {
          await loadStudentDocuments();
        }
      }
    });

    
    return {
      currentUser,
      loading,
      loginError,
      loginForm,
      authTab,
      registerForm,
      registerSuccess,
      registerError,
      register,
      documents,
      showUploadModal,
      selectedReportDoc,
      parsedReport,
      popupAlert,
      fileValidationError,
      googleFormUrlInput,
      uploadForm,
      selectedDaysArray,
      submissionDaysDiff,
      suggestedSubDate,
      adminTab,
      adminDocuments,
      completedDocuments,
      datasetRecords,
      adminLogs,
      adminSearch,
      adminStatusFilter,
      datasetSearch,
      filteredAdminDocs,
      filteredDataset,
      login,
      logout,
      fillLogin,
      onFileSelected,
      submitUpload,
      viewReport,
      deleteDocument,
      verifyGoogleFormUrl,
      loadAdminDocuments,
      changeStatusAdmin,
      deleteAdminDoc,
      uploadDatasetFile,
      unlockUser,
      generateEk17Demo,
      getStatusBadgeClass,
      getFileIcon,
      formatDate
    };
  }
}).mount('#app');
