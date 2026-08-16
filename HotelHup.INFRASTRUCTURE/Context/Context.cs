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
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Invoice> invoices { get; set; }
        public DbSet<InvoiceItem> invoiceItems { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public hotelhupContext(DbContextOptions<hotelhupContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            

            // 1. User -> RefreshTokens (One-to-Many)
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
            });

            // 2. User -> RoomTypes (One-to-Many)
            modelBuilder.Entity<RoomType>()
                .HasOne(rt => rt.CreatedByUser)
                .WithMany(u => u.RoomTypesAdded)
                .HasForeignKey(rt => rt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomType>(entity =>
            {
                entity.Property(rt => rt.Name).IsRequired().HasMaxLength(100);
                entity.Property(rt => rt.BasePrice)
                    .HasColumnType("decimal(10,2)").IsRequired();
            });

            // 3. RoomType -> Rooms (One-to-Many)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.RoomType)
                .WithMany(rt => rt.Rooms)
                .HasForeignKey(r => r.RoomTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(r => r.RoomNumber).IsRequired().HasMaxLength(20);
                entity.HasIndex(r => r.RoomNumber).IsUnique();
                entity.Property(r => r.PricePerNight).HasColumnType("decimal(10,2)").IsRequired();
            });

            // 3.1 Room -> Reservations
            modelBuilder.Entity<Reservation>()
                .HasOne(res => res.Room)
                .WithMany(r => r.Reservations)
                .HasForeignKey(res => res.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Guest -> Reservations (One-to-Many)
            modelBuilder.Entity<Reservation>()
                .HasOne(res => res.Guest)
                .WithMany(g => g.Reservations)
                .HasForeignKey(res => res.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Guest>(entity =>
            {
                entity.Property(g => g.FullName).IsRequired().HasMaxLength(150);
                entity.Property(g => g.Email).HasMaxLength(150);
                entity.HasIndex(g => g.Email).IsUnique();
                entity.Property(g => g.Phone).HasMaxLength(20);
                entity.Property(g => g.nationality).IsRequired();
            });

            // 5. Reservation <-> Invoice (One-to-One)
            modelBuilder.Entity<Reservation>()
                .HasOne(res => res.Invoice)
                .WithOne(inv => inv.Reservation)
                .HasForeignKey<Invoice>(inv => inv.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.Property(res => res.TotalAmount).HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(res => res.ReservationCode).IsRequired().HasMaxLength(30);

                entity.HasOne(res => res.CreatedBy)
                      .WithMany(u => u.ReservationsCreated)
                      .HasForeignKey(res => res.CreatedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 6. Invoice -> InvoiceItems (One-to-Many)
            modelBuilder.Entity<InvoiceItem>()
                .HasOne(item => item.Invoice)
                .WithMany(inv => inv.InvoiceItems)
                .HasForeignKey(item => item.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(item => item.InvoiceId);
                entity.Property(item => item.Description).IsRequired().HasMaxLength(200);
                entity.Property(item => item.UnitPrice).HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(item => item.Total).HasColumnType("decimal(10,2)").IsRequired();
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.Property(inv => inv.SubTotal).HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(inv => inv.TotalAmount).HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(inv => inv.InvoiceNumber).IsRequired().HasMaxLength(30);
                entity.HasIndex(inv => inv.InvoiceNumber).IsUnique();
            });

            // 7. Invoice -> Payments (One-to-Many)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(inv => inv.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(10,2)").IsRequired();

                entity.HasOne(p => p.ReceivedBy)
                      .WithMany(u => u.PaymentsReceived)
                      .HasForeignKey(p => p.ReceivedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
