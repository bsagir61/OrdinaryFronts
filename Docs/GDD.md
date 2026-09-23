# Ordinary Fronts - Hamburg Dikey Kesit Oyun Tasarım Belgesi

Belge durumu: uygulama hedefi  
Hedef Unity sürümü: `2021.3.45f2`  
Sunum: 2D, Built-In Render Pipeline

## 1. Yüksek seviye tanım

`Ordinary Fronts`, İkinci Dünya Savaşı’nı askerî zaferlerden değil, sıradan insanların kişisel sorumluluklarından anlatan bölüm tabanlı bir seçim oyunu antolojisidir. Antoloji şu an üç bölüm içerir: Hamburg 1943 (bakım elektrikçisi Matthias Krüger), Neretva 1943 (köy ebesi Milena Radić) ve Amsterdam 1945 (ilkokul öğretmeni Truus Bakker). Oyuncu tarihi değiştirmez; kimi aradığına, kime güvendiğine, ne taşıdığına, hangi emre uyduğuna ve bu seçimlerin bedelini kiminle paylaştığına karar verir.

İlk oynayış hedefi 20-30 dakikadır. Bir rota 14-18 anlamlı karardan oluşur. Veri seti en az 28 anlatı düğümü, en az beş erişilebilir final ve en az sekiz gecikmeli sonuç içerir. İlk anlamlı seçim 45 saniye dolmadan; ilk açık karar yankısı en geç dördüncü anlatı düğümünde görünür.

## 2. Deneyim hedefleri

- İki seçeneğin de ilk bakışta anlaşılması, fakat ikisinin de gerçek bir bedel taşıması.
- Sonucun yalnızca anlık kaynak değişimi değil, birkaç sahne sonra dönen ilişki ve fırsat değişimi olması.
- Oyuncunun tek bir “iyi/kötü” puanıyla yargılanmaması.
- Kısa sürede tamamlanan fakat farklı izleri ve finalleri görmek için yeniden oynanabilen bir yapı.
- Grafik şiddete başvurmadan toz, sıcaklık, sessizlik, belge, kesinti ve kalabalık üzerinden baskı kurmak.
- Nazi rejimini, askerliği veya savaşı romantikleştirmeden; zorunlu çalışma ve devlet şiddetini görünür tutmak.

## 3. Etik çerçeve

Oyunun içerdiği temalar şunlardır:

> Savaş, bombardıman, zorunlu çalışma ve devlet baskısı. Grafik şiddet içermez.

**Bu uyarı oyun içinde bir ekran olarak gösterilmez.** Erken bir sürümde yeni oyundan önce tek seferlik bir "İçerik Notu" ekranı vardı; kaldırıldı. Uyarının kendisi geçerliliğini korur ve **mağaza sayfasında beyan edilmelidir** — Steam'in olgunluk içeriği anketi bu temaların bildirilmesini zaten gerektirir. Yayına hazırlıkta bu madde atlanmamalıdır.

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

Buna karşılık **ilişkiler artık bir karşılık üretir** (bkz. §11 `İnsanlar`). Bu bir sayaç değildir: oyun sırasında hiçbir yerde görünmez, sayı ya da çubuk olarak gösterilmez ve optimize edilemez. Yalnız final raporunda, belirgin biçimde kaymış kişiler için elle yazılmış birer cümle olarak çıkar. Ölçüm gerekçesi §8'dekiyle aynı yöndedir ama sonucu terstir: iki bölümde toplam `135` ilişki etkisi birikiyor ve hiçbiri okunmuyordu; değerler ya bir karşılık üretmeli ya da veriden çıkmalıydı.

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
- Oyuncunun kararlarını bölüm başlıklarıyla veren `İzler` listesi (rotanın tamamı).
- Konumu belirgin biçimde değişmiş kişiler için `İnsanlar` listesi.
- `Yeniden Oyna` ve `Ana Menü` eylemleri.

