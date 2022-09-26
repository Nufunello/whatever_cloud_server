using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;

namespace messager
{
    public class RabbitmqMessager : IMessager
    {
        readonly string exchange = "amq.direct";
        readonly IModel channel;
        readonly string publishRoutingKey;
        public RabbitmqMessager(IModel channel, string receiveRoutingKey = "", string publishRoutingKey = "")
        {
            this.channel = channel;
            this.publishRoutingKey = publishRoutingKey;

            if (!string.IsNullOrEmpty(receiveRoutingKey))
            {
                var queue = channel.QueueDeclare(autoDelete: false, exclusive: false, durable: true, queue: receiveRoutingKey).QueueName;
                channel.QueuePurge(queue);
                channel.QueueBind(queue: queue, exchange: exchange, receiveRoutingKey);
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (sender, ea) => { OnMessage?.Invoke(sender, ea.Body); };
                channel.BasicConsume(queue: queue, consumer: consumer);
            }
        }
        public event ReceiveMessageHandler OnMessage;

        public void SendMessage(string body)
        {
            if (!string.IsNullOrEmpty(publishRoutingKey))
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(body);
                channel.BasicPublish(exchange: exchange, routingKey: publishRoutingKey, mandatory: false, basicProperties: null, body: bytes);
                channel.WaitForConfirms();
            }
        }
    }
}
