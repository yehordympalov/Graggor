namespace AttributeApi.Attributes;

public class PostAttribute(string route = "") : EndpointAttribute("POST", route);

public class GetAttribute(string route = "") : EndpointAttribute("GET", route);

public class DeleteAttribute(string route = "") : EndpointAttribute("DELETE", route);

public class PutAttribute(string route = "") : EndpointAttribute("PUT", route);

public class PatchAttribute(string route = "") : EndpointAttribute("PATCH", route);