`İnsanlar` listesi hikâye dosyasındaki `characters` bloğundan gelir: her `relation` anahtarı için bir ad ve iki cümle (`warm` / `cold`). Bir kişinin listeye girmesi için ilişkinin en az `±5` kaymış olması gerekir; tek bir küçük jest kimseyi "yanında" ya da "karşında" yapmaz. Nötr kalanlar hiç görünmez ve sayı asla yazılmaz.

Yıldız, ahlak puanı, başarı yüzdesi veya kanonik final etiketi kullanılmaz. İzler; aile arayışı, belge, sığınak davranışı, Olek’in kararı, tersane ve tahliye gibi somut seçimleri geri çağırır.

## 12. Tekrar oynanabilirlik

Tekrar oynama motivasyonu gizli içerik koleksiyonundan değil, farklı bedelleri karşılaştırmaktan gelir. Erken seçimlerin birkaç düğüm sonra farklı satır, erişim, ilişki veya rota üretmesi; aynı finale farklı `İzler` ile ulaşılabilmesi; en az beş ayrı son durum kısa demoyu yeniden oynamayı anlamlı kılar.

Ana menü doğrudan bir bölüm başlatmaz; `Savaş Hikâyeleri` ekranı bir Avrupa haritasıdır. Her oynanabilir bölüm, geçtiği yerin gerçek enlem ve boylamında (`catalog.json` içindeki `latitude`/`longitude`) bir işaret olarak durur; işaret seçildiğinde sağdaki arşiv panosu bölümün sahne görselini, tarihini ve tek cümlelik tanıtımını gösterir, hikâye oradan başlatılır. Harita kart ızgarasının yerini aldı, çünkü antolojinin iddiası aynı savaşın farklı yerlerdeki sıradan insanlarını yan yana koymaktır; yerin kendisi bu iddianın görünür hâlidir. Hazırlanmakta olan bölümlerin haritada yeri yoktur: uydurma bir işaret, ad veya tarih yanlış beklenti yaratırdı. Yeni bir bölüm eklemek için kod değişikliği gerekmez; konumlu bir katalog girdisi, iki dilde hikâye dosyası ve sahne görselleri yeterlidir.

Harita (`map_europe.png`) elle yazılmış kıyı çokgenlerinden üretilir; 11° batı–42° doğu, 35°–65° kuzey aralığını eşdikdörtgen projeksiyonla kaplar (`MapProjection`). Ayrıntı düzeyi bilinçli olarak düşüktür: bir arşiv haritasının kaba kıyı çizgisi, sınır ya da şehir adı olmadan. Sınır çizilmez, çünkü 1943'te hangi sınırın "geçerli" olduğu başlı başına bir iddiadır ve oyunun tavrı bunu söylememektir.

## 13. Arşiv — oyunun kalıcı belleği

Antoloji üç (ve ileride daha çok) bağımsız hikâyeden oluşur. Aynı savaşın farklı uçlarında geçen bu hikâyeler birbirini nedensel olarak etkileyemez: Hamburg'daki bir elektrikçi Bosna'daki bir ebenin ya da Amsterdam'daki bir öğretmenin kaderini değiştiremez. Fakat oyuncu aynı oyuncudur ve aynı türden sorularla karşılaşır — birine uzatılan defter, düşman sayılan birine yardım, rapordan saklanan bir şey. **Arşiv**, bu örüntüyü oyunun kendisine gösteren sistemdir.

Arşiv `ordinary-fronts-archive.json` dosyasında saklanır; oynanış kaydından ayrıdır ve yeni oyunla silinmez. Bozuk ya da eksikse oyun onsuz eksiksiz çalışır. Üç mekanik buradan beslenir:

### 13.1 Kesişmeler

Bir bölümde tamamlanan oynanışın bayrakları, diğer bölümdeki gecikmeli yankıların koşulu olabilir (`archive` türü, `bölüm:bayrak` anahtarı). Hamburg'da sirenler çaldığında şalteri güvene alan oyuncuya, Neretva'nın ilk düğümünde kağnıyı onarma seçeneği geldiğinde oyun şunu söyler: *"Sirenler çaldığında şalteri güvene almak için tersanede kalmıştın. Kağnıyı yola çıkarmak da aynı türden bir iş: önce makine, sonra insanlar."*

Kurallar:

