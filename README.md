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
