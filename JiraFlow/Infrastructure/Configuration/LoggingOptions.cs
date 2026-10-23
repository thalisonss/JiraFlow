namespace JiraFlow.Infrastructure.Configuration;

/// <summary>
/// Seção "Logging" do appsettings.json. Os valores iniciais valem quando a chave não existe no JSON.
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>Pasta dos logs, relativa à raiz do projeto (ou caminho absoluto).</summary>
    public string Directory { get; set; } = "logs";

    /// <summary>INFO, WARN ou ERRO. Valor inválido cai em INFO.</summary>
    public string MinLevel { get; set; } = "INFO";

    public long MaxFileSizeMb { get; set; } = 5;

    public int RetainedFiles { get; set; } = 10;
}
