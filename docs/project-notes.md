# RateScout Miner — заметки проекта (MAP)

Карта репо. Спека — `docs/spec/ratescout-miner.usecases.md`, история — git.

## Что это
GUI (C#/.NET WinForms, Windows) поверх официального **XMRig** для майнинга **Monero (XMR)**, брендинг как у
[RateScout](https://ratescout.ru) (DOS-тема). Репо `sementsul/ratescout-miner` (main защищён). Лендинг **miner.ratescout.ru**.

## Где что
```
src/RateScoutMiner/
  XmrigManager.cs   скачивает офиц. XMRig (windows-x64 zip), пишет config.json, старт/стоп, хешрейт (HTTP-API), dev-fee 1%
  Settings.cs       автосохранение пул/кошелёк/воркер/CPU% в %LocalAppData%\RateScoutMiner\settings.json
  MainForm.cs       GUI (stacked: подписи над полями), DOS-тема
  app.manifest      asInvoker (без админа), без скрытых режимов
site/               лендинг + 404 (стиль сайта) + Я.Метрика 111586112 + GA G-PPN27D6JXS
.github/workflows/  build.yml (self-contained exe + релиз по тегу) · pages.yml
```

## Ключевое / грабли
- 🧭 **Согласие/дуал-юз:** майнинг только по кнопке «Старт», только на этой машине. НЕ добавлять скрытый/фоновый
  запуск, автозапуск, обход AV, тихую установку — это криптоджекинг.
- 🔴 **AV/SmartScreen флагают** сам xmrig.exe как riskware (принято юзером). Наш exe — только качалка+GUI.
- **Dev-fee 1%** на XMR-адрес автора (переключение 1% времени) + обязательный `donate-level:1` XMRig; раскрыто в GUI/лендинге.
- 🔴 **Имена ассетов XMRig** менялись: сейчас `xmrig-*-windows-x64.zip` / `-windows-gcc-x64.zip` (не `msvc-win64`!).
  Фильтр: `windows`+`x64`+`.zip`, без `arm`, предпочтение MSVC. Если перестанет качать — проверить имена в релизе XMRig.
- Аналитика: те же счётчики, что на ratescout.ru (единый кабинет; в Метрике включить приём с поддоменов).

## Состояние
- ✅ Релиз **v1.1.0**: фикс скачивания (windows-x64), автосохранение настроек, раскладка (подписи над полями).
- ✅ Домен miner.ratescout.ru live (DNS, HTTPS); лендинг+404+метрики.
- 🔴 На человека: тест на Windows (скачивание/майнинг/хешрейт); подпись сертификатом при желании; отзыв PAT.
