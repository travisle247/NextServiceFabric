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
        private CodePackageActivationContext activationContext;
        private ConfigurationPackage configurationPackage;

        public NextGenCapture_RabbitMQService(StatelessServiceContext context)
            : base(context)
        {
            activationContext = FabricRuntime.GetActivationContext();
            configurationPackage = activationContext.GetConfigurationPackageObject("Config");          
        }

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


            ConnectionFactory factory = new ConnectionFactory
            {
                UserName = configurationPackage.Settings.Sections["RabbitMQConnection"].Parameters["Username"].Value,
                Password = configurationPackage.Settings.Sections["RabbitMQConnection"].Parameters["Password"].Value,
                VirtualHost = configurationPackage.Settings.Sections["RabbitMQConnection"].Parameters["VirtualHost"].Value,
                HostName = configurationPackage.Settings.Sections["RabbitMQConnection"].Parameters["HostName"].Value,
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
            try
            {
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: exchangeName, type: "topic", durable: true);

                        var ms = new MemoryStream();

                        using (var writer = new BsonWriter(ms))
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
