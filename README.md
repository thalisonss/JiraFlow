# JiraFlow

Aplicação .NET 8 que automatiza a **triagem inicial de chamados no Jira Cloud**: busca os chamados que atendem a uma consulta JQL, move cada um para um status de destino, atribui ao usuário que executa a aplicação e, opcionalmente, registra um comentário (público ou interno) no chamado.

A aplicação roda **sem janela** (`WinExe`), pensada para execução agendada. Todo o acompanhamento é feito por arquivos de log.

---

## Sumário

1. [Como funciona](#como-funciona)
2. [Requisitos](#requisitos)
3. [Configuração](#configuração)
4. [Execução](#execução)
5. [Agendamento no Windows](#agendamento-no-windows)
6. [Logs](#logs)
7. [Arquitetura](#arquitetura)
8. [Segurança](#segurança)
9. [Solução de problemas](#solução-de-problemas)
10. [Limitações conhecidas](#limitações-conhecidas)

---

## Como funciona

A cada execução, a aplicação percorre o fluxo abaixo.

```
Valida a configuração
        │
        ▼
Autentica no Jira (GET /myself) ──► obtém o accountId do dono do token
        │
        ▼
Busca os chamados da JQL (com paginação)
        │
        ▼  para cada chamado
Procura a transição que leva ao status de destino ──► não existe: registra WARN e segue
        │
        ▼
Move o chamado ──► falhou: registra ERRO e segue para o próximo
        │
        ▼
Atribui ao usuário do token   (opcional: AssignToMe)
        │
        ▼
Comenta no chamado            (opcional: Comment preenchido)
        │
        ▼
Registra o resumo da execução no log
```

Pontos importantes:

- **Um chamado com problema não interrompe os demais.** Cada chamado é tratado de forma isolada.
- **A atribuição só ocorre depois de mover com sucesso**, e o **comentário só é enviado se a atribuição não falhar**.
- **A execução é repetível.** Depois de movido, o chamado deixa de atender à JQL (porque o status mudou), então não é processado novamente. Por isso, **a JQL nunca deve incluir o status de destino**.
- Se não houver transição do status atual do chamado para o status de destino (conforme o workflow do projeto), o chamado é ignorado e registrado como `WARN`.

---

## Requisitos

| Item | Detalhe |
|---|---|
| SDK | [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Sistema | Windows (o executável é `WinExe`) |
| Jira | Jira Cloud com API REST v3 |
| Credencial | E-mail da conta Atlassian e um **API Token** da própria conta |
| Comentário ao cliente | Projeto **Jira Service Management** (usa o endpoint `servicedeskapi`) |

### Permissões necessárias no Jira

A conta dona do token precisa, nos projetos alvo, das permissões de **navegar no projeto**, **transitar chamados**, **ser atribuível** (assignable user) e **adicionar comentários**.

---

## Configuração

A configuração é lida de três fontes. **A última vence**:

| Ordem | Fonte | Uso | Versionado? |
|---|---|---|---|
| 1 | `appsettings.json` | Valores padrão e modelo, sem dados sensíveis | Sim |
| 2 | `appsettings.local.json` | Valores reais da sua máquina (URL, e-mail, JQL) | **Não** |
| 3 | Variáveis de ambiente `JIRAFLOW_*` | Token e qualquer ajuste pontual | Não se aplica |

### Passo 1: gerar o API Token

1. Acesse <https://id.atlassian.com/manage-profile/security/api-tokens>.
2. Crie um token e **copie-o imediatamente**. Ele não é exibido de novo.
3. Nunca grave o token em arquivo do projeto, em chat, e-mail ou commit.

### Passo 2: definir o token como variável de ambiente

No PowerShell (escopo do usuário, persiste entre reinicializações):

```powershell
[Environment]::SetEnvironmentVariable("JIRAFLOW_Jira__ApiToken", "SEU_TOKEN_AQUI", "User")
```

> O prefixo `JIRAFLOW_` é removido pela aplicação, e o `__` (dois underlines) equivale ao `:` da hierarquia do JSON. Assim, `JIRAFLOW_Jira__ApiToken` preenche `Jira:ApiToken`.

**Feche e reabra** o terminal, o Visual Studio e qualquer processo que precise do valor. Processos já abertos não enxergam variáveis criadas depois.

### Passo 3: criar o `appsettings.local.json`

Na pasta do projeto (ao lado do `appsettings.json`), crie o arquivo com os seus valores reais:

```json
{
  "Jira": {
    "Url": "https://sua-empresa.atlassian.net/",
    "Email": "seu.email@empresa.com",
    "Jql": "project IN (ABC, XYZ) AND status IN (\"Aberto\", \"Reaberto\") AND organizations IN (\"Cliente\")"
  }
}
```

Esse arquivo está no `.gitignore`. Verifique antes do primeiro commit que ele **não** aparece em `git status`.

### Referência das chaves

#### Seção `Jira`

| Chave | Tipo | Padrão | Descrição |
|---|---|---|---|
| `Url` | texto | n/d | URL do Jira Cloud. Obrigatória e deve ser `https`. |
| `Email` | texto | n/d | E-mail da conta dona do token. Obrigatório. |
| `ApiToken` | texto | n/d | **Não preencher no JSON.** Informar pela variável `JIRAFLOW_Jira__ApiToken`. |
| `Jql` | texto | n/d | Consulta que seleciona os chamados. Obrigatória. |
| `TargetStatus` | texto | n/d | Nome do status de **destino** da transição, por exemplo `Work In Progress`. A comparação ignora maiúsculas e minúsculas. Obrigatório. |
| `AssignToMe` | booleano | `true` | Atribui o chamado ao dono do token depois de movê-lo. |
| `Comment` | texto | vazio | Texto do comentário. Vazio desativa o comentário. Quebras de linha são preservadas. |
| `CommentInternal` | booleano | `false` | `false`: comentário **público**, visível ao cliente. `true`: nota **interna**. |

#### Seção `Logging`

| Chave | Tipo | Padrão | Descrição |
|---|---|---|---|
| `Directory` | texto | `logs` | Pasta dos logs. Relativa à pasta do executável, ou caminho absoluto (por exemplo `D:\\Logs\\JiraFlow` no JSON). Criada se não existir. |
| `MinLevel` | texto | `INFO` | Nível mínimo registrado: `INFO`, `WARN` ou `ERRO`. Valor inválido cai em `INFO`. |
| `MaxFileSizeMb` | número | `5` | Tamanho a partir do qual o arquivo ativo é rotacionado. |
| `RetainedFiles` | número | `10` | Quantidade de arquivos rotacionados mantidos. Os mais antigos são apagados. |

### Ajustes pontuais por variável de ambiente

Qualquer chave pode ser sobrescrita sem editar arquivo. Exemplos:

```powershell
$env:JIRAFLOW_Jira__CommentInternal = "true"      # só nesta sessão do PowerShell
$env:JIRAFLOW_Logging__MinLevel     = "WARN"
```

### Como escrever a JQL

- Liste apenas os **status de origem** (os que devem ser triados). Não inclua o `TargetStatus`.
- Use aspas duplas para nomes com espaço e escape-as no JSON: `\"Waiting for support\"`.
- Escreva tudo em **uma linha**. JSON não aceita quebra dentro de texto.
- **Valide a JQL antes** em *Filters > Advanced issue search* do Jira e confira quantos e quais chamados ela retorna. A aplicação vai agir sobre todos.

### Validação na partida

Antes de chamar o Jira, a aplicação confere `Url` (https), `Email`, `ApiToken`, `Jql` e `TargetStatus`. Qualquer problema é registrado em `[ERRO] Configuração inválida: ...` e a execução termina sem alterar nada.

---

## Execução

### Desenvolvimento

```powershell
cd JiraFlow
dotnet build
dotnet run
```

Como o executável não abre janela, **não aparece saída no terminal**. Acompanhe pelo [log](#logs).

> **Atenção:** `dotnet run` com a JQL de produção **altera chamados reais** (move, atribui e comenta; o comentário público chega ao cliente). Para testar, use uma JQL restrita a um chamado seu, por exemplo `"Jql": "key = ABC-123"`, e `"CommentInternal": true`.

### Publicação

```powershell
dotnet publish -c Release -o publish
```

Copie para a pasta `publish` o `appsettings.local.json` (o build já o copia, se existir) e execute `JiraFlow.exe`. A pasta `logs` é criada ao lado do executável, salvo se `Logging:Directory` apontar para outro local.

---

## Agendamento no Windows

Para executar periodicamente, use o **Agendador de Tarefas**:

1. *Criar Tarefa* (não *Criar Tarefa Básica*), com o **mesmo usuário** que definiu a variável `JIRAFLOW_Jira__ApiToken`.
2. Aba **Gatilhos**: defina a recorrência (por exemplo, a cada 5 minutos).
3. Aba **Ações**: *Iniciar um programa* → `JiraFlow.exe` da pasta `publish`.
   Em *Iniciar em*, informe a pasta `publish`.
4. Aba **Configurações**: marque *Não iniciar uma nova instância* se a tarefa ainda estiver em execução.

Se a tarefa rodar com outra conta, ou como `SYSTEM`, a variável precisa existir no escopo dessa conta (ou no escopo `Machine`).

---

## Logs

- **Local:** pasta `logs` **ao lado do executável** (em desenvolvimento, `bin\Debug\net8.0\logs`). Pode ser alterado em `Logging:Directory`, com caminho relativo ou absoluto.
- **Formato:** `data hora.milissegundos [NÍVEL] mensagem`

```
2026-10-23 15:14:51.982 [INFO] Execução iniciada.
2026-10-23 15:14:52.340 [INFO] 3 issue(s) encontrada(s).
2026-10-23 15:14:52.901 [INFO] ABC-101: movido para 'Work In Progress'.
2026-10-23 15:14:53.210 [INFO] ABC-101: atribuído ao usuário do token.
2026-10-23 15:14:53.544 [INFO] ABC-101: comentário adicionado (público).
2026-10-23 15:14:53.700 [WARN] ABC-102: sem transição disponível para 'Work In Progress'.
2026-10-23 15:14:54.120 [ERRO] ABC-103: falha ao mover (400) { ... }
2026-10-23 15:14:54.130 [INFO] Resumo: 3 encontrado(s), 1 movido(s), 1 sem transição, 1 falha(s).
2026-10-23 15:14:54.131 [INFO] Execução finalizada.
```

| Nível | Quando ocorre |
|---|---|
| `INFO` | Fluxo normal: início, totais, chamado movido, atribuído, comentado, resumo |
| `WARN` | Situação esperada que impede a ação: chamado sem transição para o status de destino |
| `ERRO` | Falha: configuração inválida, autenticação, mover, atribuir, comentar, exceções |

**Rotação:** o arquivo ativo é `jiraflow.log`. Ao virar o dia **ou** ultrapassar `MaxFileSizeMb`, ele é renomeado para `jiraflow-AAAAMMDD-HHmmss-fff.log`. Só os `RetainedFiles` mais recentes são mantidos.

Os logs não registram o token nem o cabeçalho de autenticação. Mesmo assim, não os publique: podem conter chaves de chamados e trechos de respostas do Jira.

---

## Arquitetura

O projeto segue **arquitetura hexagonal** (portas e adaptadores). A regra de negócio fica no centro e depende apenas de interfaces (portas). Tecnologia (HTTP, arquivo, JSON) fica nos adaptadores, na borda.

```
                ┌───────────────────────────────────────────┐
                │                Application                │
                │                                           │
 Infrastructure │   ProcessarChamadosUseCase                │ Infrastructure
 (entrada)      │            │                │             │ (saída)
                │            ▼                ▼             │
 appsettings ──►│        IJiraGateway      ILogger          │──► Jira (HTTP)
 variáveis      │          (portas)        (portas)         │──► arquivo de log
                └───────────────────────────────────────────┘
```

A dependência sempre aponta **para dentro**: `Application` não referencia nada de `Infrastructure`.

### Estrutura de pastas

```
JiraFlow/
├── Application/
│   ├── Models/
│   │   ├── ProcessamentoOptions.cs      Parâmetros do processamento
│   │   ├── Resultado.cs                 Resultado de operação sem valor
│   │   ├── ResultadoDeValor.cs          Resultado<T> (operação com valor)
│   │   └── ResumoProcessamento.cs       Totais da execução
│   ├── Ports/
│   │   ├── IJiraGateway.cs              Porta: o que a aplicação pede ao Jira
│   │   └── ILogger.cs                   Porta: registro de eventos
│   └── UseCases/
│       └── ProcessarChamadosUseCase.cs  Regra do fluxo
├── Infrastructure/
│   ├── Configuration/
│   │   ├── AppConfiguration.cs          Leitura do JSON e das variáveis de ambiente
│   │   ├── JiraOptions.cs               Seção "Jira" tipada
│   │   ├── LoggingOptions.cs            Seção "Logging" tipada
│   │   └── JiraOptionsValidator.cs      Validação na partida
│   ├── Jira/
│   │   ├── JiraHttpGateway.cs           Adaptador: API REST do Jira
│   │   └── JiraHttpClientFactory.cs     HttpClient com URL base e autenticação
│   └── Logging/
│       ├── FileLogger.cs                Adaptador: log em arquivo com rotação
│       ├── FileLoggerFactory.cs         Cria o logger a partir da configuração
│       └── NivelLog.cs                  INFO, WARN, ERRO
├── Program.cs                           Composição: monta as peças e executa
├── appsettings.json                     Modelo de configuração (sem dados sensíveis)
└── JiraFlow.csproj
```

### Endpoints do Jira utilizados

| Operação | Endpoint |
|---|---|
| Autenticar e obter o `accountId` | `GET /rest/api/3/myself` |
| Buscar chamados (paginação por `nextPageToken`) | `POST /rest/api/3/search/jql` |
| Listar transições disponíveis | `GET /rest/api/3/issue/{chave}/transitions` |
| Mover o chamado | `POST /rest/api/3/issue/{chave}/transitions` |
| Atribuir | `PUT /rest/api/3/issue/{chave}/assignee` |
| Comentar (público ou interno) | `POST /rest/servicedeskapi/request/{chave}/comment` |

---

## Segurança

- O **API Token** só existe em variável de ambiente. Nunca em arquivo, log ou commit.
- `appsettings.local.json`, `logs/`, `*.log` e `.env` estão no `.gitignore`.
- A URL do Jira precisa ser `https`; a validação recusa outras.
- O Basic Auth usa `e-mail:token` em UTF-8 e é enviado apenas ao host configurado.
- **Se um token for exposto, revogue-o imediatamente** em <https://id.atlassian.com/manage-profile/security/api-tokens> e gere outro. Apagar o arquivo não basta: o valor permanece no histórico do repositório.
- Antes do primeiro commit, rode `git status` e confirme que nenhum arquivo sensível está na lista.
- O comentário público é visível ao cliente. Revise o texto de `Comment` antes de habilitar em produção.

---

## Solução de problemas

Consulte sempre o log primeiro.

| Sintoma no log | Causa provável | O que fazer |
|---|---|---|
| `Configuração inválida: Jira:ApiToken não informado` | O processo não enxerga a variável de ambiente | Defina `JIRAFLOW_Jira__ApiToken` e **reabra** terminal e Visual Studio. Confira o nome (dois `_` entre `Jira` e `ApiToken`). |
| `Autenticação falhou no Jira (401)` | Token inválido, revogado, ou e-mail diferente do dono do token | Gere um novo token e confira o e-mail em `appsettings.local.json`. |
| `0 issue(s) encontrada(s)` e você vê os chamados no Jira | JQL diferente da usada no Jira, ou os nomes dos status não batem | Copie a JQL do filtro que funciona. Confira o nome **exato** dos status (por exemplo `Aberto`, não `Open`). |
| `sem transição disponível para '...'` | O workflow do chamado não tem transição direta do status atual para o destino | Verifique o workflow do projeto. É o comportamento esperado para esse chamado. |
| `falha ao mover (400)` | A transição exige campos obrigatórios na tela de transição | Leia o corpo da resposta registrado no log para ver o campo exigido. |
| `falha ao atribuir (4xx)` | A conta não tem a permissão "Assignable user" no projeto | Ajuste a permissão no projeto. |
| `falha ao comentar (404)` | O projeto não é Jira Service Management | O endpoint de comentário ao cliente só funciona em JSM. Deixe `Comment` vazio para desativar. |
| Nada acontece e não há log | O app não iniciou, ou a pasta de logs não é gravável | Confirme `Directory` e as permissões. Rode `dotnet run` e verifique a pasta `logs`. |
| Alterei o `appsettings.local.json` e nada mudou | O arquivo copiado para `bin\...` está desatualizado | Rode `dotnet build` de novo (o arquivo é copiado a cada build). |

---

## Limitações conhecidas

- Não há projeto de testes automatizados ainda. A arquitetura já permite testar o caso de uso com um `IJiraGateway` falso (próximo passo planejado).
- Processa os chamados em sequência, sem paralelismo, e sem controle de limite de requisições (rate limit) do Jira.
- Não faz nova tentativa (retry) em falhas transitórias de rede: o chamado afetado é tratado na próxima execução, já que continua atendendo à JQL.
- O comentário usa o endpoint do Jira Service Management; projetos Jira Software puros não são suportados para comentário.
