# Ordinary Fronts - Hamburg Dikey Kesit Oyun Tasarım Belgesi

Belge durumu: uygulama hedefi  
Hedef Unity sürümü: `2021.3.45f2`  
Sunum: 2D, Built-In Render Pipeline

## 1. Yüksek seviye tanım

`Ordinary Fronts`, İkinci Dünya Savaşı’nı askerî zaferlerden değil, sıradan insanların kişisel sorumluluklarından anlatan bölüm tabanlı bir seçim oyunu antolojisidir. Dikey kesit yalnızca Hamburg’u içerir. Oyuncu tarihi değiştirmez; Matthias Krüger’in kimi aradığına, kime güvendiğine, ne taşıdığına, hangi emre uyduğuna ve bu seçimlerin bedelini kiminle paylaştığına karar verir.

İlk oynayış hedefi 20-30 dakikadır. Bir rota 14-18 anlamlı karardan oluşur. Veri seti en az 28 anlatı düğümü, en az beş erişilebilir final ve en az sekiz gecikmeli sonuç içerir. İlk anlamlı seçim 45 saniye dolmadan; ilk açık karar yankısı en geç dördüncü anlatı düğümünde görünür.

## 2. Deneyim hedefleri

- İki seçeneğin de ilk bakışta anlaşılması, fakat ikisinin de gerçek bir bedel taşıması.
- Sonucun yalnızca anlık kaynak değişimi değil, birkaç sahne sonra dönen ilişki ve fırsat değişimi olması.
- Oyuncunun tek bir “iyi/kötü” puanıyla yargılanmaması.
- Kısa sürede tamamlanan fakat farklı izleri ve finalleri görmek için yeniden oynanabilen bir yapı.
- Grafik şiddete başvurmadan toz, sıcaklık, sessizlik, belge, kesinti ve kalabalık üzerinden baskı kurmak.
- Nazi rejimini, askerliği veya savaşı romantikleştirmeden; zorunlu çalışma ve devlet şiddetini görünür tutmak.

## 3. İçerik notu ve etik çerçeve

Yeni oyundan önce yalnızca bir kez şu not gösterilir:

> Savaş, bombardıman, zorunlu çalışma ve devlet baskısı temaları içerir. Grafik şiddet içermez.

Tüm ana karakterler kurgusaldır. Alman sivillerin bombardımanda yaşadığı felaket anlatılırken, Nazi rejimi tarafından ırksal ve hukuki olarak hedef alınan kişiler ile yabancı zorunlu işçilerin farklı konumu açık tutulur. Matthias’ın iyi niyeti yapısal zulmü ortadan kaldırmaz. Olek’in rolü oyuncuya ahlak puanı vermek değildir; kendine ait amacı, bilgisi, sınırları ve oyuncunun teklifini reddedebilen kararları vardır.

Tarihsel çerçeve ve kurgu sınırları [HISTORICAL_NOTES.md](HISTORICAL_NOTES.md) içinde kaynaklandırılmıştır.

## 4. Başkarakter ve tekrar eden kadro

### Matthias Krüger

- 24 yaşında, Hamburg’da bakım elektrikçisi.
- Savaş sanayisi için gerekli işçi sayıldığı için askere alınması geçici olarak ertelenmiştir.
- Bombardıman sonrasında ailesi, mahalle ağı, tersaneye dönüş emri ve askerlik emri arasında kalır.
- Olağanüstü fiziksel yeteneği veya tarihsel ayrıcalığı yoktur; başarısı çoğu zaman bir şeyi korurken başka bir şeyi kaybetmektir.

### Aile

Yetişkin kız kardeşi Ruth, Matthias’ın “kurtarılacak bir nesnesi” değil, kendi hareket planı ve çevre bağları olan bir kişidir. Aile arayışı tek bir doğru rotaya bağlanmaz; bulunan izler ve gecikmeler final özetini değiştirir.

### Olek Nowak

