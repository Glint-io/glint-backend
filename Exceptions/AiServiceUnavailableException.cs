using System;

namespace glint_backend.Exceptions;

public class AiServiceUnavailableException : Exception
{
    public AiServiceUnavailableException() { }
    public AiServiceUnavailableException(string message) : base(message) { }
    public AiServiceUnavailableException(string message, Exception inner) : base(message, inner) { }
}