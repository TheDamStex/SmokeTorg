# SmokeTorg (WPF, .NET 8, MVVM)

Учётная система розничной торговли с модульной архитектурой:
- Presentation (Views/ViewModels)
- Application (Services/UseCases)
- Domain (Entities/Rules)
- Infrastructure (Json/Sql storage providers, repositories)
- Common (base classes, commands, logger)

## Запуск
```bash
dotnet build SmokeTorg.csproj
dotnet run --project SmokeTorg.csproj
```

## Роли и доступ
- `Cashier`: POS, продажи
- `Manager`: POS + справочники/закупки/склад/отчёты
- `Admin`: всё + пользователи и настройки

## Пользователь по умолчанию
- login: `admin`
- password: `admin123`

## Где лежат данные
При первом запуске автоматически создаётся папка `Data` в каталоге приложения:
- products.json
- categories.json
- customers.json
- suppliers.json
- users.json
- stock.json
- stock_movements.json
- sales.json
- purchases.json
- settings.json

## Переход на SQL
Слой хранения переключается через `IStorageProviderFactory`. Сейчас включён `JsonStorageProvider`, `SqlStorageProvider` и `Sql*Repository` оставлены как TODO-заглушки.
