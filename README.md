# RateScout Miner ⛏

GUI-обёртка над официальным [XMRig](https://github.com/xmrig/xmrig) для майнинга **Monero (XMR)**.
Дизайн/брендинг — как у [RateScout](https://ratescout.ru). Страница загрузки: **https://miner.ratescout.ru**

## Что делает
- Приложение **само скачивает официальный XMRig** (последний релиз с GitHub) — мы его не хостим.
- Указываете пул + XMR-кошелёк + нагрузку CPU, жмёте «Старт» → майнинг **на вашей машине**.
- Показывает хешрейт (через HTTP-API XMRig), лог, старт/стоп.
- Тёмная DOS-тема как на сайте.

## Честность / согласие
- Майнит **только по кнопке «Старт»** и только на этом ПК. Никакого скрытого/фонового запуска, автозапуска
  без ведома, обхода антивируса или «тихой» установки — это НЕ криптоджекинг.
- ⚠️ Антивирусы часто помечают XMRig как «riskware» (это самый частый инструмент криптоджекинга). Ожидаемо.
- Комиссия: **1% dev-fee** автору (переключение на XMR-адрес автора 1% времени) + обязательный **1% XMRig**.

## Сборка
    dotnet publish src/RateScoutMiner/RateScoutMiner.csproj -c Release -o out
CI (GitHub Actions, windows) собирает self-contained `.exe` и публикует релиз по тегу `v*`.

## Структура
```
src/RateScoutMiner/  C#/.NET WinForms: XmrigManager (скачка/конфиг/запуск/хешрейт/dev-fee) + MainForm (GUI)
site/                лендинг (miner.ratescout.ru) в стиле сайта
.github/workflows/   build.yml (сборка+релиз) · pages.yml (лендинг)
```
Лицензия: MIT. Майнер: XMRig (MIT).
