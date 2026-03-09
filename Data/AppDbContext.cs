using Microsoft.EntityFrameworkCore;
using skillexa_backend.Models;

namespace skillexa_backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role)
             .HasConversion<string>()
             .HasMaxLength(20);
        });

        // ── Course ────────────────────────────────────────────────────────
        modelBuilder.Entity<Course>(e =>
        {
            e.Property(c => c.Price).HasColumnType("numeric(10,2)");

            e.HasOne(c => c.Instructor)
             .WithMany(u => u.CoursesCreated)
             .HasForeignKey(c => c.InstructorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Category)
             .WithMany(cat => cat.Courses)
             .HasForeignKey(c => c.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Lesson ────────────────────────────────────────────────────────
        modelBuilder.Entity<Lesson>(e =>
        {
            e.HasOne(l => l.Course)
             .WithMany(c => c.Lessons)
             .HasForeignKey(l => l.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Enrollment ────────────────────────────────────────────────────
        modelBuilder.Entity<Enrollment>(e =>
        {
            // Mỗi user chỉ enroll 1 lần mỗi khoá
            e.HasIndex(en => new { en.UserId, en.CourseId }).IsUnique();

            e.HasOne(en => en.User)
             .WithMany(u => u.Enrollments)
             .HasForeignKey(en => en.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(en => en.Course)
             .WithMany(c => c.Enrollments)
             .HasForeignKey(en => en.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Category ──────────────────────────────────────────────────────
        modelBuilder.Entity<Category>(e =>
        {
            e.HasIndex(cat => cat.Slug).IsUnique();
        });
    }
}