Hamburg savaş ekonomisinde çalıştırılan kurgusal Polonyalı sivil zorunlu işçi. İşyerindeki elektrik ve geçiş düzenini bilir, fakat bu bilgi onu yalnızca bir anahtar karaktere indirgemez. Önceliği kendi güvenliği, bir yakınına ilişkin haber ve zorlayıcı iş rejiminde elde edebildiği sınırlı hareket alanıdır. Matthias’a güvenebilir, koşul koyabilir veya ondan uzaklaşabilir.

### Ustabaşı

Üretimin sürmesi, çalışanların yoklama kayıtları ve kendi sorumluluğu arasında sıkışır. Matthias için geçici işçi ertelemesine destek olabilir; bunu karşılıksız bir lütuf olarak değil, işyeri ihtiyacı ve kişisel kanaat üzerinden yapar.

### Sivil savunma görevlisi/komşu

Mahalle bilgisi, yardım ağı ve resmî kurallar arasında bağlantı kurar. Oyuncunun sığınaktaki davranışını, belge güvenini ve daha sonraki kayıt/tahliye erişimini etkileyebilir.

## 5. Üç bölümlü dramatik yapı

### Bölüm I - Sirenler

**Tarih:** 24/25 Temmuz 1943 gecesi  
**Ana baskı:** İlk alarm, tersaneden dönüş, sığınak merdiveni, elektrik kesintisi, sınırlı yer ve ilk yardım kararı.  
**Sistem öğretimi:** İki seçenek, seçim sonrası zarif geçiş ve ilk gecikmeli yankı.  
**Duygusal soru:** İnsan en yakın tehlikede kime karşı sorumludur?

### Bölüm II - Kül

**Tarih:** Bombardımanın hemen ardından  
**Ana baskı:** Hasarlı sokaklar, aile izi, yardım/kayıt masası, erzak ve belge, Olek’in kendi planı.  
**Sistem genişlemesi:** Koşullu seçenek metinleri değil, koşullara göre farklı düğümler; daha önceki sığınak, belge ve güven kararlarının yankıları.  
**Duygusal soru:** Arama, yardım ve kendini koruma aynı anda mümkün olmadığında ne bırakılır?

### Bölüm III - Emir

**Tarih:** Sonraki günler, saldırı dizisi sürerken  
**Ana baskı:** Tersaneye dönüş emri, işyeri yoklaması, tahliye peronu, aile kararı ve askerlik belgesi.  
**Sistem sonucu:** Görünür durumlar tek başına oyun bitirmez; bayraklar, ilişkiler ve birikmiş baskı yazılmış sonuç düğümlerine yön verir.  
**Duygusal soru:** Bir emre uymak güvenlik sağladığında, bunun bedelini kim öder?

## 6. Temel oynanış döngüsü

1. Bölüm/tarih/konum etiketi ve sahne görseli belirir.
2. Oyuncu 55-110 kelimelik anlatı kartını okur.
3. İki açık seçenekten birini fareyle veya klavyeyle seçer.
4. Kısa geçiş sırasında etkiler görünmez katmanda çözülür; ekranda sayaç oynamaz.
5. Görünmeyen bayraklar, ilişkiler ve gecikmeli sonuçlar kaydedilir.
6. Oyun otomatik kayıt yapar ve sonraki düğüme geçer.
7. Uygun sonraki düğümde tek satırlık doğal bir “önceki kararın yankısı” anlatıya katılır.

Ana yolda TODO, sahte seçenek, boş kart veya kilitli gelecek bölüm kutusu bulunmaz.

## 7. Kontroller

| Eylem | Fare | Klavye |
|---|---|---|
| Sol seçeneği seç | Sol karta tıkla | `A` veya `Sol Ok` |
| Sağ seçeneği seç | Sağ karta tıkla | `D` veya `Sağ Ok` |
| Açık onayı kabul et | Onay düğmesine tıkla | Yalnız güvenli, tek anlamlı onaylarda `Enter` |
| Duraklat/geri dön | Arayüz düğmesi | `Escape` |
| Menü dolaşımı | Düğmelere tıkla | Oklar/standart UI seçimi ve `Enter` |

