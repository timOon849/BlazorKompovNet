# KompovNet — веб-приложение кассира

В solution один рабочий проект: **BlazorKompovNet**.

## Структура папок

```
BlazorKompovNet/
├── Components/          — интерфейс (страницы и меню)
│   ├── Layout/          — шапка, боковое меню
│   └── Pages/           — экраны: дашборд, клуб, клиенты, сессии…
├── Models/              — сущности: клиент, сессия, бронь, тариф…
├── Services/            — логика через HTTP API (KompovNetApi)
│   └── Api/             — клиент, маппинг, сервисы CRM
├── wwwroot/             — картинки, CSS
├── Program.cs           — запуск приложения, регистрация сервисов
└── appsettings.json     — настройки
```

## Как запустить

1. Запустить API: `C:\Users\super\source\repos\KompovNetApi` (`dotnet run`, порт `http://localhost:5232`).
2. В БД должны быть кассиры, клуб, зоны, ПК (через API или seed).
3. Открыть `BlazorKompovNet.sln`, запустить **BlazorKompovNet** (F5).
4. Войти под кассиром из API.

URL API задаётся в `appsettings.json`: `"Api": { "BaseUrl": "http://127.0.0.1:5232" }`.

Если API слушает `0.0.0.0:5232`, используйте **127.0.0.1**, а не `localhost` — на Windows `localhost` может идти по IPv6 и соединение обрывается.

## Данные

Все операции идут в **KompovNetApi** (PostgreSQL).

## Страницы

| Путь | Назначение |
|------|------------|
| `/` | Дашборд, открытие/закрытие смены |
| `/club-management` | Залы, ПК, сессии, пополнение |
| `/clients` | Список гостей |
| `/bookings` | Бронирования |
| `/sessions` | Игровые сессии |
| `/transactions` | Транзакции |
| `/tariffs` | Тарифы (просмотр) |
| `/cashier-shifts` | История смен |
