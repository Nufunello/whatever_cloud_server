using System;

namespace messager
{
    public delegate void ReceiveMessageHandler(object sender, ReadOnlyMemory<byte> e);
    public interface IMessager
    {
        void SendMessage(string body); 
        event ReceiveMessageHandler OnMessage;
    }
}
