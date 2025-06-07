namespace GPP_API.DTO.Notification
{
    public class NotificationDTO
    {
        public int NotificationId { get; set; }
        public string? NotificationType { get; set; }
        public string? Message { get; set; }
        public DateTime? NotificationDate { get; set; }
        public bool? IsRead { get; set; }
    }

}
