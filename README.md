# Helpdesk

A full-stack ticketing / helpdesk system built with **.NET 10** — a **Minimal API** backend (REST + JWT + EF Core) paired with a **Blazor WebAssembly** front end (MudBlazor). Built incrementally as a learning project, with a deliberate focus on authentication, role-based authorization, data modeling, and secure file handling.

Sistema de tickets / mesa de ayuda full-stack hecho con **.NET 10** — backend **Minimal API** (REST + JWT + EF Core) junto a un front end **Blazor WebAssembly** (MudBlazor). Construido de forma incremental como proyecto de aprendizaje, con foco deliberado en autenticación, autorización por roles, modelado de datos y manejo seguro de archivos.

> 🇬🇧 **English** and 🇪🇸 **Español** versions below — click to expand.
> The domain is modeled in Spanish (`Ticket`, `Usuario`, `Categoria`, …).

---

<details open>
<summary><b>🇬🇧 English</b></summary>

<br>

## Architecture

Two independently-hosted applications talking over HTTP:

```
┌─────────────────────────┐        HTTPS / JWT Bearer        ┌──────────────────────────┐
│   Helpdesk.Web (SPA)    │  ───────────────────────────►    │   Helpdesk.Api (REST)    │
│   Blazor WebAssembly    │                                  │   ASP.NET Minimal API    │
│   MudBlazor UI          │  ◄───────────────────────────    │   EF Core 10             │
│   Runs in the browser   │        JSON responses            │   SQL Server             │
└─────────────────────────┘                                  └──────────────────────────┘
        localStorage                                                     │
        (JWT token)                                          ┌───────────┴────────────────┐
                                                             │  SQL Server    Local disk   │
                                                             │  (relational)  (attachments)│
                                                             └─────────────────────────────┘
```

- **`Helpdesk.Api`** — stateless REST API. Owns all data, business rules, and authorization. Issues signed JWTs; stores attachment binaries on local disk behind an abstraction.
- **`Helpdesk.Web`** — Blazor WASM client. Holds no secrets; authenticates against the API, caches the JWT in `localStorage`, and attaches it to every outgoing request via a `DelegatingHandler`. Authorization is mirrored client-side (for UX) but always enforced server-side.
- **`Helpdesk.Tests`** — xUnit test project (scaffold).

## Feature highlights

### Authentication & authorization
- **JWT authentication** with `id`, `role`, display name, and a `debe_cambiar_credenciales` flag embedded as claims.
- **Role-based authorization** across 5 roles (`Cliente`, `Agente`, `Analista`, `Administrador`, `Gerente`), enforced in **two layers**:
  - **Policy layer** — endpoint-level restrictions via named policies (`SoloAdmins`, `SoloPersonal`, `SoloCliente`).
  - **Ownership / scope layer** — per-request filtering so a Client only ever sees their own tickets, and an Agent/Analyst only sees tickets assigned to them, regardless of the ID they request (IDOR-safe).
- **Forced credential rotation on first login** — new users must change their issued password before doing anything else. Enforced by a claim + a custom middleware gate that 403s every route except `/auth/login/changepwd`.
- **Admin bootstrap seeding** — on first run the app seeds a default `Administrador` if none exists, solving the chicken-and-egg problem of a fully auth-gated `/usuarios` endpoint.

### Ticketing
- Full ticket lifecycle: create, edit, assign/unassign, change **status**, **priority**, **category**, and **due date** — each behind its own permission check.
- **Threaded comments** (`TicketDetalle`) with an **internal / public** flag: internal notes are hidden from Clients across comments *and* attachments.
- **Ticket assignment workflow** — Admins/Managers assign tickets only to Agents/Analysts, with server-side role validation.
- **SLA / due dates** — auto-derived from the category's `DiasVencimiento`, overridable by staff, and gated globally by a company-wide toggle. Precedence: `manual ?? auto ?? null`.

### File attachments
- Upload images (`jpg/jpeg/png/webp`) and PDFs against a ticket, with a defense-in-depth validation pipeline: empty check → extension allow-list → size cap → **magic-number signature check** → per-type quota (7 images / 2 documents per ticket).
- Binaries are stored on disk behind an `IAlmacenamientoAdjuntos` abstraction and served through an **authenticated streaming endpoint** — never via static files.
- Delete is **DB-first, then disk** (an orphan DB row is a visible 404; an orphan file is invisible garbage), with permission limited to the uploader or an Admin/Manager.

### Configuration & reporting
- **Configurable categories** (name, icon, suggested priority, SLA days, active/system flags) managed by admins.
- **Company settings** (company name, due-date feature toggle).
- **Reports** (Admins/Managers): volume by category, volume by month, and average resolution time — computed server-side and **exportable to PDF** (QuestPDF).

