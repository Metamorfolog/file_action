# FileAction

🇬🇧 [English](README.md) · 🇨🇿 **Čeština** · 🌐 [fileaction.apexpiksel.cz](https://fileaction.apexpiksel.cz/?lang=cs)

**Převod souborů bez klikání pro Windows (a Linux).** Chyťte soubor myší, během tažení ťukněte na klávesu a pusťte ho na formát v kruhovém menu. Vše běží lokálně, žádné soubory se nikam neodesílají.

Inspirováno skvělou aplikací [Tangerine](https://tangerineformac.com/) pro macOS. FileAction s ní není nijak spojený, jde o nezávislou obdobu pro Windows.

<p align="center">
  <img src="docs/screenshots/cs/wheel-video-convert.png" width="32%" alt="Kolo Převést pro video" />
  <img src="docs/screenshots/cs/wheel-image-tools.png" width="32%" alt="Kolo Nástroje pro obrázek" />
  <img src="docs/screenshots/cs/wheel-actions.png" width="32%" alt="Kolo Akce" />
</p>
<p align="center">
  <img src="docs/screenshots/cs/wheel-pdf-convert.png" width="32%" alt="Kolo Převést pro PDF" />
  <img src="docs/screenshots/cs/settings.png" width="32%" alt="Nastavení" />
  <img src="docs/screenshots/cs/jobs.png" width="32%" alt="Průběh" />
</p>

## Jak to funguje

1. Chyťte soubor: v Průzkumníku, na ploše, obrázek nebo odkaz v prohlížeči, přílohu e-mailu, soubor uvnitř ZIP složky…
2. Během tažení **ťukněte** na klávesu:

   | Klávesa | Kolo | Co nabízí |
   |---|---|---|
   | **Shift** | Převést | JPG, PNG, WebP, MP4, MP3, DOCX, PDF, XLSX… podle typu souboru |
   | **Ctrl** | Nástroje | komprimovat, zmenšit, otočit, oříznout, spojit, rozdělit, zabalit… |
   | **Alt** | Akce | kopírovat do…, přesunout do…, kopírovat cestu, terminál, plovoucí panel, sdílet |

3. Pusťte soubor na výseč. Výsledek se uloží vedle originálu (originál zůstane beze změny). Puštěním do středu kola akci zrušíte.

Kolo zůstane otevřené, dokud soubor nepustíte. Klávesu tedy můžete pustit a další klávesou přepnout na jiné kolo. Klávesy lze v nastavení přehodit.

**Zopakovat poslední akci:** během tažení ťukněte na stejnou klávesu znovu (nebo na otevřeném kole stiskněte **R**). Kolo se změní na jednu výseč, která zopakuje to, co jste dělali naposledy, se stejným nastavením (třeba stejné časy oříznutí).

**Vlastní kolo:** v **Nastavení → Kola** si každé kolo poskládáte přetahováním: výseče dáte tam, kde je ruka čeká, nepoužívané přetáhnete mimo kolo, z knihovny je vrátíte, a přetažením na záložku jiného kola výseč přesunete (třeba Komprimovat na kolo Převést).

**Předvolby:** v **Nastavení → Předvolby** si poskládáte vlastní výseče, třeba „JPG 80 %, max 1920 px, bez metadat“ nebo „MP4 720p, malý soubor“. Objeví se na kole vedle vestavěných.

**Sledované složky:** v **Nastavení → Sledované složky** vyberete složku a co se má s novými soubory udělat, třeba snímky obrazovky → WebP nebo fotky z telefonu HEIC → JPG. Každý nový soubor se zpracuje, až je celý stažený nebo zkopírovaný; originály zůstávají beze změny.

**Bez myši:** vyberte soubory v Průzkumníku, nebo zkopírujte soubor či snímek obrazovky (**Win + Shift + S**), a stiskněte **Ctrl + Alt + Mezerník**. Šipkami vyberete výseč, Enter potvrdí, Tab přepne kolo, Esc zavře, čísla 1–9 spustí výseč přímo.
Kolo otevřete i přes **pravé tlačítko → FileAction…** (ve Windows 11 pod „Zobrazit další možnosti“).

Průběh převodu se ukazuje v rohu obrazovky. Odtud jde výsledek otevřít, ukázat ve složce nebo převod zrušit.

## Co umí převést

| Typ | Vstup | Výstup |
|---|---|---|
| Obrázky | JPG, PNG, WebP, HEIC, TIFF, AVIF, BMP, GIF, ICO, SVG | JPG, PNG, WebP, AVIF, TIFF, ICO, PDF, SVG (skutečné vektory, nejlépe pro loga a kresby) (GIF → MP4/WebM) |
| Audio | MP3, M4A, AAC, WAV, FLAC, OGG, Opus, AIFF, WMA | MP3, M4A, WAV, FLAC, OGG, Opus, AIFF |
| Video | MP4, MOV, MKV, WebM, AVI, WMV, FLV, MPG, 3GP, TS | MP4, MOV, MKV, WebM, GIF, MP3 |
| PDF | PDF | DOCX, XLSX (tabulky), JPG, PNG (všechny strany, 300 DPI), PPTX, TXT, MD, HTML, EPUB |
| Dokumenty | DOCX, DOC, ODT, RTF, MD, TXT, HTML, EPUB | DOCX, PDF, ODT, MD, HTML, TXT, EPUB, RTF |
| Prezentace | PPTX, PPT, ODP | PDF, PNG, JPG (všechny snímky), PPTX, ODP |
| Tabulky | XLSX, XLSM, XLS, ODS, CSV, TSV | XLSX, CSV, TSV, ODS, PDF |
| Titulky | SRT, VTT | SRT, VTT, TXT |
| Archivy | ZIP, TAR, TAR.GZ, GZ, BZ2, XZ, RAR, 7z | rozbalit, ZIP, TAR, TAR.GZ |

**Nástroje (Ctrl):**
- obrázky: komprimovat, zmenšit na 50 %, max. 1920 px, otočit, zrcadlit, oříznout na čtverec, černobíle, odstranit metadata (GPS, EXIF), spojit do PDF,
- video: komprimovat (H.264), 720p, ztlumit, oříznout podle času, otočit, zrychlit 2×, náhledový snímek, Sony S-Log3 na běžné video, použít LUT ze souboru .cube (přetáhnout spolu s videi; datum natočení zůstane),
- audio: komprimovat, normalizovat hlasitost, oříznout, spojit,
- PDF: spojit, rozdělit po stranách, otočit, optimalizovat,
- obrázky a naskenovaná PDF: **text z obrázku (OCR)** pomocí rozpoznávání vestavěného ve Windows; text se uloží jako .txt a zkopíruje do schránky,
- cokoli včetně složek: zabalit do ZIP / TAR.GZ.

Co ještě není soubor (obrázek přetažený z prohlížeče, příloha z Outlooku, snímek obrazovky ve schránce), se nejdřív uloží do **Stažené soubory\FileAction** a výsledky vzniknou vedle něj.

U více vybraných souborů se převede každý zvlášť. Nástroje typu „spojit“ vytvoří jeden výsledek.

### Pomocné programy

Obrázky, PDF, tabulky, titulky a archivy zvládá FileAction sám. Některé formáty potřebují další programy:

- **FFmpeg** (audio a video, 34 MB) a **Pandoc** (dokumenty, 35 MB) se stáhnou, až je poprvé budete potřebovat: kolo ukazuje jejich výseče ztlumené s popiskem „stáhnout FFmpeg“ a po puštění souboru se jednou zeptá, nástroj stáhne (ověří jeho zveřejněný otisk SHA-256) a převod provede. Pokud je už máte nainstalované (např. `winget install Gyan.FFmpeg`), FileAction použije ty,
- **LibreOffice** (DOC, PPTX, ODS, převod wordových dokumentů a tabulek do PDF) je potřeba doinstalovat zvlášť: `winget install TheDocumentFoundation.LibreOffice`.
- Markdown a webové stránky převádí do PDF **Microsoft Edge** (je součástí Windows) nebo Chrome, LibreOffice k tomu netřeba.

Co chybí, ukáže **Nastavení → Pomocné programy**, odkud jde i rovnou stáhnout nebo nainstalovat. Díky tomu, že nástroje nejsou v instalátoru, má instalátor jen asi 50 MB.

## Instalace

Stáhněte `FileAction-Setup-x.y.z.exe` z [Releases](../../releases) nebo z artefaktů posledního běhu [GitHub Actions](../../actions). Instalátor (asi 50 MB) nepotřebuje práva správce. K dispozici je také přenosná verze v `.zip`.

Aplikace je česky i anglicky a ve světlém i tmavém režimu; obojí se řídí nastavením Windows, jazyk jde přepnout v nastavení.

Při prvním spuštění se FileAction zapíše do automatického spouštění a do kontextové nabídky Průzkumníka (obojí jde vypnout v nastavení). Pak běží v oznamovací oblasti vedle hodin.

**Aktualizace** se instalují samy: FileAction se občas zeptá GitHubu na nejnovější vydání (o vašich souborech se nic neodesílá), nabídne aktualizaci v oznámení a v nabídce u hodin, ověří otisk SHA-256 instalátoru a nainstaluje ho na pozadí. Kontrolu jde v nastavení vypnout. Přenosná verze místo toho otevře stránku ke stažení.

Požadavky: Windows 10 (1809+) nebo Windows 11, x64.

## Linux

**Arch Linux:** u každého vydání je přiložený `PKGBUILD` (balíček `fileaction-bin`). Stáhněte ho do prázdné složky a spusťte `makepkg -si`. Do AUR (`yay -S fileaction-bin`) balíček přibude, jakmile AUR znovu otevře registraci účtů.

**Ostatní distribuce:** stáhněte `FileAction-x.y.z-linux-x64.tar.xz` (asi 32 MB) z [Releases](../../releases), rozbalte a spusťte `fileaction/app/fileaction` (.NET není potřeba).

Linux aplikacím nedovolí sledovat klávesnici a myš jiných programů (Wayland to záměrně zakazuje), takže gesto „táhnu a ťuknu na klávesu“ je jen pro Windows. Na Linuxu se stejné kolo používá takto:

- **Kolo pro přetahování:** spusťte `fileaction` (nebo *FileAction* z nabídky aplikací) a přetáhněte na kolo soubory. Zůstane na obrazovce, posunout ho jde za střed.
- **Pravé tlačítko ve správci souborů:** `fileaction integrate` přidá *FileAction…* do Nautilu (podnabídka *Skripty*), Dolphinu a Nema (balíček z AUR instaluje Dolphin a Nemo pro všechny).
- **Příkazová řádka** a klávesové zkratky prostředí:

  ```sh
  fileaction foto.heic                        # kolo pro tento soubor
  fileaction list foto.heic                   # co se s ním dá dělat
  fileaction run image.to.webp *.heic         # provést, bez okna
  fileaction run video.trim klip.mov --param start=0:10 --param end=0:25
  ```

- **Nastavení:** `fileaction --settings` (také *Nastavení* v nabídce pravého tlačítka u položky v nabídce aplikací, nebo tlačítko v rohu kola pro přetahování) – předvolby, sledované složky, zpětná vazba a podpora. Předvolby se objeví na kolech i ve `fileaction list/run`. Sledované složky obsluhuje `fileaction --watch` na pozadí, který se spouští s přihlášením, dokud je zapnutá aspoň jedna složka.

Volitelné balíčky: `ffmpeg` (audio, video), `pandoc-cli` (dokumenty), `libreoffice-fresh` (Office), `tesseract` + `tesseract-data-ces` (text z obrázku). Nastavení je v `~/.config/fileaction/settings.json`.

## Sestavení ze zdrojů

```powershell
# .NET 10 SDK
./scripts/fetch-tools.ps1                      # stáhne ffmpeg.exe a pandoc.exe do .\tools (pro testy)
$env:PATH = "$PWD\tools;$env:PATH"; dotnet test tests/FileAction.Tests
dotnet publish src/FileAction.App -c Release -r win-x64 --self-contained -o publish   # ořezané, rozbalené ~165 MB
publish\FileAction.exe
```

`FileAction.exe --self-test <složka>` sestaví všechna okna mimo obrazovku a uloží jejich náhledy do PNG v obou jazycích (používá CI).

### Vydání nové verze

Pošlete do `main` commit, jehož zpráva začíná `Release v1.2.3` (nebo ručně spusťte workflow **Build** s číslem verze). CI sestaví instalátor a zip a zveřejní je jako vydání na GitHubu. Když do repozitáře přidáte secret `RELEASE_TOKEN` (osobní přístupový token s oprávněním *Contents: read and write* k tomuto repozitáři), vydání se zveřejní pod vaším účtem místo github-actions.

Stejný běh sestaví i balíček pro Linux, na Arch Linuxu ho zkušebně nainstaluje a když secret `AUR_SSH_PRIVATE_KEY` obsahuje klíč účtu AUR, který spravuje `fileaction-bin`, nahraje nový PKGBUILD do AUR.

## Struktura

```
src/FileAction.Core     převody a nástroje (multiplatformní knihovna, testovaná i na Linuxu)
  Backends/             ImageMagick, FFmpeg, Pandoc, LibreOffice, PDF, tabulky, titulky, archivy
  Operations/Catalog.cs seznam všech výsečí na kolech „Převést“ a „Nástroje“
src/FileAction.Linux    aplikace pro Linux (Avalonia): kolo pro přetahování, příkazová řádka, napojení na správce souborů
src/FileAction.App      aplikace pro Windows (WPF)
  Input/GlobalInput.cs  globální sledování myši a kláves (gesto „táhnu + ťuknu“)
  Wheel/                kruhové menu a logika, kdy se otevře
  Actions/              kolo „Akce“, plovoucí panel, sdílení
  Jobs/                 běh převodů na pozadí a karty s průběhem
  Shell/                ikona v oznamovací oblasti, zkratka, kontextová nabídka, jediná instance
tests/FileAction.Tests  jednotkové testy a integrační testy, které spustí každý převod na vzorových souborech
installer/              Inno Setup
packaging/              balíček pro Linux a šablona PKGBUILD pro AUR
website/                webová stránka projektu (statická)
```

## Zpětná vazba a podpora

Našli jste chybu nebo máte nápad? Napište ji v aplikaci (**Nastavení → O aplikaci → Napsat zpětnou vazbu**, taky v nabídce ikony v oznamovací oblasti), přes [formulář na webu](https://fileaction.apexpiksel.cz/?lang=cs#feedback), e-mailem na [support@apexpiksel.cz](mailto:support@apexpiksel.cz) nebo založte [issue](https://github.com/Metamorfolog/file_action/issues).

FileAction je zdarma a zdarma zůstane. Pokud vám šetří čas, můžete jeho vývoj podpořit:

- **Solana** (SOL, USDC): `J7vmQqiFe6t3RSX9m8qCTFkxe6xDRzbPy5nMaeGNV7bL`  
  QR kód a tlačítko „Otevřít v peněžence“ najdete na [webu](https://fileaction.apexpiksel.cz/?lang=cs#support).
- **Kartou nebo přes PayPal:** [ko-fi.com/metamorfolog](https://ko-fi.com/metamorfolog)

## Licence

[MIT](LICENSE). FileAction používá další open-source komponenty pod jejich vlastními licencemi, viz [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Rozdíly oproti Tangerine

- Klávesy Shift / Ctrl / Alt místo Shift / Command / Control.
- Formáty Apple (Pages, Keynote, RTFD) a zápis do HEIC nejsou podporované. HEIC jde jen otevřít.
- PDF → DOCX/EPUB/HTML převádí text, ne přesné rozvržení stránky (to nabízí PDF → PPTX nebo obrázky).
- Akce „AirDrop“ odpovídá ve Windows nabídce **Sdílet** (Sdílení nablízko, Teams, Mail…).