- Kesişme yalnız **tamamlanmış** bir bölüme dayanır; yarım bırakılan oynanış "yaptığın şey" sayılmaz.
- Kesişme **nedensellik iddia etmez**. Metin her zaman oyuncunun örüntüsünden söz eder, olayların birbirine bağlı olduğunu söylemez.
- Her iki yönde de yazılır: Neretva Hamburg'u, Hamburg Neretva'yı hatırlar. Oyuncunun hangi sırayla oynadığı fark etmez.
- Kesişme arşivsiz ortamda **kapalı-güvenlidir**: hiçbiri yanlışlıkla tetiklenmez.

Mevcut içerik: Neretva'da Hamburg'a bağlı 10, Hamburg'da Neretva'ya bağlı 9 kesişme; Amsterdam'da Hamburg ve Neretva'ya bağlı 26, Hamburg ve Neretva'da Amsterdam'a bağlı üçer kesişme. Test, her kesişmenin gerçekten var olan bir bölümün gerçekten üretilen bir bayrağına bağlandığını zorunlu kılar.

### 13.2 Önceki oynanış izi

Bir bölüm yeniden oynanırken, kayıt defteri o düğümde geçen sefer verilen kararı gösterir: *"Önceki oynanışta burada — Brehm'le kalıp ana şalteri güvene al."* Bu bir ipucu değildir; hangi seçeneğin "iyi" olduğuna dair hiçbir işaret taşımaz. Oyuncu kendi kaydıyla yüzleşir. Yalnız defterde görünür, oynanış kartına yazılmaz.

### 13.3 Yapılmayanlar

Seçimler isteğe bağlı bir `omission` metni taşıyabilir: bu seçim **alınmadığında** final raporuna yazılacak satır. Rapor böylece yalnız yapılanların değil, bırakılanların da kaydı olur: *"Yanmış çiftlikteki aileyi toprağa vermedin."* Yalnız ağırlığı olan seçeneklere yazılır (bölüm başına 5-7), raporda en çok dört satır gösterilir.

### 13.4 Ne değildir

Arşiv bir koleksiyon listesi ya da tamamlanma sayacı değildir. Hiçbir ekranda "6 finalden 3'ünü gördün" yazmaz. Ulaşılan finaller saklanır ama gösterilmez; saklanmalarının tek nedeni ileride bir bölümün "bu oyuncu daha önce şu finali gördü" koşulunu yazabilmesidir. §8'deki taahhüt geçerlidir.

## 14. Ara sahneler — metnin anlatamadığını ellerin yapması

Anlatı kartı oyuncuya ne olduğunu söyler; **ara sahne** oyuncuya o anda ne yaptığını yaptırır. Bir düğüme girilirken, metin gösterilmeden önce oynanan kısa (yarım dakika civarı), hareketli ve tek girdili bir andır. Amacı oyunun sözcük dışındaki tek dilini kurmaktır: rüzgâra karşı bir el arabasını itmenin ağırlığı bir paragrafla değil, tuşu basılı tutmanın süresiyle hissedilir.

### Ne değildir

- **Mini oyun değildir.** Puan, süre sınırı, başarı/başarısızlık, tekrar deneme yoktur. Ara sahnede "kaybetmek" mümkün değildir; yalnız farklı yapmak mümkündür.
- **Refleks testi değildir.** Girdi tek ve basittir (basılı tut / bırak). Hız ya da zamanlama ödüllendirilmez.
- **Grafı değiştirmez.** Düğümün iki seçimi aynı kalır. Giriş sahnesi onlardan önce gelir ve bağlam katar; karar sahnesi ise o iki seçimi iki hareket olarak sunar ve hangisinin verileceğine oyuncunun eli karar verir. Kural yine "tam iki seçim"dir.

### Ne üretir

Sahne biter ve oyuncunun yaptığı şey **bir iki bayrağa** çevrilir (`results` listesinde ilan edilmiş anahtarlar). Sonraki düğümlerin gecikmeli yankıları bu bayraklara bakar; böylece ara sahne, hikâyenin geri kalanına diğer kararlarla aynı kanaldan sızar. Aynı düğümün metni de yankıyla yapılanı hemen kaydeder ("Rüzgâr her estiğinde arabayı durdurup bekledin…"). Bayraklar oynanış kaydına yazılır ve bölüm tamamlandığında arşive geçer; yani bir ara sahnede yapılan, başka bir bölümde kesişme olabilir.

