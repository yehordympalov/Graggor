﻿using Microsoft.AspNetCore.Routing.Patterns;

namespace AttributeApi.Core.Attributes;

public abstract class HttpEndpointAttribute(string httpMethodType, string route) : EndpointAttribute(httpMethodType, route);