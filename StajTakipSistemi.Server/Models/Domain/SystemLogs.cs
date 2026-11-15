using System;

namespace StajTakipSistemi.Server.Models.Domain
{
    public class FailedLoginLog
    {
        public int Id { get; set; }
        public string UsernameAttempted { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime AttemptTime { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public bool TriggeredLockout { get; set; } = false;
    }

    public class SystemLog
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