### Kurallar

1. **Atlanabilir.** Esc her ara sahneyi anında geçer. Geçilen sahne sonuç üretmez; ona bakan yankılar sessiz kalır, metin eksiksiz okunur.
2. **Hareket azaltma açıkken oynanmaz.** Ayar oyuncunun ara sahne istemediğinin ilanıdır; sahne "oynandı" sayılır ve geçilir.
3. **Bir kez oynanır.** Tamamlandığı kayda yazılır (`interlude:<id>`); kaydı yükleyen oyuncu aynı sahneyi yeniden görmez.
4. **Yapıya dokunmaz.** Düğümler, seçimler, karar sayıları aynıdır; ara sahne eklemek ve çıkarmak hikâye grafını değiştirmez. Doğrulayıcı türü bilinmeyen, sonucu ilan edilmemiş, finale konmuş veya kimliği yinelenen ara sahneyi reddeder.
5. **Görsel dili sahne görselleriyle aynıdır.** Hareketli parçalar (yürüyen figürler, araba, bebek arabası, kilometre taşı) arka planları çizen aynı silüet kodundan üretilir; ara sahne başka bir oyundan gelmiş gibi durmaz.

### İki zamanlama

- **Giriş sahnesi** (`chooses: false`): düğüme girilirken, metinden önce oynar; yalnız bayrak üretir.
- **Karar sahnesi** (`chooses: true`): oyuncu seçim yapacağı anda oynar. Kartta iki seçenek yine görünür, tuş etiketlerinde "ellerinle karar ver" yazar; herhangi bir tuş sahneyi açar ve seçimi sahnedeki hareket verir. Sonuç listesi seçim sırasıyla dizilir (ilk sonuç birinci seçim). Geçilirse seçim düğmelere döner.

### Ortak çerçeve (1.1.1'de yeniden kuruldu)

- **Tek kontrol dili, oynanışla aynı:** A/← sol seçenek, D/→ sağ seçenek; BOŞLUK ya da sol tık "elinle yap" (it, bağla, tut). Sağ fare ve fare ekseni yok.
- **Girdi kilidi:** sahne açıldığında basılı olan tuş (seçimi açan D, kartı geçen boşluk) sahneye sızmaz; bütün tuşlar bir kez bırakılana kadar girdi yok sayılır.
- **Talimat kartı:** başlık, tek cümlelik "nasıl" ve tuş kapakları. Sahne oyuncu ilk hareketi yapana kadar bekler; hiçbir şey oyuncu hazır olmadan başlamaz. Karar sahnelerinde kart düğümün iki seçeneğini, kartla aynı metinle, A ve D kapaklarının yanında gösterir.
- **Okunur durum:** yalnız o oyunun ihtiyacı olan tek bir gösterge (yol cetveli, ağırlık işareti, ilerleme ya da zamanlama halkası). Puan, can, süre sayacı yok.
- **Anlık komutlar ve sonuç:** ortada kısa, büyük yazı ("BIRAK!", "Tuttun. 2 / 4"); sahne bitince yapılanın tek satırlık karşılığı bir an ekranda kalır.
- **Kaybetmek yok:** yanlış zamanlama yalnız yeniden denemektir; iş ne kadar sürerse sürsün yapılır.

### Mevcut sahneler

**Karanlık merdiven (`hamburg_1943`, `sir_03_siginak_merdiveni`, karar sahnesi).** Talimat kartı iki seçeneği gösterir; A lambaya, D sıraya döner ve seçim geri alınmaz (metindeki gibi ikisi birden yapılamaz). *Lamba:* BOŞLUK/sol tık basılı tutarak kabloyu bağla; lambanın çevresindeki halka dolar. Üç kez kıvılcım uyarısı gelir (ampul beyaz çakar, "BIRAK!"): o an elini çekersen kıvılcım geçer, tutmaya devam edersen elin yanar ve ilerleme biraz geri gider. Bitince lamba yanar ve sıra kendi iner. *Sıra:* kenardaki kişinin uzanan elinin üstünde daralan bir halka ve sabit bir hedef halka belirir; halka hedefe oturduğunda BOŞLUK/sol tık ile elini tutarsın. Dördüncüden sonra gerisi elden ele gelir. Sonuçlar `il_lamp_hands` / `il_stairs_hands`.

