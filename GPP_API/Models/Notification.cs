using System;
using System.Collections.Generic;

namespace GPP_API.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string? UserEmail { get; set; }

    public string? NotificationType { get; set; }

    public string? Message { get; set; }

    public DateTime? NotificationDate { get; set; }

    public bool? IsRead { get; set; }

    public virtual User? UserEmailNavigation { get; set; }
}
