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
