using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyNotes.Data.Models;

namespace MyNotes.Data.Context
{
    public partial class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Note> Notes { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<TaskList> TaskLists { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReminderHeader).HasMaxLength(200);
                entity.Property(e => e.ReminderText).HasMaxLength(1000);
                entity.Property(e => e.ReminderDateTime).IsRequired();
                entity.Property(e => e.Repeat).IsRequired();
                entity.Property(e => e.Alarm).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);

                entity.HasOne(e => e.Note)
                      .WithMany(e => e.Reminders)
                      .HasForeignKey(e => e.NoteId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.SetNull);

                // Index for efficient queries by time and active status
                entity.HasIndex(e => new { e.ReminderDateTime, e.IsActive });
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);

                entity.HasIndex(e => e.Key).IsUnique();
            });

            modelBuilder.Entity<TaskList>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.HeaderColor).IsRequired();
                entity.Property(e => e.Order).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Text).IsRequired();
                entity.Property(e => e.Order).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.DeletedAt).IsRequired(false);

                entity.HasOne(e => e.TaskList)
                      .WithMany(e => e.TaskItems)
                      .HasForeignKey(e => e.TaskListId)
                      .IsRequired(true)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index for efficient queries by TaskListId
                entity.HasIndex(e => e.TaskListId);
            });

            modelBuilder.Entity<Note>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Reminder>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<Setting>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<TaskList>().HasQueryFilter(e => e.DeletedAt == null);
            modelBuilder.Entity<TaskItem>().HasQueryFilter(e => e.DeletedAt == null);

            modelBuilder.Entity<TaskList>().HasData([
                new TaskList
                {
                    Id = Guid.Parse("e11a24e0-0006-4651-b2a5-9985a221a06a"),
                    Name = "To Do",
                    HeaderColor = "#868686",
                    Order = 0,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                },
                new TaskList
                {
                    Id = Guid.Parse("c8fd1c75-2379-43d9-b85d-1c4e25f7b8b7"),
                    Name = "In Progress",
                    HeaderColor = "#FFFF00",
                    Order = 1,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                },
                new TaskList
                {
                    Id = Guid.Parse("e93e2b0b-2790-4128-8150-057c2ebd65cf"),
                    Name = "Done",
                    HeaderColor = "#008000",
                    Order = 2,
                    CreatedAt = new DateTime(2026, 1, 1),
                    UpdatedAt = new DateTime(2026, 1, 1)
                }
            ]);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(DatabaseLocations.ConnectionString);

                optionsBuilder.UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
                }));
            }
        }

    }
}