### API surface & docs
- **Response DTOs everywhere** — entities are projected to dedicated records; password hashes and internal flags never leak.
- **OpenAPI** document + **Scalar** interactive API reference in Development.

## Tech stack

**Backend (`Helpdesk.Api`)**

| Concern | Choice |
|---|---|
| Runtime | .NET 10 / ASP.NET Core **Minimal APIs** |
| Data access | Entity Framework Core 10 + **SQL Server** |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Password hashing | `PasswordHasher<T>` (ASP.NET Core Identity primitives) |
| Validation | Native .NET 10 validation (`AddValidation()` + Data Annotations) |
| PDF generation | **QuestPDF** (Community license) |
| API docs | `Microsoft.AspNetCore.OpenApi` + **Scalar** |
| Secrets (dev) | .NET User Secrets |

**Front end (`Helpdesk.Web`)**

| Concern | Choice |
|---|---|
| Framework | **Blazor WebAssembly** (.NET 10) |
| UI kit | **MudBlazor 9.6** (custom theme, light/dark) |
| Auth state | Custom `AuthenticationStateProvider` that parses the JWT from `localStorage` |
| HTTP | `IHttpClientFactory` + `DelegatingHandler` for Bearer injection |
| JS interop | ES modules for attachment previews & downloads (Blob + `createObjectURL`, no base64) |
| Fonts | Inter (Google Fonts) |

## Domain model

```
Usuario ──< Ticket >── Categoria
   │           │
   │           ├──< TicketDetalle (comments; EsInterno flag)
   │           └──< TicketAdjunto (attachments; optional link to a TicketDetalle)
   │
ConfiguracionEmpresa (singleton: company name + due-date toggle)
```

- **`Usuario`** — credentials, real name, `Rol` (`RolUsuario`), `Estado` (`Activo`/`Inactivo`/`Bloqueado`), `DebeCambiarCredenciales`.
- **`Ticket`** — title, description, creator, optional assignee, `Estado` (`Abierto`/`EnProgreso`/`Hecho`/`Pendiente`/`Cerrado`), `Prioridad` (`Baja`/`Media`/`Alta`/`Urgente`), optional category, `FechaVencimiento` (due) and `FechaCierre` (closed timestamp, stamped only when moving to `Cerrado`).
- **`TicketDetalle`** — a comment, its author, and an `EsInterno` flag (staff-only notes).
- **`TicketAdjunto`** — original/stored file names, content type, size, uploader, `TipoAdjunto` (`Imagen`/`Documento`), and an optional FK to a `TicketDetalle`.
- **`Categoria`** — name, icon, suggested priority, `Activa`, `EsDelSistema` (protected), and `DiasVencimiento` (SLA in days).
- **`ConfiguracionEmpresa`** — single-row company config (name + `UsarFechaVencimiento` toggle).

The DB is seeded (via `HasData`) with a default `"Otra cosa"` system category and a `"Mi empresa"` configuration row.

## Security model

| Mechanism | Where |
|---|---|
| JWT signature (HMAC-SHA256, base64 key) | `Program.cs` + `AuthEndpoints.GenerarToken` |
| Issuer / audience / lifetime validation | JWT bearer options |
| Forced password change gate | Custom middleware in `Program.cs` |
| Endpoint policies (`SoloAdmins` / `SoloPersonal` / `SoloCliente`) | `Authorization/AuthPolicies.cs` |
| Ownership checks (`EsParticipante` / `PuedeGestionar`) | `Authorization/TicketPermisos.cs` |
| IDOR protection | Handlers filter by `(Id, TicketId)` and role scope before returning data |
| Password hashing + transparent re-hash | `PasswordHasher<Usuario>` |
| Upload hardening | Extension allow-list + size cap + magic-number check + per-type quota (`AdjuntoEndpoints.cs`) |
| Internal-comment/attachment hiding from Clients | Query filters in detalle/adjunto GET handlers |
| CORS | Locked to the configured web origin (`Cors:AllowedOrigin`) |
| Secrets kept out of the repo | User Secrets (dev) / env vars (prod). The repo is public. |

## Solution layout

