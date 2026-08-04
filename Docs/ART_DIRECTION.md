# Ordinary Fronts - Sanat Yönetimi

## 1. Görsel tez

Ana tema:

> **1940’lar belediye arşivi + linol baskı + editoryal gölge tiyatrosu**

Ekran, tarihî bir fotoğraf albümü veya gerçekçi 3D rekonstrüksiyon gibi değil; arşiv masasındaki kâğıt katmanlarının, sınırlı mürekkep baskılarının ve kesilmiş silüetlerin sakin bir editoryal kompozisyonu gibi görünmelidir. Görsel kimlik savaş ihtişamı değil, kayıt, eksiklik, baskı ve taşınan yük hissi üretir.

Başka oyunların kart düzenleri, karakterleri, ikonları veya resimleri kopyalanmaz. Rastgele internet fotoğrafı kullanılmaz. Tüm sahne görselleri özgün üretilir ya da sabit seed kullanan deterministik araçlarla proje içinde oluşturulur.

## 2. Renk paleti

| Rol | Hex | Kullanım |
|---|---|---|
| İsli lacivert | `#171B21` | Ana arka plan, gece, en koyu silüet |
| Eskimiş kâğıt | `#D8CFB6` | Anlatı kartı, açık yüzey, ikincil metin zemini |
| Kor pası | `#9E4434` | Tehlike/baskı vurgusu, sınırlı odak; dekoratif kırmızı değil |
| Soğuk petrol yeşili | `#3F6468` | Bağlar, serin gölge, seçili/etkileşimli yüzey |
| Soluk hardal | `#A88B4A` | Belge, lamba, dikkat ve küçük sıcak vurgu |
| Koyu mürekkep | `#262522` | Açık kâğıt üstünde ana metin ve çizgi |

Paletin ana oranı yaklaşık `%55` isli lacivert/koyu mürekkep, `%30` kâğıt, `%10` petrol yeşili ve `%5` pas/hardal vurgu olarak düşünülür. Bu oran katı bir shader kuralı değil, sahnelerin parlak veya oyunumsu görünmesini engelleyen bir kompozisyon hedefidir.

Metin kontrastı her çözünürlükte ayrıca kontrol edilir. Kor pası ile petrol yeşili birbirinin tek anlamsal karşıtı yapılmaz; etiket, simge, desen veya yön işareti mutlaka eşlik eder.

## 3. Şekil ve doku dili

- Silüetler iki-dört büyük değer katmanından kurulur; küçük ayrıntı yerine okunabilir dış hat kullanılır.
- Kenarlar hafif pürüzlü linol kesim hissi taşır, fakat metin ve UI sınırları net kalır.
- Kâğıt dokusu düşük kontrastlı lif, seyrek is lekesi ve çok ince vignette içerir.
- Halftone/tram yalnız geniş gölge alanlarında kullanılır; metin panelinin altında titreşimli desen oluşturmaz.
- Katman maskeleri tam mekanik simetri yerine kontrollü sapma gösterir.
- Panel köşeleri kareye yakın veya çok küçük yarıçaplıdır. Aşırı yuvarlatılmış mobil kart görünümü yoktur.
- İnce çerçeve, kesik kayıt çizgisi, mühür izi ve dosya sekmesi gibi arşiv çağrışımları sınırlı kullanılır; sahte okunabilir resmî belge üretilmez.
- Vignette, is ve duman metni asla örtmez. En yoğun efekt görsel alanın dış kenarlarında kalır.

## 4. Katman şeması

Her sahne görseli mümkünse şu katmanlardan oluşur:

1. İsli lacivert temel ve yumuşak vignette.
2. Uzak mimari/ufuk silüeti.
3. Orta plan mekân öğesi: vinç, merdiven, cephe, masa veya tren.
4. Ön plan insan/nesne silüeti.
5. Çok düşük opaklıklı kâğıt ve halftone katmanı.
6. Tek bir kontrollü pas, petrol yeşili veya hardal odak.

Parallax yalnız 2-3 büyük katmanda, birkaç piksel ölçüsünde ve ağır hareketle kullanılabilir. `Hareket azaltma` açıkken katmanlar tamamen sabitlenir.

## 5. Altı ana sahne briefi

### 5.1 Akşam tersanesi ve liman vinçleri

- **Kompozisyon:** Vinç kolları üst üçte birde çapraz ritim kurar; Matthias küçük bir ön plan silüeti olarak kalır.
- **Palet:** İsli lacivert baskın, su/metal gölgelerinde petrol yeşili, tek tük hardal pencere ışığı.
- **Atmosfer:** Uzak şehir sisi, kablo ve iskele ritmi; gemi veya silah kahramanlaştırılmaz.
- **Kaçınılacaklar:** Bayrak, okunabilir şirket logosu, parlak kıvılcım yağmuru, görkemli savaş gemisi pozu.

