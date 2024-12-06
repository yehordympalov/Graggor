using AttributeApi.Core.Register;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

services.AddAttributeApi();
services.AddLogging();