Seçim girişi geçiş sırasında kilitlenir; aynı seçimin iki kez işlenmesine izin verilmez. Klavye odağı görünürdür ve renk tek başına odak bilgisi taşımaz.

## 8. Görünür sayaç yoktur

Oyun bilinçli olarak **hiçbir görünür durum çubuğu, puan veya sayaç kullanmaz.**

Erken bir sürümde `Dayanıklılık`, `Erzak`, `Bağlar` ve `Gözetim` adlı dört şerit oynanış başlığında gösteriliyordu. Ölçüm bu şeritlerin hikâye üzerinde hiçbir etkisi olmadığını gösterdi: veri setinde `104` durum etkisi vardı fakat **sıfır** durum koşulu; yani değerler sürekli yazılıyor, hiçbir yerde okunmuyordu. Hiçbir dallanmayı, yankıyı veya finali değiştirmiyorlardı. Oyuncuya anlamlı bir bilgi vermeden ekranın üst şeridini işgal eden ve kararları "optimize edilebilir" gösteren bir cila katmanıydı.

Bu yüzden mekanik tamamen kaldırıldı. Yerine yeni bir sayaç konmadı; kararların ağırlığı sayı hareketiyle değil, anlatının kendisiyle ve gecikmeli yankılarla kurulur.

Sonucu taşıyan durum bilgisi görünmez katmanda korunur (bkz. §9): bayraklar, ilişkiler ve görülmüş yankılar. Oyuncu bunları bir çubuk olarak değil, sonraki sahnelerde dönen satırlar ve final `İzler` listesi olarak görür.

Bu bir tasarım taahhüdüdür: oynanış ekranına yüzde, kalp, yıldız veya kaynak sayacı eklenmez.

## 9. Gizli durum ve koşullar

Veri odaklı hikâye en az şu kavramları destekler:

- Aileye ilişkin bulunan ipuçları ve hangi kaynaktan geldikleri.
- Tersaneye zamanında dönülmesi veya yoklamanın kaçırılması.
- Olek’in güveni ve kendi planına verilen alan.
- Belgelerin korunması, paylaşılması veya kaybedilmesi.
- Sığınakta yardım edilmesi ve bunun kim tarafından görülmesi.
- Ustabaşının Matthias hakkındaki kanaati.
- Resmî makamların şüphesi.
- Tahliye listesine erişim.
- Görülmüş gecikmeli yankılar; aynı yankı iki kez gösterilmez.

Koşullar okunabilir veri ifadeleriyle değerlendirilir. Etkiler bayrak, ilişki değişimi, gecikmeli yankı ve sonraki düğüm yönlendirmesini kapsar. Her sonuç yazılmış bir anlatı düğümüdür; hiçbir eşik oyuncuya sayı olarak gösterilmez.

### Veri şeması kuralları

Aşağıdaki adlar `StoryVocabulary` içinde tanımlıdır ve `StoryGraphValidator` tarafından açılışta doğrulanır. Tanınmayan bir tür, operasyon veya durum anahtarı oyunu sessizce farklı çalıştırmaz; doğrulama hatası üretir.

| Alan | İzin verilen değerler |
|---|---|
| `type` (koşul ve etki) | `stat`, `relation`, `flag`, `echo` |
| Etki `op` | `add` (mevcut değere ekler), `set` (değeri atar). Boş bırakılırsa `set` sayılır. |
| Koşul `op` — `stat`/`relation` | `atleast`, `atmost`, `equals`, `notequals`. Boş bırakılırsa `equals` sayılır. |
| Koşul `op` — `flag`/`echo` | Yalnız `equals`, `notequals`. Eşik operasyonları bayraklarda geçersizdir. |
| `stat` anahtarları | `resilience`, `supplies`, `bonds`, `surveillance` |

