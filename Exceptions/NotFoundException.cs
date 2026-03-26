using System;

namespace BAMF_API.Exceptions
{
    public class NotFoundException : Exception
    {
        public string Resource { get; }
        public string? Identifier { get; }

        public NotFoundException(string resource)
            : base($"{resource} not found.")
        {
            Resource = resource;
        }

        public NotFoundException(string resource, string? identifier)
            : base(identifier == null ? $"{resource} not found." : $"{resource} '{identifier}' not found.")
        {
            Resource = resource;
            Identifier = identifier;
        }
    }
}
