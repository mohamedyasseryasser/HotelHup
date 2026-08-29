namespace HotelHup.CORE.Entities
{
    public class RolePermission
    {
        public string RoleId { get; set; } = string.Empty;

        public Role Role { get; set; } = null!;

        public int PermissionId { get; set; }

        public Permission Permission { get; set; } = null!;
    }
}