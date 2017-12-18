### RPLauncher

Source: <https://github.com/0-d/RPLauncher>

#### Описание

Консольная утилита для запуска приложения на удалённой машине с перехватом кода возврата.
В отличие от PSExec использует встроенный в Windows инструментарий (WMI), т.е. не должно падать при попытке соединиться и при частых использованиях.

##### В проекте использованы компоненты:

- `ProcessWMI` class by **anandr_27**: <https://blogs.msdn.microsoft.com/padmanr/2010/05/08/execute-a-process-on-remote-machine-wait-for-it-to-exit-and-retrieve-its-exit-code-using-wmi/> (включён в состав проекта `ProcessWMI.cs`).
- `clipr` для парсинга аргументов: <https://github.com/nemec/clipr>
- `Costura.Fody` для упаковки `clipr.dll` внутрь исполняемого файла при билде: <https://github.com/Fody/Costura>

##### Прицип работы

Приложение запускается через WMI класс `win32_process`, метод `Create`. 
Отслеживание кода возврата реализовано путём создания `ManagementEventWatcher` на событие класса `Win32_ProcessStopTrace` по ID запущенного процесса.

Все команды запускаются с префиксом `cmd /c ` т.к. некоторые утилиты не создают своего `ProcessID`, а лишь выполняют команду на уже запущенном процессе/сервисе (пример - `appcmd.exe`, который не создаёт отдельного `ProcessID`, а лишь отдаёт команду сервису IIS).

----------

#### Build

```batch
Nuget restore RPLauncher.sln
MSBuild /t:clean;rebuild /p:configuration=release RPLauncher.sln
```

#### Usage

```batch
RPLauncher [ -h|--help ] [ --version ] -c|--command "<string>" [ -s|--server <string> ] [ -t|--timeout <int> ]
```

> **Note:**

> - Команду, содержащую пробелы необходимо заключать в **двойные** ковычки. Если в самой команде содержатся двойные кавычки, их нужно экранировать символом `\` (обратный слэш).
> - `timeout` in **seconds**