Bayrak etkilerinde `op` davranışı özeldir: **`op: "add"`, `boolValue` ne olursa olsun bayrağı `true` yazar** (yankı bayraklarını açmak için kısayol). Bir bayrağı kapatmak için `op: "set"` ve `boolValue: false` kullanılmalıdır.

`relation` değerleri `-100..100`, `stat` değerleri `0..100` aralığına kelepçelenir.

## 10. Seçim yazımı kuralları

- Her karar düğümünde tam iki seçenek bulunur.
- Seçenekler eylemi ve yakın niyeti açıklar; sonucu veya ahlak yorumunu söylemez.
- Her iki seçenek de anlaşılır bir gerekçeye ve farklı bir bedele sahiptir.
- “Yardım et / umursama” gibi yapay iyi-kötü ikilikleri kullanılmaz.
- Bazı sonuçlar hemen, bazıları iki-dört düğüm sonra, bazıları final izlerinde görünür.
- Oyuncu tarihsel harekâtı durduramaz, rejimi tek başına alt edemez ve kahraman asker olamaz.
- Metin kartları ideal olarak 55-110 kelimedir; sürekli şiirsel veya sloganvari dil kullanılmaz.
- Grafik ölüm tasviri ve çatışma oynanışı yoktur.

## 11. Final tasarımı

En az beş final erişilebilir olmalıdır. Final sınıfları arasında askerlik emrinin yürürlüğe girmesi, geçici işçi ertelemesi, tahliye, işyeri içinde koşullu kalış ve rejim gözetiminin ağırlaşması bulunabilir. Bunlar “en iyi” veya “en kötü” olarak sıralanmaz.

Her final ekranı şunları içerir:

- Final başlığı.
- Matthias’ın kişisel sonucunu anlatan 2-4 kısa paragraf.
- Oyuncunun önemli kararlarından 3-5 maddelik `İzler` listesi.
- `Yeniden Oyna` ve `Ana Menü` eylemleri.

Yıldız, ahlak puanı, başarı yüzdesi veya kanonik final etiketi kullanılmaz. İzler; aile arayışı, belge, sığınak davranışı, Olek’in kararı, tersane ve tahliye gibi somut seçimleri geri çağırır.

## 12. Tekrar oynanabilirlik

Tekrar oynama motivasyonu gizli içerik koleksiyonundan değil, farklı bedelleri karşılaştırmaktan gelir. Erken seçimlerin birkaç düğüm sonra farklı satır, erişim, ilişki veya rota üretmesi; aynı finale farklı `İzler` ile ulaşılabilmesi; en az beş ayrı son durum kısa demoyu yeniden oynamayı anlamlı kılar.

Ana menüde kilitli antoloji bölümleri veya sahte gelecek içerik gösterilmez. Gelecekteki bölümler ülke, tarih, karakter, görsel paket ve dil verileriyle eklenebilir; Hamburg demosu bunun reklamını yapmaz.

## 13. Ekran akışı

```text
Ana Menü
  ├─ Yeni Oyun → tek seferlik içerik notu → açılış kurgusu (atlanabilir) → Sirenler
  ├─ Devam Et → son güvenli otomatik kayıt
  ├─ Ayarlar
  ├─ Emeği Geçenler
  └─ Çıkış

Oynanış → Escape → Duraklat/Ayarlar → Oynanış
Oynanış → Final → Yeniden Oyna | Ana Menü
Bozuk kayıt → anlaşılır uyarı → Yeni Oyun | Ana Menü
```

`Devam Et`, geçerli kayıt yoksa pasiftir. Ürün adı yalnızca ana menü ve emeği geçenler ekranında görünür; oynanış, duraklatma, ayarlar, yükleme, açılış kurgusu ve final ekranlarında büyük ürün logosu yoktur. Slogan kullanılmaz.

### Açılış kurgusu

