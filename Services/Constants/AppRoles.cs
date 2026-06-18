namespace AutoStock.Services.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";

        public const string Owner = "Owner";

        public const string Staff = "Staff";

        public const string OwnerOrStaff = Owner + "," + Staff;

        public const string AdminOrOwnerOrStaff = Admin + "," + Owner + "," + Staff;

        // Eski User rolü DB'de kalabilir.
        // Yeni kullanıcı oluştururken artık bunu kullanmayacağız.
        public const string User = "User";
    }
}
