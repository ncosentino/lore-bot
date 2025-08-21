using NexusLabs.Needlr.AspNet;

internal sealed class WebPlugin : 
    IWebApplicationBuilderPlugin,
    IWebApplicationPlugin
{
    public void Configure(WebApplicationBuilderPluginOptions options)
    {
        // Add controllers
        options.Builder.Services.AddControllers();

        // Add API documentation
        options.Builder.Services.AddEndpointsApiExplorer();
        options.Builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "LoreBot API",
                Version = "v1",
                Description = "Retrieval-Augmented Generation system for game lore knowledge base"
            });
        });

        // Add CORS
        options.Builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public void Configure(WebApplicationPluginOptions options)
    {
        // Configure the HTTP request pipeline
        var app = options.WebApplication;
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoreBot API v1");
                c.RoutePrefix = string.Empty;
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.MapControllers();
    }
}
