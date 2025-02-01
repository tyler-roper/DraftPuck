namespace DraftPuck.Core.Services.Interfaces;
public interface IErrorService
{
    public Task Log(ErrorRequest request);
}
