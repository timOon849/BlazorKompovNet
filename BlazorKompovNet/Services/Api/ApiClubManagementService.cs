using BlazorKompovNet.Models;
using BlazorKompovNet.Services.Api;

namespace BlazorKompovNet.Services;

public sealed class ApiClubManagementService(KompovApiClient api, ICashierRepository cashierRepository) : IClubManagementService
{
    private const decimal MinHourlyDuration = 0.5m;

    private int? clubId;
    private Club? club;
    private List<ApiComputerStatus> computerStatuses = [];
    private List<ApiPaymentType> paymentTypes = [];

    public async Task<IReadOnlyList<ComputerZone>> GetZonesAsync()
    {
        await EnsureContextAsync();
        var apiZones = await api.GetListAsync<ApiComputerZone>(api.Admin("zones"));
        var apiComputers = await api.GetListAsync<ApiComputer>(api.Admin("computers"));

        return apiZones
            .Where(zone => zone.ClubId == clubId)
            .Select(zone =>
            {
                var mapped = ApiMapper.ToZone(zone, club);
                mapped.Computers = apiComputers
                    .Where(computer => computer.ZoneId == zone.Id && computer.ClubId == clubId)
                    .Select(computer => ApiMapper.ToComputer(computer, mapped, computerStatuses))
                    .ToList();
                return mapped;
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Tariff>> GetTariffsAsync()
    {
        var tariffs = await GetApiTariffsWithZonesAsync();
        return tariffs.Where(tariff => tariff.IsActive).Select(ApiMapper.ToTariff).ToList();
    }

    public async Task<IReadOnlyList<Client>> GetClientsAsync()
    {
        var clients = await api.GetListAsync<ApiClient>(api.Mobile("clients"));
        return clients.Where(client => client.IsActive).Select(ApiMapper.ToClient).ToList();
    }

    public async Task<Client?> GetClientAsync(int clientId)
    {
        var client = await api.GetAsync<ApiClient>(api.Mobile($"clients/{clientId}"));
        return client is null ? null : ApiMapper.ToClient(client);
    }

    public async Task<IReadOnlyList<Client>> SearchClientsAsync(string? searchText)
    {
        var clients = await GetClientsAsync();
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return clients;
        }

        var query = searchText.Trim();
        return clients
            .Where(client =>
                client.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                client.LastName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (client.PhoneNumber?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (client.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }

    public async Task<IReadOnlyList<GameSession>> GetActiveSessionsAsync()
    {
        var sessions = await GetSessionsAsync();
        return sessions.Where(session => session.Status == GameSessionStatus.Active).ToList();
    }

    public async Task<IReadOnlyList<Booking>> GetActiveBookingsAsync()
    {
        var bookings = await GetBookingsAsync();
        return bookings
            .Where(booking => booking.Status is BookingStatus.Created or BookingStatus.Active)
            .ToList();
    }

    public async Task<IReadOnlyList<Booking>> GetBookingsAsync()
    {
        await EnsureContextAsync();
        var bookings = await api.GetListAsync<ApiBooking>(api.Crm("bookings"));
        var mapped = bookings
            .Where(booking => booking.ClubId == clubId)
            .Select(booking => ApiMapper.ToBooking(booking, club))
            .ToList();
        await EnrichBookingsAsync(mapped);
        return mapped;
    }

    public async Task<Booking?> GetBookingAsync(int bookingId)
    {
        var booking = await api.GetAsync<ApiBooking>(api.Crm($"bookings/{bookingId}"));
        await EnsureContextAsync();
        if (booking is null)
        {
            return null;
        }

        var mapped = ApiMapper.ToBooking(booking, club);
        await EnrichBookingsAsync([mapped]);
        return mapped;
    }

    public async Task<IReadOnlyList<GameSession>> GetSessionsAsync()
    {
        await EnsureContextAsync();
        var sessions = await api.GetListAsync<ApiGameSession>(api.Crm("sessions"));
        var mapped = sessions
            .Where(session => session.ClubId == clubId)
            .Select(session => ApiMapper.ToSession(session, club))
            .ToList();
        await EnrichSessionsAsync(mapped);
        return mapped;
    }

    public async Task<GameSession?> GetSessionAsync(int sessionId)
    {
        var session = await api.GetAsync<ApiGameSession>(api.Crm($"sessions/{sessionId}"));
        await EnsureContextAsync();
        if (session is null)
        {
            return null;
        }

        var mapped = ApiMapper.ToSession(session, club);
        await EnrichSessionDetailsAsync(mapped);
        return mapped;
    }

    public async Task<IReadOnlyList<SessionExtension>> GetSessionExtensionsAsync(int sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        return session?.Extensions ?? [];
    }

    public async Task<IReadOnlyList<Transaction>> GetTransactionsAsync()
    {
        await EnsureContextAsync();
        var transactions = await api.GetListAsync<ApiTransaction>(api.Admin("transactions"));
        var mapped = transactions
            .Where(transaction => transaction.ClubId == clubId)
            .Select(transaction => ApiMapper.ToTransaction(transaction, club))
            .ToList();

        await EnrichTransactionsAsync(mapped);
        return mapped;
    }

    public async Task<Transaction?> GetTransactionAsync(int transactionId)
    {
        var transaction = await api.GetAsync<ApiTransaction>(api.Admin($"transactions/{transactionId}"));
        if (transaction is null)
        {
            return null;
        }

        await EnsureContextAsync();
        var mapped = ApiMapper.ToTransaction(transaction, club);
        await EnrichTransactionsAsync([mapped]);
        return mapped;
    }

    public async Task<IReadOnlyList<CashierShift>> GetCashierShiftsAsync()
    {
        await EnsureContextAsync();
        var shifts = await api.GetListAsync<ApiCashierShift>(api.Admin("cashier-shifts"));
        return shifts
            .Where(shift => shift.ClubId == clubId)
            .OrderByDescending(shift => shift.OpenedAt)
            .Select(shift => ApiMapper.ToShift(shift, club))
            .ToList();
    }

    public async Task<CashierShift?> GetCurrentShiftAsync()
    {
        var shifts = await GetCashierShiftsAsync();
        return shifts.FirstOrDefault(shift => shift.IsOpen);
    }

    public async Task<IReadOnlyList<PaymentType>> GetPaymentTypesAsync()
    {
        await EnsureContextAsync();
        return paymentTypes.Where(type => type.IsActive).Select(ApiMapper.ToPaymentType).ToList();
    }

    public async Task<ClubOperationResult> RegisterClientAsync(
        string firstName,
        string lastName,
        string? phoneNumber,
        string? email,
        string? login = null,
        string? password = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return ClubOperationResult.Failure("Укажите имя и фамилию гостя.");
            }

            var existing = await GetClientsAsync();
            var normalizedPhone = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            var normalizedLogin = string.IsNullOrWhiteSpace(login) ? null : login.Trim();
            var normalizedPassword = string.IsNullOrWhiteSpace(password) ? null : password;

            if (normalizedPhone is not null &&
                existing.Any(client => string.Equals(client.PhoneNumber, normalizedPhone, StringComparison.OrdinalIgnoreCase)))
            {
                return ClubOperationResult.Failure("Клиент с таким телефоном уже зарегистрирован.");
            }

            if (normalizedEmail is not null &&
                existing.Any(client => string.Equals(client.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return ClubOperationResult.Failure("Клиент с таким email уже зарегистрирован.");
            }

            if (normalizedLogin is not null &&
                existing.Any(client => string.Equals(client.Login, normalizedLogin, StringComparison.OrdinalIgnoreCase)))
            {
                return ClubOperationResult.Failure("Клиент с таким логином уже зарегистрирован.");
            }

            if (normalizedLogin is not null && string.IsNullOrWhiteSpace(normalizedPassword))
            {
                return ClubOperationResult.Failure("Укажите пароль для входа в мобильное приложение.");
            }

            var model = new ApiClient
            {
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                PhoneNumber = normalizedPhone,
                Email = normalizedEmail,
                Login = normalizedLogin,
                Password = normalizedPassword,
                RegisteredAt = DateTime.UtcNow,
                Balance = 0,
                IsActive = true
            };

            await api.PostAsync(api.Mobile("clients"), model);
            return ClubOperationResult.Success("Гость зарегистрирован.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> OpenShiftAsync(int cashierId, decimal openingCashAmount)
    {
        try
        {
            await EnsureContextAsync();
            var cashier = await cashierRepository.GetByIdAsync(cashierId);
            if (cashier is null)
            {
                return ClubOperationResult.Failure("Кассир не найден.");
            }

            if (await GetCurrentShiftAsync() is not null)
            {
                return ClubOperationResult.Failure("Кассовая смена уже открыта.");
            }

            if (openingCashAmount < 0)
            {
                return ClubOperationResult.Failure("Сумма в кассе не может быть отрицательной.");
            }

            var shift = new ApiCashierShift
            {
                ClubId = clubId!.Value,
                CashierId = cashierId,
                OpenedAt = DateTime.UtcNow,
                OpeningCashAmount = openingCashAmount,
                CurrentCashAmount = openingCashAmount
            };

            var created = await api.PostAsync<ApiCashierShift>(api.Admin("cashier-shifts"), shift);
            return ClubOperationResult.Success($"Смена #{created?.Id ?? 0} открыта.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> CloseShiftAsync(int shiftId, decimal closingCashAmount)
    {
        try
        {
            if (closingCashAmount < 0)
            {
                return ClubOperationResult.Failure("Сумма в кассе не может быть отрицательной.");
            }

            var shift = await api.GetAsync<ApiCashierShift>(api.Admin($"cashier-shifts/{shiftId}"));
            if (shift is null)
            {
                return ClubOperationResult.Failure("Кассовая смена не найдена.");
            }

            if (shift.ClosedAt is not null)
            {
                return ClubOperationResult.Failure("Эта смена уже закрыта.");
            }

            shift.CurrentCashAmount = closingCashAmount;
            shift.ClosedAt = DateTime.UtcNow;
            await api.PutAsync(api.Admin($"cashier-shifts/{shiftId}"), shift);

            var activeCount = (await GetActiveSessionsAsync()).Count;
            var message = activeCount > 0
                ? $"Смена #{shiftId} закрыта. Активных сессий в клубе: {activeCount}. Новые операции (пополнения, старт сессий и т.д.) недоступны до открытия смены."
                : $"Смена #{shiftId} закрыта.";

            return ClubOperationResult.Success(message);
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> TopUpClientBalanceAsync(int clientId, decimal amount, int paymentTypeId)
    {
        try
        {
            var currentShift = await GetCurrentShiftAsync();
            if (currentShift is null)
            {
                return ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции.");
            }

            await EnsureContextAsync();
            var client = await GetClientAsync(clientId);
            var paymentType = paymentTypes.FirstOrDefault(type => type.Id == paymentTypeId && type.IsActive);
            if (client is null)
            {
                return ClubOperationResult.Failure("Клиент не найден.");
            }

            if (paymentType is null)
            {
                return ClubOperationResult.Failure("Способ оплаты не найден или отключен.");
            }

            if (amount <= 0)
            {
                return ClubOperationResult.Failure("Укажите положительную сумму пополнения.");
            }

            client.Balance += amount;
            await api.PutAsync(api.Mobile($"clients/{clientId}"), ApiMapper.ToApiClient(client));

            var transactionType = paymentType.IsBonus
                ? TransactionType.BonusAccrual
                : TransactionType.BalanceTopUp;

            await api.PostAsync(api.Admin("transactions"), new ApiTransaction
            {
                ClubId = clubId!.Value,
                CashierShiftId = currentShift.Id,
                ClientId = clientId,
                PaymentTypeId = paymentTypeId,
                Amount = amount,
                Type = Enum.Parse<ApiTransactionType>(transactionType),
                Status = ApiPaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            });

            return ClubOperationResult.Success("Баланс посетителя пополнен.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> CreateBookingAsync(int clientId, int computerId, DateTime startsAt, DateTime endsAt)
    {
        try
        {
            await EnsureContextAsync();
            var currentShift = await GetCurrentShiftAsync();
            if (currentShift is null)
            {
                return ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции.");
            }

            if (await GetClientAsync(clientId) is null)
            {
                return ClubOperationResult.Failure("Клиент не найден.");
            }

            var zones = await GetZonesAsync();
            var computer = zones.SelectMany(zone => zone.Computers).FirstOrDefault(item => item.Id == computerId);
            if (computer is null)
            {
                return ClubOperationResult.Failure("Компьютер не найден.");
            }

            if (computer.Status?.Code is ComputerStatusCodes.Maintenance or ComputerStatusCodes.Disabled or ComputerStatusCodes.Busy)
            {
                return ClubOperationResult.Failure("Нельзя создать бронь на занятом, выключенном или ремонтируемом компьютере.");
            }

            if (endsAt <= startsAt)
            {
                return ClubOperationResult.Failure("Окончание брони должно быть позже начала.");
            }

            var startsUtc = ClubTimeZone.ToUtc(startsAt);
            var endsUtc = ClubTimeZone.ToUtc(endsAt);

            if (endsUtc <= DateTime.UtcNow)
            {
                return ClubOperationResult.Failure("Нельзя создать бронь в прошлом.");
            }

            var sessions = await GetActiveSessionsAsync();
            if (sessions.Any(session => session.ComputerId == computerId))
            {
                return ClubOperationResult.Failure("На выбранном компьютере уже идет активная сессия.");
            }

            var bookings = await GetBookingsAsync();
            if (HasBookingConflict(bookings, computerId, startsUtc, endsUtc))
            {
                return ClubOperationResult.Failure("На выбранное время компьютер уже забронирован.");
            }

            var status = startsUtc <= DateTime.UtcNow ? BookingStatus.Active : BookingStatus.Created;
            var booking = new ApiBooking
            {
                ClubId = clubId!.Value,
                ClientId = clientId,
                ComputerId = computerId,
                ZoneId = computer.ZoneId,
                StartsAt = startsUtc,
                EndsAt = endsUtc,
                Status = Enum.Parse<ApiBookingStatus>(status),
                CreatedByCashierId = currentShift.CashierId
            };

            await api.PostAsync(api.Crm("bookings"), booking);

            if (status == BookingStatus.Active && computer.Status?.Code == ComputerStatusCodes.Available)
            {
                await SetComputerStatusAsync(computerId, ComputerStatusCodes.Reserved);
            }

            return ClubOperationResult.Success("Бронь создана.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public Task<ClubOperationResult> CancelBookingAsync(int bookingId) =>
        ChangeBookingStatusAsync(bookingId, BookingStatus.Cancelled, "Бронь отменена.");

    public Task<ClubOperationResult> MarkBookingNoShowAsync(int bookingId) =>
        ChangeBookingStatusAsync(bookingId, BookingStatus.NoShow, "Бронь отмечена как неявка.");

    public async Task<ClubOperationResult> StartSessionAsync(
        int computerId,
        int tariffId,
        decimal hours,
        int? clientId,
        int? paymentTypeId)
    {
        try
        {
            await EnsureContextAsync();
            await EnsureBalancePaymentTypeAsync();
            var currentShift = await GetCurrentShiftAsync();
            if (currentShift is null)
            {
                return ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции.");
            }

            var zones = await GetZonesAsync();
            var computer = zones.SelectMany(zone => zone.Computers).FirstOrDefault(item => item.Id == computerId);
            var tariffs = await GetTariffsAsync();
            var tariff = tariffs.FirstOrDefault(item => item.Id == tariffId && item.IsActive);

            if (computer is null)
            {
                return ClubOperationResult.Failure("Компьютер не найден.");
            }

            if (tariff is null)
            {
                return ClubOperationResult.Failure("Тариф не найден или отключен.");
            }

            var isGuestSession = clientId is null;
            Client? client = null;
            if (clientId is int accountClientId)
            {
                client = await GetClientAsync(accountClientId);
                if (client is null)
                {
                    return ClubOperationResult.Failure("Клиент не найден.");
                }
            }

            var validation = ValidateSessionStart(
                computer,
                clientId,
                await GetActiveSessionsAsync(),
                await GetBookingsAsync());
            if (validation is not null)
            {
                return validation;
            }

            if (clientId is int activeClientId
                && (await GetActiveSessionsAsync()).Any(session => session.ClientId == activeClientId))
            {
                return ClubOperationResult.Failure("Этот посетитель уже находится за другим компьютером.");
            }

            var tariffZone = tariff.TariffZones.FirstOrDefault(zone => zone.ZoneId == computer.ZoneId && zone.IsActive);
            if (tariffZone is null)
            {
                return ClubOperationResult.Failure("Для выбранной зоны не указана цена тарифа.");
            }

            var billedHours = tariff.IsHourly ? Math.Max(hours, MinHourlyDuration) : 0m;
            var duration = tariff.IsHourly
                ? TimeSpan.FromHours((double)billedHours)
                : tariff.Duration;
            var totalPrice = tariff.IsHourly
                ? tariffZone.Price * billedHours
                : tariffZone.Price;

            int transactionPaymentTypeId;
            if (isGuestSession)
            {
                if (paymentTypeId is null)
                {
                    return ClubOperationResult.Failure("Выберите способ оплаты (наличные или карта).");
                }

                var guestPaymentType = paymentTypes.FirstOrDefault(type => type.Id == paymentTypeId.Value && type.IsActive);
                if (guestPaymentType is null
                    || guestPaymentType.IsBonus
                    || guestPaymentType.Code is PaymentTypeCodes.Balance or PaymentTypeCodes.Bonus)
                {
                    return ClubOperationResult.Failure("Для гостя без аккаунта доступны только наличные или карта.");
                }

                transactionPaymentTypeId = guestPaymentType.Id;
            }
            else
            {
                if (client!.Balance < totalPrice)
                {
                    return ClubOperationResult.Failure("Недостаточно средств на балансе посетителя.");
                }

                var balancePaymentType = paymentTypes.FirstOrDefault(type => type.Code == PaymentTypeCodes.Balance);
                if (balancePaymentType is null)
                {
                    return ClubOperationResult.Failure(
                        "В системе не настроен способ оплаты «С баланса». Обновите API (миграция/сид).");
                }

                transactionPaymentTypeId = balancePaymentType.Id;
            }

            var session = new ApiGameSession
            {
                ClubId = clubId!.Value,
                ComputerId = computerId,
                ClientId = clientId,
                CashierShiftId = currentShift.Id,
                TariffId = tariffId,
                TariffZoneId = tariffZone.Id,
                StartedAt = DateTime.UtcNow,
                PlannedEndAt = DateTime.UtcNow.Add(duration),
                InitialPrice = totalPrice,
                TotalPrice = totalPrice,
                Status = ApiGameSessionStatus.Active
            };

            var created = await api.PostAsync<ApiGameSession>(api.Crm("sessions"), session);

            if (clientId is int balanceClientId)
            {
                client!.Balance -= totalPrice;
                await api.PutAsync(api.Mobile($"clients/{balanceClientId}"), ApiMapper.ToApiClient(client));
            }

            await api.PostAsync(api.Admin("transactions"), new ApiTransaction
            {
                ClubId = clubId.Value,
                CashierShiftId = currentShift.Id,
                ClientId = clientId,
                GameSessionId = created?.Id,
                PaymentTypeId = transactionPaymentTypeId,
                Amount = totalPrice,
                Type = ApiTransactionType.SessionStart,
                Status = ApiPaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            });

            await SetComputerStatusAsync(computerId, ComputerStatusCodes.Busy);
            if (clientId is int bookingClientId)
            {
                await CompleteActiveBookingAsync(computerId, bookingClientId);
            }

            return ClubOperationResult.Success($"Сессия на {computer.Name} запущена.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> ExtendSessionByTariffAsync(int sessionId, int tariffId, decimal hours)
    {
        try
        {
            var currentShift = await GetCurrentShiftAsync();
            if (currentShift is null)
            {
                return ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции.");
            }

            var session = await GetSessionAsync(sessionId);
            var tariffs = await GetTariffsAsync();
            var tariff = tariffs.FirstOrDefault(item => item.Id == tariffId && item.IsActive);
            if (session is null || session.Status != GameSessionStatus.Active)
            {
                return ClubOperationResult.Failure("Активный сеанс не найден.");
            }

            if (tariff is null)
            {
                return ClubOperationResult.Failure("Тариф не найден или отключен.");
            }

            if (session.ClientId is not int sessionClientId)
            {
                return ClubOperationResult.Failure("Продление по тарифу доступно только для сессий с аккаунтом клиента.");
            }

            var client = await GetClientAsync(sessionClientId);
            if (client is null)
            {
                return ClubOperationResult.Failure("Клиент сеанса не найден.");
            }

            var zones = await GetZonesAsync();
            var computer = zones.SelectMany(zone => zone.Computers).FirstOrDefault(item => item.Id == session.ComputerId);
            var tariffZone = tariff.TariffZones.FirstOrDefault(zone => zone.ZoneId == (computer?.ZoneId ?? 0) && zone.IsActive);
            if (tariffZone is null)
            {
                return ClubOperationResult.Failure("Для зоны компьютера не указана цена тарифа.");
            }

            var billedHours = tariff.IsHourly ? Math.Max(hours, MinHourlyDuration) : 0m;
            var duration = tariff.IsHourly
                ? TimeSpan.FromHours((double)billedHours)
                : tariff.Duration;
            var price = tariff.IsHourly
                ? tariffZone.Price * billedHours
                : tariffZone.Price;

            if (client.Balance < price)
            {
                return ClubOperationResult.Failure("Недостаточно средств на балансе посетителя.");
            }

            client.Balance -= price;
            session.PlannedEndAt = session.PlannedEndAt.Add(duration);
            session.TotalPrice += price;

            await api.PutAsync(api.Mobile($"clients/{client.Id}"), ApiMapper.ToApiClient(client));
            await api.PutAsync(api.Crm($"sessions/{sessionId}"), ApiMapper.ToApiSession(session));

            await EnsureContextAsync();
            var balancePaymentType = paymentTypes.FirstOrDefault(type => type.Code == PaymentTypeCodes.Balance);
            if (balancePaymentType is null)
            {
                return ClubOperationResult.Failure(
                    "В системе не настроен способ оплаты «С баланса». Обновите API (миграция/сид).");
            }

            await api.PostAsync(api.Admin("transactions"), new ApiTransaction
            {
                ClubId = clubId!.Value,
                CashierShiftId = currentShift.Id,
                ClientId = client.Id,
                GameSessionId = sessionId,
                PaymentTypeId = balancePaymentType.Id,
                Amount = price,
                Type = ApiTransactionType.SessionExtension,
                Status = ApiPaymentStatus.Paid,
                CreatedAt = DateTime.UtcNow
            });

            return ClubOperationResult.Success("Сеанс продлен по выбранному тарифу.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> AddSessionTimeAsync(int sessionId, decimal hours, string reason)
    {
        try
        {
            if (await GetCurrentShiftAsync() is null)
            {
                return ClubOperationResult.Failure("Откройте кассовую смену перед выполнением операции.");
            }

            var session = await GetSessionAsync(sessionId);
            if (session is null || session.Status != GameSessionStatus.Active)
            {
                return ClubOperationResult.Failure("Активный сеанс не найден.");
            }

            if (hours <= 0)
            {
                return ClubOperationResult.Failure("Укажите положительное количество часов.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return ClubOperationResult.Failure("Укажите причину добавления времени.");
            }

            session.PlannedEndAt = session.PlannedEndAt.Add(TimeSpan.FromHours((double)hours));
            await api.PutAsync(api.Crm($"sessions/{sessionId}"), ApiMapper.ToApiSession(session));
            return ClubOperationResult.Success("Время добавлено к сеансу.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> CompleteSessionAsync(int sessionId)
    {
        try
        {
            var session = await GetSessionAsync(sessionId);
            if (session is null || session.Status != GameSessionStatus.Active)
            {
                return ClubOperationResult.Failure("Активный сеанс не найден.");
            }

            session.Status = GameSessionStatus.Completed;
            session.EndedAt = DateTime.UtcNow;
            await api.PutAsync(api.Crm($"sessions/{sessionId}"), ApiMapper.ToApiSession(session));
            await SetComputerStatusAsync(session.ComputerId, ComputerStatusCodes.Available);
            return ClubOperationResult.Success("Сеанс завершен.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<IReadOnlyList<int>> CompleteExpiredSessionsAsync()
    {
        var activeSessions = await GetActiveSessionsAsync();
        var completedIds = new List<int>();

        foreach (var session in activeSessions.Where(GameSessionTiming.ShouldAutoComplete))
        {
            var result = await CompleteSessionAsync(session.Id);
            if (result.Succeeded)
            {
                completedIds.Add(session.Id);
            }
        }

        return completedIds;
    }

    public async Task<ClubOperationResult> TurnOnComputerAsync(int computerId)
    {
        try
        {
            var computer = await GetComputerAsync(computerId);
            if (computer is null)
            {
                return ClubOperationResult.Failure("Компьютер не найден.");
            }

            if ((await GetActiveSessionsAsync()).Any(session => session.ComputerId == computerId))
            {
                return ClubOperationResult.Failure("Сначала завершите активную сессию на этом компьютере.");
            }

            if (computer.Status?.Code != ComputerStatusCodes.Disabled)
            {
                return ClubOperationResult.Failure("Компьютер уже включен.");
            }

            await SetComputerStatusAsync(computerId, ComputerStatusCodes.Available);
            return ClubOperationResult.Success("Компьютер включен.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> TurnOffComputerAsync(int computerId)
    {
        try
        {
            if ((await GetActiveSessionsAsync()).Any(session => session.ComputerId == computerId))
            {
                return ClubOperationResult.Failure("Сначала завершите активную сессию на этом компьютере.");
            }

            if (await GetComputerAsync(computerId) is null)
            {
                return ClubOperationResult.Failure("Компьютер не найден.");
            }

            await SetComputerStatusAsync(computerId, ComputerStatusCodes.Disabled);
            return ClubOperationResult.Success("Компьютер выключен.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    public async Task<ClubOperationResult> RestartComputerAsync(int computerId)
    {
        try
        {
            var computer = await GetComputerAsync(computerId);
            if (computer is null)
            {
                return ClubOperationResult.Failure("Компьютер не найден.");
            }

            if ((await GetActiveSessionsAsync()).Any(session => session.ComputerId == computerId))
            {
                return ClubOperationResult.Failure("Сначала завершите активную сессию на этом компьютере.");
            }

            if (computer.Status?.Code == ComputerStatusCodes.Disabled)
            {
                return ClubOperationResult.Failure("Сначала включите компьютер.");
            }

            if (computer.Status?.Code == ComputerStatusCodes.Busy)
            {
                return ClubOperationResult.Failure("Нельзя перезагрузить компьютер с активной сессией.");
            }

            await SetComputerStatusAsync(computerId, ComputerStatusCodes.Available);
            return ClubOperationResult.Success("Компьютер перезагружен.");
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    private async Task<IReadOnlyList<ApiTariff>> GetApiTariffsWithZonesAsync()
    {
        var tariffs = await api.GetListAsync<ApiTariff>(api.Admin("tariffs"));
        var tariffZones = await api.GetListAsync<ApiTariffZone>(api.Admin("tariff-zones"));
        var zonesByTariffId = tariffZones
            .GroupBy(zone => zone.TariffId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var tariff in tariffs)
        {
            if (zonesByTariffId.TryGetValue(tariff.Id, out var zones))
            {
                tariff.TariffZones = zones;
            }
        }

        return tariffs;
    }

    private async Task EnrichTransactionsAsync(IReadOnlyList<Transaction> transactions)
    {
        if (transactions.Count == 0)
        {
            return;
        }

        await EnsureContextAsync();

        var clients = (await api.GetListAsync<ApiClient>(api.Mobile("clients")))
            .ToDictionary(client => client.Id);
        var shifts = (await api.GetListAsync<ApiCashierShift>(api.Admin("cashier-shifts")))
            .ToDictionary(shift => shift.Id, shift => ApiMapper.ToShift(shift, club));
        var cashiers = (await api.GetListAsync<ApiCashier>(api.Admin("cashiers")))
            .ToDictionary(cashier => cashier.Id);

        foreach (var transaction in transactions)
        {
            if (transaction.ClientId is int clientId && clients.TryGetValue(clientId, out var apiClient))
            {
                transaction.Client = ApiMapper.ToClient(apiClient);
            }

            if (transaction.CashierShiftId is int shiftId && shifts.TryGetValue(shiftId, out var shift))
            {
                if (shift.Cashier is null && cashiers.TryGetValue(shift.CashierId, out var apiCashier))
                {
                    shift.Cashier = ApiMapper.ToCashier(apiCashier);
                }

                transaction.CashierShift = shift;
            }

            var paymentType = paymentTypes.FirstOrDefault(type => type.Id == transaction.PaymentTypeId);
            if (paymentType is not null)
            {
                transaction.PaymentType = ApiMapper.ToPaymentType(paymentType);
            }
        }
    }

    private async Task EnsureBalancePaymentTypeAsync()
    {
        if (paymentTypes.Any(type => type.Code == PaymentTypeCodes.Balance))
        {
            return;
        }

        clubId = null;
        await EnsureContextAsync();
    }

    private async Task EnsureContextAsync()
    {
        if (clubId.HasValue)
        {
            return;
        }

        var context = await api.GetAsync<ApiAdminPanelContext>(api.Admin("context"));
        if (context is null)
        {
            throw new InvalidOperationException("Не удалось загрузить контекст клуба из API.");
        }

        clubId = context.DefaultClubId ?? context.Clubs.FirstOrDefault()?.Id;
        club = context.Clubs.FirstOrDefault(item => item.Id == clubId) is { } apiClub
            ? ApiMapper.ToClub(apiClub)
            : null;
        computerStatuses = context.ComputerStatuses.ToList();
        paymentTypes = context.PaymentTypes.ToList();
    }

    private async Task<Computer?> GetComputerAsync(int computerId)
    {
        var zones = await GetZonesAsync();
        return zones.SelectMany(zone => zone.Computers).FirstOrDefault(computer => computer.Id == computerId);
    }

    private async Task SetComputerStatusAsync(int computerId, string statusCode)
    {
        await EnsureContextAsync();
        var status = computerStatuses.FirstOrDefault(item => item.Code == statusCode);
        if (status is null)
        {
            return;
        }

        var computer = await api.GetAsync<ApiComputer>(api.Admin($"computers/{computerId}"));
        if (computer is null)
        {
            return;
        }

        computer.ComputerStatusId = status.Id;
        await api.PutAsync(api.Admin($"computers/{computerId}"), computer);
    }

    private async Task<ClubOperationResult> ChangeBookingStatusAsync(int bookingId, string status, string successMessage)
    {
        try
        {
            var booking = await api.GetAsync<ApiBooking>(api.Crm($"bookings/{bookingId}"));
            if (booking is null)
            {
                return ClubOperationResult.Failure("Бронь не найдена.");
            }

            booking.Status = Enum.Parse<ApiBookingStatus>(status);
            await api.PutAsync(api.Crm($"bookings/{bookingId}"), booking);

            if (status is BookingStatus.Cancelled or BookingStatus.NoShow or BookingStatus.Completed)
            {
                var computer = await GetComputerAsync(booking.ComputerId);
                if (computer?.Status?.Code == ComputerStatusCodes.Reserved)
                {
                    await SetComputerStatusAsync(booking.ComputerId, ComputerStatusCodes.Available);
                }
            }

            return ClubOperationResult.Success(successMessage);
        }
        catch (Exception ex)
        {
            return ClubOperationResult.Failure(ex.Message);
        }
    }

    private async Task CompleteActiveBookingAsync(int computerId, int clientId)
    {
        var bookings = await GetBookingsAsync();
        var booking = bookings.FirstOrDefault(item =>
            item.ComputerId == computerId &&
            item.ClientId == clientId &&
            item.Status is BookingStatus.Created or BookingStatus.Active);

        if (booking is null)
        {
            return;
        }

        booking.Status = BookingStatus.Completed;
        await api.PutAsync(api.Crm($"bookings/{booking.Id}"), ApiMapper.ToApiBooking(booking));
    }

    private static bool HasBookingConflict(IReadOnlyList<Booking> bookings, int computerId, DateTime startsAt, DateTime endsAt) =>
        bookings.Any(booking =>
            booking.ComputerId == computerId &&
            booking.Status is BookingStatus.Created or BookingStatus.Active &&
            startsAt < booking.EndsAt &&
            endsAt > booking.StartsAt);

    private async Task EnrichSessionsAsync(IReadOnlyList<GameSession> sessions)
    {
        if (sessions.Count == 0)
        {
            return;
        }

        await EnsureContextAsync();
        var clients = (await api.GetListAsync<ApiClient>(api.Mobile("clients")))
            .ToDictionary(client => client.Id);
        var computers = (await api.GetListAsync<ApiComputer>(api.Admin("computers")))
            .ToDictionary(computer => computer.Id);
        var zones = (await api.GetListAsync<ApiComputerZone>(api.Admin("zones")))
            .ToDictionary(zone => zone.Id, zone => ApiMapper.ToZone(zone, club));
        var tariffs = (await GetApiTariffsWithZonesAsync())
            .ToDictionary(tariff => tariff.Id);

        foreach (var session in sessions)
        {
            if (session.ClientId is int clientId && clients.TryGetValue(clientId, out var apiClient))
            {
                session.Client = ApiMapper.ToClient(apiClient);
            }

            if (computers.TryGetValue(session.ComputerId, out var apiComputer))
            {
                zones.TryGetValue(apiComputer.ZoneId, out var mappedZone);
                session.Computer = ApiMapper.ToComputer(apiComputer, mappedZone, computerStatuses);
            }

            if (tariffs.TryGetValue(session.TariffId, out var apiTariff))
            {
                session.Tariff = ApiMapper.ToTariff(apiTariff);
            }
        }
    }

    private async Task EnrichSessionDetailsAsync(GameSession session)
    {
        await EnrichSessionsAsync([session]);

        var transactions = (await GetTransactionsAsync())
            .Where(transaction => transaction.GameSessionId == session.Id)
            .ToList();

        foreach (var transaction in transactions)
        {
            var paymentType = paymentTypes.FirstOrDefault(type => type.Id == transaction.PaymentTypeId);
            if (paymentType is not null)
            {
                transaction.PaymentType = ApiMapper.ToPaymentType(paymentType);
            }
        }

        session.Transactions = transactions;
        session.Extensions = transactions
            .Where(transaction => transaction.Type == TransactionType.SessionExtension)
            .Select(transaction => new SessionExtension
            {
                Id = transaction.Id,
                GameSessionId = session.Id,
                Amount = transaction.Amount,
                CreatedAt = transaction.CreatedAt,
                Reason = ModelLabels.GetTransactionTypeText(transaction.Type),
                Tariff = session.Tariff
            })
            .ToList();
    }

    private async Task EnrichBookingsAsync(IReadOnlyList<Booking> bookings)
    {
        if (bookings.Count == 0)
        {
            return;
        }

        await EnsureContextAsync();
        var clients = (await api.GetListAsync<ApiClient>(api.Mobile("clients")))
            .ToDictionary(client => client.Id);
        var computers = (await api.GetListAsync<ApiComputer>(api.Admin("computers")))
            .ToDictionary(computer => computer.Id);
        var zones = (await api.GetListAsync<ApiComputerZone>(api.Admin("zones")))
            .ToDictionary(zone => zone.Id, zone => ApiMapper.ToZone(zone, club));

        foreach (var booking in bookings)
        {
            if (clients.TryGetValue(booking.ClientId, out var apiClient))
            {
                booking.Client = ApiMapper.ToClient(apiClient);
            }

            if (computers.TryGetValue(booking.ComputerId, out var apiComputer))
            {
                zones.TryGetValue(apiComputer.ZoneId, out var zone);
                booking.Computer = ApiMapper.ToComputer(apiComputer, zone, computerStatuses);
                booking.Zone = zone;
            }
            else if (zones.TryGetValue(booking.ZoneId, out var bookingZone))
            {
                booking.Zone = bookingZone;
            }
        }
    }

    private static ClubOperationResult? ValidateSessionStart(
        Computer computer,
        int? clientId,
        IReadOnlyList<GameSession> activeSessions,
        IReadOnlyList<Booking> bookings)
    {
        if (computer.Status?.Code is ComputerStatusCodes.Maintenance or ComputerStatusCodes.Disabled)
        {
            return ClubOperationResult.Failure("Компьютер недоступен для запуска сессии.");
        }

        if (computer.Status?.Code == ComputerStatusCodes.Busy)
        {
            return ClubOperationResult.Failure("На компьютере уже идет сессия.");
        }

        if (computer.Status?.Code == ComputerStatusCodes.Reserved)
        {
            if (clientId is null)
            {
                return ClubOperationResult.Failure("Компьютер зарезервирован. Для гостя без аккаунта выберите другой ПК.");
            }

            var hasBooking = bookings.Any(booking =>
                booking.ComputerId == computer.Id &&
                booking.ClientId == clientId &&
                booking.Status is BookingStatus.Created or BookingStatus.Active);

            if (!hasBooking)
            {
                return ClubOperationResult.Failure("Компьютер зарезервирован другой бронью.");
            }
        }

        if (activeSessions.Any(session => session.ComputerId == computer.Id))
        {
            return ClubOperationResult.Failure("На компьютере уже идет активная сессия.");
        }

        return null;
    }
}