### 5.2 Sivil sığınak merdiveni

- **Kompozisyon:** Yukarıdan aşağı inen dar diyagonal; korkuluk ve basamaklar oyuncunun gözünü küçük kapı ışığına götürür.
- **Palet:** Koyu mürekkep/lacivert, kâğıt rengi duvar aşınması, sınırlı hardal lamba.
- **Atmosfer:** Kalabalık baş ve omuz silüetleri; bireysel yüz detayı yerine sıkışıklık ve bekleme.
- **Kaçınılacaklar:** Grafik yaralanma, aşırı titreşim, korku oyunu karanlığı nedeniyle okunmazlık.

### 5.3 Bombardıman sonrası konut sokağı

- **Kompozisyon:** Hasarlı cepheler iki yanda çerçeve, ortada geçilebilir fakat belirsiz bir yol.
- **Palet:** Kâğıt külü, koyu moloz, küçük kor pası lekeleri; alev görselin ana konusu değildir.
- **Atmosfer:** Toz, kopmuş hat, açık pencere boşlukları ve sessiz insan grupları.
- **Kaçınılacaklar:** Ceset, grafik şiddet, felaketi estetik bir “ateş gösterisi”ne dönüştürmek.

### 5.4 Yardım veya kayıt masası

- **Kompozisyon:** Yatay masa katmanı ekranın alt üçte birinde; üstte kuyruk ve duvara iliştirilmiş soyut kâğıt şekilleri.
- **Palet:** Eskimiş kâğıt baskın, petrol yeşili gölge, hardal küçük odak.
- **Atmosfer:** Kalem, dosya kenarı, el ve sıra numarası çağrışımı; görselin içinde okunabilir metin yok.
- **Kaçınılacaklar:** Gerçek kurum formunu taklit etmek, dekoratif mühür kalabalığı, oyuncuya oyun adı göstermek.

### 5.5 Tren peronu / tahliye alanı

- **Kompozisyon:** Ray ve peron çizgileri uzak kaçış noktasına yönelir; bavullar ve insanlar ritmik, düzensiz kümeler oluşturur.
- **Palet:** Lacivert/hardal şafak öncesi, petrol yeşili tren gövdesi, kâğıt sisi.
- **Atmosfer:** Bekleme, liste kontrolü ve yön belirsizliği. Tren özgürlük simgesi olarak kesinleştirilmez.
- **Kaçınılacaklar:** Okunabilir istasyon tabelası gerekiyormuş gibi davranmak, romantik buhar treni posteri, bayrak.

### 5.6 Şafakta liman silüeti

- **Kompozisyon:** Alçak ufuk, geniş negatif alan ve sabit vinç çizgileri. Final kartı için sakin fakat çözümsüz bir zemin.
- **Palet:** İsli lacivertten kâğıt grisine geçiş; küçük soğuk petrol yeşili; pas rengi minimum.
- **Atmosfer:** Dumanın ardından işleyen liman, uzaklık ve yarım kalmış sorumluluk.
- **Kaçınılacaklar:** Zafer güneşi, askerî selam, slogan veya finali “iyi son” gibi kodlayan parlaklık.

Görsellerin içinde yazı, logo, bayrak, filigran, oyun adı veya slogan bulunmaz.

## 6. Arayüz sistemi

### Anlatı ekranı

Ekran üç bölgeden oluşur ve **hiçbir HUD sayacı içermez**:

- **Başlık şeridi (üst ~%8.5):** tek satır. Solda bölüm adı (harf aralıklı, versal), sağda tarih · konum, en sağda küçük `ESC` ipucu. Altında ince pas çizgisi. Durum çubukları kaldırıldığı için bu şerit iki satırdan tek satıra indi.
- **İllüstrasyon alanı (orta ~%38):** sahne görseli buradan nefes alır. Anlatı kartı ile başlık arasındaki bu bant, kompozisyonun asıl konusudur; kart onu ezmez.
- **Anlatı kartı (alt ~%48):** açık kâğıt panel. Sol kenarında tam boy pas şeridi arşiv dosya sekmesi çağrışımı kurar. İçinde sırasıyla gecikmeli yankı (varsa), anlatı gövdesi ve iki eş ağırlıklı seçim alanı bulunur.

Yankı yoksa anlatı gövdesi o alanı da kullanır; kartta düğüm başına değişen boşluk bırakılmaz.

Oynanış ekranında ürün adı, logo, yüzde, kalp, yıldız veya kaynak sayacı bulunmaz.

### Kart ve düğmeler

