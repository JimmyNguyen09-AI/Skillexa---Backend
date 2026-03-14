using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using skillexa_backend.Domain.Enums;

namespace skillexa_backend.Domain.Entities;

[Table("content_blocks")]
public sealed class ContentBlock
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("lesson_id")]
    public Guid LessonId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("type")]
    public ContentBlockType Type { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("code_content")]
    public string? CodeContent { get; set; }

    [MaxLength(50)]
    [Column("language")]
    public string? Language { get; set; }

    [MaxLength(200)]
    [Column("file_name")]
    public string? FileName { get; set; }

    [Column("callout_variant")]
    public CalloutVariant? CalloutVariant { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    [Column("image_alt")]
    public string? ImageAlt { get; set; }

    [Column("image_caption")]
    public string? ImageCaption { get; set; }

    [Column("image_width")]
    public ImageWidth? ImageWidth { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}
