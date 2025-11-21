namespace Ashish_Backend_Folio.Repositories.Interface
{
    public interface IUnitOfWork
    {
        IRefreshTokenRepository RefreshTokens { get; }
        Task<int> CommitAsync(CancellationToken ct = default);
    }

}
