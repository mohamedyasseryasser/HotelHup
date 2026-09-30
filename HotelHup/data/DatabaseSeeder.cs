using System.Reflection;
using HotelHup.APPLICATION.Constant;
using HotelHup.CORE.Entities;
using HotelHup.CORE.Enums;
using HotelHup.INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelHup.Data;

public static class DatabaseSeeder
{
    public const string AdminUserName = "admin@hotelhup.local";
    public const string AdminPassword = "Admin@123";
    public const string ManagerUserName = "manager@hotelhup.local";
    public const string ManagerPassword = "Manager@123";
    public const string DemoPropertyCode = "DEMO-HOTEL";

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var serviceProvider = scope.ServiceProvider;
        var context = serviceProvider.GetRequiredService<hotelhupContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();

        await context.Database.MigrateAsync(cancellationToken);
        await SeedPermissionsAsync(context, cancellationToken);
        await SeedRolesAsync(roleManager, context, cancellationToken);

        var property = await SeedPropertyAsync(context, cancellationToken);
        await SeedUsersAsync(userManager, property, cancellationToken);
        var roomType = await SeedRoomTypeAsync(context, property, cancellationToken);
        var room = await SeedRoomAsync(context, property, roomType, cancellationToken);
        await SeedServicesAsync(context, property, cancellationToken);
        var guest = await SeedGuestAsync(context, property, cancellationToken);
        var reservation = await SeedReservationAsync(context, property, guest, cancellationToken);
        await SeedFolioAsync(context, reservation, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        Console.WriteLine("HotelHup demo seed completed.");
        Console.WriteLine($"Admin login: {AdminUserName} / {AdminPassword}");
        Console.WriteLine($"Manager login: {ManagerUserName} / {ManagerPassword}");
        Console.WriteLine($"Demo PropertyId: {property.ID}");
        Console.WriteLine($"Demo RoomId: {room.Id}");
        Console.WriteLine($"Demo ReservationId: {reservation.Id}");
        Console.WriteLine($"Demo FolioId: {reservation.Folio?.id}");
    }

    private static async Task SeedPermissionsAsync(
        hotelhupContext context,
        CancellationToken cancellationToken)
    {
        var permissionDefinitions = typeof(Permissions)
            .GetNestedTypes(BindingFlags.Public)
            .SelectMany(resourceType => resourceType
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(string))
                .Select(field => new
                {
                    Name = (string)field.GetValue(null)!,
                    Resource = resourceType.Name,
                    Action = field.Name
                }))
            .ToList();

        var existingNames = (await context.Permissions
       .Select(permission => permission.Name)
       .ToListAsync(cancellationToken))
       .ToHashSet(StringComparer.OrdinalIgnoreCase);