- Kartlar düşük yarıçaplı, ince mürekkep çerçeveli ve hafif kâğıt gölgeli 9-slice sprite kullanır.
- Seçim düğmeleri: sol kenarda tam boy pas vurgu şeridi, üstte küçük hardal tuş etiketi (`A / ←`), altında eylem metni. Tuş etiketi eylem metninin önüne geçmez.
- Düğme arka planı sprite'ın kendi rengiyle çizilir. **Koyu bir tint uygulanmaz**: 9-slice kenarındaki pas çizgisini karartıp düğmeyi düz siyah bir bloğa çevirir.
- Normal, üzerine gelme, odak, basılı ve pasif durumların her biri ayırt edilir; pasif seçenek metni `—` olur, yani ayrım yalnız renge dayanmaz.
- Varsayılan Unity mavi düğmeleri ve parlak gradyanlar kullanılmaz.
- Büyük metin ayarında gövde ve seçenek metinleri birlikte ölçeklenir; sabit piksel yüksekliğiyle metin kırpılmaz.

## 7. Tipografi

- Tüm çalışma zamanı metni TextMeshPro kullanır.
- Lisansı belirsiz sistem fontları projeye kopyalanmaz. Yalnız proje içinde lisansı açıkça belgelenmiş veya Unity/TMP ile güvenli dağıtılan font assetleri kullanılır.
- Başlıklar arşiv etiketini andıran sıkı fakat rahat okunan bir ağırlıkta; gövde metni uzun okumaya uygun sade bir ailede olmalıdır.
- Tamamı büyük harf yalnız kısa bölüm/konum etiketlerinde kullanılabilir; uzun metin ve seçeneklerde kullanılmaz.
- Türkçe karakterler (`ç, ğ, ı, İ, ö, ş, ü`) font atlasında ve fallback zincirinde doğrulanır.
- Normal/Büyük ayarları yalnız ölçek büyütmez; satır yüksekliği, panel minimum yüksekliği ve seçenek aralığı da uyarlanır.

## 8. Hareket dili

Standart geçişler `0.18-0.35 saniye` aralığındadır:

- Kart değişimi: çok hafif yatay kayma + kâğıt katmanı değişimi + yumuşak kararma.
- Seçim onayı: kısa çerçeve koyulaşması ve ses; zıplama veya büyüyüp küçülme yok.
- Durum değişimi: doluluk değerinin kısa interpolasyonu ve tek seferlik yön işareti.
- Arka plan: isteğe bağlı birkaç piksellik sakin parallax.
- Menü: opaklık ve küçük konum geçişi; sürekli hareket eden parça yok.

`Hareket azaltma` açıkken parallax, kamera titreşimi ve büyük yatay hareket kapanır. Ekranlar kısa cross-fade ile değişir; işlevsel durum değişimleri anında ve anlaşılır kalır.

### Açılış kurgusu hareketi

Açılış kurgusu, oynanış içi UI geçişlerinden bilinçli olarak daha yavaştır; sinematik bir giriş olduğu için `0.18-0.35 saniye` kuralının dışındadır. Kullanılan araçlar sınırlıdır:

- **Kart geçişi:** iki arka plan katmanı arasında yaklaşık `0.55 saniyelik` çapraz geçiş.
- **Yakınlaşma:** kart süresince en çok `%5.5` ölçek artışı. Kaydırma, döndürme veya kamera hareketi yoktur.
- **Letterbox:** açılışta bir kez `0.5 saniyede` içeri girer, kurgu boyunca sabit kalır.
- **Etiket:** tarih/yer etiketi `0.5 saniyede` harf harf belirir; gövde satırı yalnızca kararmayla gelir, harf harf yazılmaz.
- **Gren:** üç kare arşiv greni yaklaşık `7 fps` ile döner ve opaklığı `0.05`'i geçmez. Metnin okunurluğunu etkilemez.

Kontrast iki katmanla kurulur: tüm görüntüye serilen hafif bir is perdesi (`0.55`) sahneleri birleştirir; metin bandına serilen yumuşak dikey gradyan (`intro_text_scrim`, tepe opaklık `0.62`) kontrastı yalnız gerektiği yerde yükseltir. Tek başına güçlü bir genel perde, illüstrasyonun değer katmanlarını düzleştirdiği için tercih edilmez. Gradyan üstte ve altta tamamen saydamdır ve sert kenarlı bir bant gibi okunmamalıdır.

`Hareket azaltma` açıkken bu beş öğenin tamamı kapanır: yakınlaşma yok, letterbox anında yerleşir, etiket anında tam görünür, gren tek kareye sabitlenir ve geçiş `0.12 saniyeye` iner. Kurgu bu modda da tam olarak okunabilir ve aynı süre boyunca durur.

Açılış kurgusunda parlama, ekran sarsıntısı, hızlı kesme ve titreşimli efekt kullanılmaz. Kurgu her koşulda atlanabilir.

## 9. Yerleşim ve çözünürlük