```
Helpdesk/
├── Helpdesk.slnx                 # Root solution: Api + Tests
├── src/
│   ├── Helpdesk.Api/             # REST API
│   │   ├── Endpoints/            # Minimal API groups: Auth, Tickets, TicketDetalle,
│   │   │                         #   Adjuntos, Usuarios, Categorias, Configuracion
│   │   ├── Models/               # EF Core entities + enums
│   │   ├── Dtos/                 # Request/response records
│   │   ├── data/                 # HelpdeskDbContext (relations, constraints, seed)
│   │   ├── Authorization/        # Policies + ticket permission helpers
│   │   ├── Almacenamiento/       # IAlmacenamientoAdjuntos + disk implementation
│   │   └── Migrations/           # EF Core migrations
│   └── Helpdesk.Web/
│       └── Helpdesk.Web/         # Blazor WASM SPA (own .slnx)
│           ├── Pages/            # Routable pages (Dashboard, Tickets, TicketDetalle,
│           │                     #   Usuarios, Reportes, Settings, MiPerfil, Login)
│           ├── Components/       # Dialogs & reusable UI (attachments, galleries, forms)
│           ├── Services/         # Typed HTTP clients, auth, storage, JS interop
│           ├── Dtos/ Models/     # Client-side contracts mirroring the API
│           ├── Helpers/          # Color/icon/priority/vencimiento mappers
│           ├── Theme/            # Custom MudBlazor theme (warm cream/orange palette)
│           └── wwwroot/          # index.html, css/app.css, js/ interop modules
└── tests/
    └── Helpdesk.Tests/           # xUnit (scaffold)
```

## Getting started

### Prerequisites
- .NET 10 SDK
- SQL Server (local instance or container)
- `dotnet-ef` (pinned in `dotnet-tools.json`): run `dotnet tool restore` from the API folder.

### 1. Configure the API (User Secrets)

No secrets are committed. From `src/Helpdesk.Api`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Helpdesk;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<base64-encoded signing key>"
dotnet user-secrets set "SeedAdmin:Password" "<password for the seeded admin>"
dotnet user-secrets set "Almacenamiento:RutaAdjuntos" "<absolute path for attachment storage>"
```

Non-sensitive settings live in `appsettings.json`: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`, and `Cors:AllowedOrigin` (must match the web origin, `https://localhost:7189` by default).

### 2. Run the API

```bash
cd src/Helpdesk.Api
dotnet tool restore
dotnet ef database update
dotnet run
```

The API listens on `https://localhost:7103`. On first run a default admin (`admin@helpdesk.com`, login name `admin`) is seeded from `SeedAdmin:Password`. In Development, the interactive API reference is at `/scalar/v1` and the OpenAPI document at `/openapi/v1.json`.

### 3. Run the web client

The client reads the API base URL from `wwwroot/appsettings.json` (`ApiSettings:BaseUrl`, `https://localhost:7103` by default).

```bash
cd src/Helpdesk.Web/Helpdesk.Web
dotnet run
```

The SPA is served at `https://localhost:7189`. Log in with `admin` and the seeded password; you'll be prompted to set a new password on first login.

## API reference

All routes except `POST /auth/login` and `GET /ping` require a valid JWT (`Authorization: Bearer <token>`). While a user's `debe_cambiar_credenciales` claim is `true`, every route except `PUT /auth/login/changepwd` returns `403`.

**Access column key:** *Auth* = any authenticated user; *Admin/Mgr* = `SoloAdmins`; *Staff* = `SoloPersonal`; *Scoped* = additionally filtered by ownership/assignment; *Participant* = creator, assignee, or admin/manager of that ticket.

**Auth**

| Method | Route | Access |
|---|---|---|
| `POST` | `/auth/login` | Public |
| `PUT` | `/auth/login/changepwd` | Auth |

**Tickets**

| Method | Route | Access |
|---|---|---|
| `GET` | `/tickets` | Auth (scoped; staff see all, clients see own) |
| `GET` | `/tickets/{id}` | Scoped |
| `GET` | `/tickets/stats` | Scoped (dashboard counters) |
| `GET` | `/tickets/recents` | Scoped (top 5) |
| `GET` | `/tickets/reportes` | Admin/Mgr |
| `GET` | `/tickets/reportes/pdf` | Admin/Mgr (PDF export) |
| `POST` | `/tickets` | Auth |
| `PUT` | `/tickets/{id}` | Participant (title/description) |
| `PUT` | `/tickets/{ticketId}/assign` | Admin/Mgr |
| `PUT` | `/tickets/{ticketId}/status` | Staff + participant |
| `PUT` | `/tickets/{ticketId}/priority` | Staff + participant |
| `PUT` | `/tickets/{ticketId}/categoria` | Staff + participant |
| `PUT` | `/tickets/{ticketId}/vencimiento` | Staff + participant |
| `DELETE` | `/tickets/{id}` | Admin/Mgr (hard delete) |

**Ticket comments**