**Islak kiriş (`neretva_1943`, `ner_15_ortada`, karar sahnesi).** Grubun üstündeki işaret ağırlığın kendisidir; ıslak kiriş onu yana iter. Sağa kayarsa A/←, sola kayarsa D/→. Kenara varırsa sendelersin (kısa duraklama, gıcırtı). Ortada gevşek tahta kayar: zaman yavaşlar, öndeki diz çöker, sedye suya yatar, "TAHTA KAYDI". Kaymadan *sonra* basılıp basılı tutulan BOŞLUK/sol tık "tut" sayılır (toplam 1,1 saniye); hiçbir şey yapmazsan elin açılır. Denge için basılı olan tuş karar sayılmaz. Sonuçlar `il_plank_hands_held` / `il_plank_hands_open`.

**Afsluitdijk, rüzgâr (`amsterdam_1945`, `set_10_afsluitdijk`, giriş sahnesi).** BOŞLUK/D/sol tık basılı tutuldukça grup yürür. Alttaki cetvel yolun kalanını ve üç boranın yerini gösterir. Boradan hemen önce "Bora geliyor" ve ekranı süpüren rüzgâr dalgası; bora sırasında ya eğilip itersin (yavaş, sarsıntılı) ya bırakırsın ve grup sırtını rüzgâra verip çömelir. Herhangi bir borada bir saniyeden uzun bekleyen `il_wind_waited`, hep iten `il_wind_pushed`. Yaklaşık 25 saniye.

## 15. Ekran akışı

```text
Ana Menü
  ├─ Yeni Oyun → açılış kurgusu (atlanabilir) → Sirenler
  ├─ Devam Et → son güvenli otomatik kayıt
  ├─ Ayarlar
  └─ Çıkış

Oynanış → Escape → Duraklat/Ayarlar → Oynanış
Oynanış → Final → Yeniden Oyna | Ana Menü
Bozuk kayıt → anlaşılır uyarı → Yeni Oyun | Ana Menü
```

`Devam Et`, geçerli kayıt yoksa pasiftir. Ürün adı yalnızca ana menüde görünür; oynanış, duraklatma, ayarlar, yükleme, açılış kurgusu ve final ekranlarında büyük ürün logosu yoktur. Slogan kullanılmaz.

### Açılış kurgusu

Yeni oyun, ilk anlatı düğümünden önce kısa bir açılış kurgusuyla başlar. Amaç, oyuncuyu kararların ağırlığına hazırlamak ve tarihsel çerçeveyi oyuncunun okuma yükü olmadan kurmaktır.

- Kurgu, hikâye verisindeki `intro.beats` dizisinden okunur; kod değişikliği gerektirmeden düzenlenebilir ve yerelleştirilebilir.
- Her kart bir sahne görseli, kısa bir tarih/yer etiketi ve tek cümlelik anlatı satırı taşır.
- Toplam tutma süresi `20 saniyenin` altında tutulur; bu, GDD §19'daki "ilk seçim en geç 45 saniyede" kapısını korur ve otomatik testle sınanır.
- Kurgu **her zaman atlanabilir**: herhangi bir tuş veya tıklama doğrudan ilk düğüme geçirir. Atlama, kurguyu başlatan tıklamanın kazara sayılmaması için kısa bir gecikmeyle etkinleşir.
- `Devam Et` açılış kurgusunu oynatmaz; kurgu yalnızca yeni oyuna aittir.
- `Hareket azaltma` açıkken yakınlaşma, letterbox animasyonu, daktilo etkisi ve gren döngüsü kapanır; kartlar yalnızca çok kısa bir kararmayla değişir ve metin anında tam görünür.
- Açılış kurgusu oyunun sonucunu etkilemez, durum değiştirmez ve seçim içermez.