- Canvas Scaler: `Scale With Screen Size`.
- Referans çözünürlük: `1920x1080`.
- Güvenli test hedefleri: `1366x768`, `1920x1080`, `2560x1440`, 16:10 ve ultrawide.
- Ana içerik için merkezde maksimum okunabilir genişlik kullanılır; ultrawide’da metin satırı uzatılmaz, yan görsel alan nefes alır.
- 16:10’da dikey alan artışı görseli büyütebilir, seçimler ekran dışına itilmez.
- Anlatı ve seçenekler layout grupları/anchor’larla büyür; önemli kontroller sabit koordinata bağlanmaz.
- Büyük metin ayarında gövde alanı kaydırılabilir olabilir, fakat iki seçim ve geri/duraklatma erişimi kaybolmaz.
- UI kenar güvenliği minimum 48 referans piksel; birincil metin satır uzunluğu yaklaşık 55-85 karakter hedefler.

## 10. Doku ve sprite üretimi

Görüntü üretme aracı kullanılırsa altı sahne aynı palet, katman yoğunluğu ve silüet dilinde üretilir. Promptlar yazı, logo, bayrak, filigran ve grafik şiddeti açıkça dışlar.

Deterministik Editor üretimi kullanılırsa:

- Sabit seed ile kâğıt lifi, halftone, is lekesi ve vignette PNG’leri üretilir.
- Liman, bina, merdiven, tren ve insan silüetleri basit çokgen/maske katmanlarından oluşturulur.
- Kart ve düğmeler için kenarları güvenli 9-slice sprite’lar üretilir.
- Aynı seed ve ayarlar aynı dosyayı üretir; araç yeniden çalıştırıldığında kopya asset oluşturmaz.
- Karmaşık shader yerine Built-In Render Pipeline ile güvenilir önceden işlenmiş dokular tercih edilir.

Önerilen import tabanı:

- `Texture Type`: `Sprite (2D and UI)`.
- `sRGB`: açık.
- `Wrap Mode`: `Clamp`.
- `Filter Mode`: sahne görselinde `Bilinear`; piksel estetiği hedeflenmediği için `Point` kullanılmaz.
- UI arka planlarında mipmap kapalı; sahne görsellerinde hedef kullanım ve belleğe göre doğrulanır.
- Sıkıştırma, kâğıt dokusunda blok artefaktı üretmeyecek kaliteyle ayarlanır.
- Sprite sınırları ve 9-slice kenarları yeniden üretim sonrası otomatik doğrulanır.

## 11. Sesle görsel eşleşme

- Kâğıt geçişi, kart hareketinin başladığı anda çok düşük seviyede çalar.
- Seçim onayı görsel çerçeve vurgusuyla senkrondur.
- Liman, sığınak ve peron ambience’ı sahne değişiminde kısa cross-fade yapar.
- Siren görsel titreme üretmez; varsa kısa, uzak ve düşük seviyede kalır.
- Ses kapalıyken bütün durum ve odak bilgisi görsel olarak eksiksizdir.

## 12. Marka ve içerik yasakları

- `Ordinary Fronts` adı yalnız ana menü ve emeği geçenler ekranında görünür. İşletim sistemi pencere başlığı doğal istisnadır.
- Oynanış, duraklatma, ayarlar, yükleme ve final ekranında büyük ürün adı/logo yoktur.
- Slogan üretilmez veya gösterilmez.
- Nazi sembolleri logo, desen, menü süsü veya dekoratif tekrar olarak kullanılmaz.
- Neon, parlak mobil gradyan, aşırı yuvarlatılmış panel, rozet kalabalığı ve zıplayan düğme yoktur.
- Grafik şiddet, ceset odağı ve yangını görsel gösteriye dönüştüren kompozisyon yoktur.
- Tarihî fotoğraf rastgele indirilmez; kaynak görsel varsa yalnız araştırma referansıdır, oyuna lisanssız kopyalanmaz.

## 13. Görsel QA kontrolü

- Altı ana sahne özgün ve birbiriyle tutarlı mı?
- Her sahnede anlatı paneli için yeterli kontrast ve negatif alan var mı?
- Türkçe karakterler tüm TMP boyutlarında doğru mu?
- Normal ve Büyük metinde anlatı/iki seçenek kırpılmadan okunuyor mu?
- Fare hover, klavye odağı ve pasif durum renk dışında da ayırt ediliyor mu?
- `1366x768`, `1920x1080`, `2560x1440`, 16:10 ve ultrawide’da öğeler üst üste biniyor mu?
- Hareket azaltma açıkken parallax, titreşim ve büyük geçişler tamamen kapanıyor mu?
- Ürün adı ana menü ve emeği geçenler ekranı dışında veya görsel asset içinde yanlışlıkla görünüyor mu?
- Varsayılan Unity mavi butonu, kayıp sprite/font veya pembe shader yüzeyi var mı?
- Duman, vignette ve halftone metin okunurluğunu etkiliyor mu?