| Method | Route | Access |
|---|---|---|
| `GET` | `/tickets/{ticketId}/detalles` | Participant (clients don't see internal) |
| `POST` | `/tickets/{ticketId}/detalles` | Participant (clients can't post internal) |

**Attachments**

| Method | Route | Access |
|---|---|---|
| `POST` | `/tickets/{ticketId}/adjuntos` | Participant |
| `GET` | `/tickets/{ticketId}/adjuntos` | Participant (clients don't see internal) |
| `GET` | `/tickets/{ticketId}/adjuntos/{adjuntoId}/contenido` | Participant (streamed) |
| `DELETE` | `/tickets/{ticketId}/adjuntos/{adjuntoId}` | Uploader or Admin/Mgr |

**Users**

| Method | Route | Access |
|---|---|---|
| `GET` | `/usuarios` | Admin/Mgr |
| `GET` | `/usuarios/{id}` | Admin/Mgr |
| `GET` | `/usuarios/asignables` | Admin/Mgr (agents & analysts) |
| `GET` | `/usuarios/me` | Auth |
| `POST` | `/usuarios` | Admin/Mgr |
| `PUT` | `/usuarios/{id}` | Admin/Mgr |
| `PUT` | `/usuarios/{id}/email` | Admin/Mgr |
| `PUT` | `/usuarios/{id}/status` | Admin/Mgr |
| `PUT` | `/usuarios/{id}/password` | Admin/Mgr (reset) |
| `PUT` | `/usuarios/{id}/rol` | Admin/Mgr |
| `DELETE` | `/usuarios/{id}` | Admin/Mgr (soft delete → `Inactivo`) |

**Categories**

| Method | Route | Access |
|---|---|---|
| `GET` | `/categorias` | Admin/Mgr |
| `GET` | `/categorias/activas` | Auth |
| `POST` | `/categorias` | Admin/Mgr |
| `PUT` | `/categorias/{categoriaId}` | Admin/Mgr |
| `PUT` | `/categorias/{categoriaId}/status` | Admin/Mgr (system categories can't be disabled) |

**Company configuration**

| Method | Route | Access |
|---|---|---|
| `GET` | `/configuracion` | Auth |
| `PUT` | `/configuracion/nombre-empresa` | Admin/Mgr |
| `PUT` | `/configuracion/fecha-vencimiento` | Admin/Mgr |

**Misc**

| Method | Route | Access |
|---|---|---|
| `GET` | `/ping` | Public (health/echo) |

## Front-end overview

A role-aware Blazor WASM SPA. Highlights:

- **Auth flow** — `AuthService` logs in against the API, stores the JWT in `localStorage`, and notifies a custom `AuthenticationStateProvider` that decodes the token (base64url-safe) into claims and auto-expires it. `AuthHeaderHandler` (a `DelegatingHandler`) attaches the Bearer token to every request.
- **Routing & guards** — `AuthorizeRouteView` with a redirect-to-login for anonymous users and an "access denied" component for authenticated-but-unauthorized ones. Pages are gated with `[Authorize(Roles = …)]`.
- **Role-based navigation** — the drawer in `MainLayout` renders a different menu per role bucket (Client / Agent-Analyst / Admin-Manager).
- **Role-specific dashboards** — three dashboard variants driven by `/tickets/stats` and `/tickets/recents`.
- **Feature pages** — tickets (table + card views, with a `/tickets/{filter}` route for the "assigned to me" view), ticket detail (inline edit of status/priority/category/due date, threaded comments, image gallery + document list), users admin, categories & company settings, reports (with PDF export), and a profile/password page.
- **Attachments in the browser** — upload via `MudFileUpload`; preview images and PDFs by fetching the authenticated stream into a `Blob` and rendering an object URL through JS interop (`adjuntos.js` / `descargas.js`) — no base64 round-trips, object URLs revoked deterministically.
- **Theming** — a custom `MudTheme` (warm cream/orange palette) plus `app.css`, with a persisted light/dark toggle.
- Typed service classes wrap each API area (`TicketService`, `UsuarioService`, `CategoriaService`, `ConfiguracionService`, `ReportesService`, `AdjuntoService`, `TicketDetalleService`) and return small result records (`ResultadoApi`) that surface server-side validation messages to the UI.

## Roadmap

- Automated test coverage (unit + integration via `WebApplicationFactory`).
- Pluggable cloud/object storage behind `IAlmacenamientoAdjuntos`.
- Refresh-token flow so sessions renew instead of hard-expiring.
- Notifications and ticket activity/audit history.
- Containerization + CI.

</details>

---

<details>
<summary><b>🇪🇸 Español</b></summary>

<br>

## Arquitectura

Dos aplicaciones alojadas de forma independiente que se comunican por HTTP:

```
┌─────────────────────────┐        HTTPS / JWT Bearer        ┌──────────────────────────┐
│   Helpdesk.Web (SPA)    │  ───────────────────────────►    │   Helpdesk.Api (REST)    │
│   Blazor WebAssembly    │                                  │   ASP.NET Minimal API    │
│   UI con MudBlazor      │  ◄───────────────────────────    │   EF Core 10             │
│   Corre en el navegador │        Respuestas JSON           │   SQL Server             │
└─────────────────────────┘                                  └──────────────────────────┘
        localStorage                                                     │
        (token JWT)                                          ┌───────────┴────────────────┐
                                                             │  SQL Server    Disco local  │
                                                             │  (relacional)  (adjuntos)   │
                                                             └─────────────────────────────┘
```

- **`Helpdesk.Api`** — API REST sin estado. Dueña de todos los datos, reglas de negocio y autorización. Emite JWT firmados; guarda los binarios de los adjuntos en disco local detrás de una abstracción.
- **`Helpdesk.Web`** — cliente Blazor WASM. No guarda secretos; se autentica contra la API, cachea el JWT en `localStorage` y lo adjunta a cada request saliente vía un `DelegatingHandler`. La autorización se replica en el cliente (por UX) pero siempre se valida en el servidor.
- **`Helpdesk.Tests`** — proyecto de pruebas xUnit (andamiaje).

## Funcionalidades destacadas

### Autenticación y autorización
- **Autenticación JWT** con `id`, `rol`, nombre visible y un flag `debe_cambiar_credenciales` embebidos como claims.
- **Autorización por roles** con 5 roles (`Cliente`, `Agente`, `Analista`, `Administrador`, `Gerente`), aplicada en **dos capas**:
  - **Capa de políticas** — restricciones a nivel de endpoint mediante políticas nombradas (`SoloAdmins`, `SoloPersonal`, `SoloCliente`).
  - **Capa de pertenencia / alcance** — filtrado por request para que un Cliente solo vea sus propios tickets, y un Agente/Analista solo los que tiene asignados, sin importar el ID que pida (seguro contra IDOR).
- **Rotación de credenciales obligatoria en el primer login** — los usuarios nuevos deben cambiar la contraseña asignada antes de hacer cualquier otra cosa. Se aplica con un claim + un middleware propio que devuelve 403 en toda ruta excepto `/auth/login/changepwd`.
- **Seeding del admin inicial** — en el primer arranque se crea un `Administrador` por defecto si no existe ninguno, resolviendo el problema del huevo y la gallina de un endpoint `/usuarios` totalmente protegido.

### Tickets
- Ciclo de vida completo: crear, editar, asignar/desasignar, cambiar **estado**, **prioridad**, **categoría** y **fecha de vencimiento** — cada uno con su propio chequeo de permisos.
- **Comentarios encadenados** (`TicketDetalle`) con un flag **interno / público**: las notas internas quedan ocultas a los Clientes tanto en comentarios *como* en adjuntos.
- **Flujo de asignación** — Administradores/Gerentes asignan tickets solo a Agentes/Analistas, con validación de rol en el servidor.
- **SLA / vencimientos** — se derivan automáticamente de los `DiasVencimiento` de la categoría, el personal los puede sobrescribir, y todo está gobernado por un toggle global de la empresa. Precedencia: `manual ?? auto ?? null`.

### Adjuntos
- Subida de imágenes (`jpg/jpeg/png/webp`) y PDFs a un ticket, con un pipeline de validación en profundidad: chequeo de vacío → lista blanca de extensiones → límite de tamaño → **verificación de firma por magic numbers** → cupo por tipo (7 imágenes / 2 documentos por ticket).
- Los binarios se guardan en disco detrás de una abstracción `IAlmacenamientoAdjuntos` y se sirven por un **endpoint autenticado con streaming** — nunca por archivos estáticos.
- El borrado es **primero en base, luego en disco** (una fila huérfana es un 404 visible; un archivo huérfano es basura invisible), con permiso limitado a quien lo subió o a un Administrador/Gerente.

### Configuración y reportes
- **Categorías configurables** (nombre, ícono, prioridad sugerida, días de SLA, flags activa/del-sistema) administradas por los admins.
- **Configuración de empresa** (nombre, toggle de fecha de vencimiento).
- **Reportes** (Administradores/Gerentes): volumen por categoría, volumen por mes y tiempo de resolución promedio — calculados en el servidor y **exportables a PDF** (QuestPDF).

### Superficie de API y documentación
- **DTOs de respuesta en todos lados** — las entidades se proyectan a records dedicados; los hashes de contraseña y los flags internos nunca se filtran.
- Documento **OpenAPI** + referencia interactiva **Scalar** en Development.

## Stack tecnológico

**Backend (`Helpdesk.Api`)**

| Aspecto | Elección |
|---|---|
| Runtime | .NET 10 / ASP.NET Core **Minimal APIs** |
| Acceso a datos | Entity Framework Core 10 + **SQL Server** |
| Autenticación | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Hash de contraseñas | `PasswordHasher<T>` (primitivas de ASP.NET Core Identity) |
| Validación | Validación nativa de .NET 10 (`AddValidation()` + Data Annotations) |
| Generación de PDF | **QuestPDF** (licencia Community) |
| Docs de API | `Microsoft.AspNetCore.OpenApi` + **Scalar** |
| Secretos (dev) | .NET User Secrets |

**Front end (`Helpdesk.Web`)**

| Aspecto | Elección |
|---|---|
| Framework | **Blazor WebAssembly** (.NET 10) |
| Kit de UI | **MudBlazor 9.6** (tema propio, claro/oscuro) |
| Estado de auth | `AuthenticationStateProvider` propio que parsea el JWT desde `localStorage` |
| HTTP | `IHttpClientFactory` + `DelegatingHandler` para inyectar el Bearer |
| Interop JS | Módulos ES para previsualizar y descargar adjuntos (Blob + `createObjectURL`, sin base64) |
| Tipografía | Inter (Google Fonts) |

## Modelo de dominio

```
Usuario ──< Ticket >── Categoria
   │           │
   │           ├──< TicketDetalle (comentarios; flag EsInterno)
   │           └──< TicketAdjunto (adjuntos; enlace opcional a un TicketDetalle)
   │
ConfiguracionEmpresa (única fila: nombre de empresa + toggle de vencimiento)
```

- **`Usuario`** — credenciales, nombre real, `Rol` (`RolUsuario`), `Estado` (`Activo`/`Inactivo`/`Bloqueado`), `DebeCambiarCredenciales`.
- **`Ticket`** — título, descripción, creador, asignado opcional, `Estado` (`Abierto`/`EnProgreso`/`Hecho`/`Pendiente`/`Cerrado`), `Prioridad` (`Baja`/`Media`/`Alta`/`Urgente`), categoría opcional, `FechaVencimiento` y `FechaCierre` (se estampa solo al pasar a `Cerrado`).
- **`TicketDetalle`** — un comentario, su autor y un flag `EsInterno` (notas solo para el personal).
- **`TicketAdjunto`** — nombre original/almacenado, content type, tamaño, quién lo subió, `TipoAdjunto` (`Imagen`/`Documento`) y una FK opcional a un `TicketDetalle`.
- **`Categoria`** — nombre, ícono, prioridad sugerida, `Activa`, `EsDelSistema` (protegida) y `DiasVencimiento` (SLA en días).
- **`ConfiguracionEmpresa`** — config de empresa de una sola fila (nombre + toggle `UsarFechaVencimiento`).

La base se inicializa (vía `HasData`) con una categoría de sistema `"Otra cosa"` y una fila de configuración `"Mi empresa"`.

## Modelo de seguridad

| Mecanismo | Dónde |
|---|---|
| Firma JWT (HMAC-SHA256, clave base64) | `Program.cs` + `AuthEndpoints.GenerarToken` |
| Validación de issuer / audience / lifetime | Opciones del bearer JWT |
| Gate de cambio de contraseña obligatorio | Middleware propio en `Program.cs` |
| Políticas de endpoint (`SoloAdmins` / `SoloPersonal` / `SoloCliente`) | `Authorization/AuthPolicies.cs` |
| Chequeos de pertenencia (`EsParticipante` / `PuedeGestionar`) | `Authorization/TicketPermisos.cs` |
| Protección contra IDOR | Los handlers filtran por `(Id, TicketId)` y alcance de rol antes de devolver datos |
| Hash de contraseñas + re-hash transparente | `PasswordHasher<Usuario>` |
| Endurecimiento de subidas | Lista blanca de extensiones + límite de tamaño + magic numbers + cupo por tipo (`AdjuntoEndpoints.cs`) |
| Ocultar comentarios/adjuntos internos a los Clientes | Filtros de query en los GET de detalle/adjunto |
| CORS | Restringido al origen web configurado (`Cors:AllowedOrigin`) |
| Secretos fuera del repo | User Secrets (dev) / variables de entorno (prod). El repo es público. |

## Estructura de la solución

```
Helpdesk/
├── Helpdesk.slnx                 # Solución raíz: Api + Tests
├── src/
│   ├── Helpdesk.Api/             # API REST
│   │   ├── Endpoints/            # Grupos Minimal API: Auth, Tickets, TicketDetalle,
│   │   │                         #   Adjuntos, Usuarios, Categorias, Configuracion
│   │   ├── Models/               # Entidades EF Core + enums
│   │   ├── Dtos/                 # Records de request/response
│   │   ├── data/                 # HelpdeskDbContext (relaciones, restricciones, seed)
│   │   ├── Authorization/        # Políticas + helpers de permisos de ticket
│   │   ├── Almacenamiento/       # IAlmacenamientoAdjuntos + implementación en disco
│   │   └── Migrations/           # Migraciones de EF Core
│   └── Helpdesk.Web/
│       └── Helpdesk.Web/         # SPA Blazor WASM (.slnx propio)
│           ├── Pages/            # Páginas ruteables (Dashboard, Tickets, TicketDetalle,
│           │                     #   Usuarios, Reportes, Settings, MiPerfil, Login)
│           ├── Components/       # Diálogos y UI reutilizable (adjuntos, galerías, formularios)
│           ├── Services/         # Clientes HTTP tipados, auth, storage, interop JS
│           ├── Dtos/ Models/     # Contratos del cliente que reflejan la API
│           ├── Helpers/          # Mapeos de color/ícono/prioridad/vencimiento
│           ├── Theme/            # Tema propio de MudBlazor (paleta crema/naranja cálida)
│           └── wwwroot/          # index.html, css/app.css, módulos de interop js/
└── tests/
    └── Helpdesk.Tests/           # xUnit (andamiaje)
```

## Puesta en marcha

### Requisitos previos
- SDK de .NET 10
- SQL Server (instancia local o contenedor)
- `dotnet-ef` (fijado en `dotnet-tools.json`): ejecutá `dotnet tool restore` desde la carpeta de la API.

### 1. Configurar la API (User Secrets)

No se commitea ningún secreto. Desde `src/Helpdesk.Api`:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Helpdesk;Trusted_Connection=True;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "<clave de firma en base64>"
dotnet user-secrets set "SeedAdmin:Password" "<contraseña para el admin sembrado>"
dotnet user-secrets set "Almacenamiento:RutaAdjuntos" "<ruta absoluta para guardar los adjuntos>"
```

Los ajustes no sensibles viven en `appsettings.json`: `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` y `Cors:AllowedOrigin` (debe coincidir con el origen web, `https://localhost:7189` por defecto).

### 2. Ejecutar la API

```bash
cd src/Helpdesk.Api
dotnet tool restore
dotnet ef database update
dotnet run
```

La API escucha en `https://localhost:7103`. En el primer arranque se siembra un admin por defecto (`admin@helpdesk.com`, nombre de login `admin`) usando `SeedAdmin:Password`. En Development, la referencia interactiva está en `/scalar/v1` y el documento OpenAPI en `/openapi/v1.json`.

### 3. Ejecutar el cliente web

El cliente lee la URL base de la API desde `wwwroot/appsettings.json` (`ApiSettings:BaseUrl`, `https://localhost:7103` por defecto).

```bash
cd src/Helpdesk.Web/Helpdesk.Web
dotnet run
```

La SPA se sirve en `https://localhost:7189`. Ingresá con `admin` y la contraseña sembrada; se te pedirá definir una nueva en el primer login.

## Referencia de API

Todas las rutas excepto `POST /auth/login` y `GET /ping` requieren un JWT válido (`Authorization: Bearer <token>`). Mientras el claim `debe_cambiar_credenciales` de un usuario sea `true`, toda ruta salvo `PUT /auth/login/changepwd` devuelve `403`.

**Referencia de la columna Acceso:** *Auth* = cualquier usuario autenticado; *Admin/Ger* = `SoloAdmins`; *Personal* = `SoloPersonal`; *Alcance* = además filtrado por pertenencia/asignación; *Participante* = creador, asignado o admin/gerente de ese ticket.

**Auth**

| Método | Ruta | Acceso |
|---|---|---|
| `POST` | `/auth/login` | Público |
| `PUT` | `/auth/login/changepwd` | Auth |

**Tickets**

| Método | Ruta | Acceso |
|---|---|---|
| `GET` | `/tickets` | Auth (con alcance; el personal ve todos, los clientes los propios) |
| `GET` | `/tickets/{id}` | Alcance |
| `GET` | `/tickets/stats` | Alcance (contadores del dashboard) |
| `GET` | `/tickets/recents` | Alcance (top 5) |
| `GET` | `/tickets/reportes` | Admin/Ger |
| `GET` | `/tickets/reportes/pdf` | Admin/Ger (exportación a PDF) |
| `POST` | `/tickets` | Auth |
| `PUT` | `/tickets/{id}` | Participante (título/descripción) |
| `PUT` | `/tickets/{ticketId}/assign` | Admin/Ger |
| `PUT` | `/tickets/{ticketId}/status` | Personal + participante |
| `PUT` | `/tickets/{ticketId}/priority` | Personal + participante |
| `PUT` | `/tickets/{ticketId}/categoria` | Personal + participante |
| `PUT` | `/tickets/{ticketId}/vencimiento` | Personal + participante |
| `DELETE` | `/tickets/{id}` | Admin/Ger (borrado físico) |

**Comentarios de ticket**

| Método | Ruta | Acceso |
|---|---|---|
| `GET` | `/tickets/{ticketId}/detalles` | Participante (los clientes no ven los internos) |
| `POST` | `/tickets/{ticketId}/detalles` | Participante (los clientes no pueden postear internos) |

**Adjuntos**

| Método | Ruta | Acceso |
|---|---|---|
| `POST` | `/tickets/{ticketId}/adjuntos` | Participante |
| `GET` | `/tickets/{ticketId}/adjuntos` | Participante (los clientes no ven los internos) |
| `GET` | `/tickets/{ticketId}/adjuntos/{adjuntoId}/contenido` | Participante (streaming) |
| `DELETE` | `/tickets/{ticketId}/adjuntos/{adjuntoId}` | Quien lo subió o Admin/Ger |

**Usuarios**

| Método | Ruta | Acceso |
|---|---|---|
| `GET` | `/usuarios` | Admin/Ger |
| `GET` | `/usuarios/{id}` | Admin/Ger |
| `GET` | `/usuarios/asignables` | Admin/Ger (agentes y analistas) |
| `GET` | `/usuarios/me` | Auth |
| `POST` | `/usuarios` | Admin/Ger |
| `PUT` | `/usuarios/{id}` | Admin/Ger |
| `PUT` | `/usuarios/{id}/email` | Admin/Ger |
| `PUT` | `/usuarios/{id}/status` | Admin/Ger |
| `PUT` | `/usuarios/{id}/password` | Admin/Ger (reseteo) |
| `PUT` | `/usuarios/{id}/rol` | Admin/Ger |
| `DELETE` | `/usuarios/{id}` | Admin/Ger (borrado lógico → `Inactivo`) |

**Categorías**

| Método | Ruta | Acceso |
|---|---|---|
| `GET` | `/categorias` | Admin/Ger |
| `GET` | `/categorias/activas` | Auth |
| `POST` | `/categorias` | Admin/Ger |
| `PUT` | `/categorias/{categoriaId}` | Admin/Ger |
| `PUT` | `/categorias/{categoriaId}/status` | Admin/Ger (las categorías del sistema no se pueden desactivar) |

**Configuración de empresa**

| Método | Ruta | Acceso |
|---|---|---|
| `GET` | `/configuracion` | Auth |
| `PUT` | `/configuracion/nombre-empresa` | Admin/Ger |
| `PUT` | `/configuracion/fecha-vencimiento` | Admin/Ger |

**Varios**

| Método | Ruta | Acceso |
|---|---|---|
| `GET` | `/ping` | Público (health/echo) |

## Vista general del front end

Una SPA Blazor WASM consciente de los roles. Puntos clave:

- **Flujo de auth** — `AuthService` inicia sesión contra la API, guarda el JWT en `localStorage` y notifica a un `AuthenticationStateProvider` propio que decodifica el token (base64url-safe) en claims y lo expira solo. `AuthHeaderHandler` (un `DelegatingHandler`) adjunta el Bearer a cada request.
- **Ruteo y guards** — `AuthorizeRouteView` con redirección a login para anónimos y un componente de "acceso denegado" para autenticados-pero-no-autorizados. Las páginas se protegen con `[Authorize(Roles = …)]`.
- **Navegación por rol** — el drawer de `MainLayout` renderiza un menú distinto por grupo de rol (Cliente / Agente-Analista / Admin-Gerente).
- **Dashboards por rol** — tres variantes de dashboard alimentadas por `/tickets/stats` y `/tickets/recents`.
- **Páginas** — tickets (vistas de tabla + tarjetas, con una ruta `/tickets/{filtro}` para "asignados a mí"), detalle de ticket (edición inline de estado/prioridad/categoría/vencimiento, comentarios, galería de imágenes + lista de documentos), administración de usuarios, categorías y configuración de empresa, reportes (con exportación a PDF) y una página de perfil/contraseña.
- **Adjuntos en el navegador** — subida con `MudFileUpload`; previsualización de imágenes y PDFs trayendo el stream autenticado a un `Blob` y renderizando un object URL vía interop JS (`adjuntos.js` / `descargas.js`) — sin idas y vueltas en base64, con revocación determinista de los object URLs.
- **Tematización** — un `MudTheme` propio (paleta crema/naranja cálida) más `app.css`, con un toggle claro/oscuro persistido.
- Clases de servicio tipadas envuelven cada área de la API (`TicketService`, `UsuarioService`, `CategoriaService`, `ConfiguracionService`, `ReportesService`, `AdjuntoService`, `TicketDetalleService`) y devuelven pequeños records de resultado (`ResultadoApi`) que exponen a la UI los mensajes de validación del servidor.

## Roadmap

- Cobertura de pruebas automatizadas (unitarias + integración vía `WebApplicationFactory`).
- Almacenamiento en la nube / objetos intercambiable detrás de `IAlmacenamientoAdjuntos`.
- Flujo de refresh-token para renovar sesiones en lugar de expirarlas de golpe.
- Notificaciones e historial de actividad/auditoría de tickets.
- Contenerización + CI.

</details>