Yeni oyun, ilk anlatı düğümünden önce kısa bir açılış kurgusuyla başlar. Amaç, oyuncuyu kararların ağırlığına hazırlamak ve tarihsel çerçeveyi oyuncunun okuma yükü olmadan kurmaktır.

- Kurgu, hikâye verisindeki `intro.beats` dizisinden okunur; kod değişikliği gerektirmeden düzenlenebilir ve yerelleştirilebilir.
- Her kart bir sahne görseli, kısa bir tarih/yer etiketi ve tek cümlelik anlatı satırı taşır.
- Toplam tutma süresi `20 saniyenin` altında tutulur; bu, GDD §16'daki "ilk seçim en geç 45 saniyede" kapısını korur ve otomatik testle sınanır.
- Kurgu **her zaman atlanabilir**: herhangi bir tuş veya tıklama doğrudan ilk düğüme geçirir. Atlama, kurguyu başlatan tıklamanın kazara sayılmaması için kısa bir gecikmeyle etkinleşir.
- `Devam Et` açılış kurgusunu oynatmaz; kurgu yalnızca yeni oyuna aittir.
- `Hareket azaltma` açıkken yakınlaşma, letterbox animasyonu, daktilo etkisi ve gren döngüsü kapanır; kartlar yalnızca çok kısa bir kararmayla değişir ve metin anında tam görünür.
- Açılış kurgusu oyunun sonucunu etkilemez, durum değiştirmez ve seçim içermez.

## 14. Kayıt ve ayarlar

Her seçimden sonra `Application.persistentDataPath` altında otomatik kayıt alınır. Kayıt; şema sürümü, bölüm/düğüm, bayraklar, ilişkiler, görülmüş yankılar, izler ve tamamlanma durumunu içerir. Durum çubukları kaldırıldığında şema sürümü `2`'ye yükseltildi; sürüm `1` kayıtları uyumsuz sayılır ve oyuncuya yeni oyun yolu sunulur. Önce geçici dosyaya yazılır, ardından asıl kayıt güvenli biçimde değiştirilir. Bozuk dosya oyunu çökertmez; kullanıcıya yeni oyun yolu sunulur.

Ayarlar kalıcıdır:

- Ana ses seviyesi.
- Ortam sesi seviyesi.
- Efekt sesi seviyesi.
- Tam ekran.
- Metin boyutu: Normal/Büyük.
- Hareket azaltma.

## 15. Görsel ve ses sunumu

Görsel tema `1940’lar belediye arşivi + linol baskı + editoryal gölge tiyatrosu`dur. Ayrıntılı palet, kompozisyon, sahne briefleri ve hareket kuralları [ART_DIRECTION.md](ART_DIRECTION.md) içindedir.

Ses, müzik yerine düşük seviyeli özgün atmosferi öne çıkarır: uzak liman/şehir, sığınak içi düşük frekans, tren peronu, kâğıt geçişi, seçim onayı ve menü geri dönüşü. Siren kullanılırsa kısa, uzak ve düşük seviyededir; kesintisiz döngü yapılmaz.

## 16. İçerik kalite kapıları

- En az 28 benzersiz ve erişilebilir anlatı düğümü.
- Bir rotada 14-18 karar.
- En az beş validator tarafından erişilebilir final.
- En az sekiz geçerli gecikmeli sonuç.
- İlk seçim en geç 45 saniyede; ilk yankı en geç dördüncü düğümde.
- Her karar düğümünde tam iki dolu seçenek ve geçerli sonraki düğüm.
- Ana yolda dead-end, boş kart, TODO veya sahte buton yok.
- Beş final de 3-5 somut iz gösterir ve sıralanmaz.
- Ürün adı yalnız ana menü ve emeği geçenler ekranı; slogan yok.
- Grafik şiddet, savaş propagandası ve dekoratif Nazi sembolü yok.
- Tarihsel gerçek, tanıklık ve dramatik bileşim [HISTORICAL_NOTES.md](HISTORICAL_NOTES.md) ile ayrılır.