        foreach (var definition in permissionDefinitions)
        {
            if (existingNames.Contains(definition.Name))
            {
                continue;
            }

            context.Permissions.Add(new Permission
            {
                Name = definition.Name,
                Resource = definition.Resource,
                Action = definition.Action,
                Description = $"{definition.Action} permission for {definition.Resource}."
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(
        RoleManager<Role> roleManager,
        hotelhupContext context,
        CancellationToken cancellationToken)
    {
        var roleNames = new[]
        {
            UserRole.Admin.ToString(),
            UserRole.Manager.ToString(),
            UserRole.Receptionist.ToString()
        };

        foreach (var roleName in roleNames)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new Role
            {
                Name = roleName,
                Description = $"Demo {roleName} role.",
                IsActive = true
            });

            EnsureIdentitySuccess(result, $"create role {roleName}");
        }

        var permissions = await context.Permissions.ToListAsync(cancellationToken);
        var roles = await context.Roles
            .Where(role => roleNames.Contains(role.Name!))
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            var existingPermissionIds =
                (await context.RolePermissions
                    .Where(item => item.RoleId == role.Id)
                    .Select(item => item.PermissionId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            foreach (var permission in permissions)
            {
                if (existingPermissionIds.Contains(permission.Id))
                {
                    continue;
                }

                context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }


        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Property> SeedPropertyAsync(
        hotelhupContext context,
        CancellationToken cancellationToken)
    {
        var property = await context.Properties
            .Include(item => item.Settings)
            .SingleOrDefaultAsync(
                item => item.Code == DemoPropertyCode,
                cancellationToken);

        if (property is null)
        {
            property = new Property
            {
                Name = "HotelHup Demo Hotel",
                Code = DemoPropertyCode,
                Description = "Seeded property for manual Swagger testing.",
                Status = PropertyStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "seed"
            };
            context.Properties.Add(property);
            await context.SaveChangesAsync(cancellationToken);
        }

        if (property.Settings is null)
        {
            property.Settings = new PropertySettings
            {
                PropertyId = property.ID,
                DefaultCurrency = "USD",
                TimeZone = "UTC",
                CheckInTime = new TimeSpan(14, 0, 0),
                CheckOutTime = new TimeSpan(12, 0, 0),
                AllowEarlyCheckIn = true,
                RequireDepositForReservation = false,
                RequireFullPaymentBeforeCheckOut = false,
                AllowOverpayment = false,
                RequireInspectionBeforeAvailable = false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "seed"
            };
            await context.SaveChangesAsync(cancellationToken);
        }

        return property;
    }

    private static async Task SeedUsersAsync(
        UserManager<User> userManager,
        Property property,
        CancellationToken cancellationToken)
    {
        await EnsureUserAsync(
            userManager,
            AdminUserName,
            AdminPassword,
            "HotelHup Demo Administrator",
            null,
            UserRole.Admin.ToString(),
            cancellationToken);

        await EnsureUserAsync(
            userManager,
            ManagerUserName,
            ManagerPassword,
            "HotelHup Demo Manager",
            property.ID,
            UserRole.Manager.ToString(),
            cancellationToken);
    }

    private static async Task EnsureUserAsync(
        UserManager<User> userManager,
        string userName,
        string password,
        string fullName,
        int? propertyId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new User
            {
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = userName,
                NormalizedEmail = userName.ToUpperInvariant(),
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true,
                PropertyId = propertyId,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureIdentitySuccess(createResult, $"create user {userName}");
        }
        else
        {
            var changed = false;
            if (!user.IsActive)
            {
                user.IsActive = true;
                changed = true;
            }

            if (user.PropertyId != propertyId)
            {
                user.PropertyId = propertyId;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                EnsureIdentitySuccess(updateResult, $"update user {userName}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            EnsureIdentitySuccess(roleResult, $"assign role {roleName} to {userName}");
        }
    }

    private static async Task<RoomType> SeedRoomTypeAsync(
        hotelhupContext context,
        Property property,
        CancellationToken cancellationToken)
    {
        var roomType = await context.RoomTypes
            .SingleOrDefaultAsync(
                item => item.property_id == property.ID && item.Name == "Deluxe Room",
                cancellationToken);

        if (roomType is not null)
        {
            return roomType;
        }

        roomType = new RoomType
        {
            Name = "Deluxe Room",
            Description = "Demo room type for Swagger reservation tests.",
            BasePrice = 150m,
            MaxAdults = 2,
            MaxChildren = 2,
            property_id = property.ID,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed"
        };
        context.RoomTypes.Add(roomType);
        await context.SaveChangesAsync(cancellationToken);
        return roomType;
    }

    private static async Task<Room> SeedRoomAsync(
        hotelhupContext context,
        Property property,
        RoomType roomType,
        CancellationToken cancellationToken)
    {
        var room = await context.Rooms
            .SingleOrDefaultAsync(
                item => item.property_id == property.ID && item.RoomNumber == "101",
                cancellationToken);

        if (room is not null)
        {
            return room;
        }

        room = new Room
        {
            RoomNumber = "101",
            Status = RoomStatus.Available,
            PricePerNight = 150m,
            IsActive = true,
            RoomTypeId = roomType.Id,
            property_id = property.ID,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed"
        };
        context.Rooms.Add(room);
        await context.SaveChangesAsync(cancellationToken);
        return room;
    }

    private static async Task SeedServicesAsync(
        hotelhupContext context,
        Property property,
        CancellationToken cancellationToken)
    {
        var services = new[]
        {
            new { Name = "Breakfast", Description = "Daily breakfast buffet.", Category = "Food & Beverage", Price = 30m, Tax = 4.50m, IsActive = true },
            new { Name = "Laundry", Description = "Standard laundry service.", Category = "Housekeeping", Price = 20m, Tax = 3m, IsActive = true },
            new { Name = "Airport Transfer", Description = "One-way airport transfer.", Category = "Transportation", Price = 45m, Tax = 6.75m, IsActive = true },
            new { Name = "Old Spa Offer", Description = "Inactive service for negative test cases.", Category = "Wellness", Price = 60m, Tax = 9m, IsActive = false }
        };

        foreach (var item in services)
        {
            var exists = await context.Services.AnyAsync(
                service => service.propertyid == property.ID && service.Name == item.Name,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            context.Services.Add(new Service
            {
                Name = item.Name,
                Description = item.Description,
                RevenueCategory = item.Category,
                Price = item.Price,
                Tax = item.Tax,
                IsActive = item.IsActive,
                propertyid = property.ID,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = "seed"
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Guest> SeedGuestAsync(
        hotelhupContext context,
        Property property,
        CancellationToken cancellationToken)
    {
        var guest = await context.Guests
            .SingleOrDefaultAsync(
                item => item.propertyid == property.ID && item.Email == "guest.one@hotelhup.local",
                cancellationToken);

        if (guest is not null)
        {
            return guest;
        }

        guest = new Guest
        {
            FirstName = "John",
            LastName = "Guest",
            Email = "guest.one@hotelhup.local",
            Phone = "+201000000001",
            IdentityType = "Passport",
            IdentityNumber = "DEMO-PASSPORT-001",
            NationalId = "DEMO-NATIONAL-001",
            nationality = "Egyptian",
            Address = "Cairo, Egypt",
            IsActive = true,
            propertyid = property.ID,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed"
        };
        context.Guests.Add(guest);
        await context.SaveChangesAsync(cancellationToken);
        return guest;
    }

    private static async Task<Reservation> SeedReservationAsync(
        hotelhupContext context,
        Property property,
        Guest guest,
        CancellationToken cancellationToken)
    {
        var reservation = await context.Reservations
            .SingleOrDefaultAsync(
                item => item.PropertyId == property.ID && item.GuestId == guest.Id,
                cancellationToken);

        if (reservation is not null)
        {
            return reservation;
        }

        var checkIn = DateTime.UtcNow.Date.AddDays(1);
        var checkOut = checkIn.AddDays(2);
        reservation = new Reservation
        {
            GuestId = guest.Id,
            PropertyId = property.ID,
            Status = ReservationStatus.Confirmed,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            Adults = 1,
            Children = 0,
            Source = BookingSource.Direct,
            BaseAmount = 300m,
            TotalDiscountAmount = 0m,
            TotalTaxAmount = 0m,
            TotalFeeAmount = 0m,
            TotalAmount = 300m,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed"
        };
        context.Reservations.Add(reservation);
        await context.SaveChangesAsync(cancellationToken);
        return reservation;
    }

    private static async Task SeedFolioAsync(
        hotelhupContext context,
        Reservation reservation,
        CancellationToken cancellationToken)
    {
        var exists = await context.Folios
            .AnyAsync(item => item.ReservationId == reservation.Id, cancellationToken);

        if (exists)
        {
            return;
        }

        context.Folios.Add(new Folio
        {
            ReservationId = reservation.Id,
            Status = FolioStatus.Open,
            Currency = "USD",
            Total = 0m,
            Balance = 0m,
            CreditBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "seed"
        });
    }

    private static void EnsureIdentitySuccess(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Could not {operation}. {errors}");
    }
}
