# Bricker

Marketplace local para compra e revenda de materiais de construcao excedentes.

## Estrutura

- `Bricker.Api`: API ASP.NET Core.
- `bricker-web`: aplicacao React + TypeScript.

## Executar localmente

1. Copie `Bricker.Api/appsettings.Development.example.json` para `appsettings.Development.local.json` e mantenha a cadeia de conexao local.
2. Execute a API: `dotnet run --project Bricker.Api --urls http://localhost:5190`.
3. Copie `bricker-web/.env.example` para `bricker-web/.env.local`.
4. Execute o frontend: `npm run dev --prefix bricker-web`.

O endpoint inicial da API e `GET /api/v1/health`.

## Testes

Os testes unitários e de integração do backend usam um banco SQL Server temporário com o prefixo `BrickerTests_`; ele é removido ao final da execução. Com o SQL Express local disponível, execute:

```powershell
dotnet test Bricker.Api.Tests/Bricker.Api.Tests.csproj --configuration Release
```

Os testes de interface usam Vitest e uma API/SignalR simulados:

```powershell
npm test --prefix bricker-web
```

Antes de publicar uma alteração, a validação completa é:

```powershell
dotnet test Bricker.Api.Tests/Bricker.Api.Tests.csproj --configuration Release
npm run lint --prefix bricker-web
npm test --prefix bricker-web
npm run build --prefix bricker-web
```

O GitHub Actions executa essa mesma validação em cada push e pull request. Somente um push na `main` aprovado pelos testes segue para publicação no Azure.

### Roteiro manual de integrações externas

- **Google:** confirme que o botão abre a seleção de conta, que uma conta nova é levada a `/completar-perfil` e que uma conta existente mantém seus dados.
- **SignalR:** em duas contas diferentes, envie uma mensagem, confirme que ela chega sem recarregar a página e que o contador de não lidas é atualizado ao abrir a negociação.

Para usar outra instância, informe a conexão base (o nome do banco será substituído automaticamente por um banco temporário):

```powershell
$env:BRICKER_TEST_CONNECTION_STRING = "Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
dotnet test Bricker.Api.Tests/Bricker.Api.Tests.csproj --configuration Release
```

## Contas de demonstração

Em desenvolvimento, um banco novo recebe anúncios completos, favoritos, interesses com negociações, uma venda e uma avaliação. Todas as contas usam a senha `Bricker123`:

- `ana@demo.bricker.com.br` — anunciante com interesses recebidos e mensagens não lidas.
- `carlos@demo.bricker.com.br` — anunciante avaliado e com uma avaliação pendente.
- `marina@demo.bricker.com.br` — compradora e anunciante.

As fotografias dos anúncios ficam em `Bricker.Api/DemoAssets/Listings` e são copiadas automaticamente para a pasta local de uploads.

Para apagar e recriar somente o banco local de desenvolvimento com esses dados:

```powershell
dotnet run --project Bricker.Api -- --reset-demo-data
```

## Configurar o login com Google

Crie um cliente OAuth do tipo **Aplicativo da Web** no Google Cloud e cadastre a URI de redirecionamento:

`http://localhost:5190/signin-google`

Guarde as credenciais fora do Git usando User Secrets, a partir da raiz do projeto:

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "SEU_CLIENT_ID" --project Bricker.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "SEU_CLIENT_SECRET" --project Bricker.Api
```

Também é possível usar as variáveis de ambiente `Authentication__Google__ClientId` e `Authentication__Google__ClientSecret`. Depois de configurar, reinicie a API pelo Visual Studio.

## Publicar no Azure

O build de produção reúne o React e a API em um único App Service, mantendo o desenvolvimento local separado. O workflow do GitHub compila e publica a aplicação, enquanto banco, credenciais e uploads são configurados no Azure.

Consulte [docs/AZURE_DEPLOY.md](docs/AZURE_DEPLOY.md) para criar o App Service F1, o Azure SQL gratuito, configurar o Google e autorizar o deploy automático por OIDC.
