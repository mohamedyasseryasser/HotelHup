
using HotelHup.APPLICATION.DTO.General;
using HotelHup.APPLICATION.DTO.reservation;
using HotelHup.APPLICATION.interfacesrepo;
using HotelHup.APPLICATION.services.implementation;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Numerics;
using Xunit;
using static HotelHup.APPLICATION.Constant.Permissions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HotelHup.Tests;

public sealed class ReservationServiceComprehensiveTests
{
    private static readonly DateTime CheckIn = new(2030, 6, 10);
    private static readonly DateTime CheckOut = new(2030, 6, 13);

    private const int PropertyId = 7;
    private const int ReservationId = 42;
    //done
    [Test]
    public async Task searchavalibalityascyn_reject_check_actor_propertyid_not_equal_request_propertyid()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var request = AvailabilityRequest(
            1,
            CheckIn,
            CheckIn.AddDays(-1));

        // Act
        var result = await service.SearchAvailabilityAsync(Actor(), request);

        // Assert    
        AssertFailure(result, 403, "PROPERTY_SCOPE_INVALID");

        repository.VerifyNoOtherCalls();
    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_rejects_check_out_before_check_in_without_repository_calls()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var request = AvailabilityRequest(
            PropertyId,
            CheckIn,
            CheckIn.AddDays(-1));

        // Act
        var result = await service.SearchAvailabilityAsync(Actor(), request);

        // Assert    
        AssertFailure(result, 400, "INVALID_DATES");

        repository.VerifyNoOtherCalls();

    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_rejects_equal_dates_without_repository_calls()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var request = AvailabilityRequest(
            PropertyId,
            CheckIn,
            CheckIn);

        // Act
        var result = await service.SearchAvailabilityAsync(Actor(), request);

        // Assert
        AssertFailure(result, 400, "INVALID_DATES");

