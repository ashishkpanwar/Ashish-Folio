namespace Ashish_Backend_Folio.Data.Repositories.Interface
{
    public interface IUnitOfWork
    {
        IRefreshTokenRepository RefreshTokens { get; }
        Task<int> CommitAsync(CancellationToken ct = default);
    }

}
