# RateScout Miner — юзер-кейсы (живой список)

Статусы: ✅ проверено · 🟡 собрано, ждёт ручной приёмки · 🔴 нужна проверка на Windows.

## UC-1. Запуск майнинга — 🔴
**Предусловие:** Windows, пользователь скачал и запустил RateScout-Miner.exe, есть интернет.
**Шаги:** ввести пул + XMR-кошелёк + нагрузку CPU% → «Старт».
**Ожидаемо:** `XmrigManager.EnsureXmrigAsync` качает последний официальный XMRig (если ещё нет) → пишет config.json →
запускает xmrig процесс на этой машине; в логе — вывод XMRig; таймер обновляет хешрейт по HTTP-API `127.0.0.1:46081/2/summary`.
**РАДИУС:** `XmrigManager` (download/config/start/hashrate), `MainForm`. **Статус:** 🔴 сеть/процесс/AV — проверить на Windows.

## UC-2. Остановка / закрытие — 🔴
**Ожидаемо:** «Стоп» или закрытие окна → `XmrigManager.Stop()` убивает процесс XMRig и fee-цикл; хешрейт «—».
**РАДИУС:** `MainForm.StopMining`, `XmrigManager.Stop`.

## UC-3. Dev-fee 1% (прозрачно) — 🔴
**Ожидаемо:** при заданном `DevFeeXmr` (XMR-адрес автора) `FeeLoopAsync` 1% времени (36с из 3600) переключает майнинг
на пул/адрес автора, затем обратно; событие видно в логе. Плюс встроенный `donate-level:1` XMRig (разработчикам XMRig).
Если адрес пуст — dev-fee выключен (100% пользователю). **РАДИУС:** `XmrigManager.FeeLoopAsync/WriteConfig`.
**Согласие:** майнинг только по кнопке, только на этой машине; скрытых/фоновых режимов нет.

## UC-4. Сборка и релиз (CI) — 🟡
**Ожидаемо:** windows-раннер `dotnet publish` (self-contained single-file) → zip-артефакт; тег `v*` → релиз
`RateScout-Miner-Windows.zip`. **РАДИУС:** `.github/workflows/build.yml`, `RateScoutMiner.csproj`.

## UC-5. Лендинг miner.ratescout.ru — 🟡
**Предусловие:** DNS `miner` → `sementsul.github.io`.
**Ожидаемо:** `pages.yml` деплоит `site/` (стиль сайта, раскрытие комиссии/AV) на Pages; `site/CNAME`=miner.ratescout.ru;
кнопка «Скачать» → `releases/latest`. **РАДИУС:** `site/index.html`, `pages.yml`. **Статус:** 🟡 ждёт DNS.

## UC-6. Защита репозитория — 🟡
**Ожидаемо:** ветка `main` защищена (force-push/удаление off, enforce_admins) сразу после первого пуша.
