using Microsoft.EntityFrameworkCore;
using skillexa_backend.Models;

namespace skillexa_backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<User>            Users            => Set<User>();
    public DbSet<RefreshToken>    RefreshTokens    => Set<RefreshToken>();
    public DbSet<Course>          Courses          => Set<Course>();
    public DbSet<Enrollment>      Enrollments      => Set<Enrollment>();
    public DbSet<Lesson>          Lessons          => Set<Lesson>();
    public DbSet<ContentBlock>    ContentBlocks    => Set<ContentBlock>();
    public DbSet<LessonProgress>  LessonProgresses => Set<LessonProgress>();
    public DbSet<Quiz>            Quizzes          => Set<Quiz>();
    public DbSet<QuizQuestion>    QuizQuestions    => Set<QuizQuestion>();
    public DbSet<QuizAttempt>     QuizAttempts     => Set<QuizAttempt>();
    public DbSet<UserAnswer>      UserAnswers      => Set<UserAnswer>();
    public DbSet<Comment>         Comments         => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ── User ──────────────────────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();

            // Store enum as string for readability in DB
            // "Employee" | "Trainer" | "Admin"
            e.Property(u => u.Role)
             .HasConversion<string>()
             .HasMaxLength(20);

            // "Active" | "Inactive"
            e.Property(u => u.Status)
             .HasConversion<string>()
             .HasMaxLength(20);
        });

        // ── RefreshToken ──────────────────────────────────────────────────────
        b.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.Token).IsUnique();

            e.HasOne(rt => rt.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Course ────────────────────────────────────────────────────────────
        b.Entity<Course>(e =>
        {
            // "Beginner" | "Intermediate" | "Advanced"
            e.Property(c => c.Level)
             .HasConversion<string>()
             .HasMaxLength(20);

            e.HasOne(c => c.Instructor)
             .WithMany(u => u.CoursesAsInstructor)
             .HasForeignKey(c => c.InstructorId)
             .OnDelete(DeleteBehavior.Restrict);    // keep course if instructor deleted
        });

        // ── Enrollment ────────────────────────────────────────────────────────
        // Unique constraint: one enrollment per (user, course)
        b.Entity<Enrollment>(e =>
        {
            e.HasIndex(en => new { en.UserId, en.CourseId }).IsUnique();

            e.Property(en => en.ProgressPercent)
             .HasDefaultValue(0);

            e.HasOne(en => en.User)
             .WithMany(u => u.Enrollments)
             .HasForeignKey(en => en.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(en => en.Course)
             .WithMany(c => c.Enrollments)
             .HasForeignKey(en => en.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Lesson ────────────────────────────────────────────────────────────
        b.Entity<Lesson>(e =>
        {
            // (courseId, order) pair must be unique
            e.HasIndex(l => new { l.CourseId, l.Order }).IsUnique();

            e.HasOne(l => l.Course)
             .WithMany(c => c.Lessons)
             .HasForeignKey(l => l.CourseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ContentBlock ──────────────────────────────────────────────────────
        // Single-table discriminated union: type TEXT | CODE | CALLOUT | IMAGE
        b.Entity<ContentBlock>(e =>
        {
            e.HasIndex(cb => new { cb.LessonId, cb.Order });

            // "Text" | "Code" | "Callout" | "Image"
            e.Property(cb => cb.Type)
             .HasConversion<string>()
             .HasMaxLength(20);

            // "Info" | "Tip" | "Warning" | "Danger"
            e.Property(cb => cb.CalloutVariant)
             .HasConversion<string>()
             .HasMaxLength(20);

            // "Full" | "Half" | "Auto"
            e.Property(cb => cb.ImageWidth)
             .HasConversion<string>()
             .HasMaxLength(10);

            e.HasOne(cb => cb.Lesson)
             .WithMany(l => l.ContentBlocks)
             .HasForeignKey(cb => cb.LessonId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LessonProgress ────────────────────────────────────────────────────
        // Unique constraint: one progress row per (user, lesson)
        b.Entity<LessonProgress>(e =>
        {
            e.HasIndex(lp => new { lp.UserId, lp.LessonId }).IsUnique();

            e.HasOne(lp => lp.User)
             .WithMany()
             .HasForeignKey(lp => lp.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(lp => lp.Lesson)
             .WithMany(l => l.LessonProgresses)
             .HasForeignKey(lp => lp.LessonId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Quiz ──────────────────────────────────────────────────────────────
        // One-to-one: each Lesson has at most one Quiz
        b.Entity<Quiz>(e =>
        {
            e.HasIndex(q => q.LessonId).IsUnique();

            e.HasOne(q => q.Lesson)
             .WithOne(l => l.Quiz)
             .HasForeignKey<Quiz>(q => q.LessonId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── QuizQuestion ──────────────────────────────────────────────────────
        b.Entity<QuizQuestion>(e =>
        {
            e.HasIndex(qq => new { qq.QuizId, qq.Order });

            e.HasOne(qq => qq.Quiz)
             .WithMany(q => q.Questions)
             .HasForeignKey(qq => qq.QuizId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── QuizAttempt ───────────────────────────────────────────────────────
        b.Entity<QuizAttempt>(e =>
        {
            e.HasOne(a => a.User)
             .WithMany(u => u.QuizAttempts)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Quiz)
             .WithMany(q => q.Attempts)
             .HasForeignKey(a => a.QuizId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserAnswer ────────────────────────────────────────────────────────
        b.Entity<UserAnswer>(e =>
        {
            e.HasOne(ua => ua.Attempt)
             .WithMany(a => a.UserAnswers)
             .HasForeignKey(ua => ua.AttemptId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ua => ua.Question)
             .WithMany(q => q.UserAnswers)
             .HasForeignKey(ua => ua.QuestionId)
             .OnDelete(DeleteBehavior.Restrict);    // keep answer history if question deleted
        });

        // ── Comment ───────────────────────────────────────────────────────────
        b.Entity<Comment>(e =>
        {
            e.HasOne(c => c.User)
             .WithMany(u => u.Comments)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Lesson)
             .WithMany(l => l.Comments)
             .HasForeignKey(c => c.LessonId)
             .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing for threaded replies
            e.HasOne(c => c.Parent)
             .WithMany(c => c.Replies)
             .HasForeignKey(c => c.ParentId)
             .OnDelete(DeleteBehavior.Restrict);    // deleting parent doesn't cascade to replies
        });
    }
}