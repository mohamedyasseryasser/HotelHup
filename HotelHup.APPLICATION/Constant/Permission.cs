using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelHup.APPLICATION.Constant
{
    public static class Permissions
    {
    
        public static class Users
        {
            public const string Read = "User.Read";
            public const string Create = "User.Create";
            public const string Update = "User.Update";
            public const string Activate = "User.Activate";
            public const string Deactivate = "User.Deactivate";
            public const string AssignRole = "User.AssignRole";
        }

 

        public static class Roles
        {
            public const string Read = "Role.Read";
            public const string Create = "Role.Create";
            public const string Update = "Role.Update";
            public const string Deactivate = "Role.Deactivate";
            public const string AssignPermission = "Role.AssignPermission";
        }
 

        public static class Guests
        {
            public const string Read = "Guest.Read";
            public const string Create = "Guest.Create";
            public const string Update = "Guest.Update";
        }

 

        public static class Reservations
        {
            public const string Read = "Reservation.Read";
            public const string Create = "Reservation.Create";
            public const string Update = "Reservation.Update";
            public const string Cancel = "Reservation.Cancel";
            public const string Modify = "Reservation.Modify";
        }

 

        public static class Rooms
        {
            public const string Read = "Room.Read";
            public const string Create = "Room.Create";
            public const string Update = "Room.Update";
            public const string ChangeStatus = "Room.ChangeStatus";
            public const string Assign = "Room.Assign";
        }

 

        public static class RoomTypes
        {
            public const string Read = "RoomType.Read";
            public const string Create = "RoomType.Create";
            public const string Update = "RoomType.Update";
            public const string Deactivate = "RoomType.Deactivate";
        }

 
        public static class RatePlans
        {
            public const string Read = "RatePlan.Read";
            public const string Create = "RatePlan.Create";
            public const string Update = "RatePlan.Update";
            public const string Activate = "RatePlan.Activate";
            public const string Deactivate = "RatePlan.Deactivate";
        }

 

        public static class RateRules
        {
            public const string Read = "RateRule.Read";
            public const string Create = "RateRule.Create";
            public const string Update = "RateRule.Update";
            public const string Delete = "RateRule.Delete";
        }

 

        public static class Folios
        {
            public const string Read = "Folio.Read";
            public const string Create = "Folio.Create";
            public const string Update = "Folio.Update";
            public const string AddCharge = "Folio.AddCharge";
            public const string Transfer = "Folio.Transfer";
            public const string Close = "Folio.Close";
        }
 
        public static class Payments
        {
            public const string Read = "Payment.Read";
            public const string Create = "Payment.Create";
            public const string Void = "Payment.Void";
        }

 

        public static class Refunds
        {
            public const string Read = "Refund.Read";
            public const string Create = "Refund.Create";
            public const string Approve = "Refund.Approve";
        }

 

        public static class Services
        {
            public const string Read = "Service.Read";
            public const string Create = "Service.Create";
            public const string Update = "Service.Update";
            public const string Deactivate = "Service.Deactivate";
            public const string AddToFolio = "Service.AddToFolio";
        }
 

        public static class Housekeeping
        {
            public const string Read = "Housekeeping.Read";
            public const string Assign = "Housekeeping.Assign";
            public const string Start = "Housekeeping.Start";
            public const string Complete = "Housekeeping.Complete";
            public const string ReportIssue = "Housekeeping.ReportIssue";
        }
 

        public static class Maintenance
        {
            public const string Read = "Maintenance.Read";
            public const string Create = "Maintenance.Create";
            public const string Update = "Maintenance.Update";
            public const string Assign = "Maintenance.Assign";
            public const string Complete = "Maintenance.Complete";
        }

 

        public static class Reports
        {
            public const string Read = "Report.Read";
            public const string Export = "Report.Export";
        }

 

        public static class AuditLogs
        {
            public const string Read = "AuditLog.Read";
        }
 

        public static class Properties
        {
            public const string Read = "Property.Read";
            public const string Create = "Property.Create";
            public const string Update = "Property.Update";
            public const string Activate = "Property.Activate";
            public const string Deactivate = "Property.Deactivate";
        }
    }
}
