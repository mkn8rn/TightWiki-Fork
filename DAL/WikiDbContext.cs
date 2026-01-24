using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public sealed class WikiDbContext(DbContextOptions<WikiDbContext> options) : DbContext(options)
{
    public DbSet<Emoji> Emojis => Set<Emoji>();
    public DbSet<EmojiCategory> EmojiCategories => Set<EmojiCategory>();

    public DbSet<ConfigurationGroup> ConfigurationGroups => Set<ConfigurationGroup>();
    public DbSet<ConfigurationEntry> ConfigurationEntries => Set<ConfigurationEntry>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<Theme> Themes => Set<Theme>();

    // Users/Auth entities
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionDisposition> PermissionDispositions => Set<PermissionDisposition>();
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();
    public DbSet<AccountPermissionEntity> AccountPermissions => Set<AccountPermissionEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();

    // Statistics entities
    public DbSet<CompilationStatisticsEntity> CompilationStatistics => Set<CompilationStatisticsEntity>();

    // Exception entities
    public DbSet<WikiExceptionEntity> WikiExceptions => Set<WikiExceptionEntity>();

    // Page entities
    public DbSet<PageEntity> Pages => Set<PageEntity>();
    public DbSet<PageRevisionEntity> PageRevisions => Set<PageRevisionEntity>();
    public DbSet<PageFileEntity> PageFiles => Set<PageFileEntity>();
    public DbSet<PageFileRevisionEntity> PageFileRevisions => Set<PageFileRevisionEntity>();
    public DbSet<PageRevisionAttachmentEntity> PageRevisionAttachments => Set<PageRevisionAttachmentEntity>();
    public DbSet<PageTagEntity> PageTags => Set<PageTagEntity>();
    public DbSet<ProcessingInstructionEntity> ProcessingInstructions => Set<ProcessingInstructionEntity>();
    public DbSet<PageCommentEntity> PageComments => Set<PageCommentEntity>();
    public DbSet<PageReferenceEntity> PageReferences => Set<PageReferenceEntity>();
    public DbSet<PageTokenEntity> PageTokens => Set<PageTokenEntity>();
    public DbSet<DeletedPageEntity> DeletedPages => Set<DeletedPageEntity>();
    public DbSet<DeletedPageRevisionEntity> DeletedPageRevisions => Set<DeletedPageRevisionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Emoji>(b =>
        {
            b.ToTable("Emoji");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Shortcut).IsRequired();
            b.Property(x => x.MimeType).IsRequired();
        });

        modelBuilder.Entity<EmojiCategory>(b =>
        {
            b.ToTable("EmojiCategory");
            b.HasKey(x => new { x.EmojiId, x.Category });
            b.Property(x => x.Category).IsRequired();

            b.HasOne<Emoji>()
                .WithMany()
                .HasForeignKey(x => x.EmojiId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfigurationGroup>(b =>
        {
            b.ToTable("ConfigurationGroup");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<ConfigurationEntry>(b =>
        {
            b.ToTable("ConfigurationEntry");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();

            b.HasOne<ConfigurationGroup>()
                .WithMany()
                .HasForeignKey(x => x.ConfigurationGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItem>(b =>
        {
            b.ToTable("MenuItem");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Link).IsRequired();
        });

        modelBuilder.Entity<Theme>(b =>
        {
            b.ToTable("Theme");
            b.HasKey(x => x.Name);
            b.Property(x => x.Name).IsRequired();
        });

        // Users/Auth entity configurations
        modelBuilder.Entity<Profile>(b =>
        {
            b.ToTable("Profile");
            b.HasKey(x => x.UserId);
            b.Property(x => x.AccountName).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Role");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permission");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<PermissionDisposition>(b =>
        {
            b.ToTable("PermissionDisposition");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<AccountRole>(b =>
        {
            b.ToTable("AccountRole");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountPermissionEntity>(b =>
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

        modelBuilder.Entity<RolePermissionEntity>(b =>
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
        modelBuilder.Entity<CompilationStatisticsEntity>(b =>
        {
            b.ToTable("CompilationStatistics");
            b.HasKey(x => x.Id);
            b.Property(x => x.PageId).IsRequired();
            b.Property(x => x.CreatedDate).IsRequired();
        });

        // Exception entity configurations
        modelBuilder.Entity<WikiExceptionEntity>(b =>
        {
            b.ToTable("WikiException");
            b.HasKey(x => x.Id);
            b.Property(x => x.CreatedDate).IsRequired();
        });

        // Page entity configurations
        modelBuilder.Entity<PageEntity>(b =>
        {
            b.ToTable("Page");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<PageRevisionEntity>(b =>
        {
            b.ToTable("PageRevision");
            b.HasKey(x => new { x.PageId, x.Revision });
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<PageFileEntity>(b =>
        {
            b.ToTable("PageFile");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<PageFileRevisionEntity>(b =>
        {
            b.ToTable("PageFileRevision");
            b.HasKey(x => x.Id);
            b.Property(x => x.ContentType).IsRequired();
            b.Property(x => x.Data).IsRequired();
        });

        modelBuilder.Entity<PageRevisionAttachmentEntity>(b =>
        {
            b.ToTable("PageRevisionAttachment");
            b.HasKey(x => x.Id);
        });

        modelBuilder.Entity<PageTagEntity>(b =>
        {
            b.ToTable("PageTag");
            b.HasKey(x => x.Id);
            b.Property(x => x.Tag).IsRequired();
        });

        modelBuilder.Entity<ProcessingInstructionEntity>(b =>
        {
            b.ToTable("ProcessingInstruction");
            b.HasKey(x => new { x.PageId, x.Instruction });
            b.Property(x => x.Instruction).IsRequired();
        });

        modelBuilder.Entity<PageCommentEntity>(b =>
        {
            b.ToTable("PageComment");
            b.HasKey(x => x.Id);
            b.Property(x => x.Body).IsRequired();
            b.Property(x => x.CreatedDate).IsRequired();
        });

        modelBuilder.Entity<PageReferenceEntity>(b =>
        {
            b.ToTable("PageReference");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReferencesPageNavigation).IsRequired();
        });

        modelBuilder.Entity<PageTokenEntity>(b =>
        {
            b.ToTable("PageToken");
            b.HasKey(x => x.Id);
            b.Property(x => x.Token).IsRequired();
        });

        modelBuilder.Entity<DeletedPageEntity>(b =>
        {
            b.ToTable("DeletedPage");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });

        modelBuilder.Entity<DeletedPageRevisionEntity>(b =>
        {
            b.ToTable("DeletedPageRevision");
            b.HasKey(x => new { x.PageId, x.Revision });
            b.Property(x => x.Name).IsRequired();
            b.Property(x => x.Navigation).IsRequired();
        });
    }
}
