using System.Net;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Domain.Entities;
using skillexa_backend.Infrastructure.Data;

namespace skillexa_backend.Features.Quizzes;

public sealed class QuizService(AppDbContext dbContext) : IQuizService
{
    public async Task<QuizDto> GetByLessonAsync(Guid lessonId, bool includeAnswers, CancellationToken cancellationToken)
    {
        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.LessonId == lessonId, cancellationToken)
            ?? throw new AppException("Quiz was not found.", HttpStatusCode.NotFound);

        return Map(quiz, includeAnswers);
    }

    public async Task<QuizDto> UpsertAsync(Guid lessonId, UpsertQuizRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Questions.Count == 0)
        {
            throw new AppException("Quiz title and questions are required.");
        }

        if (!await dbContext.Lessons.AnyAsync(x => x.Id == lessonId, cancellationToken))
        {
            throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);
        }

        var quiz = await dbContext.Quizzes.Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.LessonId == lessonId, cancellationToken);

        if (quiz is null)
        {
            quiz = new Quiz { LessonId = lessonId };
            dbContext.Quizzes.Add(quiz);
        }
        else
        {
            dbContext.QuizQuestions.RemoveRange(quiz.Questions);
        }

        quiz.Title = request.Title.Trim();
        quiz.UpdatedAtUtc = DateTime.UtcNow;
        quiz.Questions = request.Questions.OrderBy(x => x.OrderIndex).Select(x =>
        {
            ValidateQuestion(x);
            return new QuizQuestion
            {
                QuizId = quiz.Id,
                OrderIndex = x.OrderIndex,
                Prompt = x.Prompt.Trim(),
                Options = x.Options.Select(o => o.Trim()).ToList(),
                CorrectOptionIndex = x.CorrectOptionIndex,
                Explanation = string.IsNullOrWhiteSpace(x.Explanation) ? null : x.Explanation.Trim()
            };
        }).ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(quiz, includeAnswers: true);
    }

    public async Task<QuizResultDto> SubmitAsync(Guid userId, Guid lessonId, SubmitQuizRequest request, CancellationToken cancellationToken)
    {
        var lesson = await dbContext.Lessons.FirstOrDefaultAsync(x => x.Id == lessonId, cancellationToken)
            ?? throw new AppException("Lesson was not found.", HttpStatusCode.NotFound);

        var isEnrolled = await dbContext.Enrollments.AnyAsync(x => x.UserId == userId && x.CourseId == lesson.CourseId, cancellationToken);
        if (!isEnrolled)
        {
            throw new AppException("User is not enrolled in this course.");
        }

        var quiz = await dbContext.Quizzes
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.LessonId == lessonId, cancellationToken)
            ?? throw new AppException("Quiz was not found.", HttpStatusCode.NotFound);

        var answersByQuestion = request.Answers.ToDictionary(x => x.QuestionId, x => x.SelectedOptionIndex);
        var attempt = new QuizAttempt
        {
            UserId = userId,
            QuizId = quiz.Id,
            TotalQuestions = quiz.Questions.Count,
            SubmittedAtUtc = DateTime.UtcNow
        };

        var resultQuestions = new List<QuizResultQuestionDto>();
        foreach (var question in quiz.Questions.OrderBy(x => x.OrderIndex))
        {
            answersByQuestion.TryGetValue(question.Id, out var selectedIndex);
            var isCorrect = selectedIndex == question.CorrectOptionIndex;
            if (isCorrect)
            {
                attempt.CorrectCount++;
            }

            attempt.UserAnswers.Add(new UserAnswer
            {
                QuestionId = question.Id,
                SelectedOptionIndex = selectedIndex,
                IsCorrect = isCorrect
            });

            resultQuestions.Add(new QuizResultQuestionDto(
                question.Id,
                question.Prompt,
                selectedIndex,
                question.CorrectOptionIndex,
                isCorrect,
                question.Explanation));
        }

        attempt.Score = attempt.CorrectCount;
        dbContext.QuizAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new QuizResultDto(attempt.Id, quiz.Id, attempt.Score, attempt.CorrectCount, attempt.TotalQuestions, attempt.SubmittedAtUtc, resultQuestions);
    }

    private static void ValidateQuestion(QuizQuestionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new AppException("Question prompt is required.");
        }

        if (request.Options.Count < 2)
        {
            throw new AppException("Question must have at least two options.");
        }

        if (request.CorrectOptionIndex < 0 || request.CorrectOptionIndex >= request.Options.Count)
        {
            throw new AppException("Correct option index is out of range.");
        }
    }

    private static QuizDto Map(Quiz quiz, bool includeAnswers)
        => new(
            quiz.Id,
            quiz.LessonId,
            quiz.Title,
            quiz.Questions.OrderBy(x => x.OrderIndex).Select(x => new QuizQuestionDto(
                x.Id,
                x.OrderIndex,
                x.Prompt,
                x.Options,
                x.Explanation,
                includeAnswers ? x.CorrectOptionIndex : null)).ToList());
}
