---
name: windows11-ui
description: Создание UI в стиле Windows 11 Fluent Design для WPF приложений
framework: WPF
---

# Windows 11 UI Design Skill

## Применение
Используй для создания UI компонентов в стиле Windows 11.

## Обязательные NuGet пакеты
```xml
<PackageReference Include="WPF-UI" Version="3.0.5" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
```

## Структура главного окна ContextGUI

### MainWindow.xaml
```xml
<ui:FluentWindow 
    x:Class="ContextGUI.Views.MainWindow"
    xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
    Title="ContextGUI - Windows 11 Context Menu Manager"
    Height="700" Width="1200"
    WindowStartupLocation="CenterScreen"
    ExtendsContentIntoTitleBar="True"
    WindowBackdropType="Mica">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Title Bar с админ-индикатором -->
        <ui:TitleBar Grid.Row="0" Title="ContextGUI">
            <ui:TitleBar.Header>
                <StackPanel Orientation="Horizontal" Margin="10,0">
                    <ui:SymbolIcon Symbol="Shield24" 
                                   Foreground="{DynamicResource SystemFillColorCriticalBrush}"
                                   Visibility="{Binding IsAdministrator, Converter={StaticResource BoolToVisibilityConverter}}"
                                   ToolTip="Запущено с правами администратора"/>
                </StackPanel>
            </ui:TitleBar.Header>
        </ui:TitleBar>

        <!-- Основной контент с 3-колоночной структурой -->
        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="300"/>
            </Grid.ColumnDefinitions>

            <!-- Левая панель навигации -->
            <ui:NavigationView Grid.Column="0" />

            <!-- Центральная панель со списком -->
            <ui:Card Grid.Column="1" Margin="10" />

            <!-- Правая панель с деталями -->
            <ui:Card Grid.Column="2" Margin="0,10,10,10" />
        </Grid>

        <!-- Status Bar -->
        <ui:InfoBar Grid.Row="2" 
                    IsOpen="{Binding ShowStatusMessage}"
                    Title="{Binding StatusTitle}"
                    Message="{Binding StatusMessage}"
                    Severity="{Binding StatusSeverity}"/>
    </Grid>
</ui:FluentWindow>
```

## Темы и стили

### App.xaml
```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ui:ThemesDictionary Theme="Dark"/>
            <ui:ControlsDictionary/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Design Principles для ContextGUI

1. **Mica материал** для фона главного окна
2. **Rounded corners** на всех Card элементах (CornerRadius="8")
3. **Acrylic blur** для модальных окон
4. **Иконки из Fluent System Icons** (WPF-UI включены)
5. **Анимации** при переключении между разделами
6. **Тёмная тема** по умолчанию с поддержкой светлой

## Цветовая схема

**Dark Theme (default):**
- Primary: `#0078D4` (Windows Accent Blue)
- Success: `#107C10` (Green)
- Warning: `#FFC83D` (Yellow)
- Danger: `#E74856` (Red)
- Background: `#202020`
- Surface: `#2C2C2C`
- Text: `#FFFFFF`

**Light Theme:**
- Background: `#F3F3F3`
- Surface: `#FFFFFF`
- Text: `#000000`

## Компоненты

**Кнопки:**
- Primary: Для основных действий (Применить, Сохранить)
- Secondary: Для второстепенных (Отмена, Закрыть)
- Danger: Для деструктивных (Удалить)

**Spacing:**
```xml
<!-- Между группами элементов -->
<StackPanel Spacing="16">

<!-- Внутри группы -->
<StackPanel Spacing="8">

<!-- Padding для Card -->
<ui:Card Padding="20">
```

## Адаптивность

```csharp
// Минимальные размеры окна
MinWidth = 900;
MinHeight = 600;

// При ширине < 1000px скрыть правую панель
```

## Accessibility

- Всегда устанавливай AutomationProperties.Name
- Keyboard navigation должна работать везде
- Используй ToolTip для иконок

## Иконки WPF-UI

Используй SymbolIcon:
- Файлы: `Document24`
- Папки: `Folder24`
- Удаление: `Delete24`
- Редактирование: `Edit24`
- Настройки: `Settings24`
- Backup: `Save24`
- Админ: `Shield24`
- Поиск: `Search24`
