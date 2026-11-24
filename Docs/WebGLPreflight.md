# WebGL: проверка перед сборкой

Проверено по файлам проекта и локальному профилю Web от 24 ноября 2026.
При проверке настроек Unity не запускалась. Позже выполнены редакторские проверки
вертикального интерфейса (см. `Docs/PortraitLayout.md`). WebGL-сборка и проверка её
ответов на хостинге не выполнялись.

## Настройки проекта

| Настройка | Значение | Причина |
| --- | --- | --- |
| Compression Format | Gzip | Для первого теста через панель хостинга; быстрее сжимается, чем Brotli |
| Decompression Fallback | On | Распаковка через загрузчик, если сервер не настроен для нативной распаковки |
| Name Files As Hashes | On | Изменённые файлы сборки получают новые адреса при обновлении |
| Data Caching | On | Повторные открытия могут использовать кеш сборки |
| Initial Memory Size | 128 MB | Начальная настройка для первого замера вместо 32 MB; не измеренный расход приложения |
| Maximum Memory Size | 2048 MB | Существующий верхний предел; не резервирует сразу 2 GB и не гарантирует доступную память |
| Memory Growth | Geometric, 0.2, cap 96 MB | Существующий рост кучи сохранён |
| Threads | Off | Не требует cross-origin isolation и COOP/COEP для многопоточного WASM |
| Default Canvas | 540 × 960 | Совпадает с вертикальным HealthCloud template; CSS управляет фактическим размером |
| WebGL Template | HealthCloud | Авторизация, гость, сохранения и аналитика |
| Debug Symbols / Diagnostics | Off | Существующие настройки для пользовательского теста |
| Run In Background | Off | Существующая настройка; таймер упражнения также учитывает потерю фокуса |

До правок были Brotli без Decompression Fallback, canvas 960 × 600, начальная куча 32 MB
и выключенные имена файлов по хешу.

Локальный Web Build Profile в Library: Development Build, Script Debugging, Autoconnect
Profiler и Deep Profiling выключены. Это настройки компьютера, а не переносимые файлы
репозитория: перед нажатием Build проверить их снова. Сжатие Publishing Settings
применяется к release build, а не Development Build.

Включена одна сцена — `Assets/Scenes/SampleScene.unity`. Крупные фоны Day/Night используют
автоматический формат текстур, сжатие Normal и max size 2048; Read/Write и mipmaps
выключены. Локальный Texture Compression subtarget — Generic. Глобально менять это на
ASTC без проверки десктопных и мобильных браузеров не стали: сжатие текстур влияет
на совместимость GPU, Gzip — на передачу файлов, это разные настройки.
Выбор качества WebGL — High с тем же URP 2D asset, что у остальных уровней качества;
одна смена пресета не является доказанной оптимизацией. Нужен замер на телефоне.

## Apache и загрузка через панель

1. После сборки положить `Deployment/WebGL/.htaccess` в корень готовой сборки,
   рядом с `index.html`. При загрузке через панель включить отображение скрытых файлов.
   Если в каталоге на хостинге уже есть `.htaccess`, объединить настройки с ним.
2. Этот файл рассчитан на **Gzip + Fallback**, а не Brotli. Он задаёт `Content-Encoding: gzip`
   для `.unityweb`, типы WASM/JavaScript и переопределяет кеширование страницы и модулей.
   Все пути относительные; размещение в `/HealthPlusPlus/` поддерживается шаблоном.
3. Если хостинг запрещает директивы .htaccess и возвращает HTTP 500, убрать добавленный файл
   и проверить ограничения в панели: Decompression Fallback оставляет запасной путь загрузки.
   `.mjs` всё равно должен отдаваться как JavaScript, а не HTML/текст.
4. После загрузки в DevTools → Network проверить ответы: `index.html`, `cloud.js`,
   `analytics.mjs`, `guest-progress.mjs`, `progress-store.mjs`, `portrait-viewport.mjs` и файлы `Build/` должны
   возвращать 200. Для нативной распаковки `.unityweb` нужен `Content-Encoding: gzip`;
   у WASM — `Content-Type: application/wasm`. Не должно быть HTML-ответов по этим адресам.
5. Проверить запуск, гостевую тренировку, вход Google, сохранение/перезагрузку и DebugView
   на телефоне и десктопе. Начальную память скорректировать по измерению, а не по размеру ZIP.

## Осталось до размещения

- `MainMenuTransitionView` и VideoPlayer в сцене всё ещё ссылаются на встроенные VideoClip.
  Для Web нужны URL-источники видео; текущий таймаут лишь возвращает статический фон.
- URL упражнений пока ведут на `/HealthPlusPlus/*.mp4`. При переносе роликов в `Videos/`
  нужно обновить URL и загрузить ролики по новым адресам одновременно с приложением.
- В Firebase Authentication → Authorized domains должен быть `verlema.life` без пути.
  Публикация Firestore Rules подтверждена пользователем, реальная авторизация ещё не проверена.

Источники: [Unity deployment and compression](https://docs.unity3d.com/6000.1/Documentation/Manual/webgl-deploying.html),
[Apache configuration](https://docs.unity3d.com/6000.1/Documentation/Manual/webgl-server-configuration-code-samples.html),
[Web memory](https://docs.unity3d.com/6000.1/Documentation/Manual/webgl-memory.html),
[Web video](https://docs.unity3d.com/6000.1/Documentation/Manual/webgl-video.html).
