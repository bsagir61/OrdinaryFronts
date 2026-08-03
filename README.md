# Ordinary Fronts

Hamburg, 1943 dikey kesiti. Proje Unity `2021.3.45f2`, 3D Built-In Render Pipeline şablonu üzerinde 2D sunum kullanır. Yeni proje oluşturmayın, render pipeline veya paketleri topluca yükseltmeyin.

## Projeyi açma ve oynatma

1. Unity Hub’da **Open > Add project from disk** ile bu kök klasörü seçin.
2. Editör sürümü olarak kesinlikle `2021.3.45f2` seçin.
3. Gerekirse **Ordinary Fronts > Build Demo Assets and Scene** menüsünü çalıştırın; bunun batch eşdeğeri `OrdinaryFronts.Editor.DemoBuilder.BuildAll` metodudur.
4. `Assets/OrdinaryFronts/Scenes/Main.unity` sahnesini açın ve **Play** düğmesine basın.

Ana menüde `Yeni Oyun`, geçerli kayıt varsa `Devam Et`, `Ayarlar`, `Künye` ve `Çıkış` bulunur. Seçimler fareyle veya `A`/`Sol Ok` ve `D`/`Sağ Ok` ile; duraklatma `Escape` ile çalışır.

## Batch doğrulama

Batch mode çalıştırmadan önce Unity Editor’ü normal biçimde kapatın; çalışan bir editör sürecini zorla sonlandırmayın. PowerShell örneği:

```powershell
$UnityExe = 'C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe'
$ProjectPath = (Get-Location).Path

if (-not (Test-Path -LiteralPath $UnityExe)) {
    throw "Unity 2021.3.45f2 bulunamadı: $UnityExe"
}

New-Item -ItemType Directory -Force -Path (Join-Path $ProjectPath 'Logs') | Out-Null

& $UnityExe -projectPath $ProjectPath -batchmode -quit -logFile (Join-Path $ProjectPath 'Logs\01-import.log')
& $UnityExe -projectPath $ProjectPath -batchmode -quit -executeMethod OrdinaryFronts.Editor.DemoBuilder.BuildAll -logFile (Join-Path $ProjectPath 'Logs\02-build-all.log')
& $UnityExe -projectPath $ProjectPath -batchmode -quit -logFile (Join-Path $ProjectPath 'Logs\03-recompile.log')
```

Yalnız exit code’a güvenmeyin. Loglarda en az `error CS`, derleme hatası, eksik script, `NullReferenceException`, shader/font/sprite kaybı ve build hatası arayın.

## Testler

EditMode ve PlayMode testlerini ayrı çalıştırın:

```powershell
& $UnityExe -projectPath $ProjectPath -batchmode -runTests -testPlatform EditMode -testResults (Join-Path $ProjectPath 'Logs\editmode-results.xml') -logFile (Join-Path $ProjectPath 'Logs\04-editmode.log')
& $UnityExe -projectPath $ProjectPath -batchmode -runTests -testPlatform PlayMode -testResults (Join-Path $ProjectPath 'Logs\playmode-results.xml') -logFile (Join-Path $ProjectPath 'Logs\05-playmode.log')
```

Sonuç XML’lerinde başarısız/atlanmış testleri ve ilgili logları inceleyin. Son doğrulama 3 Ağustos 2026 tarihinde yapıldı: `10/10` EditMode ve `1/1` PlayMode testi geçti. Kanıt dosyaları `Logs/EditModeResultsFinalRelease.xml`, `Logs/PlayModeResultsFinalRelease.xml`, `Logs/21_editmode_final_release.log` ve `Logs/22_playmode_final_release.log` altındadır; yeni değişikliklerden sonra testleri yeniden çalıştırın.

## Windows build

Editörde **File > Build Settings** açın, `Assets/OrdinaryFronts/Scenes/Main.unity` sahnesinin listede olduğunu doğrulayın, hedefi **PC, Mac & Linux Standalone / Windows / x86_64** yapın ve çıktıyı şuraya alın:

```text
Builds/Windows/OrdinaryFrontsDemo.exe
```

Projede sağlanan batch build komutu:

```powershell
& $UnityExe -projectPath $ProjectPath -batchmode -quit -executeMethod OrdinaryFronts.Editor.DemoBuilder.BuildWindowsDevelopment -logFile (Join-Path $ProjectPath 'Logs\06-windows-build.log')
```

Windows Standalone Build Support modülü kurulu değilse Unity Hub üzerinden yalnız `2021.3.45f2` editörüne ait modülü ekleyin; proje veya diğer paketleri yükseltmeyin.

Bu çalışma sürecinde Windows x86_64 Development Build başarıyla üretildi: `Builds/Windows/OrdinaryFrontsDemo.exe`. Derlenmiş player 1366×768, 1920×1080, 1920×1200 ve 2560×1080 çözünürlüklerinde ayrı kayıt dizinleriyle açıldı; her koşu iki seçimi, node geçişini, autosave’i ve hem Normal hem Büyük metinde TMP taşma kontrolünü tamamladı. Loglar `Logs/PlayerSmokeRelease/`, görsel QA çıktıları `Logs/PlayerCapturesRelease/` altındadır.

## İçerik ve veri

- Ana sahne: `Assets/OrdinaryFronts/Scenes/Main.unity`
- Türkçe Hamburg hikâyesi: `Assets/StreamingAssets/Story/tr-TR/hamburg_1943.json`
- Tasarım: `Docs/GDD.md`
- Sanat yönü: `Docs/ART_DIRECTION.md`
- Tarihsel kaynak ve kurgu ayrımı: `Docs/HISTORICAL_NOTES.md`
- Kayıtlar: `Application.persistentDataPath` altında JSON
- TMP Essentials: Unity TextMeshPro `3.0.6` paketinden batch-safe içe aktarılır; Liberation Sans için OFL metni `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` içindedir.

Tüm ana karakterler kurgusaldır. Demo savaş, bombardıman, zorunlu çalışma ve devlet baskısı temaları içerir; grafik şiddet içermez.
