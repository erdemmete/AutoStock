namespace AutoStock.Repositories.Entities
{
    public class EntityEditLock
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public string EntityType { get; set; } = null!;

        public int EntityId { get; set; }

        public int LockedByUserId { get; set; }

        public string LockToken { get; set; } = null!;

        public DateTime AcquiredAt { get; set; }

        public DateTime LastHeartbeatAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public Workshop Workshop { get; set; } = null!;

        public AppUser LockedByUser { get; set; } = null!;
    }
}
