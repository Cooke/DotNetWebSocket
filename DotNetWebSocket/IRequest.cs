using System;
using DotNetWebSocket.Collections;

namespace DotNetWebSocket
{
    public interface IRequest
    {
        string Resource { get; }

        IFieldCollection AllFields { get; }

        IFieldCollection Cookies { get; }
    }
}