using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace messager
{
    public class Service
        : IDisposable
    {
        readonly IConnection connection;
        readonly IModel channel;

        public Service(string hostname)
        {
            var factory = new ConnectionFactory { HostName = hostname };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ModelShutdown += (sender, args) =>
            {
                Console.WriteLine("");
            };
        }

        public IModel Channel => channel;

        public void Dispose()
        {
            channel.Dispose();
            connection.Dispose();
        }
        ~Service()
        {
            Console.WriteLine("Destructe");
        }
    }
}
