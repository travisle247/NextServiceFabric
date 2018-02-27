using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using NextGenCapture_Common.Model.Export;
using System;
using System.Threading.Tasks;

[assembly: FabricTransportServiceRemotingProvider(RemotingListener = RemotingListener.V2Listener, RemotingClient = RemotingClient.V2Client)]

namespace NextGenCapture_RabbitMQService.Model
{
    public interface IMQService : IService
    {
        Task<MessageResponse> SendToRabbitMessageQueue(STPBatch batch);
        Task<MessageResponse> ReceiveFromRabbitMessageQueue();
    }
}
