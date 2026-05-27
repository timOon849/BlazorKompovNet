using BlazorKompovNet.Models;

namespace BlazorKompovNet.Services;

public sealed class LocalClubManagementService : IClubManagementService
{
    private const decimal MaxHourlyDuration = 24m;

    private readonly ICashierRepository cashierRepository;
    private readonly object syncRoot = new();
    private readonly Club club = new() { Id = 1, Name = "KompovNet", Address = "ул. Ленина, 15" };
    private readonly List<ComputerStatus> computerStatuses;
    private readonly List<PaymentType> paymentTypes;
    private readonly List<ComputerZone> zones;
    private readonly List<Client> clients;
    private readonly List<Tariff> tariffs;
    private readonly List<Booking> bookings = [];
    private readonly List<Transaction> transactions = [];
    private readonly List<GameSession> sessions = [];
    private readonly List<SessionExtension> sessionExtensions = [];
    private readonly List<CashierShift> cashierShifts = [];
    private int nextClientId = 4;
    private int nextBookingId = 2;
    private int nextShiftId = 3;
    private int nextTransactionId = 1;
    private int nextSessionId = 1;
    private int nextSessionExtensionId = 1;

    public LocalClubManagementService(ICashierRepository cashierRepository)
    {
        this.cashierRepository = cashierRepository;

        computerStatuses =
        [
            new() { Id = 1, Code = ComputerStatusCodes.Available, Name = "Свободен", CanStartSession = true },
            new() { Id = 2, Code = ComputerStatusCodes.Busy, Name = "Занят", CanStartSession = false },
            new() { Id = 3, Code = ComputerStatusCodes.Reserved, Name = "Бронь", CanStartSession = false },
            new() { Id = 4, Code = ComputerStatusCodes.Maintenance, Name = "Ремонт", CanStartSession = false },
            new() { Id = 5, Code = ComputerStatusCodes.Disabled, Name = "Выключен", CanStartSession = false }
        ];

        paymentTypes =
        [
            new() { Id = 1, Code = "Cash", Name = "Наличные" },
            new() { Id = 2, Code = "Card", Name = "Банковская карта" },
            new() { Id = 3, Code = "Sbp", Name = "СБП" },
            new() { Id = 4, Code = "Bonus", Name = "Бонусное пополнение", IsBonus = true },
            new() { Id = 5, Code = "Balance", Name = "Баланс гостя", IsActive = false }
        ];

        zones =
        [
            new()
            {
                Id = 1,
                ClubId = club.Id,
                Club = club,
                Name = "Стандарт",
                Description = "Основной зал для обычных игровых сессий",
                Computers =
                [
                    CreateComputer(1, "S-01", 1, ComputerStatusCodes.Available, "RTX 3060", 16),
                    CreateComputer(2, "S-02", 1, ComputerStatusCodes.Available, "RTX 3060", 16),
                    CreateComputer(3, "S-03", 1, ComputerStatusCodes.Available, "RTX 3060", 16),
                    CreateComputer(4, "S-04", 1, ComputerStatusCodes.Reserved, "RTX 3060", 16)
                ]
            },
            new()
            {
                Id = 2,
                ClubId = club.Id,
                Club = club,
                Name = "VIP",
                Description = "Мощные станции и повышенный комфорт",
                Computers =
                [
                    CreateComputer(5, "V-01", 2, ComputerStatusCodes.Available, "RTX 4070", 32),
                    CreateComputer(6, "V-02", 2, ComputerStatusCodes.Available, "RTX 4070", 32),
                    CreateComputer(7, "V-03", 2, ComputerStatusCodes.Maintenance, "RTX 4070", 32)
                ]
            },
            new()
            {
                Id = 3,
                ClubId = club.Id,
                Club = club,
                Name = "Bootcamp",
                Description = "Командная зона для турниров и тренировок",
                Computers =
                [
                    CreateComputer(8, "B-01", 3, ComputerStatusCodes.Available, "RTX 4080", 32),
                    CreateComputer(9, "B-02", 3, ComputerStatusCodes.Available, "RTX 4080", 32),
                    CreateComputer(10, "B-03", 3, ComputerStatusCodes.Available, "RTX 4080", 32)
                ]
            }
        ];

        foreach (var zone in zones)
        {
            foreach (var computer in zone.Computers)
            {
                computer.Zone = zone;
            }
        }

        clients =
        [
            new() { Id = 1, FirstName = "Иван", LastName = "Петров", PhoneNumber = "+7 900 111-22-33", Balance = 1200 },
            new() { Id = 2, FirstName = "Анна", LastName = "Смирнова", PhoneNumber = "+7 900 222-33-44", Balance = 800 },
            new() { Id = 3, FirstName = "Максим", LastName = "Козлов", PhoneNumber = "+7 900 333-44-55", Balance = 2400 }
        ];

        var reservedComputer = FindComputer(4);
        var bookingClient = clients[1];
        if (reservedComputer is not null)
        {
            var booking = new Booking
            {
                Id = 1,
                ClubId = club.Id,
                Club = club,
                ClientId = bookingClient.Id,
                Client = bookingClient,
                ComputerId = reservedComputer.Id,
                Computer = reservedComputer,
                ZoneId = reservedComputer.ZoneId,
                Zone = reservedComputer.Zone,
                StartsAt = DateTime.Today.AddHours(10),
                EndsAt = DateTime.Today.AddHours(14),
                Status = DateTime.Now >= DateTime.Today.AddHours(10) && DateTime.Now < DateTime.Today.AddHours(14)
                    ? BookingStatus.Active
                    : BookingStatus.Created
            };

            if (DateTime.Now < booking.EndsAt)
            {
                bookings.Add(booking);
                bookingClient.Bookings.Add(booking);
            }
            else
            {
                SetComputerStatus(reservedComputer, ComputerStatusCodes.Available);
            }
        }

        tariffs =
        [
            CreateTariff(1, "Почасовой", "Оплата за выбранное количество часов", TimeSpan.FromHours(1), true, [250, 450, 600]),
            CreateTariff(2, "Пакет 3 часа", "Фиксированная сессия на 3 часа", TimeSpan.FromHours(3), false, [650, 1200, 1600]),
            CreateTariff(3, "Ночной", "Ночной пакет на 8 часов", TimeSpan.FromHours(8), false, [1200, 2200, 3000]),
            CreateTariff(4, "30 минут", "Короткое продление или старт на полчаса", TimeSpan.FromMinutes(30), false, [150, 250, 350])
        ];

        SeedClosedShifts();
        RefreshBookingStatuses();
    }

