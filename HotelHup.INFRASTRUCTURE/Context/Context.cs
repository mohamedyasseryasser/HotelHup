using HotelHup.CORE.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.INFRASTRUCTURE.Context
{
    public class hotelhupContext : IdentityDbContext<User>
    {

        public hotelhupContext(DbContextOptions<hotelhupContext> options) : base(options)
        {

        }
        public DbSet<Property> Properties => Set<Property>();

        public DbSet<RoomType> RoomTypes => Set<RoomType>();

        public DbSet<Room> Rooms => Set<Room>();

        public DbSet<Guest> Guests => Set<Guest>();

        public DbSet<Reservation> Reservations => Set<Reservation>();

        public DbSet<ReservationRoom> ReservationRooms => Set<ReservationRoom>();

        public DbSet<RatePlan> RatePlans => Set<RatePlan>();

        public DbSet<Folio> Folios => Set<Folio>();

        public DbSet<FolioItem> FolioItems => Set<FolioItem>();

        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<Refund> Refunds => Set<Refund>();

        public DbSet<Service> Services => Set<Service>();

        public DbSet<HousekeepingTask> HousekeepingTasks => Set<HousekeepingTask>();

        public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();

        public DbSet<Permission> Permissions => Set<Permission>();

        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //user :
            //each user belong to one property
            //each user has alot of auditlogs and alot of refreshtoken

            //property :
            builder.Entity<Property>(entity =>
            {
                entity.HasKey(p => p.ID);
                entity.Property(x => x.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.Currency)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(x => x.TimeZone)
                    .HasMaxLength(100)
                    .IsRequired();
            });
            //roomtype

            builder.Entity<RoomType>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.BasePrice)
                    .HasPrecision(18, 2);

                entity.HasIndex(x => x.Name)
                    .IsClustered(false).IsUnique();

                 

                entity.HasOne(x => x.Property)
                    .WithMany(x => x.RoomTypes)
                    .HasForeignKey(x => x.property_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //room
            builder.Entity<Room>(entity =>
            {
          
                entity.HasKey(x => x.Id)
                    .IsClustered();

                entity.Property(x => x.RoomNumber)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(x => x.PricePerNight)
                    .HasPrecision(10, 2);
                
                entity.HasIndex(x => x.RoomNumber)
                    .IsUnique()
                    .IsClustered(false);
               
                entity.HasOne(x => x.RoomType)
                    .WithMany(x => x.Rooms)
                    .HasForeignKey(x => x.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Property)
                    .WithMany(x => x.Rooms)
                    .HasForeignKey(x => x.property_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //Guest
            builder.Entity<Guest>(entity =>
            {
                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Email)
                    .HasMaxLength(256);

                entity.Property(x => x.Phone)
                    .HasMaxLength(30);

                entity.Property(x => x.IdentityNumber)
                    .HasMaxLength(100);

                entity.HasIndex(x => x.Email);

                entity.HasIndex(x => x.Phone);
            });
            //reservation
            builder.Entity<Reservation>(entity =>
            {
                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DiscountAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TaxAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.FeeAmount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Guest)
                    .WithMany(x => x.Reservations)
                    .HasForeignKey(x => x.GuestId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Property)
                    .WithMany(x => x.Reservations)
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //reservationroom
            builder.Entity<ReservationRoom>(entity =>
            {
                entity.Property(x => x.NightlyRate)
                    .HasPrecision(18, 2);

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Reservation)
                    .WithMany(x => x.ReservationRooms)
                    .HasForeignKey(x => x.ReservationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Room)
                    .WithMany(x => x.ReservationRoom)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //rateplane

            builder.Entity<RatePlan>(entity =>
            {
               
                entity.HasKey(x => x.id).IsClustered(true);
                entity.Property(x => x.Price)
                    .HasPrecision(18, 2);

                 entity.Property(x => x.Name)
                    .HasMaxLength(100)
                    .IsRequired();


                 entity.Property(x => x.Rules)
                    .HasMaxLength(2000);

                // Dates
                entity.Property(x => x.ValidFrom)
                    .IsRequired();

                entity.Property(x => x.ValidTo)
                    .IsRequired(false);


                // Active
                entity.Property(x => x.IsActive)
                    .IsRequired();


                entity.HasOne(x => x.RoomType)
                    .WithMany(x => x.RatePlans)
                    .HasForeignKey(x => x.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
                // Index
                entity.HasIndex(x => new
                {
                    x.RoomTypeId,
                    x.Name
                })
                .IsUnique()
                .IsClustered(false);
            });
            //folio
            builder.Entity<Folio>(entity =>
            {
                entity.Property(x => x.Currency)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(x => x.Total)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Balance)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Reservation)
                    .WithOne(x => x.Folio)
                    .HasForeignKey<Folio>(x => x.ReservationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //folioitem
            builder.Entity<FolioItem>(entity =>
            {
                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Tax)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Folio)
                    .WithMany(x => x.Items)
                    .HasForeignKey(x => x.FolioId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Service)
                    .WithMany(x => x.FolioItems)
                    .HasForeignKey(x => x.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //payment
            builder.Entity<Payment>(entity =>
            {
                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasIndex(x => x.ExternalId)
                    .IsUnique().IsClustered(false);

                entity.HasIndex(x => x.IdempotencyKey)
                    .IsUnique().IsClustered(false);

                entity.HasOne(x => x.Folio)
                    .WithMany(x => x.Payments)
                    .HasForeignKey(x => x.FolioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //refund
            builder.Entity<Refund>(entity =>
            {
                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.HasOne(x => x.Payment)
                    .WithMany(x => x.Refunds)
                    .HasForeignKey(x => x.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Folio)
                    .WithMany()
                    .HasForeignKey(x => x.FolioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            //service
            builder.Entity<Service>(entity =>
            {
                entity.Property(x => x.Name)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Price)
                    .HasPrecision(18, 2);

                entity.Property(x => x.Tax)
                    .HasPrecision(18, 2);
            });
            //housekeepingtask
            builder.Entity<HousekeepingTask>(entity =>
            {
                entity.HasOne(x => x.Room)
                    .WithMany(x => x.HousekeepingTasks)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Assignee)
                    .WithMany()
                    .HasForeignKey(x => x.AssigneeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            //maintenanceticket
            builder.Entity<MaintenanceTicket>(entity =>
            {
                entity.Property(x => x.Issue)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.HasOne(x => x.Room)
                    .WithMany(x => x.MaintenanceTickets)
                    .HasForeignKey(x => x.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Assignee)
                    .WithMany()
                    .HasForeignKey(x => x.AssigneeId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            //auditlog
            builder.Entity<AuditLog>(entity =>
            {
                entity.Property(x => x.EntityName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.EntityId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Action)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.AuditLogs)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(x => x.Property)
                    .WithMany(x => x.AuditLogs)
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            //user
            builder.Entity<User>(entity =>
            {
                entity.Property(x => x.FullName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasOne(x => x.Property)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.PropertyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            //role
            builder.Entity<Role>(entity =>
            {
                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.IsActive)
                    .IsRequired();
            });

            builder.Entity<Permission>(entity =>
            {
                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Resource)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Action)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.HasIndex(x => x.Name)
                    .IsUnique();
            });

            builder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(x => new
                {
                    x.RoleId,
                    x.PermissionId
                });

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Permission)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
