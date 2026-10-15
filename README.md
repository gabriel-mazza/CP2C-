# Checkpoint 2 — ExpenseHub

Checkpoint de C# em grupos de até 3 pessoas para construção de uma Application Programming Interface (API) corporativa de reembolsos.

O prazo de entrega é 13 de outubro de 2026. O grupo deverá implementar autenticação, autorização, fluxo de aprovação e reprovação, pagamento simulado, histórico e testes unitários.

## Criar seu repositório

1. Clique em **Use this template**.
2. Selecione **Create a new repository**.
3. Crie um repositório público em uma das contas do grupo.
4. Adicione os demais integrantes como colaboradores.
5. Clone o repositório.

Não use fork. As issues permanecem neste repositório original como especificação comum da turma.

Os commits serão utilizados para avaliar a participação. Todos os membros do grupo devem possuir mais de um commit no repositório.

## Fluxo de trabalho

Para cada issue:

1. leia os critérios no repositório original;
2. crie uma branch com o identificador, por exemplo `i06-ownership`;
3. implemente e valide a feature;
4. abra uma pull request no seu próprio repositório;
5. use um título como `I06 — Ownership e matriz de acesso`;
6. adicione na descrição uma referência completa, como `Racass/checkpoint-csharpracass-expensehub#6`;
7. não use `Closes`, `Fixes` ou `Resolves`, pois a issue original deve permanecer aberta;
8. conclua a auto-revisão e faça o merge.

## Estrutura inicial

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
└── ExpenseHub.UnitTests/
```

A solução começa sem Identity, banco, domínio ou testes funcionais. Toda implementação avaliada deve ser criada por você.

## Comandos

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

O endpoint inicial `GET /health` existe apenas para confirmar que a aplicação inicia.

## Documentação

- Enunciado
- Requisitos e contratos
- Rubrica
- Matriz de autorização
- Processo no GitHub
- Uso de Inteligência Artificial
- Regras do pipeline de qualidade

## Banco de dados

Você pode utilizar Microsoft SQL Server LocalDB, Oracle Database, SQLite ou outro provider relacional compatível com Entity Framework Core.

A escolha não gera pontos. Documente no README do seu repositório:

- provider e pacote utilizado;
- configuração necessária;
- criação ou atualização do banco;
- como iniciar a aplicação.

Não versione senhas, tokens ou connection strings sensíveis.

## Testes

Somente testes unitários escritos por você entram na nota. Testes de integração, end-to-end ou de interface são permitidos, mas opcionais e sem pontuação.

Os testes unitários devem executar sem banco, rede ou serviço externo.

## Entrega

Entregue:

- URL do repositório público;
- commit Secure Hash Algorithm (SHA) final;
- integração contínua executada;
- documentação atualizada.

O projeto deve compilar sem erros e ser entregue sem warnings para receber a pontuação integral de Qualidade de Código.

## Implementação do grupo

### Tecnologias e decisões

- **ASP.NET Core 10** com controllers, **ASP.NET Core Identity** e **Entity Framework Core** com provider **SQLite** (arquivo `expensehub.db`, ignorado pelo git).
- **Autenticação:** token bearer nativo do ASP.NET Core (`AddBearerToken`). O `POST /login` devolve `accessToken`, que deve ser enviado em `Authorization: Bearer <token>`. As roles ficam no token, por isso é preciso autenticar de novo após uma alteração de roles.
- **Autorização em camadas:** `[Authorize(Roles = ...)]` nos controllers (role) e `ExpenseService` (escopo de leitura, ownership e estado). Nada que venha do cliente define proprietário, estado, ator ou horário.
- **Regras de resposta:** 400 entrada inválida, 401 sem credencial, 403 sem permissão (inclusive tentar aprovar/pagar o próprio reembolso), 404 inexistente ou fora do escopo de leitura, 409 transição incompatível ou repetida (também usado quando duas requisições disputam o mesmo reembolso: o estado é um token de concorrência).
- **Visibilidade (`ExpenseVisibility`):** um filtro único, traduzido para SQL, usado em listagem, detalhe e histórico. Employee vê os próprios, Approver os enviados, Finance os aprovados e pagos, Auditor todos. Roles acumuladas somam permissões; Admin sozinho não acessa reembolsos.
- **Histórico:** reembolso, entrada de histórico e pagamento são gravados em um único `SaveChanges` (mesma transação).

### Configuração e execução

1. Instale o SDK do .NET 10 e a ferramenta de migrations (uma vez): `dotnet tool install --global dotnet-ef`.

2. Configure o Admin inicial (nunca no código; use user-secrets):

```shell
   cd sources/ExpenseHub.Api
   dotnet user-secrets set "Admin:Email" "admin@expensehub.local"
   dotnet user-secrets set "Admin:Password" "<defina-uma-senha-forte>"
```

   Alternativa: variáveis de ambiente `Admin__Email` e `Admin__Password`.

3. A migration inicial já está versionada na pasta `Migrations`, então não é preciso gerá-la de novo: ela é aplicada automaticamente na inicialização. Para criar uma nova migration após alterar o modelo: `dotnet ef migrations add NomeDaMigration --project sources/ExpenseHub.Api`.

4. Execute: `dotnet run --project sources/ExpenseHub.Api/ExpenseHub.Api.csproj`. Na inicialização a aplicação aplica as migrations, cria as 5 roles, as categorias padrão e o Admin (de forma idempotente).

5. Para recomeçar do zero, apague o arquivo `expensehub.db`.

As requisições de exemplo estão em `sources/ExpenseHub.Api/ExpenseHub.Api.http`.

### Testes

```shell
dotnet test ./sources/ExpenseHub.slnx
```

Os testes unitários usam repositório e relógio falsos (sem banco nem rede) e cobrem criação, validações, edição, envio, aprovação, reprovação, pagamento, visibilidade por perfil, histórico e regras de roles, com foco nos casos negativos de autorização.
