namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class AdminWorkshopDetailViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int SubscriptionStatus { get; set; }

        public string SubscriptionStatusText { get; set; } = string.Empty;

        public DateTime SubscriptionStartDate { get; set; }

        public DateTime? SubscriptionEndDate { get; set; }

        public string? SubscriptionNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public AdminWorkshopProfileViewModel? Profile { get; set; }

        public List<AdminWorkshopUserViewModel> Users { get; set; } = new();
        public List<AdminWorkshopPartnerViewModel> Partners { get; set; } = new();
        public List<AdminWorkshopBankAccountViewModel> BankAccounts { get; set; } = new();
        public List<AdminEntityEditLockViewModel> EditLocks { get; set; } = new();
    }

    public class AdminEntityEditLockViewModel
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string EntityReference { get; set; } = string.Empty;
        public string LockedByDisplayName { get; set; } = string.Empty;
        public DateTime AcquiredAt { get; set; }
        public DateTime LastHeartbeatAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }
}
