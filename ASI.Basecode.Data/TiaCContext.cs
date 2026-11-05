using System;
using System.Collections.Generic;
using ASI.Basecode.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ASI.Basecode.Data;

public partial class TiaCContext : DbContext
{
    public TiaCContext(DbContextOptions<TiaCContext> options)
        : base(options)
    {
    }
    public virtual DbSet<SystemSetting> SystemSettings { get; set; }
    public virtual DbSet<BorrowingTransaction> BorrowingTransactions { get; set; }

    public virtual DbSet<EquipmentCategory> EquipmentCategories { get; set; }

    public virtual DbSet<EquipmentItem> EquipmentItems { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<InfoChangeRequest> InfoChangeRequests { get; set; }

    public virtual DbSet<InfoChangeRequestFile> InfoChangeRequestFiles { get; set; }

    public virtual DbSet<InfoChangeRequestItem> InfoChangeRequestItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BorrowingTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Borrowin__3213E83F58D0F761");

            entity.HasIndex(e => e.BorrowerId, "IX_BorrowingTransactions_borrower_id");

            entity.HasIndex(e => e.EquipmentItemId, "IX_BorrowingTransactions_equipment_item_id");

            entity.HasIndex(e => e.ReleasedBy, "IX_BorrowingTransactions_released_by");

            entity.HasIndex(e => e.ReturnedTo, "IX_BorrowingTransactions_returned_to");

            entity.HasIndex(e => e.ReservationId, "UQ_Transaction_Reservation")
                .IsUnique()
                .HasFilter("([reservation_id] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualPickupTime).HasColumnName("actual_pickup_time");
            entity.Property(e => e.ActualReturnTime).HasColumnName("actual_return_time");
            entity.Property(e => e.BorrowerId).HasColumnName("borrower_id");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_time");
            entity.Property(e => e.DamageNotes).HasColumnName("damage_notes");
            entity.Property(e => e.DelayDurationHours).HasColumnName("delay_duration_hours");
            entity.Property(e => e.EquipmentItemId).HasColumnName("equipment_item_id");
            entity.Property(e => e.IsOverdue).HasColumnName("is_overdue");
            entity.Property(e => e.ReleasedBy).HasColumnName("released_by");
            entity.Property(e => e.ReservationId).HasColumnName("reservation_id");
            entity.Property(e => e.ReturnCondition)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("return_condition");
            entity.Property(e => e.ReturnedTo).HasColumnName("returned_to");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");

            entity.HasOne(d => d.Borrower).WithMany(p => p.BorrowingTransactionBorrowers)
                .HasForeignKey(d => d.BorrowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_Borrower");

            entity.HasOne(d => d.EquipmentItem).WithMany(p => p.BorrowingTransactions)
                .HasForeignKey(d => d.EquipmentItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_Item");

            entity.HasOne(d => d.ReleasedByNavigation).WithMany(p => p.BorrowingTransactionReleasedByNavigations)
                .HasForeignKey(d => d.ReleasedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transactions_ReleasedBy");

            entity.HasOne(d => d.Reservation).WithOne(p => p.BorrowingTransaction)
                .HasForeignKey<BorrowingTransaction>(d => d.ReservationId)
                .HasConstraintName("FK_Transactions_Reservation");

            entity.HasOne(d => d.ReturnedToNavigation).WithMany(p => p.BorrowingTransactionReturnedToNavigations)
                .HasForeignKey(d => d.ReturnedTo)
                .HasConstraintName("FK_Transactions_ReturnedTo");
        });

        modelBuilder.Entity<EquipmentCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Equipmen__3213E83FB3B8F0CA");

            entity.HasIndex(e => e.CategoryCode, "UQ_Category_Code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryCode)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("category_code");
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("category_name");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_time");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");
        });

        modelBuilder.Entity<EquipmentItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Equipmen__3213E83F8AE4C201");

            entity.HasIndex(e => e.CategoryId, "IX_EquipmentItems_category_id");

            entity.HasIndex(e => e.ItemCode, "UQ_Item_Code").IsUnique();

            entity.HasIndex(e => e.SerialNumber, "UQ_Item_Serial")
                .IsUnique()
                .HasFilter("([serial_number] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BrandModel)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("brand_model");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ConditionRating)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("good")
                .HasColumnName("condition_rating");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_time");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("image_path");
            entity.Property(e => e.SuggestedCategory)
                .HasMaxLength(100)
                .HasColumnName("suggested_category");
            entity.Property(e => e.CategoryReviewNote)
                .HasMaxLength(255)
                .HasColumnName("category_review_note");
            entity.Property(e => e.ItemCode)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("item_code");
            entity.Property(e => e.SerialNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("serial_number");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("available")
                .HasColumnName("status");
            entity.Property(e => e.StorageLocation)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("storage_location");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");

            entity.HasOne(d => d.Category).WithMany(p => p.EquipmentItems)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EquipmentItems_Categories");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reservat__3213E83F95CD2E81");

            entity.HasIndex(e => e.BorrowerId, "IX_Reservations_borrower_id");

            entity.HasIndex(e => e.EquipmentItemId, "IX_Reservations_equipment_item_id");

            entity.HasIndex(e => e.ReviewedBy, "IX_Reservations_reviewed_by");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BorrowerId).HasColumnName("borrower_id");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_time");
            entity.Property(e => e.EquipmentItemId).HasColumnName("equipment_item_id");
            entity.Property(e => e.EstimatedReturnDate).HasColumnName("estimated_return_date");
            entity.Property(e => e.RequestedPickupDate).HasColumnName("requested_pickup_date");
            entity.Property(e => e.ReviewNotes).HasColumnName("review_notes");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");

            entity.HasOne(d => d.Borrower).WithMany(p => p.ReservationBorrowers)
                .HasForeignKey(d => d.BorrowerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservations_Borrower");

            entity.HasOne(d => d.EquipmentItem).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.EquipmentItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservations_Item");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ReservationReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_Reservations_Reviewer");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__user__3213E83F202ED11A");

            entity.ToTable("user");

            entity.HasIndex(e => e.Email, "UQ_User_Email").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_User_UserId").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BorrowerStatus)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active")
                .HasColumnName("borrower_status");
            entity.Property(e => e.AccountStatus)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("account_status");
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("contact_number");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_time");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("last_name");
            entity.Property(e => e.MiddleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("middle_name");
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.ProfileImagePath)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("profile_image_path");
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("user")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");
            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("user_id");
        });
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey).HasName("PK_SystemSettings");

            entity.ToTable("system_settings");

            entity.Property(e => e.SettingKey)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("setting_key");
            entity.Property(e => e.SettingValue)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("setting_value");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("updated_by");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");
        });
        modelBuilder.Entity<InfoChangeRequest>(entity =>
        {
            entity.ToTable("info_change_requests");
            entity.HasKey(e => e.Id).HasName("PK_InfoChangeRequests");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.ReviewNote)
                .HasMaxLength(255)
                .HasColumnName("review_note");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("created_time");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("updated_time");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_InfoRequests_User");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany()
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_InfoRequests_Reviewer");
        });

        modelBuilder.Entity<InfoChangeRequestItem>(entity =>
        {
            entity.ToTable("info_change_request_items");
            entity.HasKey(e => e.Id).HasName("PK_InfoRequestItems");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.FieldKey)
                .IsRequired()
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("field_key");
            entity.Property(e => e.OldValue).HasMaxLength(100).HasColumnName("old_value");
            entity.Property(e => e.NewValue)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("new_value");
            entity.Property(e => e.ApprovedValue).HasMaxLength(100).HasColumnName("approved_value");

            entity.HasOne(d => d.Request).WithMany(p => p.Items)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InfoRequestItems_Request");
        });

        modelBuilder.Entity<InfoChangeRequestFile>(entity =>
        {
            entity.ToTable("info_change_request_files");
            entity.HasKey(e => e.Id).HasName("PK_InfoRequestFiles");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.StoredName)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("stored_name");
            entity.Property(e => e.OriginalName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("original_name");
            entity.Property(e => e.ContentType)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("content_type");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.UploadedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("uploaded_time");

            entity.HasOne(d => d.Request).WithMany(p => p.Files)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_InfoRequestFiles_Request");
        });
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
