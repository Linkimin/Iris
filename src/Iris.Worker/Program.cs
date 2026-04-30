using Iris.Worker.Services;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<IrisWorkerService>();

IHost host = builder.Build();
host.Run();
