# OrdinaryFronts çalışma notları

- Projeyi yalnız Unity `2021.3.45f2` ile açın; render pipeline Built-In kalmalıdır.
- Üretilen bütün demo içeriği `Assets/OrdinaryFronts/` altındadır. Hikâye verisi `Assets/StreamingAssets/Story/tr-TR/hamburg_1943.json` dosyasındadır.
- Sahne YAML'ını elle düzenlemeyin. `OrdinaryFronts.Editor.DemoBuilder.BuildAll` idempotent üreticisini kullanın.
- Ürün adı yalnız `BrandConfig` üzerinden gelir. Oynanış, ayarlar, duraklatma ve final ekranlarına logo/ad eklemeyin.
- Paket sürümlerini, render pipeline'ı veya input sistemini yükseltmeden önce proje kısıtlarını yeniden okuyun.
- Değişikliklerden sonra sırayla compile, `BuildAll`, EditMode ve PlayMode testlerini çalıştırın; logları `Logs/` altında tutun.
