namespace AttributeApi.Attributes;

public class PostAttribute(string route) : EndpointAttribute("post", route);

public class GetAttribute(string route) : EndpointAttribute("get", route);

public class DeleteAttribute(string route) : EndpointAttribute("delete", route);

public class PutAttribute(string route) : EndpointAttribute("put", route);

public class PatchAttribute(string route) : EndpointAttribute("patch", route);
