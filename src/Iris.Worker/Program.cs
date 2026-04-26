using Iris.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<IrisWorkerService>();

var host = builder.Build();
host.Run();
