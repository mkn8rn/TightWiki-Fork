using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public sealed class WikiDbContext(DbContextOptions<WikiDbContext> options) : DbContext(options)
{
    public DbSet<EmojiDB> Emojis => Set<EmojiDB>();
    public DbSet<EmojiCategoryDB> EmojiCategories => Set<EmojiCategoryDB>();

    public DbSet<ConfigurationGroupDB> ConfigurationGroups => Set<ConfigurationGroupDB>();
    public DbSet<ConfigurationEntryDB> ConfigurationEntries => Set<ConfigurationEntryDB>();
    public DbSet<MenuItemDB> MenuItems => Set<MenuItemDB>();

    public DbSet<ThemeDB> Themes => Set<ThemeDB>();

    // Users/Auth entities
    public DbSet<ProfileDB> Profiles => Set<ProfileDB>();
    public DbSet<RoleDB> Roles => Set<RoleDB>();
    public DbSet<PermissionDB> Permissions => Set<PermissionDB>();
    public DbSet<PermissionDispositionDB> PermissionDispositions => Set<PermissionDispositionDB>();
    public DbSet<AccountRoleDB> AccountRoles => Set<AccountRoleDB>();
    public DbSet<AccountPermissionEntityDB> AccountPermissions => Set<AccountPermissionEntityDB>();
    public DbSet<RolePermissionEntityDB> RolePermissions => Set<RolePermissionEntityDB>();

    // Statistics entities
    public DbSet<CompilationStatisticsEntityDB> CompilationStatistics => Set<CompilationStatisticsEntityDB>();

    // Exception entities
    public DbSet<WikiExceptionEntityDB> WikiExceptions => Set<WikiExceptionEntityDB>();

    // Page entities
    public DbSet<PageEntityDB> Pages => Set<PageEntityDB>();
    public DbSet<PageRevisionEntityDB> PageRevisions => Set<PageRevisionEntityDB>();
    public DbSet<PageFileEntityDB> PageFiles => Set<PageFileEntityDB>();
    public DbSet<PageFileRevisionEntityDB> PageFileRevisions => Set<PageFileRevisionEntityDB>();
    public DbSet<PageRevisionAttachmentEntityDB> PageRevisionAttachments => Set<PageRevisionAttachmentEntityDB>();
    public DbSet<PageTagEntityDB> PageTags => Set<PageTagEntityDB>();
    public DbSet<ProcessingInstructionEntityDB> ProcessingInstructions => Set<ProcessingInstructionEntityDB>();
    public DbSet<PageCommentEntityDB> PageComments => Set<PageCommentEntityDB>();
    public DbSet<PageReferenceEntityDB> PageReferences => Set<PageReferenceEntityDB>();
    public DbSet<PageTokenEntityDB> PageTokens => Set<PageTokenEntityDB>();
    public DbSet<DeletedPageEntityDB> DeletedPages => Set<DeletedPageEntityDB>();
    public DbSet<DeletedPageRevisionEntityDB> DeletedPageRevisions => Set<DeletedPageRevisionEntityDB>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmojiDB>(b =>
        {
            b.ToTable("Emoji");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Shortcut).IsRequired();
            b.Property(x => x.MimeType).IsRequired();
        });

        modelBuilder.Entity<EmojiCategoryDB>(b =>
        {
            b.ToTable("EmojiCategory");
            b.HasKey(x => new { x.EmojiId, x.Category });
            b.Property(x => x.Category).IsRequired();

            b.HasOne<EmojiDB>()
                .WithMany()
                .HasForeignKey(x => x.EmojiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfigurationGroupDB>(b =>
        {
            b.ToTable("ConfigurationGroup");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<ConfigurationEntryDB>(b =>
        {
            b.ToTable("ConfigurationEntry");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();

            b.HasOne<ConfigurationGroupDB>()
                .WithMany()
                .HasForeignKey(x => x.ConfigurationGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItemDB>(b =>
        {
            b.ToTable("MenuItem");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Link).IsRequired();
        });

        modelBuilder.Entity<ThemeDB>(b =>
        {
            b.ToTable("Theme");
            b.HasKey(x => x.Name);
            b.Property(x => x.Name).IsRequired();
        });

        // Users/Auth entity configurations
        modelBuilder.Entity<ProfileDB>(b =>
        {
            b.ToTable("Profile");
            b.HasKey(x => x.UserId);
            b.Property(x => x.AccountName).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<RoleDB>(b =>
        {
            b.ToTable("Role");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<PermissionDB>(b =>
        {
            b.ToTable("Permission");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<PermissionDispositionDB>(b =>
        {
            b.ToTable("PermissionDisposition");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<AccountRoleDB>(b =>
        {
            b.ToTable("AccountRole");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountPermissionEntityDB>(b =>
        {
            b.ToTable("AccountPermission");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Permission)
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.PermissionDisposition)
                .WithMany()
                .HasForeignKey(x => x.PermissionDispositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RolePermissionEntityDB>(b =>
        {
            b.ToTable("RolePermission");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Permission)
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.PermissionDisposition)
                .WithMany()
                .HasForeignKey(x => x.PermissionDispositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Statistics entity configurations
        modelBuilder.Entity<CompilationStatisticsEntityDB>(b =>
        {
            b.ToTable("CompilationStatistics");
            b.HasKey(x => x.Id);
            b.Property(x => x.PageId).IsRequired();
            b.Property(x => x.CreatedDate).IsRequired();
        });

        // Exception entity configurations
        modelBuilder.Entity<WikiExceptionEntityDB>(b =>
        {
            b.ToTable("WikiException");
            b.HasKey(x => x.Id);
            b.Property(x => x.CreatedDate).IsRequired();
        });

        // Page entity configurations
        modelBuilder.Entity<PageEntityDB>(b =>
        {
            b.ToTable("Page");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<PageRevisionEntityDB>(b =>
        {
            b.ToTable("PageRevision");
            b.HasKey(x => new { x.PageId, x.Revision });
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<PageFileEntityDB>(b =>
        {
            b.ToTable("PageFile");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<PageFileRevisionEntityDB>(b =>
        {
            b.ToTable("PageFileRevision");
            b.HasKey(x => x.Id);
            b.Property(x => x.ContentType).IsRequired();
            b.Property(x => x.Data).IsRequired();
        });

        modelBuilder.Entity<PageRevisionAttachmentEntityDB>(b =>
        {
            b.ToTable("PageRevisionAttachment");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<PageTagEntityDB>(b =>
        {
            b.ToTable("PageTag");
            b.HasKey(x => x.Id);
            b.Property(x => x.Tag).IsRequired();
        });

        modelBuilder.Entity<ProcessingInstructionEntityDB>(b =>
        {
            b.ToTable("ProcessingInstruction");
            b.HasKey(x => new { x.PageId, x.Instruction });
            b.Property(x => x.Instruction).IsRequired();
        });

        modelBuilder.Entity<PageCommentEntityDB>(b =>
        {
            b.ToTable("PageComment");
            b.HasKey(x => x.Id);
            b.Property(x => x.Body).IsRequired();
            b.Property(x => x.CreatedDate).IsRequired();
        });

        modelBuilder.Entity<PageReferenceEntityDB>(b =>
        {
            b.ToTable("PageReference");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReferencesPageNavigation).IsRequired();
        });

        modelBuilder.Entity<PageTokenEntityDB>(b =>
        {
            b.ToTable("PageToken");
            b.HasKey(x => x.Id);
            b.Property(x => x.Token).IsRequired();
        });

        modelBuilder.Entity<DeletedPageEntityDB>(b =>
        {
            b.ToTable("DeletedPage");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<DeletedPageRevisionEntityDB>(b =>
        {
            b.ToTable("DeletedPageRevision");
            b.HasKey(x => new { x.PageId, x.Revision });
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });
    }
}
