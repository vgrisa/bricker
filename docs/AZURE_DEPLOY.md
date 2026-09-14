# Publicar a Bricker no Azure

Este guia publica frontend, API e SignalR no mesmo App Service. O banco fica no Azure SQL e as imagens em `/home/uploads`, fora da pasta substituída durante o deploy.

## 1. Recursos criados

| Recurso | Configuração |
| --- | --- |
| Assinatura | Azure for Students |
| Resource Group | `bricker-demo-rg` |
| Web App | `bricker-vgrisa-demo` |
| URL pública | `https://bricker-vgrisa-demo-gzfqhnf9dvfmg2dk.chilecentral-01.azurewebsites.net` |
| Runtime | Linux, .NET 10, Free F1 |
| Região do Web App | Chile Central |
| SQL Server | `bricker-vgrisa-sql-us.database.windows.net` |
| Banco | `bricker-demo-db` |
| Região do banco | Italy North |
| Camada do banco | Oferta gratuita, serverless GP, excedente desativado |

## 2. Acesso ao banco

O App Service usa a identidade gerenciada `bricker-vgrisa-demo`; não existe senha do banco na aplicação. No banco, essa identidade possui `db_datareader`, `db_datawriter` e `db_ddladmin`.

A cadeia configurada no App Service se chama `BrickerDb`:

```text
Server=tcp:bricker-vgrisa-sql-us.database.windows.net,1433;Initial Catalog=bricker-demo-db;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Managed Identity;
```

O acesso administrativo utiliza Microsoft Entra ID. O IP pessoal deve permanecer liberado no firewall apenas enquanto o acesso local pelo SSMS ou editor de consultas for necessário.

## 3. Variáveis do App Service

| Nome | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Authentication__Google__ClientId` | Client ID do Google |
| `Authentication__Google__ClientSecret` | Client Secret do Google, protegido no Azure |
| `Frontend__BaseUrl` | URL pública do Web App |
| `Storage__UploadsPath` | `/home/uploads` |
| `DataProtection__KeysPath` | `/home/data-protection` |
| `DemoData__Seed` | `true` no ambiente demonstrativo |
| `WEBSITES_ENABLE_APP_SERVICE_STORAGE` | `true` |

A connection string `BrickerDb` fica na seção **Cadeias de conexão**, com tipo **SQLAzure**. A carga demonstrativa é idempotente: reinicializações e deploys não duplicam dados nem apagam o banco.

## 4. Configurar o Google

No cliente OAuth da Bricker no Google Cloud, mantenha o callback local e adicione:

```text
https://bricker-vgrisa-demo-gzfqhnf9dvfmg2dk.chilecentral-01.azurewebsites.net/signin-google
```

O Client Secret nunca deve ser colocado no Git, no workflow ou na documentação.

## 5. Autorizar o GitHub Actions por OIDC

1. No GitHub, abra `vgrisa/bricker > Settings > Environments` e crie o ambiente `production`.
2. No Azure, foi criada a identidade gerenciada atribuída pelo usuário `bricker-github-deploy`, com Client ID `a82424b1-613b-4b4b-97fd-6a8d2b837c3c`.
3. Na identidade, em **Credenciais federadas**, foi criada `bricker-production` para o ambiente GitHub `production`. A credencial usa o emissor `https://token.actions.githubusercontent.com`, a audiência `api://AzureADTokenExchange` e o subject imutável do repositório.
4. No Web App, em **IAM (Controle de acesso)**, a identidade recebeu a função **Contribuidor do Site**, limitada ao recurso `bricker-vgrisa-demo`.
5. No GitHub, em `Settings > Secrets and variables > Actions`, crie os secrets:
   - `AZURE_CLIENT_ID`: `a82424b1-613b-4b4b-97fd-6a8d2b837c3c`;
   - `AZURE_TENANT_ID`: `d0910ffd-8a37-4bdb-ba65-7d2d0eb57de9`;
   - `AZURE_SUBSCRIPTION_ID`: `85bdddea-24e0-4319-9182-e84809b60b58`.
6. Na aba **Variables**, crie `AZURE_WEBAPP_NAME` com `bricker-vgrisa-demo`.

O workflow `.github/workflows/deploy-azure.yml` compila, verifica e publica a aplicação a cada push na `main`. Também pode ser iniciado manualmente em **Actions > Deploy Bricker to Azure > Run workflow**.

## 6. Conferência após o deploy

1. Abra a rota `/api/v1/health` no domínio público.
2. Abra a página inicial e entre com `ana@demo.bricker.com.br` / `Bricker123`.
3. Confira fotos, catálogo, interesses, mensagens e avaliações.
4. Teste o login Google.
5. Publique uma imagem, reinicie o Web App e confirme que ela continua acessível.
6. Em **Cost Management**, crie um orçamento e alertas. O alerta não bloqueia cobranças sozinho; a proteção principal é manter o F1 e selecionar `AutoPause` no limite gratuito do SQL.

## Limites deste ambiente

- O plano F1 é para demonstração, não possui SLA e pode ter inicialização lenta.
- Linux F1 permite até cinco conexões WebSocket simultâneas.
- O conteúdo persistente e as imagens compartilham a cota de armazenamento do plano.
- O domínio próprio e uma disponibilidade mais previsível exigirão uma troca de plano.
