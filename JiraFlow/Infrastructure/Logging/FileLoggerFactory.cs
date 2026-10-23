using JiraFlow.Application.Ports;
using JiraFlow.Infrastructure.Configuration;

namespace JiraFlow.Infrastructure.Logging;

public static class FileLoggerFactory
{
    public static ILogger Criar(LoggingOptions opcoes)
    {
        var nivelMinimo = Enum.TryParse<NivelLog>(opcoes.MinLevel, ignoreCase: true, out var nivel)
            ? nivel
            : NivelLog.INFO;

        // Path.Combine: se Directory for um caminho absoluto, ele prevalece sobre a raiz do projeto.
        return new FileLogger(
            Path.Combine(LocalizarRaizDoProjeto(), opcoes.Directory),
            tamanhoMaximoBytes: opcoes.MaxFileSizeMb * 1024 * 1024,
            arquivosRetidos: opcoes.RetainedFiles,
            nivelMinimo);
    }

    // Em desenvolvimento o .exe fica em bin\Debug\net8.0; subimos as pastas até achar o .csproj (raiz do projeto).
    // Publicado sem .csproj, usa a pasta do próprio executável.
    private static string LocalizarRaizDoProjeto()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;
        }

        return AppContext.BaseDirectory;
    }
}