### Kayıt defteri

Duraklatma ekranından `Kayıt Defteri` açılır. O ana kadar verilmiş kararları, final raporundaki `İzler` ile aynı veriden, bölüm başlıklarıyla gösterir.

Gerekçesi ölçülebilir: bir rota 14-18 karardır ve gecikmeli yankılar oyuncunun saatler önce verdiği bir karara gönderme yapar. Araya bir oturum girdiğinde o bağ kopuyor, yankı anlamsız bir cümleye dönüşüyordu. Defter yeni bir kurgu getirmez; Neretva'da Milena'nın hikâye içinde zaten tuttuğu defterin oynanıştaki karşılığıdır.

Defter salt okunurdur: içinde seçim yapılmaz, hiçbir şey açılmaz, tamamlanma yüzdesi göstermez.

## 16. Kayıt ve ayarlar

Her seçimden sonra `Application.persistentDataPath` altında otomatik kayıt alınır. Aynı klasörde `ordinary-fronts-archive.json` (bkz. §13) ayrı durur ve yeni oyunla silinmez. Kayıt; şema sürümü, bölüm/düğüm, bayraklar, ilişkiler, görülmüş yankılar, izler ve tamamlanma durumunu içerir. Durum çubukları kaldırıldığında şema sürümü `2`'ye yükseltildi; sürüm `1` kayıtları uyumsuz sayılır ve oyuncuya yeni oyun yolu sunulur. Önce geçici dosyaya yazılır, ardından asıl kayıt güvenli biçimde değiştirilir. Bozuk dosya oyunu çökertmez; kullanıcıya yeni oyun yolu sunulur.

Ayarlar kalıcıdır:

- **Dil: English / Türkçe.**
- Ana ses seviyesi.
- Ortam sesi seviyesi.
- Efekt sesi seviyesi.
- Tam ekran.
- Metin boyutu: Normal/Büyük.
- Hareket azaltma.

## 17. Dil ve yerelleştirme

Oyun **varsayılan olarak İngilizce başlar**; ayar dosyası bulunmayan bir kurulumda dil `en-US` olur. Oyuncu dili Ayarlar ekranındaki ilk satırdan değiştirir. Dil adı her zaman kendi dilinde yazılır (`English`, `Türkçe`) ki oyuncu anlamadığı bir dilde açtığında da seçeneği tanıyabilsin; bu yüzden dil satırı listenin en üstündedir.

Yerelleştirme tamamen veri katmanındadır. Yeni bir dil eklemek **kod değişikliği gerektirmez**, iki dosya eklemek yeterlidir:

| Dosya | İçerik |
|---|---|
| `StreamingAssets/Localization/<locale>.json` | Arayüz metinleri (anahtar → değer) |
| `StreamingAssets/Story/<locale>/catalog.json` | Bölüm kataloğu (kartlar) |
| `StreamingAssets/Story/<locale>/hamburg_1943.json` | Hamburg bölümünün hikâye verisi |
| `StreamingAssets/Story/<locale>/neretva_1943.json` | Neretva bölümünün hikâye verisi |
| `StreamingAssets/Story/<locale>/amsterdam_1945.json` | Amsterdam bölümünün hikâye verisi |

Kurallar:

- Arayüzde kullanılan bütün anahtarlar `UiKey` içinde sabit olarak tanımlıdır; testler her anahtarın her dilde dolu bir karşılığı olmasını zorunlu kılar.
- Hikâye dosyaları **yalnız metinde** ayrışır. Düğüm kimlikleri, seçim kimlikleri, hedef düğümler, etkiler ve yankı kimlikleri diller arasında birebir aynı olmalıdır; bir test bunu doğrular. Bu sayede oyuncu oyunun ortasında dil değiştirse bile kaydı geçerli kalır ve aynı daldan devam eder.
- Kayıt dosyası dil bilgisi tutmaz; ilerleme dilden bağımsızdır.
- Bir dil dosyası yüklenemezse oyun varsayılan dile düşer ve metinsiz kalmaz.

