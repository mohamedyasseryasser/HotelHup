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
        // The requirements define seven least-privilege roles. Admin is the
        // only role that receives every permission; all other roles are
        // synchronized to the explicit matrix below on every seed run.
        var roleDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRole.Admin.ToString()] = "Full system administration and configuration.",
            [UserRole.Manager.ToString()] = "Hotel operations, pricing, approvals, and reports.",
            [UserRole.Receptionist.ToString()] = "Front desk reservations, check-in/out, rooms, and services.",
            [UserRole.Housekeeper.ToString()] = "Assigned room cleaning and housekeeping tasks.",
            [UserRole.Accountant.ToString()] = "Folios, payments, refunds, shift closing, and financial reports.",
          };

        var rolePermissionNames = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [UserRole.Manager.ToString()] =
            [
                Permissions.Guests.Read, Permissions.Guests.Create, Permissions.Guests.Update,
                Permissions.Reservations.Read, Permissions.Reservations.Create, Permissions.Reservations.Update,
                Permissions.Reservations.Cancel, Permissions.Reservations.Modify,
                Permissions.Rooms.Read, Permissions.Rooms.Create, Permissions.Rooms.Update,
                Permissions.Rooms.ChangeStatus, Permissions.Rooms.Assign,
                Permissions.RoomTypes.Read, Permissions.RoomTypes.Create, Permissions.RoomTypes.Update,
                Permissions.RoomTypes.Deactivate,
                Permissions.RatePlans.Read, Permissions.RatePlans.Create, Permissions.RatePlans.Update,
                Permissions.RatePlans.Activate, Permissions.RatePlans.Deactivate,
                Permissions.RateRules.Read, Permissions.RateRules.Create, Permissions.RateRules.Update,
                Permissions.RateRules.Delete,
                Permissions.Folios.Read, Permissions.Folios.Create, Permissions.Folios.Update,
                Permissions.Folios.AddCharge, Permissions.Folios.Transfer, Permissions.Folios.Close,
                Permissions.Folios.Reopen,
                Permissions.Payments.Read, Permissions.Payments.Create, Permissions.Payments.Void,
                 Permissions.Refunds.Read, Permissions.Refunds.Create, Permissions.Refunds.Approve,
                Permissions.Services.Read, Permissions.Services.Create, Permissions.Services.Update,
                Permissions.Services.Deactivate, Permissions.Services.AddToFolio,
                Permissions.Housekeeping.Read, Permissions.Housekeeping.Assign,
                Permissions.Housekeeping.Start, Permissions.Housekeeping.Complete,
                Permissions.Housekeeping.ReportIssue,
                Permissions.Expenses.Read, Permissions.Expenses.Create, Permissions.Expenses.Update,
                Permissions.Expenses.Void,
                Permissions.Reports.Read, Permissions.Reports.Export, Permissions.AuditLogs.Read,
                Permissions.Properties.Read, Permissions.Properties.Update,
                
            ],
            [UserRole.Receptionist.ToString()] =
            [
                Permissions.Guests.Read, Permissions.Guests.Create, Permissions.Guests.Update,
                Permissions.Reservations.Read, Permissions.Reservations.Create, Permissions.Reservations.Update,
                Permissions.Reservations.Cancel, Permissions.Reservations.Modify,
                Permissions.Rooms.Read, Permissions.Rooms.Assign, Permissions.RoomTypes.Read,
                Permissions.RatePlans.Read,
                Permissions.Folios.Read, Permissions.Folios.AddCharge,
                Permissions.Services.Read, Permissions.Services.AddToFolio,
                Permissions.Housekeeping.Read, Permissions.Housekeeping.ReportIssue
            ],
            [UserRole.Housekeeper.ToString()] =
            [
                Permissions.Rooms.Read,
                Permissions.Housekeeping.Read, Permissions.Housekeeping.Start,
                Permissions.Housekeeping.Complete, Permissions.Housekeeping.ReportIssue
            ],
            [UserRole.Accountant.ToString()] =
            [
                Permissions.Reservations.Read, Permissions.Guests.Read,
                Permissions.Folios.Read, Permissions.Folios.Update, Permissions.Folios.Close,
                Permissions.Folios.Reopen,
                Permissions.Payments.Read, Permissions.Payments.Create, Permissions.Payments.Void,
                 Permissions.Refunds.Read, Permissions.Refunds.Create, Permissions.Refunds.Approve,
                Permissions.Expenses.Read, Permissions.Expenses.Create, Permissions.Expenses.Update,
                Permissions.Expenses.Void,
                Permissions.Reports.Read, Permissions.Reports.Export
            ],
          
           
        };

        foreach (var (roleName, description) in roleDescriptions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                var result = await roleManager.CreateAsync(new Role
                {
                    Name = roleName,
                    Description = description,
                    IsActive = true
                });
                EnsureIdentitySuccess(result, $"create role {roleName}");
            }
            else if (role.Description != description || !role.IsActive)
            {
                role.Description = description;
                role.IsActive = true;
                EnsureIdentitySuccess(await roleManager.UpdateAsync(role), $"update role {roleName}");
            }
        }

        var permissions = await context.Permissions.ToListAsync(cancellationToken);
        var permissionsByName = permissions.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var roles = await context.Roles
            .Where(role => roleDescriptions.Keys.Contains(role.Name!))
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            var desiredNames = string.Equals(role.Name, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase)
                ? permissions.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : rolePermissionNames[role.Name!].ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unknownNames = desiredNames.Where(name => !permissionsByName.ContainsKey(name)).ToArray();
            if (unknownNames.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Role '{role.Name}' references unknown permissions: {string.Join(", ", unknownNames)}");
            }

            var existingLinks = await context.RolePermissions
                .Include(item => item.Permission)
                .Where(item => item.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            context.RolePermissions.RemoveRange(existingLinks.Where(link =>
                !desiredNames.Contains(link.Permission.Name)));

            var existingPermissionIds = existingLinks
                .Where(link => desiredNames.Contains(link.Permission.Name))
                .Select(link => link.PermissionId)
                .ToHashSet();

            foreach (var permissionName in desiredNames)
            {
                var permission = permissionsByName[permissionName];
                if (!existingPermissionIds.Contains(permission.Id))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
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

        await EnsureUserAsync(
            userManager,
            "receptionist@hotelhup.local",
            "Receptionist@123",
            "HotelHup Demo Receptionist",
            property.ID,
            UserRole.Receptionist.ToString(),
            cancellationToken);

        await EnsureUserAsync(
            userManager,
            "housekeeper@hotelhup.local",
            "Housekeeper@123",
            "HotelHup Demo Housekeeper",
            property.ID,
            UserRole.Housekeeper.ToString(),
            cancellationToken);

        await EnsureUserAsync(
            userManager,
            "accountant@hotelhup.local",
            "Accountant@123",
            "HotelHup Demo Accountant",
            property.ID,
            UserRole.Accountant.ToString(),
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
