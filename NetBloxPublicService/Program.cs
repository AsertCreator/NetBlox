using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace NetBloxPublicService
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllers();
			builder.Services.AddDbContext<AssetContext>(opt => opt.UseSqlite("Data Source=commonassets.db"));
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Environment.ContentRootPath = Path.GetFullPath("./content/res");

			var app = builder.Build();

			app.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
				RequestPath = "/res"
			});
			app.MapGet("/join", new RequestDelegate(x =>
			{
				x.Response.StatusCode = 200;
				x.Response.ContentType = "text/html";
				return x.Response.WriteAsync(File.ReadAllText("./content/joingame.html"));
			}));
			app.MapGet("/login", new RequestDelegate(x =>
			{
				x.Response.StatusCode = 200;
				x.Response.ContentType = "text/html";
				return x.Response.WriteAsync(File.ReadAllText("./content/login.html"));
			}));
			app.MapGet("/search", new RequestDelegate(x =>
			{
				x.Response.StatusCode = 200;
				x.Response.ContentType = "text/html";
				return x.Response.WriteAsync(File.ReadAllText("./content/search.html"));
			}));
			app.MapGet("/", new RequestDelegate(x =>
			{
				x.Response.StatusCode = 200;
				x.Response.ContentType = "text/html";
				return x.Response.WriteAsync(File.ReadAllText("./content/index.html"));
			}));

			app.MapFallback(new RequestDelegate(x =>
			{
				x.Response.StatusCode = 404;
				x.Response.ContentType = "text/html";
				return x.Response.WriteAsync(File.ReadAllText("./content/notfound.html"));
			}));

			app.UseSwagger();
			app.UseSwaggerUI();

			app.UseHttpsRedirection();
			app.MapControllers();
			app.Run();
		}
	}
}
