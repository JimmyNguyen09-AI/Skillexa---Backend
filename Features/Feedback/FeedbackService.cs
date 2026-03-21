using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using skillexa_backend.Common.Exceptions;
using skillexa_backend.Infrastructure.Data;
using FeedbackEntity = skillexa_backend.Domain.Entities.Feedback;

namespace skillexa_backend.Features.Feedback;

public sealed class FeedbackService(AppDbContext dbContext) : IFeedbackService
{
    public async Task SubmitAsync(FeedbackRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var message = request.Message.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new AppException("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out var replyToAddress))
        {
            throw new AppException("A valid email is required.");
        }

        if (string.IsNullOrWhiteSpace(message) || message.Length < 10)
        {
            throw new AppException("Feedback message must be at least 10 characters.");
        }

        dbContext.Add(new FeedbackEntity
        {
            Name = name,
            Email = replyToAddress.Address,
            Message = message
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeedbackSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Feedback
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new FeedbackSummaryDto(x.Id, x.Name, x.Email, x.Message, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
