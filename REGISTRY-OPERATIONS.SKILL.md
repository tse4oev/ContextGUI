---
name: registry-operations
description: Безопасная работа с реестром Windows 11 для управления контекстным меню
requires_admin: true
---

# Registry Operations Skill

## Применение
Используй этот skill когда нужно:
- Читать/записывать значения реестра Windows
- Модифицировать контекстное меню Windows 11
- Создавать резервные копии ключей реестра

## Критические правила безопасности

### 1. ВСЕГДА проверяй права администратора
```csharp
using System.Security.Principal;

public static bool IsAdministrator()
{
    var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
```

### 2. ВСЕГДА создавай backup перед изменением
```csharp
public async Task<string> CreateRegistryBackup(string keyPath)
{
    var backupPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ContextGUI", "Backups", 
        $"{DateTime.Now:yyyyMMdd_HHmmss}_{SanitizePath(keyPath)}.reg"
    );

    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "reg",
            Arguments = $"export \"{keyPath}\" \"{backupPath}\" /y",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }
    };

    process.Start();
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
        throw new InvalidOperationException("Не удалось создать backup реестра");

    return backupPath;
}
```

### 3. Используй Result pattern для операций
```csharp
public class RegistryResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public string? BackupPath { get; init; }
}
```

## Windows 11 Context Menu Structure

### Современное контекстное меню (Win11 Native)
```
HKEY_CLASSES_ROOT\
├── *\shell\                           # Для всех файлов
├── Directory\shell\                   # Для папок
├── Directory\Background\shell\        # Для фона проводника
└── Drive\shell\                       # Для дисков
```

Каждый элемент имеет структуру:
```
[ключ_команды]\
├── (Default) = "Название в меню"
├── Icon = "путь_к_иконке.ico"
├── Extended = ""                      # Только через Shift
├── command\
    └── (Default) = "команда_для_выполнения"
```

### Legacy контекстное меню (Show more options)
```
HKEY_CLASSES_ROOT\
├── *\shellex\ContextMenuHandlers\
├── AllFilesystemObjects\shellex\ContextMenuHandlers\
├── Folder\shellex\ContextMenuHandlers\
└── Directory\Background\shellex\ContextMenuHandlers\
```

## Типовые операции

### Отключение элемента меню (не удаление)
```csharp
public async Task<RegistryResult<bool>> DisableContextMenuItem(string keyPath)
{
    if (!IsAdministrator())
        return new RegistryResult<bool> 
        { 
            Success = false, 
            Error = "Требуются права администратора" 
        };

    var backupPath = await CreateRegistryBackup(keyPath);

    try
    {
        using var key = Registry.ClassesRoot.OpenSubKey(keyPath, writable: true);
        if (key == null)
            return new RegistryResult<bool> 
            { 
                Success = false, 
                Error = $"Ключ не найден: {keyPath}" 
            };

        // Создаём значение LegacyDisable для отключения
        key.SetValue("LegacyDisable", "", RegistryValueKind.String);

        _logger.LogInformation("Отключен элемент меню: {KeyPath}", keyPath);

        return new RegistryResult<bool>
        {
            Success = true,
            Value = true,
            BackupPath = backupPath
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ошибка отключения элемента меню: {KeyPath}", keyPath);
        return new RegistryResult<bool>
        {
            Success = false,
            Error = ex.Message,
            BackupPath = backupPath
        };
    }
}
```

### Чтение всех элементов контекстного меню
```csharp
public List<ContextMenuItem> GetAllContextMenuItems()
{
    var items = new List<ContextMenuItem>();

    var basePaths = new[]
    {
        @"*\shell",
        @"Directory\shell",
        @"Directory\Background\shell",
        @"Drive\shell",
        @"*\shellex\ContextMenuHandlers",
        @"Folder\shellex\ContextMenuHandlers"
    };

    foreach (var basePath in basePaths)
    {
        try
        {
            using var baseKey = Registry.ClassesRoot.OpenSubKey(basePath);
            if (baseKey == null) continue;

            foreach (var subKeyName in baseKey.GetSubKeyNames())
            {
                using var subKey = baseKey.OpenSubKey(subKeyName);
                if (subKey == null) continue;

                var item = new ContextMenuItem
                {
                    Name = subKeyName,
                    DisplayName = subKey.GetValue("")?.ToString() ?? subKeyName,
                    RegistryPath = $@"HKEY_CLASSES_ROOT\{basePath}\{subKeyName}",
                    IsEnabled = subKey.GetValue("LegacyDisable") == null,
                    IconPath = subKey.GetValue("Icon")?.ToString(),
                    IsSystemItem = IsSystemItem(subKeyName),
                    Category = GetCategoryFromPath(basePath)
                };

                // Получаем команду
                using var commandKey = subKey.OpenSubKey("command");
                item.Command = commandKey?.GetValue("")?.ToString();

                items.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось прочитать {BasePath}", basePath);
        }
    }

    return items;
}
```

## Commands для этого skill

При генерации кода всегда:
1. Проверяй права администратора в начале метода
2. Создавай backup перед изменением
3. Используй `using` для RegistryKey
4. Логируй все операции
5. Возвращай Result<T> с информацией о backup
6. Обрабатывай SecurityException, UnauthorizedAccessException, IOException

## Тестирование

НИКОГДА не работай с реальным реестром в тестах! Используй:
```csharp
// Mock для тестов
public interface IRegistryWrapper
{
    IRegistryKey OpenSubKey(string name, bool writable = false);
    string[] GetSubKeyNames();
    object? GetValue(string name);
    void SetValue(string name, object value);
}
```

## Критические ключи (НЕ трогать без предупреждения)

- `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon`
- `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control`
- `HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID` (системные CLSID)
