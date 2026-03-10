using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace skillexa_backend.Models;

// ─── Enum ─────────────────────────────────────────────────────────────────────

// Mock ContentBlock.type: "text" | "code" | "callout" | "image"
public enum ContentBlockType
{
    Text    = 0,
    Code    = 1,
    Callout = 2,
    Image   = 3,
}

// Mock CalloutBlock.variant: "info" | "warning" | "tip" | "danger"
public enum CalloutVariant
{
    Info    = 0,
    Tip     = 1,
    Warning = 2,
    Danger  = 3,
}

// Mock ImageBlock.width: "full" | "half" | "auto"
public enum ImageWidth
{
    Full = 0,
    Half = 1,
    Auto = 2,
}

// ─── Lesson ───────────────────────────────────────────────────────────────────

[Table("lessons")]
public class Lesson
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Mock: title
    [Required]
    [MaxLength(300)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    // Mock: order (1-based position inside course)
    [Column("order")]
    public int Order { get; set; } = 1;

    // Mock: duration — human-readable e.g. "15m", "1h 20m"
    [MaxLength(20)]
    [Column("duration")]
    public string? Duration { get; set; }

    [Column("is_free_preview")]
    public bool IsFreePreview { get; set; } = false;

    [Column("course_id")]
    public Guid CourseId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    // Mock: contentBlocks[]
    public ICollection<ContentBlock>  ContentBlocks  { get; set; } = [];

    // Mock: quiz (one per lesson)
    public Quiz?                      Quiz           { get; set; }

    // Mock: done — per-user lesson completion
    public ICollection<LessonProgress> LessonProgresses { get; set; } = [];

    public ICollection<Comment> Comments { get; set; } = [];
}

// ─── ContentBlock ─────────────────────────────────────────────────────────────
// Single-table approach — discriminated by Type column.
// Nullable columns used only when relevant to that block type.
//
// Text    → content
// Code    → code_content, lang, filename
// Callout → content, callout_variant
// Image   → image_url, image_alt, image_caption, image_width

[Table("content_blocks")]
public class ContentBlock
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("lesson_id")]
    public Guid LessonId { get; set; }

    // Display order within the lesson
    [Column("order")]
    public int Order { get; set; } = 0;

    // Mock: ContentBlock.type — "text" | "code" | "callout" | "image"
    [Column("type")]
    public ContentBlockType Type { get; set; }

    // ── TextBlock ─────────────────────────────────────────────
    // Mock: TextBlock.content
    [Column("content")]
    public string? Content { get; set; }

    // ── CodeBlock ─────────────────────────────────────────────
    // Mock: CodeBlock.content (the actual code string)
    [Column("code_content")]
    public string? CodeContent { get; set; }

    // Mock: CodeBlock.lang — "tsx" | "ts" | "js" | "bash" | "json" | "sql" ...
    [MaxLength(30)]
    [Column("lang")]
    public string? Lang { get; set; }

    // Mock: CodeBlock.filename — optional e.g. "HelloWorld.tsx"
    [MaxLength(200)]
    [Column("filename")]
    public string? Filename { get; set; }

    // ── CalloutBlock ──────────────────────────────────────────
    // Mock: CalloutBlock.variant — "info" | "warning" | "tip" | "danger"
    [Column("callout_variant")]
    public CalloutVariant? CalloutVariant { get; set; }

    // CalloutBlock.content shares the [content] column above

    // ── ImageBlock ────────────────────────────────────────────
    // Mock: ImageBlock.url — Firebase Storage download URL
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    // Mock: ImageBlock.alt
    [MaxLength(500)]
    [Column("image_alt")]
    public string? ImageAlt { get; set; }

    // Mock: ImageBlock.caption (optional)
    [Column("image_caption")]
    public string? ImageCaption { get; set; }

    // Mock: ImageBlock.width — "full" | "half" | "auto"
    [Column("image_width")]
    public ImageWidth? ImageWidth { get; set; }

    // Navigation
    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}

// ─── LessonProgress ───────────────────────────────────────────────────────────
// Mock: Lesson.done — per-user completion state

[Table("lesson_progresses")]
public class LessonProgress
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("lesson_id")]
    public Guid LessonId { get; set; }

    // Mock: Lesson.done
    [Column("done")]
    public bool Done { get; set; } = false;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}