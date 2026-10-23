using JiraFlow.Application.Ports;
using JiraFlow.Application.UseCases;
using JiraFlow.Infrastructure.Configuration;
using JiraFlow.Infrastructure.Jira;
using JiraFlow.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;

// Composição: lê a configuração, cria os adaptadores e entrega tudo ao caso de uso.
var config = AppConfiguration.Carregar();
var jiraOpcoes = config.GetSection("Jira").Get<JiraOptions>() ?? new JiraOptions();
var logOpcoes = config.GetSection("Logging").Get<LoggingOptions>() ?? new LoggingOptions();

var log = FileLoggerFactory.Criar(logOpcoes);
log.Info("Execução iniciada.");

// O Jira trata credencial vazia/inválida na busca como acesso anônimo (200 com 0 issues),
// então validamos a configuração antes para não confundir "sem permissão" com "sem resultados".
var erros = JiraOptionsValidator.Validar(jiraOpcoes);
if (erros.Count > 0)
{
    foreach (var erro in erros)
        log.Erro($"Configuração inválida: {erro}");

    return;
}

using var client = JiraHttpClientFactory.Criar(jiraOpcoes);
IJiraGateway jira = new JiraHttpGateway(client);

await new ProcessarChamadosUseCase(jira, log, jiraOpcoes.ParaProcessamento()).ExecutarAsync();

log.Info("Execução finalizada.");
