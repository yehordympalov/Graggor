using Microsoft.AspNetCore.Http;

namespace AttributeApi.Exceptions;

public class AttributeApiException(string message, IResult result): Exception(message)
{
    public IResult Result { get; } = result;
}
