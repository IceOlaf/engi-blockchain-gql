using RazorLight;

namespace Engi.Substrate.Server.Email;

public static class RazorLightExtensions
{
    public static async Task<string> RenderAsync(this IRazorLightEngine engine, string templateName, dynamic model)
    {
        var cached = engine.Handler.Cache.RetrieveTemplate(templateName);

        if (!cached.Success)
        {
            return await engine.CompileRenderAsync(templateName, model);
        }

        return await engine.RenderTemplateAsync(cached.Template.TemplatePageFactory(), model);
    }

    public static Task<string[]> RenderAsync(this IRazorLightEngine engine, string[] templateNames, dynamic model)
    {
        Task<string>[] tasks = templateNames
            .Select(templateName => (Task<string>)RenderAsync(engine, templateName, model))
            .ToArray();

        return Task.WhenAll(tasks);
    }

    public static Task<string[]> RenderSubjectAndContentAsync(this IRazorLightEngine engine, string templateName, dynamic model)
    {
        return RenderAsync(engine, new[]
        {
            $"{templateName}.Subject",
            $"{templateName}.Text",
            $"{templateName}.Html"
        }, model);
    }
}