using Microsoft.Extensions.Configuration;

namespace JiraFlow.Infrastructure.Configuration;

public static class AppConfiguration
{
    /// <summary>
    /// Ordem de leitura (a última vence):
    /// 1. appsettings.json: valores padrão, sem dados sensíveis (vai para o repositório);
    /// 2. appsettings.local.json: valores reais da máquina (e-mail, URL, JQL); fica fora do repositório;
    /// 3. variáveis de ambiente JIRAFLOW_: é por onde entra o token (JIRAFLOW_Jira__ApiToken).
    /// </summary>
    public static IConfigurationRoot Carregar() =>
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("JIRAFLOW_")
            .Build();
}
