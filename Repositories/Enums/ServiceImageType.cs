namespace AutoStock.Repositories.Enums
{
    public enum ServiceImageType
    {
        BeforeRepair = 1,      // Geliş / kabul fotoğrafı
        AfterRepair = 2,       // Teslim / işlem sonrası
        Odometer = 3,          // Kilometre göstergesi
        FuelGauge = 4,         // Yakıt göstergesi
        Damage = 5,            // Hasar / dış kaporta
        Interior = 6,          // İç mekan
        Other = 99             // Diğer
    }
}