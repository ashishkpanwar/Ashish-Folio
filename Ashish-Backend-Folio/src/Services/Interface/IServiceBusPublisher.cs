namespace Ashish_Backend_Folio.Services.Interface
{
    public interface IServiceBusPublisher
    {
        Task PublishAsync<T>(string entityName, T payload);
    }
}
