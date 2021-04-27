using System;

namespace VerifyNodeModules
{
    public class JsonException : Exception
    {
        public JsonException(string path, Exception jsonException)
            : base($"Error loading '{path}': {jsonException.Message}", jsonException)
        {
        }
    }
}