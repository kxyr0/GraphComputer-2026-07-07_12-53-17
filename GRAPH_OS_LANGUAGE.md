# Graph OS Internal Language

Документ описывает текущий DSL, который проверяет встроенный IDE build analyzer. Это не полноценный язык общего назначения: сейчас он работает как декларативное описание OS-модуля, зависимостей, бюджета, runtime-действий и экспортируемых метрик.

## JSON initial code

В JSON-описании ноды можно задать стартовый код, который откроется в IDE при первом открытии документа ноды:

```json
{
  "id": "firewall",
  "title": "Firewall",
  "mode": "strict",
  "initialCode": "module Firewall\n\nmode = strict\nscanTraffic = true\nallowUnknownPorts = false\n\nrequire Security as security\n\nbudget {\n    cpu <= 18\n    memory <= 24\n}\n\nboot {\n    load rules\n    allow trustedApps\n    block unknownPorts\n}\n\nexport {\n    protection = firewall.score\n}"
}
```

`initialCode` обычная JSON-строка, поэтому переносы строк пишутся как `\n`, а кавычки внутри кода экранируются как `\"`. Если поле пустое или отсутствует, IDE использует встроенный fallback-шаблон по id ноды.

`mode` в JSON задаёт режим fallback-шаблона, если `initialCode` не указан. Если `initialCode` указан, главным источником становится сам код, и режим нужно писать внутри него строкой `mode = strict`/`mode = balanced` и так далее.

## Module

Каждый документ должен начинаться с объявления модуля:

```text
module SystemCore
```

Build падает с ошибкой, если `module` отсутствует. Известные системные модули сейчас: `SystemCore`, `Kernel`, `Data`, `Security`, `Control`, `Time`. Остальные имена считаются utility/passive module и проверяются мягче.

## Module settings

Настройки модуля пишутся отдельными присваиваниями, обычно сразу после `module`:

```text
module Firewall

mode = strict
platform = PC
scanTraffic = true
allowUnknownPorts = false
```

Поддерживаемые режимы:

| Mode | Смысл |
| --- | --- |
| `balanced` | Базовый режим без сильного перекоса. Используется по умолчанию. |
| `strict` | Больше безопасности/надёжности, но выше риск false blocks и нагрузки. |
| `permissive` | Меньше блокировок и нагрузки, но больше уязвимостей/риска. |
| `aggressive` | Больше производительности или жёстких действий, но выше нестабильность/долг. |
| `safe` | Осторожный режим: меньше риск, но слабее скорость/эффект. |
| `performance` | Упор в скорость/throughput, с побочками по ресурсам. |

Дополнительные настройки сейчас используются Firewall-логикой:

| Setting | Значения | Эффект |
| --- | --- | --- |
| `platform` | `PC` | Платформа публикуемого OS build. `Supported Devices` в release info показывает `PC` только после успешного `SystemCore` build с `platform = PC`. |
| `scanTraffic` | `true`/`false` | Включает активное сканирование трафика: меньше угроз, больше network/load. |
| `allowUnknownPorts` | `true`/`false` | Разрешает неизвестные порты: меньше false blocks, больше vulnerabilities. |

## Dependencies

Зависимости описываются так:

```text
require Kernel as kernel
optional Security as security
```

`require` означает обязательную зависимость. `optional` означает, что модуль может работать без нее. Сейчас analyzer проверяет наличие деклараций и использует их как архитектурный сигнал; runtime-эффекты завязаны на успешный build через `NodeCodeRegistry`.

## Budget

Блок бюджета описывает аппаратные лимиты:

```text
budget {
    cpu <= 72
    memory <= 68
    storage <= 80
}
```

Для OS-модулей отсутствие `budget` дает предупреждение и технический долг. Внутри блоков разрешены обычные присваивания и сравнения: `=`, `<=`, `>=`.

## Blocks

Analyzer принимает блоки, если строка заканчивается на `{`, а закрытие идет через `}`:

```text
boot {
    start kernel.scheduler
}

tick every 1s {
    monitor kernel.load
}

export {
    load = kernel.load
}
```

Типовые блоки: `boot`, `tick every ...`, `scheduler`, `budget`, `export`. Имена блоков пока не являются строгой грамматикой, но известные шаблоны используют именно их.

## Conditions

Условие открывается `if` и закрывается `end`:

```text
if kernel.load > 80
    throttle data.index by 25
    alert "High Load"
end
```

Analyzer считает количество `if` и `end`. Если `end` не хватает, build получает ошибку. Если `end` лишний, build получает предупреждение.

## Known statements

Сейчас analyzer принимает такие команды:

```text
alert "Message"
warning "Message"
start service
mount fileSystem
load rules
scan packets
rule allowTrusted
allow trustedApps
block unknownPorts
protect userFiles
encrypt storage
backup userFiles
cache data.index
index directories
watch processes
monitor kernel.load
route input to devices
recover journal
schedule task
bind display
throttle scheduler by 20
flush tempFiles
reduce inputDelay by 10
enable service
```

Также analyzer принимает известные настройки модуля: `mode`, `platform`, `scanTraffic`, `allowUnknownPorts`. Неизвестная непустая строка дает build error. Комментарии начинаются с `//`.

## Exports and effects

`export` описывает значения, которые модуль отдает системе:

```text
export {
    stability = 100 - kernel.load * 0.25
    load = kernel.load
}
```

Analyzer не исполняет выражения как полноценный runtime. Он читает код как набор сигналов: `monitor`, `scan`, `protect`, `cache`, `throttle`, `backup`, `flush`, `reduce` и другие команды меняют build report effects: stability, system load, technical debt или специальные эффекты модуля.

## Runtime tradeoffs

Build влияет не только на состояние "успешно/неуспешно". `IdeBuildAnalyzer` извлекает `ModuleBuildSettings`, `NodeCodeRegistry` сохраняет их вместе с результатом build, а `OsRuntimeMetricsService` применяет режимы к runtime-метрикам.

Текущие сильные ноды работают как компромиссы:

| Node | Плюс | Побочка |
| --- | --- | --- |
| `firewall` | Меньше vulnerabilities, больше blocked threats/security. | `strict` и `scanTraffic` повышают false blocks, network/load; `permissive` снижает false blocks, но открывает угрозы. |
| `file_system` | Больше read/write speed и data safety. | `performance`/`aggressive` повышают fragmentation и storage/load; `safe` снижает fragmentation, но режет скорость. |
| `memory_manager` | Больше process limit и лучше response time. | `aggressive`/`performance` повышают leak risk/crash risk и могут снижать stability; предупреждения/technical debt тоже растят leak risk. |
| `display_manager` | Лучше usability/device support и меньше input delay. | `performance`/`aggressive` повышают GPU usage, load и memory pressure; `safe` экономит GPU, но слабее улучшает UX. |

## Time module

`Time` имеет отдельную проверку скорости:

```text
module Time

boot {
    start clock
}

speed 60x
```

Разрешенные формы:

```text
speed 60x
time = 60x
speed = 60x
speed: 60x
speed to 60x
gameSpeed = 0.5x
timeSpeed = 2x
set speed to 10x
run time at 30x
```

Скорость должна быть конечным числом и не может быть отрицательной.

## Build profiles

`SystemCore`, `Kernel`, `Data`, `Security`, `Control` проверяются как основные OS-модули. Для них analyzer предупреждает об отсутствии `require`, `budget`, `export`, conditional logic и alert path.

`Time` проверяется отдельно и принимает speed statements.

Все остальные модули считаются utility/passive module: generic assignments разрешены, но обязательные OS-блоки не требуются.
