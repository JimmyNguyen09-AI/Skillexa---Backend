using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace skillexa_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = current_schema()
                          AND table_name = 'courses'
                          AND column_name = 'categories'
                    ) THEN
                        ALTER TABLE courses
                        ADD COLUMN categories text[] NOT NULL DEFAULT ARRAY['Fundamentals']::text[];
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = current_schema()
                          AND table_name = 'courses'
                          AND column_name = 'category'
                    ) THEN
                        UPDATE courses
                        SET categories = CASE
                            WHEN category IS NULL OR btrim(category) = '' THEN ARRAY['Fundamentals']::text[]
                            ELSE ARRAY[category]::text[]
                        END
                        WHERE categories IS NULL
                           OR cardinality(categories) = 0
                           OR categories = ARRAY['Fundamentals']::text[];

                        ALTER TABLE courses DROP COLUMN category;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = current_schema()
                          AND table_name = 'courses'
                          AND column_name = 'category'
                    ) THEN
                        ALTER TABLE courses
                        ADD COLUMN category character varying(30) NOT NULL DEFAULT 'Fundamentals';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = current_schema()
                          AND table_name = 'courses'
                          AND column_name = 'categories'
                    ) THEN
                        UPDATE courses
                        SET category = COALESCE(categories[1], 'Fundamentals');

                        ALTER TABLE courses DROP COLUMN categories;
                    END IF;
                END $$;
                """);
        }
    }
}
