# Ordinary Fronts

Hamburg, 1943 dikey kesiti. Proje Unity `2021.3.45f2`, 3D Built-In Render Pipeline şablonu üzerinde 2D sunum kullanır. Yeni proje oluşturmayın, render pipeline veya paketleri topluca yükseltmeyin.

## Projeyi açma ve oynatma

1. Unity Hub’da **Open > Add project from disk** ile bu kök klasörü seçin.
2. Editör sürümü olarak kesinlikle `2021.3.45f2` seçin.
3. Gerekirse **Ordinary Fronts > Build Demo Assets and Scene** menüsünü çalıştırın; bunun batch eşdeğeri `OrdinaryFronts.Editor.DemoBuilder.BuildAll` metodudur.
4. `Assets/OrdinaryFronts/Scenes/Main.unity` sahnesini açın ve **Play** düğmesine basın.

Oyun **varsayılan olarak İngilizce başlar**. Dil, Ayarlar ekranındaki ilk satırdan `English` ↔ `Türkçe` olarak değiştirilir; değişiklik oyunun ortasında yapılsa bile ilerleme korunur. Ayrıntılar için `Docs/GDD.md` §15.

Ana menüde `New Game`, geçerli kayıt varsa `Continue`, `Settings`, `Credits` ve `Exit` bulunur. Seçimler fareyle veya `A`/`Sol Ok` ve `D`/`Sağ Ok` ile; duraklatma `Escape` ile çalışır.

Oynanış ekranı üç bölgeden oluşur: ince bir başlık şeridi (bölüm · tarih · konum), nefes alan sahne illüstrasyonu ve altta arşiv kâğıdı anlatı kartı. **Görünür durum çubuğu, puan veya sayaç yoktur** — bkz. `Docs/GDD.md` §8.

## Batch doğrulama

Batch mode çalıştırmadan önce Unity Editor’ü normal biçimde kapatın; çalışan bir editör sürecini zorla sonlandırmayın. PowerShell örneği:

`Unity.exe` bir GUI uygulaması olduğu için `& $UnityExe ...` çağrısı **beklemeden döner**. Komutlar art arda `&` ile yazılırsa hepsi aynı anda başlar ve ikinciden itibaren `It looks like another Unity instance is running with this project open` hatasıyla düşer. Bu yüzden her çağrı `Start-Process -Wait` ile yapılmalıdır:

```powershell
$UnityExe = 'C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe'
$ProjectPath = (Get-Location).Path

if (-not (Test-Path -LiteralPath $UnityExe)) {
    throw "Unity 2021.3.45f2 bulunamadı: $UnityExe"
}

New-Item -ItemType Directory -Force -Path (Join-Path $ProjectPath 'Logs') | Out-Null

function Invoke-Unity {
    param(
        [Parameter(Mandatory = $true)][string]$ExtraArgs,
        [Parameter(Mandatory = $true)][string]$LogName
    )
    $logPath = Join-Path $ProjectPath ('Logs\' + $LogName)
    $line = '-projectPath "{0}" -batchmode {1} -logFile "{2}"' -f $ProjectPath, $ExtraArgs, $logPath
    $process = Start-Process -FilePath $UnityExe -ArgumentList $line -Wait -PassThru -NoNewWindow
    Write-Host ("{0} -> exit {1}" -f $LogName, $process.ExitCode)
}

Invoke-Unity '-quit' '01-import.log'
Invoke-Unity '-quit -executeMethod OrdinaryFronts.Editor.DemoBuilder.BuildAll' '02-build-all.log'
Invoke-Unity '-quit' '03-recompile.log'
```

Yalnız exit code’a güvenmeyin. Loglarda en az `error CS`, derleme hatası, eksik script, `NullReferenceException`, shader/font/sprite kaybı ve build hatası arayın.

## Testler

EditMode ve PlayMode testlerini ayrı çalıştırın:

Yukarıdaki `Invoke-Unity` yardımcısı tanımlıyken:

