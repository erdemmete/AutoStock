namespace AutoStock.Services.Dtos.EditLocks
{
    public class EntityEditLockRequestDto
    {
        public string EntityType { get; set; } = null!;

        public int EntityId { get; set; }

        public string? LockToken { get; set; }
    }

    public class EntityEditLockDto
    {
        public string EntityType { get; set; } = null!;

        public int EntityId { get; set; }

        public bool IsEditable { get; set; }

        public bool IsLockedByAnotherUser { get; set; }

        public string? LockToken { get; set; }

        public string? LockedByDisplayName { get; set; }

        public DateTime? AcquiredAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public int HeartbeatIntervalSeconds { get; set; } = 35;

        public int LockDurationSeconds { get; set; } = 120;
    }
}
