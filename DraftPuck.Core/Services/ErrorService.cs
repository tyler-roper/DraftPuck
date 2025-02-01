
using DraftPuck.Infrastructure.Database;

namespace DraftPuck.Core.Services;
public class ErrorService : IErrorService
{
    private readonly DraftPuckContext _dbContext;

    public ErrorService(DraftPuckContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Log(ErrorRequest request)
    {
        var errorLog = new ErrorLog()
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            Info = request.Info
        };

        try
        {
            errorLog.Error = System.Text.Json.JsonSerializer.Serialize(request.Error);
        } catch
        {
            Console.WriteLine($"Unable to serialize error log (ID: {errorLog.Id}");
        }

        _dbContext.ErrorLogs.Add(errorLog);
        await _dbContext.SaveChangesAsync();
    }
}
