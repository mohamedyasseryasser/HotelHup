using HotelHup.APPLICATION.Constant;
using HotelHup.APPLICATION.DTO.Expense;
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.Reports;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.interfaces;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;

namespace HotelHup.APPLICATION.services.implementation;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reports;
    private readonly IUserRepository _users;

    public ReportService(
        IReportRepository reports,
        IUserRepository users)
    {
        _reports = reports;
        _users = users;
    }

    public async Task<ResponseStatus<OccupancyReportResponse>> GetOccupancyAsync(
        OccupancyReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct);

        if (!access.Success)
        {
            return Fail<OccupancyReportResponse>(access);
        }

        var propertyId = access.Data!.Value;

        var from = request.From?.DateTime.Date
                   ?? DateTime.UtcNow.Date;

        var to = request.To?.DateTime.Date.AddDays(1)
                 ?? from.AddDays(1);

        if (from >= to)
        {
            return Fail<OccupancyReportResponse>(
                "From must be before To.",
                "INVALID_DATE_RANGE",
                422);
        }

        var rooms = await _reports.GetRoomsAsync(
            propertyId,
            request.RoomTypeId,
            null,
            ct);

        var reservations = await _reports.GetReservationsAsync(
            propertyId,
            request.From,
            request.To,
            null,
            request.RoomTypeId,
            null,
            ct);

        var sellable = rooms
            .Where(x =>
                x.IsActive &&
                x.Status != RoomStatus.OutOfOrder &&
                x.Status != RoomStatus.OutOfService)
            .ToList();

        var occupiedIds = reservations
            .Where(x => x.Status == ReservationStatus.CheckedIn)
            .SelectMany(x => x.ReservationRooms)
            .Where(x =>
                x.CheckInDate < to &&
                x.CheckOutDate >= from)
            .Select(x => x.RoomId)
            .Distinct()
            .ToHashSet();

        var soldNights = reservations
            .Where(x =>
                x.Status is not ReservationStatus.Cancelled
                and not ReservationStatus.NoShow)
            .SelectMany(x => x.ReservationRooms)
            .Sum(x =>
                OverlapNights(
                    x.CheckInDate,
                    x.CheckOutDate,
                    from,
                    to));

        var availableNights = Math.Max(
            0,
            sellable.Count * (to.Date - from.Date).Days
            - soldNights);

        var occupancyBase =
            sellable.Count * (to.Date - from.Date).Days;

        return new ResponseStatus<OccupancyReportResponse>(
            new OccupancyReportResponse
            {
                PropertyId = propertyId,
                From = request.From,
                To = request.To,
                TotalRooms = rooms.Count,
                SellableRooms = sellable.Count,
                OccupiedRooms = occupiedIds.Count,
                OutOfOrderRooms = rooms.Count(x =>
                    x.Status is RoomStatus.OutOfOrder
                    or RoomStatus.OutOfService),
                AvailableRooms = Math.Max(
                    0,
                    sellable.Count - occupiedIds.Count),
                OccupancyPercentage = occupancyBase == 0
                    ? 0
                    : decimal.Round(
                        soldNights * 100m / occupancyBase,
                        2),
                SoldRoomNights = soldNights,
                AvailableRoomNights = availableNights
            });
    }

    public async Task<ResponseStatus<RevenueReportResponse>> GetRevenueAsync(
      RevenueReportRequest request,
      User actor,
      CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct,
            Permissions.Folios.Read,
            Permissions.Expenses.Read);

        if (!access.Success)
        {
            return Fail<RevenueReportResponse>(access);
        }

        var propertyId = access.Data!.Value;

        // ---------------------------------------
        // Dates
        // ---------------------------------------

        var fromDate = request.From?.DateTime.Date
                       ?? DateTime.UtcNow.Date;

        var toDate = request.To?.DateTime.Date
                     ?? DateTime.UtcNow.Date.AddDays(1);

        // Number of days in the report period
        var days = Math.Max(
            1,
            (toDate - fromDate).Days);

        // ---------------------------------------
        // Revenue
        // ---------------------------------------

        var items = await _reports.GetRevenueItemsAsync(
            propertyId,
            request.From,
            request.To,
            request.RevenueType,
            request.RoomTypeId,
            ct);

        // ---------------------------------------
        // Expenses
        // ---------------------------------------

        var expenses = await _reports.GetExpensesAsync(
            propertyId,
            request.From,
            request.To,
            null,
            null,
            request.Currency,
            false,
            ct);

        // ---------------------------------------
        // Currency filter
        // ---------------------------------------

        var filtered = string.IsNullOrWhiteSpace(request.Currency)
            ? items
            : items
                .Where(x =>
                    x.Folio.Currency.Equals(
                        request.Currency.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        // ---------------------------------------
        // Revenue breakdown
        // ---------------------------------------

        var room = Sum(
            filtered,
            FolioItemType.RoomCharge);

        var service = Sum(
            filtered,
            FolioItemType.Service);

        var taxes = filtered
            .Where(x => x.Type == FolioItemType.Tax)
            .Sum(x => x.Amount + x.Tax);

        var discounts = Math.Abs(
            Sum(
                filtered,
                FolioItemType.Discount));

        var fees = Sum(
            filtered,
            FolioItemType.Fee);

        var refunds = filtered
            .Where(x => x.Type == FolioItemType.Refund)
            .Sum(x => Math.Abs(x.Amount));

        // ---------------------------------------
        // Gross / Net Revenue
        // ---------------------------------------

        var gross =
            room +
            service +
            taxes +
            fees -
            discounts;

        var net = gross - refunds;

        // ---------------------------------------
        // Reservations
        // ---------------------------------------

        var selectedReservations =
            await _reports.GetReservationsAsync(
                propertyId,
                request.From,
                request.To,
                null,
                request.RoomTypeId,
                null,
                ct);

        var soldNights = selectedReservations
            .Where(x =>
                x.Status is not ReservationStatus.Cancelled
                    and not ReservationStatus.NoShow)
            .SelectMany(x => x.ReservationRooms)
            .Sum(x => x.Nights);

        // ---------------------------------------
        // Rooms / Available Room Nights
        // ---------------------------------------

        var rooms = await _reports.GetRoomsAsync(
            propertyId,
            request.RoomTypeId,
            null,
            ct);

        var activeRooms = rooms.Count(x =>
            x.IsActive &&
            x.Status is not RoomStatus.OutOfOrder
                and not RoomStatus.OutOfService);

        var availableNights = activeRooms * days;

        // ---------------------------------------
        // Expenses
        // ---------------------------------------

        var expenseTotal = expenses
            .Where(x =>
                string.IsNullOrWhiteSpace(request.Currency)
                || x.Currency.Equals(
                    request.Currency.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            .Sum(x => x.Amount);

        // ---------------------------------------
        // Response
        // ---------------------------------------

        return new ResponseStatus<RevenueReportResponse>(
            new RevenueReportResponse
            {
                PropertyId = propertyId,

                Currency = string.IsNullOrWhiteSpace(request.Currency)
                    ? null
                    : request.Currency.Trim().ToUpperInvariant(),

                RoomRevenue = room,
                ServiceRevenue = service,
                Taxes = taxes,
                Discounts = discounts,
                Refunds = refunds,
                Fees = fees,

                GrossRevenue = gross,
                NetRevenue = net,

                Expenses = expenseTotal,
                OperatingProfit = net - expenseTotal,

                SoldRoomNights = soldNights,

                ADR = soldNights == 0
                    ? 0
                    : decimal.Round(
                        room / soldNights,
                        2),

                RevPAR = availableNights == 0
                    ? 0
                    : decimal.Round(
                        room / availableNights,
                        2)
            });
    }

    public async Task<ResponseStatus<IReadOnlyList<ArrivalReportItem>>> GetArrivalsAsync(
        ReservationReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<ArrivalReportItem>>(access);
        }

        var rows = await _reports.GetReservationsAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Status,
            request.RoomTypeId,
            request.Source,
            ct);

        return new ResponseStatus<IReadOnlyList<ArrivalReportItem>>(
            rows
                .Where(x =>
                    x.Status is not ReservationStatus.Cancelled
                    and not ReservationStatus.NoShow)
                .Select(x =>
                    new ArrivalReportItem
                    {
                        ReservationId = x.Id,
                        Guest = GuestName(x.Guest),
                        Room = x.ReservationRooms
                            .FirstOrDefault()?
                            .Room?
                            .RoomNumber,
                        RoomType = x.ReservationRooms
                            .FirstOrDefault()?
                            .Room?
                            .RoomType?
                            .Name,
                        CheckInDate = x.CheckInDate,
                        Adults = x.Adults,
                        Children = x.Children,
                        Status = x.Status,
                        Source = x.Source,
                        FolioBalance = x.Folio?.Balance
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<DepartureReportItem>>> GetDeparturesAsync(
        ReservationReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct,
            Permissions.Folios.Read);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<DepartureReportItem>>(access);
        }

        var rows = await _reports.GetReservationsAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Status,
            request.RoomTypeId,
            request.Source,
            ct);

        return new ResponseStatus<IReadOnlyList<DepartureReportItem>>(
            rows
                .Where(x =>
                    x.Status is not ReservationStatus.Cancelled
                    and not ReservationStatus.NoShow)
                .Select(x =>
                    new DepartureReportItem
                    {
                        ReservationId = x.Id,
                        Guest = GuestName(x.Guest),
                        Room = x.ReservationRooms
                            .FirstOrDefault()?
                            .Room?
                            .RoomNumber,
                        CheckOutDate = x.CheckOutDate,
                        Status = x.Status,
                        FolioBalance = x.Folio?.Balance,
                        LateDeparture =
                            x.CheckOutDate.TimeOfDay
                            > TimeSpan.FromHours(12)
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<CurrentGuestReportItem>>> GetCurrentGuestsAsync(
        ReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<CurrentGuestReportItem>>(access);
        }

        var now = DateTime.UtcNow;

        var rows = await _reports.GetReservationsAsync(
            access.Data!.Value,
            request.From,
            request.To,
            ReservationStatus.CheckedIn,
            null,
            null,
            ct);

        return new ResponseStatus<IReadOnlyList<CurrentGuestReportItem>>(
            rows
                .Where(x =>
                    x.CheckInDate <= now &&
                    x.CheckOutDate > now)
                .Select(x =>
                    new CurrentGuestReportItem
                    {
                        ReservationId = x.Id,
                        Guest = GuestName(x.Guest),
                        Room = x.ReservationRooms
                            .FirstOrDefault()?
                            .Room?
                            .RoomNumber,
                        RoomType = x.ReservationRooms
                            .FirstOrDefault()?
                            .Room?
                            .RoomType?
                            .Name,
                        CheckInDate = x.CheckInDate,
                        ExpectedCheckOut = x.CheckOutDate,
                        Nights = Math.Max(
                            0,
                            (
                                x.CheckOutDate.Date -
                                x.CheckInDate.Date
                            ).Days),
                        Status = x.Status
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<RoomStatusReportItem>>> GetRoomStatusAsync(
        RoomStatusReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<RoomStatusReportItem>>(access);
        }

        var propertyId = access.Data!.Value;

        var rooms = await _reports.GetRoomsAsync(
            propertyId,
            request.RoomTypeId,
            request.Status,
            ct);

        var reservations = await _reports.GetReservationsAsync(
            propertyId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            request.RoomTypeId,
            null,
            ct);

        var housekeeping =
            await _reports.GetHousekeepingTasksAsync(
                propertyId,
                null,
                null,
                null,
                request.RoomTypeId,
                ct);
 
        return new ResponseStatus<IReadOnlyList<RoomStatusReportItem>>(
            rooms
                .Select(room =>
                    new RoomStatusReportItem
                    {
                        RoomId = room.Id,
                        RoomNumber = room.RoomNumber,
                        RoomType =
                            room.RoomType?.Name
                            ?? string.Empty,

                        Status = room.Status,

                        CurrentReservationId =
                            reservations
                                .FirstOrDefault(r =>
                                    r.Status ==
                                        ReservationStatus.CheckedIn &&
                                    r.ReservationRooms.Any(
                                        a => a.RoomId == room.Id))
                                ?.Id,

                        HousekeepingStatus =
                            housekeeping
                                .Where(x => x.RoomId == room.Id)
                                .OrderByDescending(x => x.CreatedAt)
                                .FirstOrDefault()?
                                .Status
                                .ToString(),

 
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<PaymentReportItem>>> GetPaymentsAsync(
        PaymentReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct,
            Permissions.Payments.Read);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<PaymentReportItem>>(access);
        }

        var rows = await _reports.GetPaymentsAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Status,
            request.Method,
            ct);

        return new ResponseStatus<IReadOnlyList<PaymentReportItem>>(
            rows
                .Select(x =>
                    new PaymentReportItem
                    {
                        PaymentId = x.Id,
                        ReservationId = x.Folio.ReservationId,
                        Method = x.Method,
                        Status = x.Status,
                        Amount = x.Amount,
                        Currency = x.Folio.Currency,
                        PaidAt = x.PaidAt
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<RefundReportItem>>> GetRefundsAsync(
        RefundReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct,
            Permissions.Refunds.Read);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<RefundReportItem>>(access);
        }

        var rows = await _reports.GetRefundsAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Status,
            request.Method,
            ct);

        return new ResponseStatus<IReadOnlyList<RefundReportItem>>(
            rows
                .Select(x =>
                    new RefundReportItem
                    {
                        RefundId = x.id,
                        ReservationId = x.Folio.ReservationId,
                        Method = x.Method,
                        Status = x.Status,
                        Amount = x.Amount,
                        Currency = x.Folio.Currency,
                        Reason = x.Reason,
                        CreatedAt = x.CreatedAt
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<CancellationReportItem>>> GetCancellationsAsync(
        CancellationReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<CancellationReportItem>>(access);
        }

        var rows = await _reports.GetStatusHistoryAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Status ?? ReservationStatus.Cancelled,
            request.Reason,
            ct);

        return new ResponseStatus<IReadOnlyList<CancellationReportItem>>(
            rows
                .Select(x =>
                    new CancellationReportItem
                    {
                        ReservationId = x.ReservationId,
                        Guest = GuestName(x.Reservation.Guest),
                        Status = x.ToStatus,
                        Reason = x.Reason,
                        ChangedAt = x.ChangedAt,
                        CancellationFee =
                            x.Reservation.TotalFeeAmount
                    })
                .ToList());
    }

    public async Task<ResponseStatus<IReadOnlyList<HousekeepingReportItem>>> GetHousekeepingAsync(
        HousekeepingReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct);

        if (!access.Success)
        {
            return Fail<IReadOnlyList<HousekeepingReportItem>>(access);
        }

        var rows = await _reports.GetHousekeepingTasksAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Status,
            request.RoomTypeId,
            ct);

        return new ResponseStatus<IReadOnlyList<HousekeepingReportItem>>(
            rows
                .Select(x =>
                    new HousekeepingReportItem
                    {
                        TaskId = x.id,
                        RoomId = x.RoomId,
                        RoomNumber = x.Room.RoomNumber,
                        RoomType =
                            x.Room.RoomType?.Name
                            ?? string.Empty,
                        Status = x.Status,
                        DueAt = x.DueAt,
                        StartedAt = x.StartedAt,
                        CompletedAt = x.CompletedAt,
                        AssigneeId = x.AssigneeId
                    })
                .ToList());
    }

 
    public async Task<ResponseStatus<ExpenseReportResponse>> GetExpenseReportAsync(
        ExpenseReportRequest request,
        User actor,
        CancellationToken ct = default)
    {
        var access = await AuthorizeAsync(
            actor,
            request.PropertyId,
            ct,
            Permissions.Expenses.Read);

        if (!access.Success)
        {
            return Fail<ExpenseReportResponse>(access);
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? null
            : request.Currency
                .Trim()
                .ToUpperInvariant();

        var rows = await _reports.GetExpensesAsync(
            access.Data!.Value,
            request.From,
            request.To,
            request.Category?.Trim(),
            request.PaymentMethod,
            currency,
            request.IncludeVoided,
            ct);

        var posted = rows
            .Where(x => x.Status == ExpenseStatus.Posted)
            .ToList();

        return new ResponseStatus<ExpenseReportResponse>(
            new ExpenseReportResponse
            {
                PropertyId = access.Data.Value,
                From = request.From,
                To = request.To,
                TotalExpenses = posted.Sum(x => x.Amount),
                ExpenseCount = posted.Count,

                ByCurrency = posted
                    .GroupBy(x => x.Currency)
                    .Select(g =>
                        new ExpenseReportCurrencySummary
                        {
                            Currency = g.Key,
                            Total = g.Sum(x => x.Amount),
                            Count = g.Count()
                        })
                    .ToList(),

                ByCategory = posted
                    .GroupBy(x => new
                    {
                        x.Category,
                        x.Currency
                    })
                    .Select(g =>
                        new ExpenseReportCategorySummary
                        {
                            Category = g.Key.Category,
                            Currency = g.Key.Currency,
                            Total = g.Sum(x => x.Amount),
                            Count = g.Count()
                        })
                    .ToList(),

                Items = rows
                    .Select(x =>
                        new ExpenseReportItem
                        {
                            Id = x.Id,
                            Category = x.Category,
                            Description = x.Description,
                            Amount = x.Amount,
                            Currency = x.Currency,
                            ExpenseDate = x.ExpenseDate,
                            PaymentMethod = x.PaymentMethod,
                            VendorName = x.VendorName,
                            ReferenceNumber = x.ReferenceNumber,
                            Status = x.Status
                        })
                    .ToList()
            });
    }

    private async Task<ResponseStatus<int?>> AuthorizeAsync(
        User actor,
        int? requestedPropertyId,
        CancellationToken ct,
        params string[] additionalPermissions)
    {
        if (actor is null || !actor.IsActive)
        {
            return new ResponseStatus<int?>(
                "User is inactive or unauthenticated.",
                statusCode: 401,
                code: "UNAUTHENTICATED");
        }

        var permissions =
            await _users.GetPermissionNamesAsync(
                actor.Id,
                ct);

        if (!permissions.Contains(Permissions.Reports.Read) ||
            additionalPermissions.Any(
                x => !permissions.Contains(x)))
        {
            return new ResponseStatus<int?>(
                "The required report permission is missing.",
                statusCode: 403,
                code: "PERMISSION_DENIED");
        }

        int propertyId;

        if (actor.PropertyId.HasValue)
        {
            if (requestedPropertyId.HasValue &&
                requestedPropertyId.Value != actor.PropertyId.Value)
            {
                return new ResponseStatus<int?>(
                    "The requested property is outside your scope.",
                    statusCode: 403,
                    code: "PROPERTY_FORBIDDEN");
            }

            propertyId = actor.PropertyId.Value;
        }
        else
        {
            var roles = await _users.GetRolesAsync(
                actor.Id,
                ct);

            if (!roles.Any(x =>
                x.IsActive &&
                string.Equals(
                    x.Name,
                    nameof(UserRole.Admin),
                    StringComparison.OrdinalIgnoreCase)))
            {
                return new ResponseStatus<int?>(
                    "A property scope is required.",
                    statusCode: 403,
                    code: "PROPERTY_SCOPE_REQUIRED");
            }

            if (!requestedPropertyId.HasValue)
            {
                return new ResponseStatus<int?>(
                    "propertyId is required for an administrator report.",
                    statusCode: 422,
                    code: "PROPERTY_REQUIRED");
            }

            propertyId = requestedPropertyId.Value;
        }

        var property = await _users.GetPropertyAsync(
            propertyId,
            ct);

        if (property is null)
        {
            return new ResponseStatus<int?>(
                "Property was not found.",
                statusCode: 404,
                code: "PROPERTY_NOT_FOUND");
        }

        if (property.Status == PropertyStatus.Inactive)
        {
            return new ResponseStatus<int?>(
                "Inactive property cannot be reported.",
                statusCode: 422,
                code: "PROPERTY_INACTIVE");
        }

        return new ResponseStatus<int?>(propertyId);
    }

    private static int OverlapNights(
        DateTime start,
        DateTime end,
        DateTime from,
        DateTime to)
    {
        var overlapStart =
            start.Date > from.Date
                ? start.Date
                : from.Date;

        var overlapEnd =
            end.Date < to.Date
                ? end.Date
                : to.Date;

        return Math.Max(
            0,
            (overlapEnd - overlapStart).Days);
    }

    private static decimal Sum(
        IEnumerable<FolioItem> rows,
        FolioItemType type)
    {
        return rows
            .Where(x => x.Type == type)
            .Sum(x => x.Amount);
    }

    private static string GuestName(Guest guest)
    {
        return string.Join(
            " ",
            new[]
            {
                guest.FirstName,
                guest.LastName
            }
            .Where(x =>
                !string.IsNullOrWhiteSpace(x)))
            .Trim();
    }

    private static ResponseStatus<T> Fail<T>(
        ResponseStatus<int?> source)
    {
        return new ResponseStatus<T>(
            source.Message,
            statusCode: source.StatusCode,
            code: source.Code);
    }

    private static ResponseStatus<T> Fail<T>(
        string message,
        string code,
        int status)
    {
        return new ResponseStatus<T>(
            message,
            statusCode: status,
            code: code);
    }
}