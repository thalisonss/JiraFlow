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

        // Por padrão a pasta fica ao lado do executável. Path.Combine: se Directory for um
        // caminho absoluto (ex.: D:\Logs\JiraFlow), ele prevalece e a pasta base é ignorada.
        return new FileLogger(
            Path.Combine(AppContext.BaseDirectory, opcoes.Directory),
            tamanhoMaximoBytes: opcoes.MaxFileSizeMb * 1024 * 1024,
            arquivosRetidos: opcoes.RetainedFiles,
            nivelMinimo);
    }
}