```powershell
Invoke-Unity ('-runTests -testPlatform EditMode -testResults "' + (Join-Path $ProjectPath 'Logs\editmode-results.xml') + '"') '04-editmode.log'
Invoke-Unity ('-runTests -testPlatform PlayMode -testResults "' + (Join-Path $ProjectPath 'Logs\playmode-results.xml') + '"') '05-playmode.log'
```

Sonuç XML’lerinde başarısız/atlanmış testleri ve ilgili logları inceleyin. Son doğrulama 4 Ağustos 2026 tarihinde yapıldı: `23/23` EditMode ve `2/2` PlayMode testi geçti. Kanıt dosyaları `Logs/42-editmode.xml`, `Logs/42-playmode.xml` ve eşlik eden `.log` dosyalarıdır; yeni değişikliklerden sonra testleri yeniden çalıştırın.

Açılış kurgusunun görsel doğrulaması için derlenmiş player `-ordinaryFrontsCaptureDirectory` ile çalıştırılır; kurgudan üç kare (`intro_1..3`) otomatik yakalanır. Kurgunun toplam süresi ayrıca `Intro_StaysWithinFirstChoiceTimeBudget` testiyle sınanır — yalnız `holdSeconds` toplamı değil, geçiş ve kararma yükü dahil tahmini ekran süresi ölçülür.

## Windows build

Editörde **File > Build Settings** açın, `Assets/OrdinaryFronts/Scenes/Main.unity` sahnesinin listede olduğunu doğrulayın, hedefi **PC, Mac & Linux Standalone / Windows / x86_64** yapın ve çıktıyı şuraya alın:

```text
Builds/Windows/OrdinaryFrontsDemo.exe
```

Projede sağlanan batch build komutu:

```powershell
Invoke-Unity '-quit -executeMethod OrdinaryFronts.Editor.DemoBuilder.BuildWindowsDevelopment' '06-windows-build.log'
```

Windows Standalone Build Support modülü kurulu değilse Unity Hub üzerinden yalnız `2021.3.45f2` editörüne ait modülü ekleyin; proje veya diğer paketleri yükseltmeyin.

Bu çalışma sürecinde Windows x86_64 Development Build başarıyla üretildi: `Builds/Windows/OrdinaryFrontsDemo.exe`. Derlenmiş player 1366×768, 1920×1080, 1920×1200 ve 2560×1080 çözünürlüklerinde ayrı kayıt dizinleriyle açıldı; her koşu iki seçimi, node geçişini, autosave’i ve hem Normal hem Büyük metinde TMP taşma kontrolünü tamamladı. Loglar `Logs/PlayerSmokeRelease/`, görsel QA çıktıları `Logs/PlayerCapturesRelease/` altındadır.

## İçerik ve veri

- Ana sahne: `Assets/OrdinaryFronts/Scenes/Main.unity`
- Hikâye verisi: `Assets/StreamingAssets/Story/<locale>/hamburg_1943.json` (`en-US`, `tr-TR`)
- Arayüz metinleri: `Assets/StreamingAssets/Localization/<locale>.json`
- Yeni oyun açılış kurgusu: hikâye dosyasındaki `intro.beats` dizisi (kod değişikliği gerektirmeden düzenlenebilir; her zaman atlanabilir)
- Tasarım: `Docs/GDD.md`
- Sanat yönü: `Docs/ART_DIRECTION.md`
- Tarihsel kaynak ve kurgu ayrımı: `Docs/HISTORICAL_NOTES.md`
- Kayıtlar: `Application.persistentDataPath` altında JSON
- TMP Essentials: Unity TextMeshPro `3.0.6` paketinden batch-safe içe aktarılır; Liberation Sans için OFL metni `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` içindedir.

Tüm ana karakterler kurgusaldır. Demo savaş, bombardıman, zorunlu çalışma ve devlet baskısı temaları içerir; grafik şiddet içermez.
