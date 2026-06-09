namespace AutoStock.Services.Dtos.Admin.Workshops
{
    public class CreateAdminWorkshopUserRequestDto
    {
        public string FullName { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        // Artık ana akışta kullanılmıyor.
        // Kullanıcı şifresini davet linki üzerinden kendi belirleyecek.
        public string? Password { get; set; }

        // Sadece Owner veya Staff kabul edeceğiz.
        public string Role { get; set; } = "Staff";
    }
}