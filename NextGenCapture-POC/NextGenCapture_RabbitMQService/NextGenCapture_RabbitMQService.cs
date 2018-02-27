using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NextGenCapture_Common.Model.Export;
using NextGenCapture_RabbitMQService.Model;
using RabbitMQ.Client;

namespace NextGenCapture_RabbitMQService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class NextGenCapture_RabbitMQService : StatelessService, IMQService
    {
        private const string exchangeName = "CaptureExchange";
        private const string routingKey = "WebService";

        public NextGenCapture_RabbitMQService(StatelessServiceContext context)
            : base(context)
        { }

        public Task<MessageResponse> ReceiveFromRabbitMessageQueue()
        {
            throw new NotImplementedException();
        }

        public async Task<MessageResponse> SendToRabbitMessageQueue(STPBatch batch)
        {

            MessageResponse response = new MessageResponse()
            {
                Success = true
            };

            var factory = new ConnectionFactory() { HostName = "localhost" };
            try
            {
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: exchangeName, type: "topic", durable: true);

                        var ms = new MemoryStream();

                        using (BsonWriter writer = new BsonWriter(ms))
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            serializer.Serialize(writer, batch);
                        }

                        var properties = channel.CreateBasicProperties();
                        properties.Persistent = true;

                        channel.BasicPublish(exchange : exchangeName,routingKey : routingKey,basicProperties : null, body: ms.ToArray());

                    }
                }
            }
            catch(Exception ex)
            {
                response.Success = false;
                throw new Exception(ex.Message);

            }

            return response;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
