using Microsoft.EntityFrameworkCore;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<ContentBlock> ContentBlocks => Set<ContentBlock>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();

            entity.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(x => x.Token).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Course>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();

            entity.Property(x => x.Level)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        builder.Entity<Enrollment>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
            entity.Property(x => x.ProgressPercent).HasDefaultValue(0);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Lesson>(entity =>
        {
            entity.HasIndex(x => new { x.CourseId, x.OrderIndex }).IsUnique();

            entity.HasOne(x => x.Course)
                .WithMany(x => x.Lessons)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContentBlock>(entity =>
        {
            entity.HasIndex(x => new { x.LessonId, x.OrderIndex });

            entity.Property(x => x.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(x => x.CalloutVariant)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(x => x.ImageWidth)
                .HasConversion<string>()
                .HasMaxLength(10);

            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.ContentBlocks)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<LessonProgress>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.LessonProgresses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.LessonProgresses)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Quiz>(entity =>
        {
            entity.HasIndex(x => x.LessonId).IsUnique();

            entity.HasOne(x => x.Lesson)
                .WithOne(x => x.Quiz)
                .HasForeignKey<Quiz>(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuizQuestion>(entity =>
        {
            entity.HasIndex(x => new { x.QuizId, x.OrderIndex }).IsUnique();
        });

        builder.Entity<QuizAttempt>(entity =>
        {
            entity.HasOne(x => x.User)
                .WithMany(x => x.QuizAttempts)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Quiz)
                .WithMany(x => x.Attempts)
                .HasForeignKey(x => x.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserAnswer>(entity =>
        {
            entity.HasOne(x => x.Attempt)
                .WithMany(x => x.UserAnswers)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.UserAnswers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.HasIndex(x => new { x.LessonId, x.ParentCommentId, x.CreatedAtUtc });

            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Lesson)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ParentComment)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(builder);
    }
}
