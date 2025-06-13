using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GPP_API.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Alert> Alerts { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BudgetPart> BudgetParts { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<RoleChangeRequest> RoleChangeRequests { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Modern_Spanish_CI_AS");

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__Alerts__4B8FB03A083AD9F2");

            entity.Property(e => e.AlertId).HasColumnName("alert_id");
            entity.Property(e => e.AlertDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("alert_date");
            entity.Property(e => e.AlertType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("alert_type");
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("message");

            entity.Property(e => e.Status)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName("status");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.Project).WithMany(p => p.Alerts)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__Alerts__project___5629CD9C");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__AuditLog__9E2397E033E2D975");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("action_date");
            entity.Property(e => e.ActionDescription)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("action_description");
            entity.Property(e => e.ActionType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AuditLogs__user___59FA5E80");
        });

        modelBuilder.Entity<BudgetPart>(entity =>
        {
            entity.HasKey(e => e.BudgetPartId).HasName("PK__BudgetPa__F296F5F0B9C7DCCC");

            entity.Property(e => e.BudgetPartId).HasColumnName("budget_part_id");
            entity.Property(e => e.AllocatedAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("allocated_amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PartName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("part_name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RemainingAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("remaining_amount");

            entity.HasOne(d => d.Project).WithMany(p => p.BudgetParts)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK__BudgetPar__proje__45F365D3");

            entity.Property(e => e.Status)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName("status");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.ExpenseId).HasName("PK__Expenses__404B6A6BD608D8E1");

            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.BudgetPartId).HasColumnName("budget_part_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.DocumentReference)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("document_reference");
            entity.Property(e => e.ExpenseAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("expense_amount");
            entity.Property(e => e.Status)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName("status");
            entity.Property(e => e.ExpenseDate).HasColumnName("expense_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.BudgetPart).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.BudgetPartId)
                .HasConstraintName("FK__Expenses__budget__4BAC3F29");

            //entity.HasOne(d => d.Project).WithMany(p => p.Expenses)
            //    .HasForeignKey(d => d.ProjectId)
            //    .HasConstraintName("FK__Expenses__projec__4AB81AF0");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__E059842F9B907828");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("message");
            entity.Property(e => e.NotificationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("notification_date");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("notification_type");
            entity.Property(e => e.UserEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_email");

            entity.Property(e => e.Status)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName("status");

            entity.HasOne(d => d.UserEmailNavigation).WithMany(p => p.Notifications)
                .HasPrincipalKey(p => p.Email)
                .HasForeignKey(d => d.UserEmail)
                .HasConstraintName("FK__Notificat__user___5EBF139D");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__BC799E1F4C59B508");

            entity.HasIndex(e => e.ProjectCode, "UQ__Projects__891B3A6F2F6F2790").IsUnique();

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Budget)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("budget");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.ManagerEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("manager_email");
            entity.Property(e => e.ProjectCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("project_code");
            entity.Property(e => e.ProjectName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("project_name");
            entity.Property(e => e.RemainingBudget)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("remaining_budget");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.ManagerEmailNavigation).WithMany(p => p.Projects)
                .HasPrincipalKey(p => p.Email)
                .HasForeignKey(d => d.ManagerEmail)
                .HasConstraintName("FK__Projects__manage__403A8C7D");
        });

        modelBuilder.Entity<RoleChangeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__RoleChan__18D3B90FBAF19D00");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Justification)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("justification");
            entity.Property(e => e.RequestedRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("requested_role");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.EmailAddress)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("user_email");

            entity.HasOne(d => d.UserEmailNavigation).WithMany(p => p.RoleChangeRequests)
                .HasPrincipalKey(p => p.Email)
                .HasForeignKey(d => d.EmailAddress)
                .HasConstraintName("FK__RoleChang__user___5165187F");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FA3E8455C");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E616495701B88").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Status)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName ("status");
        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