    public Task<IReadOnlyList<ComputerZone>> GetZonesAsync()
    {
        lock (syncRoot)
        {
            RefreshBookingStatuses();
            return Task.FromResult<IReadOnlyList<ComputerZone>>(zones);
        }
    }

    public Task<IReadOnlyList<Tariff>> GetTariffsAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<Tariff>>(tariffs.Where(tariff => tariff.IsActive).ToList());
        }
    }

    public Task<IReadOnlyList<Client>> GetClientsAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<Client>>(clients
                .OrderBy(client => client.LastName)
                .ThenBy(client => client.FirstName)
                .ToList());
        }
    }

    public Task<Client?> GetClientAsync(int clientId)
    {
        lock (syncRoot)
        {
            return Task.FromResult(clients.FirstOrDefault(client => client.Id == clientId));
        }
    }

    public Task<IReadOnlyList<Client>> SearchClientsAsync(string? searchText)
    {
        lock (syncRoot)
        {
            var normalizedSearch = searchText?.Trim();
            var result = string.IsNullOrWhiteSpace(normalizedSearch)
                ? clients
                : clients.Where(client =>
                    client.FirstName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    client.LastName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    (client.PhoneNumber?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (client.Email?.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            return Task.FromResult<IReadOnlyList<Client>>(result);
        }
    }

    public Task<IReadOnlyList<GameSession>> GetActiveSessionsAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<GameSession>>(sessions.Where(session => session.Status == GameSessionStatus.Active).ToList());
        }
    }

    public Task<IReadOnlyList<Booking>> GetActiveBookingsAsync()
    {
        lock (syncRoot)
        {
            RefreshBookingStatuses();
            var now = DateTime.Now;
            return Task.FromResult<IReadOnlyList<Booking>>(bookings
                .Where(booking =>
                    (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
                    booking.EndsAt > now)
                .OrderBy(booking => booking.StartsAt)
                .ToList());
        }
    }

    public Task<IReadOnlyList<Booking>> GetBookingsAsync()
    {
        lock (syncRoot)
        {
            RefreshBookingStatuses();
            return Task.FromResult<IReadOnlyList<Booking>>(bookings
                .OrderByDescending(booking => booking.StartsAt)
                .ToList());
        }
    }

    public Task<Booking?> GetBookingAsync(int bookingId)
    {
        lock (syncRoot)
        {
            RefreshBookingStatuses();
            return Task.FromResult(bookings.FirstOrDefault(booking => booking.Id == bookingId));
        }
    }

    public Task<IReadOnlyList<GameSession>> GetSessionsAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<GameSession>>(sessions
                .OrderByDescending(session => session.StartedAt)
                .ToList());
        }
    }

    public Task<GameSession?> GetSessionAsync(int sessionId)
    {
        lock (syncRoot)
        {
            var session = sessions.FirstOrDefault(session => session.Id == sessionId);
            if (session is not null)
            {
                AttachSessionExtensions(session);
                AttachSessionTransactions(session);
            }

            return Task.FromResult(session);
        }
    }

    public Task<IReadOnlyList<SessionExtension>> GetSessionExtensionsAsync(int sessionId)
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<SessionExtension>>(sessionExtensions
                .Where(extension => extension.GameSessionId == sessionId)
                .OrderBy(extension => extension.CreatedAt)
                .ToList());
        }
    }

    public Task<IReadOnlyList<Transaction>> GetTransactionsAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<Transaction>>(transactions
                .OrderByDescending(transaction => transaction.CreatedAt)
                .ToList());
        }
    }

    public Task<Transaction?> GetTransactionAsync(int transactionId)
    {
        lock (syncRoot)
        {
            return Task.FromResult(transactions.FirstOrDefault(transaction => transaction.Id == transactionId));
        }
    }

    public Task<IReadOnlyList<CashierShift>> GetCashierShiftsAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<CashierShift>>(cashierShifts
                .OrderByDescending(shift => shift.OpenedAt)
                .ToList());
        }
    }

    public Task<CashierShift?> GetCurrentShiftAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult(GetOpenShift());
        }
    }

    public Task<IReadOnlyList<PaymentType>> GetPaymentTypesAsync()
    {
        lock (syncRoot)
        {
            return Task.FromResult<IReadOnlyList<PaymentType>>(paymentTypes.Where(paymentType => paymentType.IsActive).ToList());
        }
    }

    public Task<ClubOperationResult> RegisterClientAsync(string firstName, string lastName, string? phoneNumber, string? email)
    {
        lock (syncRoot)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return Task.FromResult(ClubOperationResult.Failure("Укажите имя и фамилию гостя."));
            }

            var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            if (normalizedPhone is not null &&
                clients.Any(client => string.Equals(client.PhoneNumber, normalizedPhone, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(ClubOperationResult.Failure("Клиент с таким телефоном уже зарегистрирован."));
            }

            clients.Add(new Client
            {
                Id = nextClientId++,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                PhoneNumber = normalizedPhone,
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                RegisteredAt = DateTime.UtcNow
            });

            return Task.FromResult(ClubOperationResult.Success("Гость зарегистрирован."));
        }
    }

    public async Task<ClubOperationResult> OpenShiftAsync(int cashierId, decimal openingCashAmount)
    {
        var cashier = await cashierRepository.GetByIdAsync(cashierId);
        if (cashier is null)
        {
            return ClubOperationResult.Failure("Кассир не найден.");
        }

        lock (syncRoot)
        {
            if (GetOpenShift() is not null)
            {
                return ClubOperationResult.Failure("Кассовая смена уже открыта.");
            }

            if (openingCashAmount < 0)
            {
                return ClubOperationResult.Failure("Сумма в кассе не может быть отрицательной.");
            }
            var shift = new CashierShift
            {
                Id = nextShiftId++,
                ClubId = club.Id,
                Club = club,
                CashierId = cashier.Id,
                Cashier = cashier,
                OpenedAt = DateTime.Now,
                OpeningCashAmount = openingCashAmount,
                CurrentCashAmount = openingCashAmount
            };

            cashierShifts.Add(shift);

            return ClubOperationResult.Success($"Смена #{shift.Id} открыта.");
        }
    }

    public Task<ClubOperationResult> CloseShiftAsync(int shiftId, decimal closingCashAmount)
    {
        lock (syncRoot)
        {
            var shift = cashierShifts.FirstOrDefault(shift => shift.Id == shiftId);
            if (shift is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Кассовая смена не найдена."));
            }

            if (!shift.IsOpen)
            {
                return Task.FromResult(ClubOperationResult.Failure("Эта смена уже закрыта."));
            }

            if (sessions.Any(session => session.Status == GameSessionStatus.Active && session.CashierShiftId == shift.Id))
            {
                return Task.FromResult(ClubOperationResult.Failure("Завершите все активные сессии текущей смены перед закрытием."));
            }

            if (closingCashAmount < 0)
            {
                return Task.FromResult(ClubOperationResult.Failure("Сумма в кассе не может быть отрицательной."));
            }

            shift.CurrentCashAmount = closingCashAmount;
            shift.ClosedAt = DateTime.Now;

            return Task.FromResult(ClubOperationResult.Success($"Смена #{shift.Id} закрыта."));
        }
    }

    public Task<ClubOperationResult> TopUpClientBalanceAsync(int clientId, decimal amount, int paymentTypeId)
    {
        lock (syncRoot)
        {
            var currentShift = GetOpenShift();
            if (currentShift is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции."));
            }

            var client = clients.FirstOrDefault(client => client.Id == clientId);
            var paymentType = paymentTypes.FirstOrDefault(paymentType => paymentType.Id == paymentTypeId && paymentType.IsActive);
            if (client is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Клиент не найден."));
            }

            if (paymentType is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Способ оплаты не найден или отключен."));
            }

            if (amount <= 0)
            {
                return Task.FromResult(ClubOperationResult.Failure("Укажите положительную сумму пополнения."));
            }

            client.Balance += amount;
            AddTransaction(client, paymentType, amount, paymentType.IsBonus ? TransactionType.BonusAccrual : TransactionType.BalanceTopUp, currentShift);

            return Task.FromResult(ClubOperationResult.Success("Баланс посетителя пополнен."));
        }
    }

    public Task<ClubOperationResult> CreateBookingAsync(int clientId, int computerId, DateTime startsAt, DateTime endsAt)
    {
        lock (syncRoot)
        {
            RefreshBookingStatuses();

            var currentShift = GetOpenShift();
            if (currentShift is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции."));
            }

            var client = clients.FirstOrDefault(client => client.Id == clientId);
            if (client is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Клиент не найден."));
            }

            var computer = FindComputer(computerId);
            if (computer is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Компьютер не найден."));
            }

            if (computer.Status?.Code is ComputerStatusCodes.Maintenance or ComputerStatusCodes.Disabled or ComputerStatusCodes.Busy)
            {
                return Task.FromResult(ClubOperationResult.Failure("Нельзя создать бронь на занятом, выключенном или ремонтируемом компьютере."));
            }

            if (endsAt <= startsAt)
            {
                return Task.FromResult(ClubOperationResult.Failure("Окончание брони должно быть позже начала."));
            }

            if (endsAt <= DateTime.Now)
            {
                return Task.FromResult(ClubOperationResult.Failure("Нельзя создать бронь в прошлом."));
            }

            if (sessions.Any(session => session.Status == GameSessionStatus.Active && session.ComputerId == computer.Id))
            {
                return Task.FromResult(ClubOperationResult.Failure("На выбранном компьютере уже идет активная сессия."));
            }

            if (HasBookingConflict(computer.Id, startsAt, endsAt))
            {
                return Task.FromResult(ClubOperationResult.Failure("На выбранное время компьютер уже забронирован."));
            }

            var booking = new Booking
            {
                Id = nextBookingId++,
                ClubId = club.Id,
                Club = club,
                ClientId = client.Id,
                Client = client,
                ComputerId = computer.Id,
                Computer = computer,
                ZoneId = computer.ZoneId,
                Zone = computer.Zone,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Status = startsAt <= DateTime.Now ? BookingStatus.Active : BookingStatus.Created,
                CreatedByCashierId = currentShift.CashierId,
                CreatedByCashier = currentShift.Cashier
            };

            bookings.Add(booking);
            client.Bookings.Add(booking);

            if (booking.Status == BookingStatus.Active && computer.Status?.Code == ComputerStatusCodes.Available)
            {
                SetComputerStatus(computer, ComputerStatusCodes.Reserved);
            }

            return Task.FromResult(ClubOperationResult.Success("Бронь создана."));
        }
    }

    public Task<ClubOperationResult> CancelBookingAsync(int bookingId)
    {
        lock (syncRoot)
        {
            return Task.FromResult(ChangeBookingStatus(bookingId, BookingStatus.Cancelled, "Бронь отменена."));
        }
    }

    public Task<ClubOperationResult> MarkBookingNoShowAsync(int bookingId)
    {
        lock (syncRoot)
        {
            return Task.FromResult(ChangeBookingStatus(bookingId, BookingStatus.NoShow, "Бронь отмечена как неявка."));
        }
    }

    public Task<ClubOperationResult> StartSessionAsync(int computerId, int clientId, int tariffId, decimal hours)
    {
        lock (syncRoot)
        {
            var currentShift = GetOpenShift();
            if (currentShift is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции."));
            }

            var computer = FindComputer(computerId);
            var client = clients.FirstOrDefault(client => client.Id == clientId);
            var tariff = tariffs.FirstOrDefault(tariff => tariff.Id == tariffId && tariff.IsActive);

            if (computer is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Компьютер не найден."));
            }

            if (client is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Клиент не найден."));
            }

            if (tariff is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Тариф не найден или отключен."));
            }

            var startValidation = ValidateSessionStart(computer, clientId);
            if (startValidation is not null)
            {
                return Task.FromResult(startValidation);
            }

            if (sessions.Any(session => session.Status == GameSessionStatus.Active && session.ClientId == client.Id))
            {
                return Task.FromResult(ClubOperationResult.Failure("Этот посетитель уже находится за другим компьютером."));
            }

            var tariffZone = FindTariffZone(tariff.Id, computer.ZoneId);
            if (tariffZone is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Для выбранной зоны не указана цена тарифа."));
            }

            var billedHours = tariff.IsHourly ? Math.Clamp(hours, 0.5m, MaxHourlyDuration) : 0m;
            var duration = tariff.IsHourly
                ? TimeSpan.FromHours((double)billedHours)
                : tariff.Duration;
            var totalPrice = tariff.IsHourly
                ? tariffZone.Price * billedHours
                : tariffZone.Price;

            if (client.Balance < totalPrice)
            {
                return Task.FromResult(ClubOperationResult.Failure("Недостаточно средств на балансе посетителя."));
            }

            var session = new GameSession
            {
                Id = nextSessionId++,
                ClubId = club.Id,
                Club = club,
                ComputerId = computer.Id,
                Computer = computer,
                ClientId = client.Id,
                Client = client,
                CashierShiftId = currentShift.Id,
                CashierShift = currentShift,
                TariffId = tariff.Id,
                Tariff = tariff,
                TariffZoneId = tariffZone.Id,
                TariffZone = tariffZone,
                StartedAt = DateTime.Now,
                PlannedEndAt = DateTime.Now.Add(duration),
                InitialPrice = totalPrice,
                TotalPrice = totalPrice
            };

            sessions.Add(session);

            SetComputerStatus(computer, ComputerStatusCodes.Busy);
            client.Balance -= totalPrice;
            AddBalanceTransaction(client, totalPrice, TransactionType.SessionStart, session, currentShift);
            CompleteActiveBooking(computer.Id, client.Id);

            return Task.FromResult(ClubOperationResult.Success($"Сессия на {computer.Name} запущена."));
        }
    }

    public Task<ClubOperationResult> ExtendSessionByTariffAsync(int sessionId, int tariffId, decimal hours)
    {
        lock (syncRoot)
        {
            var currentShift = GetOpenShift();
            if (currentShift is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции."));
            }

            var session = sessions.FirstOrDefault(session => session.Id == sessionId && session.Status == GameSessionStatus.Active);
            var tariff = tariffs.FirstOrDefault(tariff => tariff.Id == tariffId && tariff.IsActive);

            if (session is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Активный сеанс не найден."));
            }

            if (tariff is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Тариф не найден или отключен."));
            }

            var client = clients.FirstOrDefault(client => client.Id == session.ClientId);
            if (client is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Клиент сеанса не найден."));
            }

            var tariffZone = FindTariffZone(tariff.Id, session.Computer?.ZoneId ?? 0);
            if (tariffZone is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Для зоны компьютера не указана цена тарифа."));
            }

            var billedHours = tariff.IsHourly ? Math.Clamp(hours, 0.5m, MaxHourlyDuration) : 0m;
            var duration = tariff.IsHourly
                ? TimeSpan.FromHours((double)billedHours)
                : tariff.Duration;
            var price = tariff.IsHourly
                ? tariffZone.Price * billedHours
                : tariffZone.Price;

            if (client.Balance < price)
            {
                return Task.FromResult(ClubOperationResult.Failure("Недостаточно средств на балансе посетителя."));
            }

            client.Balance -= price;
            session.PlannedEndAt = session.PlannedEndAt.Add(duration);
            session.TotalPrice += price;
            AddBalanceTransaction(client, price, TransactionType.SessionExtension, session, currentShift);
            RecordSessionExtension(session, tariff, (int)duration.TotalMinutes, price, $"Продление: {tariff.Name}");

            return Task.FromResult(ClubOperationResult.Success("Сеанс продлен по выбранному тарифу."));
        }
    }

    public Task<ClubOperationResult> AddSessionTimeAsync(int sessionId, decimal hours, string reason)
    {
        lock (syncRoot)
        {
            if (GetOpenShift() is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции."));
            }

            var session = sessions.FirstOrDefault(session => session.Id == sessionId && session.Status == GameSessionStatus.Active);
            if (session is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Активный сеанс не найден."));
            }

            if (hours <= 0)
            {
                return Task.FromResult(ClubOperationResult.Failure("Укажите положительное количество часов."));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Task.FromResult(ClubOperationResult.Failure("Укажите причину добавления времени."));
            }

            var duration = TimeSpan.FromHours((double)hours);
            session.PlannedEndAt = session.PlannedEndAt.Add(duration);
            RecordSessionExtension(session, null, (int)duration.TotalMinutes, 0, reason.Trim());

            return Task.FromResult(ClubOperationResult.Success("Время добавлено к сеансу."));
        }
    }

    public Task<ClubOperationResult> TurnOnComputerAsync(int computerId)
    {
        lock (syncRoot)
        {
            var computer = FindComputer(computerId);
            if (computer is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Компьютер не найден."));
            }

            if (HasActiveSessionOnComputer(computerId))
            {
                return Task.FromResult(ClubOperationResult.Failure("Сначала завершите активную сессию на этом компьютере."));
            }

            if (computer.Status?.Code != ComputerStatusCodes.Disabled)
            {
                return Task.FromResult(ClubOperationResult.Failure("Компьютер уже включен."));
            }

            SetComputerStatus(computer, ComputerStatusCodes.Available);
            return Task.FromResult(ClubOperationResult.Success("Компьютер включен."));
        }
    }

    public Task<ClubOperationResult> CompleteSessionAsync(int sessionId)
    {
        lock (syncRoot)
        {
            var session = sessions.FirstOrDefault(session => session.Id == sessionId && session.Status == GameSessionStatus.Active);
            if (session is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Активный сеанс не найден."));
            }

            session.Status = GameSessionStatus.Completed;
            session.EndedAt = DateTime.Now;

            var computer = FindComputer(session.ComputerId);
            if (computer is not null)
            {
                SetComputerStatus(computer, ComputerStatusCodes.Available);
            }

            FinalizeSessionBookings(session);

            return Task.FromResult(ClubOperationResult.Success("Сеанс завершен."));
        }
    }

    public Task<ClubOperationResult> TurnOffComputerAsync(int computerId)
    {
        lock (syncRoot)
        {
            var computer = FindComputer(computerId);
            if (computer is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Компьютер не найден."));
            }

            if (HasActiveSessionOnComputer(computerId))
            {
                return Task.FromResult(ClubOperationResult.Failure("Сначала завершите активную сессию на этом компьютере."));
            }

            SetComputerStatus(computer, ComputerStatusCodes.Disabled);
            return Task.FromResult(ClubOperationResult.Success("Компьютер выключен."));
        }
    }

    public Task<ClubOperationResult> RestartComputerAsync(int computerId)
    {
        lock (syncRoot)
        {
            var computer = FindComputer(computerId);
            if (computer is null)
            {
                return Task.FromResult(ClubOperationResult.Failure("Компьютер не найден."));
            }

            if (HasActiveSessionOnComputer(computerId))
            {
                return Task.FromResult(ClubOperationResult.Failure("Сначала завершите активную сессию на этом компьютере."));
            }

            if (computer.Status?.Code == ComputerStatusCodes.Disabled)
            {
                return Task.FromResult(ClubOperationResult.Failure("Сначала включите компьютер."));
            }

            if (computer.Status?.Code == ComputerStatusCodes.Busy)
            {
                return Task.FromResult(ClubOperationResult.Failure("Нельзя перезагрузить занятый компьютер."));
            }

            SetComputerStatus(computer, ComputerStatusCodes.Available);
            ReleaseComputerReservationIfNeeded(computerId);
            return Task.FromResult(ClubOperationResult.Success("Компьютер перезагружен."));
        }
    }

    private Computer? FindComputer(int computerId)
    {
        return zones.SelectMany(zone => zone.Computers).FirstOrDefault(computer => computer.Id == computerId);
    }

    private CashierShift? GetOpenShift()
    {
        return cashierShifts.FirstOrDefault(shift => shift.IsOpen);
    }

    private static Cashier CreateCashier(int cashierId)
    {
        return cashierId == 1
            ? new Cashier { Id = cashierId, FullName = "Главный кассир", UserName = "admin" }
            : new Cashier { Id = cashierId, FullName = $"Кассир #{cashierId}", UserName = $"cashier{cashierId}" };
    }

    private Computer CreateComputer(int id, string number, int zoneId, string statusCode, string graphicsCard, int ramGb)
    {
        var status = computerStatuses.First(computerStatus => computerStatus.Code == statusCode);

        return new Computer
        {
            Id = id,
            ClubId = club.Id,
            Club = club,
            Number = number,
            Name = $"PC {number}",
            ZoneId = zoneId,
            ComputerStatusId = status.Id,
            Status = status,
            Processor = "Intel Core i5",
            GraphicsCard = graphicsCard,
            RamGb = ramGb,
            Monitor = "27\" 144 Hz"
        };
    }

    private Tariff CreateTariff(int id, string name, string description, TimeSpan duration, bool isHourly, decimal[] zonePrices)
    {
        var tariff = new Tariff
        {
            Id = id,
            Name = name,
            Description = description,
            Duration = duration,
            IsHourly = isHourly
        };

        tariff.TariffZones = zonePrices.Select((price, index) => new TariffZone
        {
            Id = ((id - 1) * zonePrices.Length) + index + 1,
            TariffId = tariff.Id,
            Tariff = tariff,
            ZoneId = index + 1,
            Zone = zones.First(zone => zone.Id == index + 1),
            Price = price
        }).ToList();

        return tariff;
    }

    private TariffZone? FindTariffZone(int tariffId, int zoneId)
    {
        return tariffs.SelectMany(tariff => tariff.TariffZones)
            .FirstOrDefault(tariffZone => tariffZone.TariffId == tariffId && tariffZone.ZoneId == zoneId && tariffZone.IsActive);
    }

    private bool HasBookingConflict(int computerId, DateTime startsAt, DateTime endsAt, int? ignoredBookingId = null)
    {
        return bookings.Any(booking =>
            booking.Id != ignoredBookingId &&
            booking.ComputerId == computerId &&
            (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
            startsAt < booking.EndsAt &&
            endsAt > booking.StartsAt);
    }

    private ClubOperationResult ChangeBookingStatus(int bookingId, BookingStatus status, string successMessage)
    {
        RefreshBookingStatuses();

        var booking = bookings.FirstOrDefault(booking => booking.Id == bookingId);
        if (booking is null)
        {
            return ClubOperationResult.Failure("Бронь не найдена.");
        }

        if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled or BookingStatus.NoShow)
        {
            return ClubOperationResult.Failure("Эта бронь уже закрыта.");
        }

        if (status == BookingStatus.NoShow && booking.StartsAt > DateTime.Now)
        {
            return ClubOperationResult.Failure("Нельзя отметить неявку до начала брони.");
        }

        booking.Status = status;
        ReleaseComputerReservationIfNeeded(booking.ComputerId);

        return ClubOperationResult.Success(successMessage);
    }

    private void RefreshBookingStatuses()
    {
        var now = DateTime.Now;

        foreach (var booking in bookings.Where(booking => booking.Status == BookingStatus.Created && booking.StartsAt <= now && booking.EndsAt > now))
        {
            booking.Status = BookingStatus.Active;
            var computer = FindComputer(booking.ComputerId);
            if (computer?.Status?.Code == ComputerStatusCodes.Available)
            {
                SetComputerStatus(computer, ComputerStatusCodes.Reserved);
            }
        }

        foreach (var booking in bookings.Where(booking =>
                     (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
                     booking.EndsAt <= now))
        {
            booking.Status = BookingStatus.Completed;
            ReleaseComputerReservationIfNeeded(booking.ComputerId);
        }
    }

    private void CompleteActiveBooking(int computerId, int clientId)
    {
        var now = DateTime.Now;
        var booking = bookings.FirstOrDefault(booking =>
            booking.ComputerId == computerId &&
            booking.ClientId == clientId &&
            (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
            booking.StartsAt <= now &&
            booking.EndsAt > now);

        if (booking is not null)
        {
            booking.Status = BookingStatus.Completed;
            ReleaseComputerReservationIfNeeded(computerId);
        }
    }

    private void FinalizeSessionBookings(GameSession session)
    {
        var now = DateTime.Now;
        foreach (var booking in bookings.Where(booking =>
                     booking.ComputerId == session.ComputerId &&
                     booking.ClientId == session.ClientId &&
                     (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
                     booking.StartsAt <= now))
        {
            booking.Status = BookingStatus.Completed;
        }

        ReleaseComputerReservationIfNeeded(session.ComputerId);
    }

    private bool HasActiveSessionOnComputer(int computerId) =>
        sessions.Any(session => session.Status == GameSessionStatus.Active && session.ComputerId == computerId);

    private ClubOperationResult? ValidateSessionStart(Computer computer, int clientId)
    {
        if (HasActiveSessionOnComputer(computer.Id))
        {
            return ClubOperationResult.Failure("На выбранном компьютере уже идет активная сессия.");
        }

        if (computer.Status?.Code is ComputerStatusCodes.Maintenance or ComputerStatusCodes.Disabled)
        {
            return ClubOperationResult.Failure("На выбранном компьютере нельзя начать сессию.");
        }

        if (computer.Status?.Code == ComputerStatusCodes.Busy)
        {
            return ClubOperationResult.Failure("Компьютер занят другой сессией.");
        }

        if (computer.Status?.Code == ComputerStatusCodes.Reserved)
        {
            var now = DateTime.Now;
            var booking = bookings.FirstOrDefault(booking =>
                booking.ComputerId == computer.Id &&
                (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
                booking.StartsAt <= now &&
                booking.EndsAt > now);

            if (booking is null)
            {
                return ClubOperationResult.Failure("Компьютер в статусе брони, но активная бронь не найдена.");
            }

            if (booking.ClientId != clientId)
            {
                return ClubOperationResult.Failure("Этот компьютер забронирован другим посетителем.");
            }
        }
        else if (computer.Status?.CanStartSession != true)
        {
            return ClubOperationResult.Failure("На выбранном компьютере нельзя начать сессию.");
        }

        return null;
    }

    private void AttachSessionTransactions(GameSession session)
    {
        session.Transactions = transactions
            .Where(transaction => transaction.GameSessionId == session.Id)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ToList();
    }

    private void ReleaseComputerReservationIfNeeded(int computerId)
    {
        var computer = FindComputer(computerId);
        if (computer?.Status?.Code != ComputerStatusCodes.Reserved)
        {
            return;
        }

        var now = DateTime.Now;
        var hasActiveBooking = bookings.Any(booking =>
            booking.ComputerId == computerId &&
            (booking.Status is BookingStatus.Created or BookingStatus.Active) &&
            booking.EndsAt >= now);

        if (!hasActiveBooking)
        {
            SetComputerStatus(computer, ComputerStatusCodes.Available);
        }
    }

    private void AddBalanceTransaction(Client client, decimal amount, TransactionType type, GameSession? session, CashierShift currentShift)
    {
        var balancePaymentType = paymentTypes.First(paymentType => paymentType.Code == "Balance");
        AddTransaction(client, balancePaymentType, amount, type, currentShift, session);
    }

    private void AddTransaction(Client client, PaymentType paymentType, decimal amount, TransactionType type, CashierShift currentShift, GameSession? session = null)
    {
        var transaction = new Transaction
        {
            Id = nextTransactionId++,
            ClubId = club.Id,
            Club = club,
            CashierShiftId = currentShift.Id,
            CashierShift = currentShift,
            ClientId = client.Id,
            Client = client,
            GameSessionId = session?.Id,
            GameSession = session,
            PaymentTypeId = paymentType.Id,
            PaymentType = paymentType,
            Amount = amount,
            Type = type,
            Status = PaymentStatus.Paid,
            CreatedAt = DateTime.UtcNow
        };

        transactions.Add(transaction);
        session?.Transactions.Add(transaction);

        if (paymentType.Code == "Cash")
        {
            currentShift.CurrentCashAmount += amount;
        }
    }

    private void SetComputerStatus(Computer computer, string statusCode)
    {
        var status = computerStatuses.First(computerStatus => computerStatus.Code == statusCode);
        computer.ComputerStatusId = status.Id;
        computer.Status = status;
    }

    private void RecordSessionExtension(GameSession session, Tariff? tariff, int addedMinutes, decimal amount, string reason)
    {
        var extension = new SessionExtension
        {
            Id = nextSessionExtensionId++,
            GameSessionId = session.Id,
            GameSession = session,
            TariffId = tariff?.Id,
            Tariff = tariff,
            AddedMinutes = addedMinutes,
            Amount = amount,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        sessionExtensions.Add(extension);
        session.Extensions.Add(extension);
    }

    private void AttachSessionExtensions(GameSession session)
    {
        session.Extensions = sessionExtensions
            .Where(extension => extension.GameSessionId == session.Id)
            .OrderBy(extension => extension.CreatedAt)
            .ToList();
    }

    private void SeedClosedShifts()
    {
        var cashier = CreateCashier(1);
        var yesterday = DateTime.Today.AddDays(-1);

        var firstShift = new CashierShift
        {
            Id = nextShiftId++,
            ClubId = club.Id,
            Club = club,
            CashierId = cashier.Id,
            Cashier = cashier,
            OpenedAt = yesterday.AddHours(9),
            ClosedAt = yesterday.AddHours(17),
            OpeningCashAmount = 5000,
            CurrentCashAmount = 12800
        };

        var secondShift = new CashierShift
        {
            Id = nextShiftId++,
            ClubId = club.Id,
            Club = club,
            CashierId = cashier.Id,
            Cashier = cashier,
            OpenedAt = yesterday.AddHours(17),
            ClosedAt = yesterday.AddHours(23),
            OpeningCashAmount = 12800,
            CurrentCashAmount = 21400
        };

        cashierShifts.Add(firstShift);
        cashierShifts.Add(secondShift);

        var demoClient = clients[0];
        var cardPayment = paymentTypes.First(paymentType => paymentType.Code == "Card");
        demoClient.Balance += 800;
        AddTransaction(demoClient, cardPayment, 500, TransactionType.BalanceTopUp, firstShift);
        AddTransaction(demoClient, cardPayment, 300, TransactionType.BalanceTopUp, secondShift);

        SeedDemoCompletedSession(firstShift, demoClient);
    }

    private void SeedDemoCompletedSession(CashierShift shift, Client client)
    {
        var computer = FindComputer(1);
        var tariff = tariffs.First(t => t.Id == 1);
        var tariffZone = FindTariffZone(tariff.Id, 1);
        if (computer is null || tariffZone is null)
        {
            return;
        }

        var startedAt = shift.OpenedAt.AddHours(1);
        var session = new GameSession
        {
            Id = nextSessionId++,
            ClubId = club.Id,
            Club = club,
            ComputerId = computer.Id,
            Computer = computer,
            ClientId = client.Id,
            Client = client,
            CashierShiftId = shift.Id,
            CashierShift = shift,
            TariffId = tariff.Id,
            Tariff = tariff,
            TariffZoneId = tariffZone.Id,
            TariffZone = tariffZone,
            StartedAt = startedAt,
            PlannedEndAt = startedAt.AddHours(1),
            InitialPrice = tariffZone.Price,
            TotalPrice = tariffZone.Price + 150,
            Status = GameSessionStatus.Completed,
            EndedAt = startedAt.AddHours(1).AddMinutes(30)
        };

        sessions.Add(session);
        client.Balance -= session.TotalPrice;
        AddBalanceTransaction(client, tariffZone.Price, TransactionType.SessionStart, session, shift);
        AddBalanceTransaction(client, 150, TransactionType.SessionExtension, session, shift);
        RecordSessionExtension(session, tariffs.First(t => t.Id == 4), 30, 150, "Продление: 30 минут");
    }
}