## 18. Görsel ve ses sunumu

Görsel tema `1940’lar belediye arşivi + linol baskı + editoryal gölge tiyatrosu`dur. Ayrıntılı palet, kompozisyon, sahne briefleri ve hareket kuralları [ART_DIRECTION.md](ART_DIRECTION.md) içindedir.

Ses, müzik yerine düşük seviyeli özgün atmosferi öne çıkarır: uzak liman/şehir, sığınak içi düşük frekans, tren peronu, kâğıt geçişi, seçim onayı ve menü geri dönüşü. Siren kullanılırsa kısa, uzak ve düşük seviyededir; kesintisiz döngü yapılmaz.

### Sunum katmanı (1.1)

1.1 sürümü hikâyeye, karara ve kayda dokunmadan oyunun nasıl göründüğünü değiştirir. Kural: sunum anlatının önüne geçmez, hiçbir şey oyuncuyu bekletmez, her hareket bir tuşla tamamlanır ve **hareket azaltma** açıkken hepsi durur.

- **Arka plan geçişi ve yavaş kamera.** Sahne görseli değiştiğinde eskisi yenisinin üstünde ~1 saniyede söner; kesme yoktur. Görsel sürekli, çok yavaş bir nefes içindedir (%0,6–5 ölçek, birkaç piksellik kayma); fare paralaksıyla toplanır.
- **Atmosfer.** Her sahnenin kendi havası vardır ve bu hava görseli değil yeri anlatır: Hamburg sokağında kül ve yükselen kıvılcım, sığınakta ve ahırda ışıkta asılı toz, Neretva köprüsünde yağmur ve sis, dağda ve polderde rüzgârlı kar, Afsluitdijk'te yoğun kar ve rüzgâr şeritleri, peronda buhar, limanda sis. Üstüne sahneye özgü düşük opaklıkta bir renk ayarı gelir; üretilmiş ve elle çizilmiş görseller aynı ışık altında durur.
- **Perde kartları.** Bölüm başında (açılış kurgusunun ardından) ve perde değiştiğinde tam ekran bir başlık: "PERDE II", perdenin adı, pas çizgisi, tarih ve yer; alçak bir tonla. İki buçuk saniye sürer, herhangi bir tuş geçer. Oyuncuya bölümün hangi evresinde olduğunu sayı ya da ilerleme çubuğu olmadan söyler.
- **Mürekkep açılışı.** Anlatı metni soldan sağa yumuşak bir cepheyle belirir (uzunluğa göre 0,5–1,5 sn). İlk tuş metni tamamlar ve seçim sayılmaz; fareyle bir seçeneğe tıklamak doğrudan seçer.
- **Canlı ana menü.** Bölümlerin görselleri kendi havalarıyla dokuz saniyede bir yer değiştirir; sağ altta künye ("HAMBURG · TEMMUZ 1943"). Başlığın altında antolojinin türü ve katalogdan hesaplanan yıl aralığı, altta sürüm.
- **Mühürlü final.** Final ekranı bir dosya gibi kapanır: üstte bölüm künyesi, sağ üstte kısa bir gecikmeyle inen "DOSYA KAPANDI" mührü.

## 19. İçerik kalite kapıları

- En az 28 benzersiz ve erişilebilir anlatı düğümü.
- Bir rotada 14-18 karar.
- En az beş validator tarafından erişilebilir final.
- En az sekiz geçerli gecikmeli sonuç.
- İlk seçim en geç 45 saniyede; ilk yankı en geç dördüncü düğümde.
- Her karar düğümünde tam iki dolu seçenek ve geçerli sonraki düğüm.
- Ana yolda dead-end, boş kart, TODO veya sahte buton yok.
- Beş final de 3-5 somut iz gösterir ve sıralanmaz.
- Ürün adı yalnız ana menü; slogan yok.
- Grafik şiddet, savaş propagandası ve dekoratif Nazi sembolü yok.
- Tarihsel gerçek, tanıklık ve dramatik bileşim [HISTORICAL_NOTES.md](HISTORICAL_NOTES.md) ile ayrılır.