        repository.VerifyNoOtherCalls();
    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_returns_not_found_when_property_does_not_exist()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Property?)null);

        // Act
        var result = await service.SearchAvailabilityAsync(
            Actor(),
            AvailabilityRequest(PropertyId, CheckIn, CheckOut));

        // Assert
        AssertFailure(result, 404, "PROPERTY_NOT_FOUND");

        repository.Verify(
            x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_rejects_inactive_property()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Property
            {
                ID = PropertyId,
                Status = PropertyStatus.Inactive
            });

        // Act
        var result = await service.SearchAvailabilityAsync(
            Actor(),
            AvailabilityRequest(PropertyId, CheckIn, CheckOut));

        // Assert
        AssertFailure(result, 400, "PROPERTY_INACTIVE");

        repository.Verify(
            x => x.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_returns_empty_result_when_no_rooms_are_available()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var request = AvailabilityRequest(
            PropertyId,
            CheckIn,
            CheckOut,
            adults: 2,
            children: 1,
            roomTypeId: 3,
            excludedReservationId: 99);

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Property
            {
                ID = PropertyId,
                Status = PropertyStatus.Active
            });

        repository
            .Setup(x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                2,
                1,
                3,
                99,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Room>());

        // Act
        var result = await service.SearchAvailabilityAsync(Actor(), request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Empty);
        Assert.That(result.Message, Is.EqualTo("not has any room avaliable"));

        repository.Verify(
            p => p.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()
            ), Times.AtLeastOnce());

        repository.Verify(
            x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                2,
                1,
                3,
                99,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_maps_multiple_rooms_and_uses_room_price_when_positive()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var type = new RoomType
        {
            Id = 3,
            Name = "Suite",
            BasePrice = 80m
        };

        var rooms = new[]
        {
            new Room
            {
                Id = 1,
                RoomNumber = "101",
                RoomTypeId = 3,
                RoomType = type,
                PricePerNight = 100m
            },
            new Room
            {
                Id = 2,
                RoomNumber = "102",
                RoomTypeId = 3,
                RoomType = type,
                PricePerNight = 0m
            }
        };

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Property
            {
                ID = PropertyId,
                Status = PropertyStatus.Active
            });

        repository
            .Setup(x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                1,
                0,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rooms);

        // Act
        var result = await service.SearchAvailabilityAsync(
            Actor(),
            AvailabilityRequest(PropertyId, CheckIn, CheckOut));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(2));

        Assert.That(
            result.Data!.Select(x => x.RoomId),
            Is.EqualTo(new[] { 1, 2 }));

        Assert.That(
            result.Data.Select(x => x.RoomNumber),
            Is.EqualTo(new[] { "101", "102" }));

        Assert.That(
            result.Data.Select(x => x.NightlyRate),
            Is.EqualTo(new[] { 100m, 80m }));

        foreach (var item in result.Data)
        {
            Assert.That(item.Nights, Is.EqualTo(3));
        }

        Assert.That(
            result.Data.Select(x => x.TotalAmount),
            Is.EqualTo(new[] { 300m, 240m }));
    }
    //done
    [Test]
    public async Task SearchAvailabilityAsync_maps_null_room_type_using_zero_rate_and_empty_name()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var room = new Room
        {
            Id = 5,
            RoomNumber = "201",
            RoomTypeId = 8,
            RoomType = null,
            PricePerNight = 0m
        };

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Property
            {
                ID = PropertyId,
                Status = PropertyStatus.Active
            });

        repository
            .Setup(x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                1,
                0,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { room });

        // Act
        var result = await service.SearchAvailabilityAsync(
            Actor(),
            AvailabilityRequest(PropertyId, CheckIn, CheckOut));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(1));

        var item = result.Data![0];

        Assert.That(item.RoomTypeId, Is.EqualTo(8));
        Assert.That(item.RoomTypeName, Is.EqualTo(string.Empty));
        Assert.That(item.NightlyRate, Is.EqualTo(0m));
        Assert.That(item.TotalAmount, Is.EqualTo(0m));
    }
    //done
    [Test]
    public async Task ListAsync_uses_actor_property_and_maps_paging_and_guest_data()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var reservation = Reservation(
            status: ReservationStatus.Confirmed);

        reservation.Guest = new Guest
        {
            FirstName = "Ada",
            LastName = "Lovelace"
        };

        reservation.ReservationRooms.Add(new ReservationRoom
        {
            RoomId = 12,
            IsCurrent = true
        });

        var request = new ReservationSearchRequest
        {
            Page = 2,
            PageSize = 5
        };

        repository
            .Setup(x => x.SearchAsync(
                PropertyId,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                (IReadOnlyList<Reservation>)new[] { reservation },
                9));

        // Act
        var result = await service.ListAsync(Actor(), request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!.Page, Is.EqualTo(2));
        Assert.That(result.Data.PageSize, Is.EqualTo(5));
        Assert.That(result.Data.TotalCount, Is.EqualTo(9));

        Assert.That(result.Data.Items, Has.Count.EqualTo(1));

        var item = result.Data.Items[0];

        Assert.That(item.GuestName, Is.EqualTo("Ada Lovelace"));
        Assert.That(item.RoomId, Is.EqualTo(12));

        repository.Verify(
            x => x.SearchAsync(
                PropertyId,
                request,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    //done
    [Test]
    public async Task GetAsync_rejects_property_outside_scope_without_repository_call()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        // Act
        var result = await service.GetAsync(
            Actor(),
            PropertyId + 1,
            ReservationId);

        // Assert
        AssertFailure(result, 404, "RESERVATION_NOT_FOUND");

        repository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    //done
    [Test]
    public async Task GetAsync_maps_reservation_details_and_room_ids()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        var reservation = Reservation(
            status: ReservationStatus.Confirmed);

        reservation.Guest = new Guest
        {
            FirstName = "Grace",
            LastName = "Hopper"
        };

        reservation.ReservationRooms.Add(new ReservationRoom
        {
            Id = 3,
            RoomId = 11,
            Room = new Room { RoomNumber = "301" },
            IsCurrent = true,
            TotalAmount = 120m
        });

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.GetAsync(
            Actor(),
            PropertyId,
            ReservationId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!.GuestName, Is.EqualTo("Grace Hopper"));
        Assert.That(
            result.Data.Status,
            Is.EqualTo(ReservationStatus.Confirmed));

        Assert.That(result.Data.Rooms, Has.Count.EqualTo(1));
        Assert.That(result.Data.Rooms[0].RoomId, Is.EqualTo(11));
    }
    //done
    [Test]
    public async Task GetAsync_returns_not_found_when_reservation_is_null()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        // Act
        var result = await service.GetAsync(
            Actor(),
            PropertyId,
            ReservationId);

        // Assert

        AssertFailure(result, 404, "RESERVATION_NOT_FOUND");

    }

    [Test]
    public async Task CreateAsync_rejects_when_property_is_missing()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Property?)null);

        var request = CreateRequest();

        // Act
        var result = await service.CreateAsync(Actor(), request);

        // Assert
        AssertFailure(result, 404, "PROPERTY_NOT_FOUND");

        repository.Verify(
            x => x.GetGuestAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<Folio>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateAsync_rejects_inactive_guest_and_empty_rooms()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);
        SetupActivePropertyAndGuest(repository);

        repository
            .Setup(x => x.GetGuestAsync(
                PropertyId,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guest
            {
                Id = 10,
                IsActive = false
            });

        // Act
        var result = await service.CreateAsync(
            Actor(),
            CreateRequest());

        // Assert
        AssertFailure(result, 409, "GUEST_INACTIVE");

        repository.Verify(
            x => x.GetActiveTaxesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateAsync_rejects_empty_room_requests()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);
        SetupActivePropertyAndGuest(repository);

        var request = new CreateReservationRequest
        {
            PropertyId = PropertyId,
            GuestId = 10,
            CheckInDate = CheckIn,
            CheckOutDate = CheckOut
        };

        // Act
        var result = await service.CreateAsync(Actor(), request);

        // Assert
        AssertFailure(result, 422, "ROOMS_REQUIRED");

        repository.Verify(
            x => x.GetActiveTaxesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateAsync_rejects_room_request_without_room_or_type()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);
        SetupActivePropertyAndGuest(repository);

        var request = CreateRequest(
            new CreateReservationRoomRequest
            {
                RatePlanId = 1
            });

        // Act
        var result = await service.CreateAsync(Actor(), request);

        // Assert
        AssertFailure(result, 422, "ROOM_REQUIRED");

        repository.Verify(
            x => x.GetActiveTaxesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateAsync_rejects_room_type_mismatch()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);
        SetupActivePropertyAndGuest(repository);

        var room = RoomWithType(1, 3, 100m);

        repository
            .Setup(x => x.GetActiveTaxesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tax>());

        repository
            .Setup(x => x.GetRoomAsync(
                PropertyId,
                1,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        var request = CreateRequest(
            new CreateReservationRoomRequest
            {
                RoomId = 1,
                RoomTypeId = 99,
                RatePlanId = 1
            });

        // Act
        var result = await service.CreateAsync(Actor(), request);

        // Assert
        AssertFailure(result, 422, "ROOM_TYPE_MISMATCH");

        repository.Verify(
            x => x.GetAvailableRoomsAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CreateAsync_happy_path_builds_reservation_and_calculates_amounts()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);
        SetupActivePropertyAndGuest(repository);

        var room = RoomWithType(1, 3, 100m);
        var plan = RatePlanWithVersion(1, 3, 120m);

        repository
            .Setup(x => x.GetActiveTaxesAsync(
                PropertyId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tax>());

        repository
            .Setup(x => x.GetRoomAsync(
                PropertyId,
                1,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        repository
            .Setup(x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                2,
                1,
                3,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { room });

        repository
            .Setup(x => x.GetRatePlanAsync(
                PropertyId,
                3,
                1,
                CheckIn,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        Reservation? added = null;

        repository
            .Setup(x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<Folio>(),
                It.IsAny<CancellationToken>()))
            .Callback<Reservation, Folio, CancellationToken>(
                (reservation, _, _) => added = reservation)
            .Returns(Task.CompletedTask);

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                0,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        // Act
        var result = await service.CreateAsync(
            Actor(),
            CreateRequest(
                new CreateReservationRoomRequest
                {
                    RoomId = 1,
                    RatePlanId = 1,
                    Adults = 2,
                    Children = 1
                }));

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.StatusCode, Is.EqualTo(201));
        Assert.That(result.Message, Is.EqualTo("Reservation created."));
        Assert.That(added, Is.Not.Null);

        Assert.That(added!.BaseAmount, Is.EqualTo(360m));
        Assert.That(added.TotalAmount, Is.EqualTo(360m));

        Assert.That(
            added.ReservationRooms.Single().Nights,
            Is.EqualTo(3));

        Assert.That(added.Adults, Is.EqualTo(2));
        Assert.That(added.Children, Is.EqualTo(1));

        repository.Verify(
            x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.Is<Folio>(
                    folio =>
                        folio.Status == FolioStatus.Open &&
                        folio.Total == 360m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateAsync_converts_concurrency_exception_to_conflict()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineExecution(repository);
        SetupActivePropertyAndGuest(repository);

        var room = RoomWithType(1, 3, 100m);

        repository
            .Setup(x => x.GetActiveTaxesAsync(
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Tax>());

        repository
            .Setup(x => x.GetRoomAsync(
                PropertyId,
                1,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        repository
            .Setup(x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                1,
                0,
                3,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { room });

        repository
            .Setup(x => x.GetRatePlanAsync(
                PropertyId,
                3,
                1,
                CheckIn,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RatePlanWithVersion(1, 3, 120m));

        repository
            .Setup(x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<Folio>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await service.CreateAsync(
            Actor(),
            CreateRequest(
                new CreateReservationRoomRequest
                {
                    RoomId = 1,
                    RatePlanId = 1
                }));

        // Assert
        AssertFailure(result, 409, "CONCURRENCY_CONFLICT");
    }

    [Test]
    public async Task ConfirmAsync_rejects_non_pending_reservation()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineActionExecution(repository);

        var reservation = Reservation(
            status: ReservationStatus.Confirmed);

        reservation.Guest = new Guest { IsActive = true };

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveProperty());

        repository
            .Setup(x => x.GetGuestAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guest { IsActive = true });

        // Act
        var result = await service.ConfirmAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new ConfirmReservationRequest());

        // Assert
        AssertFailure(result, 409, "INVALID_STATE_TRANSITION");

        repository.Verify(
            x => x.PersistTransitionAsync(
                It.IsAny<Reservation>(),
                It.IsAny<AuditLog>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ConfirmAsync_happy_path_persists_confirmed_transition()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineActionExecution(repository);

        var reservation = Reservation(
            status: ReservationStatus.Pending);

        reservation.Guest = new Guest { IsActive = true };

        reservation.ReservationRooms.Add(new ReservationRoom
        {
            RoomId = 1,
            Adults = 1,
            Children = 0,
            IsCurrent = true,
            Room = RoomWithType(1, 3, 100m)
        });

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveProperty());

        repository
            .Setup(x => x.GetAvailableRoomsAsync(
                PropertyId,
                CheckIn,
                CheckOut,
                1,
                0,
                3,
                ReservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new[] { reservation.ReservationRooms.Single().Room! });

        // Act
        var result = await service.ConfirmAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new ConfirmReservationRequest
            {
                Reason = "Approved"
            });

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(
            result.Data!.Status,
            Is.EqualTo(ReservationStatus.Confirmed));

        Assert.That(result.Data.Reason, Is.EqualTo("Approved"));

        repository.Verify(
            x => x.PersistTransitionAsync(
                reservation,
                It.Is<AuditLog>(
                    auditLog =>
                        auditLog.Action ==
                        nameof(ReservationStatus.Confirmed)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CancelAsync_rejects_policy_override_for_non_manager()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithReceptionist();

        InlineActionExecution(repository);

        var reservation = Reservation(
            status: ReservationStatus.Pending);

        reservation.Guest = new Guest { IsActive = true };

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveProperty());

        // Act
        var result = await service.CancelAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new CancelReservationRequest
            {
                Reason = "Override",
                OverridePolicy = true
            });

        // Assert
        AssertFailure(result, 403, "POLICY_OVERRIDE_NOT_ALLOWED");

        repository.Verify(
            x => x.PersistTransitionAsync(
                It.IsAny<Reservation>(),
                It.IsAny<AuditLog>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task NoShowAsync_rejects_invalid_pending_transition_when_reservation_is_checked_out()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineActionExecution(repository);

        var reservation = Reservation(
            status: ReservationStatus.CheckedOut);

        reservation.Guest = new Guest { IsActive = true };

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveProperty());

        repository
            .Setup(x => x.GetGuestAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guest { IsActive = true });

        // Act
        var result = await service.NoShowAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new NoShowRequest { OverridePolicy = true });

        // Assert
        AssertFailure(result, 409, "INVALID_STATE_TRANSITION");
    }

    [Test]
    public async Task AssignRoomCoreAsync_rejects_already_assigned_room()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        var reservation = Reservation(
            status: ReservationStatus.Pending);

        reservation.ReservationRooms.Add(new ReservationRoom
        {
            RoomId = 5,
            IsCurrent = true
        });

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.AssignRoomCoreAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new AssignRoomRequest
            {
                Rooms = new List<AssignRoomItemRequest>
                {
                    new()
                    {
                        RoomId = 5,
                        RatePlanId = 1
                    }
                }
            });

        // Assert
        AssertFailure(result, 409, "ROOM_ALREADY_ASSIGNED");

        repository.Verify(
            x => x.GetRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CheckInAsync_happy_path_marks_rooms_occupied_and_persists_lifecycle()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        var reservation = Reservation(
            status: ReservationStatus.Confirmed);

        reservation.ReservationRooms.Add(new ReservationRoom
        {
            RoomId = 1,
            IsCurrent = true
        });

        var room = RoomWithType(1, 3, 100m);

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        repository
            .Setup(x => x.GetRoomAsync(
                PropertyId,
                1,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(room);

        // Act
        var result = await service.CheckInAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new CheckInRequest
            {
                IdentityVerified = true,
                RegistrationNotes = "Verified"
            });

        // Assert
        Assert.That(result.Success, Is.True);

        Assert.That(
            reservation.Status,
            Is.EqualTo(ReservationStatus.CheckedIn));

        Assert.That(room.Status, Is.EqualTo(RoomStatus.Occupied));

        repository.Verify(
            x => x.PersistLifecycleAsync(
                reservation,
                It.Is<IReadOnlyCollection<Room>>(
                    rooms => rooms.Single() == room),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ReleaseRoomAsync_rejects_releasing_the_last_current_room()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        InlineActionExecution(repository);

        var reservation = Reservation(
            status: ReservationStatus.Pending);

        reservation.ReservationRooms.Add(new ReservationRoom
        {
            Id = 9,
            RoomId = 1,
            IsCurrent = true
        });

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.ReleaseRoomAsync(
            Actor(),
            PropertyId,
            ReservationId,
            9);

        // Assert
        AssertFailure(result, 400, "ROOM_REQUIRED");

        Assert.That(
            reservation.ReservationRooms.Single().IsCurrent,
            Is.True);

        repository.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task CheckOutAsync_rejects_reservation_not_checked_in()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();

        var reservation = Reservation(
            status: ReservationStatus.Confirmed);

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.CheckOutAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new CheckOutRequest());

        // Assert
        AssertFailure(result, 409, "INVALID_STATE_TRANSITION");

        repository.Verify(
            x => x.PersistLifecycleAsync(
                It.IsAny<Reservation>(),
                It.IsAny<IReadOnlyCollection<Room>>(),
                It.IsAny<IReadOnlyCollection<HousekeepingTask>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetStatusHistoryAsync_returns_not_found_without_history_mapping_when_missing()
    {
        // Arrange
        var (service, repository, _) = CreateService();

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        // Act
        var result = await service.GetStatusHistoryAsync(
            Actor(),
            PropertyId,
            ReservationId);

        // Assert
        AssertFailure(result, 404, "RESERVATION_NOT_FOUND");
    }


    [Test]
    public async Task UpdateAsync_rejects_checked_out_reservation_without_saving()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();
        InlineExecution(repository);

        var reservation = Reservation(status: ReservationStatus.CheckedOut);

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.UpdateAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new UpdateReservationRequest
            {
                Adults = 2
            });

        // Assert
        AssertFailure(result, 409, "INVALID_STATE_TRANSITION");

        repository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            x => x.GetGuestAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task EarlyCheckoutAsync_rejects_reservation_that_is_not_checked_in()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);

        var reservation = Reservation(status: ReservationStatus.Confirmed);
        reservation.Property = ActiveProperty();

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.EarlyCheckoutAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new EarlyCheckoutRequest
            {
                CheckOutDate = CheckIn.AddDays(1),
                ManagerOverride = true,
                Notes = "Test"
            });

        // Assert
        AssertFailure(result, 409, "INVALID_STATE_TRANSITION");

        repository.Verify(
            x => x.PersistLifecycleAsync(
                It.IsAny<Reservation>(),
                It.IsAny<IReadOnlyCollection<Room>>(),
                It.IsAny<IReadOnlyCollection<HousekeepingTask>?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task ExtendCheckoutAsync_rejects_a_date_not_later_than_current_checkout()
    {
        // Arrange
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);

        var reservation = Reservation(status: ReservationStatus.CheckedIn);
        reservation.Property = ActiveProperty();
        reservation.ReservationRooms.Add(new ReservationRoom
        {
            Id = 9,
            RoomId = 1,
            Adults = 1,
            Children = 0,
            IsCurrent = true
        });

        repository
            .Setup(x => x.GetByIdAsync(
                PropertyId,
                ReservationId,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        // Act
        var result = await service.ExtendCheckoutAsync(
            Actor(),
            PropertyId,
            ReservationId,
            new ExtendCheckoutRequest
            {
                NewCheckOutDate = reservation.CheckOutDate.AddDays(-1),
                Reason = "Invalid extension"
            });

        // Assert
        AssertFailure(result, 422, "INVALID_EXTENSION_DATE");

        repository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            x => x.GetRoomAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AssignRoomAsync_rejects_actor_without_reservation_permission()
    {
        // Arrange
        var (service, repository, _) = CreateService();
        InlineActionExecution(repository);

        // CreateService configures no active role for the actor.
        // Therefore authorization must stop before loading the reservation.
        var request = new AssignRoomRequest
        {
            Rooms = new List<AssignRoomItemRequest>
            {
                new()
                {
                    RoomId = 1,
                    RatePlanId = 1
                }
            }
        };

        // Act
        var result = await service.AssignRoomAsync(
            Actor(),
            PropertyId,
            ReservationId,
            request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(403));

        repository.Verify(
            x => x.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task UpdateAsync_replaces_room_and_recalculates_all_amounts()
    {
        var (service, repository, _) = CreateServiceWithManager();
        InlineExecution(repository);
        var oldRoom = RoomWithType(1, 3, 100m);
        var newRoom = RoomWithType(2, 3, 100m);
        var reservation = Reservation(ReservationStatus.Pending);
        reservation.Folio = new Folio { Status = FolioStatus.Open, Total = 360m, Balance = 360m };
        reservation.ReservationRooms.Add(new ReservationRoom
        {
            Id = 11,
            RoomId = 1,
            Adults = 1,
            Children = 0,
            Nights = 3,
            NightlyRate = 100m,
            BaseAmount = 300m,
            TotalAmount = 300m,
            IsCurrent = true,
            Room = oldRoom
        });

        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, false, It.IsAny<CancellationToken>())).ReturnsAsync((Reservation?)null);
        repository.Setup(x => x.GetActiveTaxesAsync(PropertyId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Tax>());
        repository.Setup(x => x.GetRoomAsync(PropertyId, 2, false, It.IsAny<CancellationToken>())).ReturnsAsync(newRoom);
        repository.Setup(x => x.GetAvailableRoomsAsync(PropertyId, CheckIn, CheckOut, 2, 1, 3, ReservationId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { newRoom });
        repository.Setup(x => x.GetRatePlanAsync(PropertyId, 3, 1, CheckIn, It.IsAny<CancellationToken>())).ReturnsAsync(RatePlanWithVersion(1, 3, 150m));
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.UpdateAsync(Actor(), PropertyId, ReservationId, new UpdateReservationRequest
        {
            Rooms = new List<UpdateReservationRoomRequest>
            {
                new() { RoomId = 2, RoomTypeId = 3, RatePlanId = 1, Adults = 2, Children = 1, DiscountAmount = 20m, FeeAmount = 10m }
            }
        });

        Assert.That(result.Success, Is.True);
        Assert.That(reservation.TotalAmount, Is.EqualTo(440m));
        Assert.That(reservation.BaseAmount, Is.EqualTo(450m));
        Assert.That(reservation.TotalDiscountAmount, Is.EqualTo(20m));
        Assert.That(reservation.TotalFeeAmount, Is.EqualTo(10m));
        Assert.That(reservation.Adults, Is.EqualTo(2));
        Assert.That(reservation.Children, Is.EqualTo(1));
        Assert.That(reservation.ReservationRooms.Count(x => x.IsCurrent), Is.EqualTo(1));
        Assert.That(reservation.ReservationRooms.Single(x => x.IsCurrent).Nights, Is.EqualTo(3));
        Assert.That(reservation.ReservationRooms.Single(x => x.IsCurrent).BaseAmount, Is.EqualTo(450m));
        Assert.That(reservation.Folio.Total, Is.EqualTo(440m));
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CancelAsync_calculates_fee_refund_and_balance()
    {
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);
        var property = ActiveProperty();
        property.Settings = new PropertySettings { DefaultCurrency = "USD" };
        var reservation = Reservation(ReservationStatus.Pending);
        reservation.Property = property;
        reservation.Guest = new Guest { Id = 10, IsActive = true };
        reservation.TotalAmount = 500m;
        reservation.Folio = new Folio
        {
            Status = FolioStatus.Open,
            Total = 500m,
            Payments = new List<Payment>
            {
                new() { Amount = 300m, Status = PaymentStatus.Paid, Refunds = new List<Refund>() }
            }
        };
        reservation.CancellationPolicySnapshot = new ReservationCancellationPolicySnapshot
        {
            FreeCancellationHours = int.MaxValue,
            CancellationFeePercentage = 10m,
            FixedCancellationFee = 20m,
            IsNonRefundable = false,
            CutoffHours = 0
        };

        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetPropertyAsync(PropertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        repository.Setup(x => x.PersistTransitionAsync(reservation, It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.CancelAsync(Actor(), PropertyId, ReservationId, new CancelReservationRequest
        {
            Reason = "Guest cancellation",
            OverridePolicy = false
        });

        Assert.That(result.Success, Is.True);
        Assert.That(reservation.Status, Is.EqualTo(ReservationStatus.Cancelled));
        Assert.That(result.Data!.CancellationFee, Is.EqualTo(70m));
        Assert.That(result.Data.PaidAmount, Is.EqualTo(300m));
        Assert.That(result.Data.RefundableAmount, Is.EqualTo(230m));
        Assert.That(result.Data.HeldAmount, Is.EqualTo(70m));
        Assert.That(reservation.Folio.Items.Single(x => x.Description == "Cancellation fee").Amount, Is.EqualTo(70m));
        repository.Verify(x => x.PersistTransitionAsync(reservation, It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task NoShowAsync_calculates_noshow_charge_from_current_room_rate()
    {
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);
        var property = ActiveProperty();
        property.Settings = new PropertySettings { HotelDayClosingTime = TimeSpan.Zero };
        var reservation = Reservation(ReservationStatus.Confirmed);
        reservation.Property = property;
        reservation.Guest = new Guest { IsActive = true };
        reservation.TotalAmount = 360m;
        reservation.Folio = new Folio { Status = FolioStatus.Open, Total = 360m, Payments = new List<Payment>() };
        reservation.ReservationRooms.Add(new ReservationRoom { RoomId = 1, IsCurrent = true, NightlyRate = 120m });

        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetPropertyAsync(PropertyId, It.IsAny<CancellationToken>())).ReturnsAsync(property);
        repository.Setup(x => x.PersistTransitionAsync(reservation, It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.NoShowAsync(Actor(), PropertyId, ReservationId, new NoShowRequest
        {
            OverridePolicy = true,
            Reason = "Guest did not arrive"
        });

        Assert.That(result.Success, Is.True);
        Assert.That(reservation.Status, Is.EqualTo(ReservationStatus.NoShow));
        Assert.That(result.Data!.NoShowCharge, Is.EqualTo(120m));
        Assert.That(reservation.Folio.Items.Single(x => x.Description == "No-show charge").Amount, Is.EqualTo(120m));
        repository.Verify(x => x.PersistTransitionAsync(reservation, It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AssignRoomAsync_adds_room_and_recalculates_totals()
    {
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);
        var reservation = Reservation(ReservationStatus.Pending);
        reservation.Folio = new Folio { Status = FolioStatus.Open, Total = 0m, Balance = 0m };
        var room = RoomWithType(5, 3, 100m);
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetRoomAsync(PropertyId, 5, false, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        repository.Setup(x => x.GetAvailableRoomsAsync(PropertyId, CheckIn, CheckOut, 2, 1, 3, ReservationId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { room });
        repository.Setup(x => x.GetRatePlanAsync(PropertyId, 3, 1, CheckIn, It.IsAny<CancellationToken>())).ReturnsAsync(RatePlanWithVersion(1, 3, 120m));
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.AssignRoomAsync(Actor(), PropertyId, ReservationId, new AssignRoomRequest
        {
            Reason = "Room selected",
            Rooms = new List<AssignRoomItemRequest> { new() { RoomId = 5, RatePlanId = 1, Adults = 2, Children = 1 } }
        });

        Assert.That(result.Success, Is.True);
        Assert.That(reservation.ReservationRooms.Single().Nights, Is.EqualTo(3));
        Assert.That(reservation.ReservationRooms.Single().NightlyRate, Is.EqualTo(120m));
        Assert.That(reservation.ReservationRooms.Single().BaseAmount, Is.EqualTo(360m));
        Assert.That(reservation.TotalAmount, Is.EqualTo(360m));
        Assert.That(reservation.Adults, Is.EqualTo(2));
        Assert.That(reservation.Children, Is.EqualTo(1));
        Assert.That(reservation.Folio!.Total, Is.EqualTo(360m));
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReleaseRoomAsync_removes_one_of_two_rooms_and_recalculates_totals()
    {
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);
        var reservation = Reservation(ReservationStatus.Pending);
        reservation.Folio = new Folio { Status = FolioStatus.Open, Total = 600m, Balance = 600m };
        reservation.ReservationRooms.Add(new ReservationRoom { Id = 9, RoomId = 1, Adults = 1, IsCurrent = true, Nights = 3, NightlyRate = 100m, BaseAmount = 300m, TotalAmount = 300m });
        reservation.ReservationRooms.Add(new ReservationRoom { Id = 10, RoomId = 2, Adults = 1, IsCurrent = true, Nights = 3, NightlyRate = 100m, BaseAmount = 300m, TotalAmount = 300m });
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.ReleaseRoomAsync(Actor(), PropertyId, ReservationId, 9);

        Assert.That(result.Success, Is.True);
        Assert.That(reservation.ReservationRooms.Single(x => x.Id == 9).IsCurrent, Is.False);
        Assert.That(reservation.ReservationRooms.Single(x => x.Id == 10).IsCurrent, Is.True);
        Assert.That(reservation.TotalAmount, Is.EqualTo(300m));
        Assert.That(reservation.Adults, Is.EqualTo(1));
        Assert.That(reservation.Folio!.Total, Is.EqualTo(300m));
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CheckOutAsync_closes_folio_releases_room_and_creates_housekeeping_task()
    {
        var (service, repository, _) = CreateServiceWithManager();
        var property = ActiveProperty();
        property.Settings = new PropertySettings { RequireFullPaymentBeforeCheckOut = false, RequireInspectionBeforeAvailable = false };
        var reservation = Reservation(ReservationStatus.CheckedIn);
        reservation.Property = property;
        reservation.Folio = new Folio { Status = FolioStatus.Open, Total = 360m, Balance = 0m };
        reservation.ReservationRooms.Add(new ReservationRoom { RoomId = 1, IsCurrent = true });
        var room = RoomWithType(1, 3, 100m);
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetRoomAsync(PropertyId, 1, true, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        repository.Setup(x => x.PersistLifecycleAsync(reservation, It.IsAny<IReadOnlyCollection<Room>>(), It.IsAny<IReadOnlyCollection<HousekeepingTask>?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.CheckOutAsync(Actor(), PropertyId, ReservationId, new CheckOutRequest { Notes = "Checked out" });

        Assert.That(result.Success, Is.True);
        Assert.That(reservation.Status, Is.EqualTo(ReservationStatus.CheckedOut));
        Assert.That(room.Status, Is.EqualTo(RoomStatus.Available));
        Assert.That(reservation.ReservationRooms.Single().IsCurrent, Is.False);
        Assert.That(reservation.Folio.Status, Is.EqualTo(FolioStatus.Closed));
        repository.Verify(x => x.PersistLifecycleAsync(reservation, It.Is<IReadOnlyCollection<Room>>(r => r.Count == 1 && r.Single() == room), It.Is<IReadOnlyCollection<HousekeepingTask>?>(t => t != null && t.Count == 1 && t.Single().RoomId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task EarlyCheckoutAsync_recalculates_historical_rate_and_refund()
    {
        var (service, repository, user) = CreateServiceWithManager();
        InlineActionExecution(repository);
        var today = DateTime.UtcNow.Date;
        var property = ActiveProperty();
        property.Settings = new PropertySettings { RequireInspectionBeforeAvailable = false, RequireFullPaymentBeforeCheckOut = false };
        var reservation = new Reservation { Id = ReservationId, PropertyId = PropertyId, GuestId = 10, Status = ReservationStatus.CheckedIn, CheckInDate = today.AddDays(-1), CheckOutDate = today.AddDays(2), Property = property, Folio = new Folio { Status = FolioStatus.Open, Total = 360m, Payments = new List<Payment> { new() { Amount = 300m, Status = PaymentStatus.Paid, Refunds = new List<Refund>() } } } };
        var assignment = new ReservationRoom { RoomId = 1, IsCurrent = true, Nights = 3, NightlyRate = 120m, BaseAmount = 360m, TotalAmount = 360m, RatePlanSnapshot = new ReservationRoomRatePlanSnapshot { NightlyRate = 120m, IsRefundable = true } };
        reservation.ReservationRooms.Add(assignment);
        var room = RoomWithType(1, 3, 999m);
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetRoomAsync(PropertyId, 1, true, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        repository.Setup(x => x.PersistLifecycleAsync(reservation, It.IsAny<IReadOnlyCollection<Room>>(), It.IsAny<IReadOnlyCollection<HousekeepingTask>?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.EarlyCheckoutAsync(Actor(), PropertyId, ReservationId, new EarlyCheckoutRequest { CheckOutDate = today, Notes = "Early departure" });

        Assert.That(result.Success, Is.True);
        Assert.That(assignment.Nights, Is.EqualTo(1));
        Assert.That(assignment.NightlyRate, Is.EqualTo(120m));
        Assert.That(assignment.BaseAmount, Is.EqualTo(120m));
        Assert.That(reservation.TotalAmount, Is.EqualTo(120m));
        Assert.That(result.Data!.PaidAmount, Is.EqualTo(300m));
        Assert.That(result.Data.RefundableAmount, Is.EqualTo(240m));
        Assert.That(reservation.Status, Is.EqualTo(ReservationStatus.CheckedOut));
        Assert.That(room.Status, Is.EqualTo(RoomStatus.Available));
        Assert.That(assignment.IsCurrent, Is.False);
        repository.Verify(x => x.PersistLifecycleAsync(reservation, It.IsAny<IReadOnlyCollection<Room>>(), It.IsAny<IReadOnlyCollection<HousekeepingTask>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ExtendCheckoutAsync_recalculates_amount_for_extra_nights()
    {
        var (service, repository, _) = CreateServiceWithManager();
        InlineActionExecution(repository);
        var reservation = Reservation(ReservationStatus.CheckedIn);
        reservation.Property = ActiveProperty();
        reservation.Folio = new Folio { Status = FolioStatus.Open, Total = 240m, Balance = 240m };
        var assignment = new ReservationRoom { RoomId = 1, IsCurrent = true, Adults = 1, Children = 0, Nights = 2, NightlyRate = 120m, BaseAmount = 240m, TotalAmount = 240m, RatePlanSnapshot = new ReservationRoomRatePlanSnapshot { NightlyRate = 120m } };
        reservation.ReservationRooms.Add(assignment);
        var room = RoomWithType(1, 3, 999m);
        var newCheckout = CheckOut.AddDays(2);
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, true, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);
        repository.Setup(x => x.GetRoomAsync(PropertyId, 1, true, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        repository.Setup(x => x.GetAvailableRoomsAsync(PropertyId, CheckOut, newCheckout, 1, 0, 3, ReservationId, It.IsAny<CancellationToken>())).ReturnsAsync(new[] { room });
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await service.ExtendCheckoutAsync(Actor(), PropertyId, ReservationId, new ExtendCheckoutRequest { NewCheckOutDate = newCheckout, Reason = "Extra nights" });

        Assert.That(result.Success, Is.True);
        Assert.That(assignment.Nights, Is.EqualTo(5));
        Assert.That(assignment.NightlyRate, Is.EqualTo(120m));
        Assert.That(assignment.BaseAmount, Is.EqualTo(600m));
        Assert.That(assignment.TotalAmount, Is.EqualTo(600m));
        Assert.That(reservation.TotalAmount, Is.EqualTo(600m));
        Assert.That(reservation.Folio!.Total, Is.EqualTo(600m));
        Assert.That(result.Data!.Status, Is.EqualTo(ReservationStatus.CheckedIn));
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetStatusHistoryAsync_returns_ordered_history_values()
    {
        var (service, repository, _) = CreateService();
        var reservation = Reservation(ReservationStatus.CheckedIn);
        reservation.StatusHistory.Add(new ReservationStatusHistory { Id = 2, FromStatus = ReservationStatus.Confirmed, ToStatus = ReservationStatus.CheckedIn, Reason = "Check in", ActorId = "actor-1", ChangedAt = DateTimeOffset.UtcNow.AddMinutes(1) });
        reservation.StatusHistory.Add(new ReservationStatusHistory { Id = 1, FromStatus = ReservationStatus.Pending, ToStatus = ReservationStatus.Confirmed, Reason = "Confirm", ActorId = "actor-1", ChangedAt = DateTimeOffset.UtcNow });
        repository.Setup(x => x.GetByIdAsync(PropertyId, ReservationId, false, It.IsAny<CancellationToken>())).ReturnsAsync(reservation);

        var result = await service.GetStatusHistoryAsync(Actor(), PropertyId, ReservationId);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Has.Count.EqualTo(2));
        Assert.That(result.Data![0].Id, Is.EqualTo(1));
        Assert.That(result.Data[0].ToStatus, Is.EqualTo(ReservationStatus.Confirmed));
        Assert.That(result.Data[1].Id, Is.EqualTo(2));
        Assert.That(result.Data[1].ToStatus, Is.EqualTo(ReservationStatus.CheckedIn));
    }

    private static (
        ReservationService Service,
        Mock<IReservationRepository> Repository,
        Mock<IUserRepository> User)
        CreateService()
    {
        var repository = new Mock<IReservationRepository>();
        var user = new Mock<IUserRepository>();

        user
            .Setup(x => x.GetRolesAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Role>());

        return (
            new ReservationService(user.Object, repository.Object),
            repository,
            user);
    }

    private static (
        ReservationService Service,
        Mock<IReservationRepository> Repository,
        Mock<IUserRepository> User)
        CreateServiceWithManager()
    {
        var setup = CreateService();

        setup.User
            .Setup(x => x.GetRolesAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Role
                {
                    Name = nameof(UserRole.Manager),
                    IsActive = true
                }
            });

        return setup;
    }

    private static (
        ReservationService Service,
        Mock<IReservationRepository> Repository,
        Mock<IUserRepository> User)
        CreateServiceWithReceptionist()
    {
        var setup = CreateService();

        setup.User
            .Setup(x => x.GetRolesAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Role
                {
                    Name = nameof(UserRole.Receptionist),
                    IsActive = true
                }
            });

        return setup;
    }

    private static User Actor() => new()
    {
        Id = "actor-1",
        PropertyId = PropertyId,
        IsActive = true
    };

    private static AvailabilityRequest AvailabilityRequest(
        int propertyId,
        DateTime checkIn,
        DateTime checkOut,
        int adults = 1,
        int children = 0,
        int? roomTypeId = null,
        int? excludedReservationId = null) => new()
        {
            PropertyId = propertyId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Adults = adults,
            Children = children,
            RoomTypeId = roomTypeId,
            ExcludedReservationId = excludedReservationId
        };

    private static CreateReservationRequest CreateRequest(
        params CreateReservationRoomRequest[] rooms) => new()
        {
            PropertyId = PropertyId,
            GuestId = 10,
            CheckInDate = CheckIn,
            CheckOutDate = CheckOut,
            CancellationPolicyId = 1,
            CancellationPolicyVersionId = 1,
            DepositPolicyId = 1,
            DepositPolicyVersionId = 1,
            createReservationRoomRequests = rooms.ToList()
        };

    private static Reservation Reservation(
        ReservationStatus status) => new()
        {
            Id = ReservationId,
            PropertyId = PropertyId,
            GuestId = 10,
            Status = status,
            CheckInDate = CheckIn,
            CheckOutDate = CheckOut,
            RowVersion = new byte[] { 1 }
        };

    private static Property ActiveProperty() => new()
    {
        ID = PropertyId,
        Status = PropertyStatus.Active
    };

    private static Room RoomWithType(
        int id,
        int typeId,
        decimal price) => new()
        {
            Id = id,
            RoomNumber = id.ToString(),
            RoomTypeId = typeId,
            PricePerNight = price,
            RoomType = new RoomType
            {
                Id = typeId,
                Name = "Standard",
                BasePrice = 80m,
                MaxAdults = 4,
                MaxChildren = 3,
                property_id = PropertyId
            }
        };

    private static RatePlan RatePlanWithVersion(
        int id,
        int roomTypeId,
        decimal price) => new()
        {
            Id = id,
            RoomTypeId = roomTypeId,
            PropertyId = PropertyId,
            Name = "Standard Plan",
            Type = RatePlanType.Standard,
            ValidFrom = CheckIn.AddDays(-1),
            Versions = new List<RatePlanVersion>
        {
            new()
            {
                Id = 1,
                RatePlanId = id,
                VersionNumber = 1,
                Price = price,
                ValidFrom = CheckIn.AddDays(-1),
                IsActive = true
            }
        }
        };

    private static void SetupActivePropertyAndGuest(
        Mock<IReservationRepository> repository)
    {
        repository
            .Setup(x => x.GetPropertyAsync(
                PropertyId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveProperty());

        repository
            .Setup(x => x.GetGuestAsync(
                PropertyId,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guest
            {
                Id = 10,
                IsActive = true
            });

        repository
            .Setup(x => x.GetCancellationPolicyVersionAsync(
                PropertyId, 1, 1, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancellationPolicyVersion
            {
                Id = 1,
                CancellationPolicyId = 1,
                Version = 1,
                ValidFrom = DateTimeOffset.MinValue,
                ValidTo = null,
                Rules = "{}",
                FreeCancellationHours = 24,
                CancellationFeePercentage = 10m,
                FixedCancellationFee = 20m,
                CutoffHours = 24
            });

        repository
            .Setup(x => x.GetDepositPolicyVersionAsync(
                PropertyId, 1, 1, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DepositPolicyVersion
            {
                Id = 1,
                DepositId = 1,
                Version = 1,
                Name = "Deposit",
                Type = DepositType.FixedAmount,
                Amount = 0m,
                Percentage = 0m,
                ValidFrom = DateTimeOffset.MinValue,
                ValidTo = null,
                IsActive = true
            });
    }

    private static void InlineExecution(
        Mock<IReservationRepository> repository)
    {
        repository
            .Setup(x => x.ExecuteSerializableAsync(
                It.IsAny<Func<Task<ResponseStatus<ReservationResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<ResponseStatus<ReservationResponse>>> operation,
                CancellationToken _) => operation());

        InlineActionExecution(repository);
    }

    private static void InlineActionExecution(
        Mock<IReservationRepository> repository)
    {
        repository
            .Setup(x => x.ExecuteSerializableAsync(
                It.IsAny<Func<Task<ResponseStatus<ReservationActionResponse>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((
                Func<Task<ResponseStatus<ReservationActionResponse>>> operation,
                CancellationToken _) => operation());
    }

    private static void AssertFailure<T>(
        ResponseStatus<T> result,
        int statusCode,
        string code)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(statusCode));
        Assert.That(result.Code, Is.EqualTo(code));
    }
}
