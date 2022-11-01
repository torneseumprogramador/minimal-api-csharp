using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using minimal_api.DTOs;
using minimal_api.Models;
using minimal_api.Repositorio;
using minimal_api.Servicos;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

#region Mysql
string? conexao = "server=localhost;port=3306;database=minimal;uid=root;password=root;persistsecurityinfo=True";
// string? conexao = Environment.GetEnvironmentVariable("DATABASE_URL_MINIMAL_API");

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(conexao, ServerVersion.AutoDetect(conexao));
});
#endregion

#region JWT
JToken jAppSettings = JToken.Parse(File.ReadAllText(Path.Combine(Environment.CurrentDirectory,"appsettings.json")));
var key = Encoding.ASCII.GetBytes(jAppSettings["Secret"].ToString());

builder.Services.AddMvc(config =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("administrador", policy => policy.RequireClaim("administrador"));
    options.AddPolicy("editor", policy => policy.RequireClaim("editor"));
});
#endregion

#region Swagger
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API",
        Description = "Torne-se um programador API Minima",
        Contact = new OpenApiContact { Name = "Danilo Aparecido", Email = "suporte@torneseumprogramador.com.br" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT como no exemplo: Bearer {SEU_TOKEN}",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
#endregion

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

MapRoutes(app);

app.Run();

void MapRoutes(WebApplication app)
{
    #region Home
    app.MapGet("/", [AllowAnonymous] () => new {Mensagem = "Bem vindo a API"});
    #endregion

    #region login
    app.MapPost("/login", [AllowAnonymous] async (DbContexto context, [FromBody] Login login) => {
        var clienteDb = await context.Clientes.Where(c => c.Email == login.Email && c.Senha == login.Senha).ToListAsync();
        if(clienteDb.Count == 0) return Results.NotFound(new { Mensagem = "Email ou senha não encontrado" });
        var cliente = clienteDb[0];
        return Results.Ok(new {
            Id = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email,
            CPF = cliente.CPF,
            Token = TokenServico.Gerar(cliente)
        });
    })
    .WithName("GetLogin")
    .WithTags("Autenticação");
    #endregion

    MapRoutesClientes(app);
}

void MapRoutesClientes(WebApplication app){

    #region GET /clientes
    app.MapGet("/clientes", [Authorize] [Authorize(Roles = "editor, administrador")] async (DbContexto context) =>
        await context.Clientes.ToListAsync()
    )
    .Produces<ICollection<Cliente>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("GetClientes")
    .WithTags("Clientes");
    #endregion

    #region POST /clientes
    app.MapPost("/clientes", [Authorize] [Authorize(Roles = "administrador")] async (DbContexto context, Cliente cliente) => {
        context.Clientes.Add(cliente);
        var result = await context.SaveChangesAsync();
        return result > 0
            //? Results.CreatedAtRoute("GetClientePorId", new { id = cliente.Id }, cliente)
            ? Results.Created($"/clientes/{cliente.Id}", cliente)
            : Results.BadRequest("Falha ao salvar cliente");
    })
    .Produces<Cliente>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .WithName("PostClientes")
    .WithTags("Clientes");
    #endregion

    #region GET /clientes/1
    app.MapGet("/clientes/{id}", [Authorize] [Authorize(Roles = "editor, administrador")] async (int id, DbContexto context) =>
        await context.Clientes.FindAsync(id)
    )
    .WithName("GetClientePorId")
    .WithTags("Clientes");
    #endregion

    #region PUT /clientes/1
    app.MapPut("/clientes/{id}", [Authorize] [Authorize(Roles = "administrador")] async (DbContexto context, int id, Cliente cliente) => {
        var clienteDb = await context.Clientes.FindAsync(id);
        if (clienteDb == null) return Results.NotFound();

        clienteDb.Nome = cliente.Nome;
        clienteDb.Telefone = cliente.Telefone;
        clienteDb.CPF = cliente.CPF;
        clienteDb.Email = cliente.Email;
        clienteDb.Senha = cliente.Senha;

        context.Clientes.Update(clienteDb);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.Ok(cliente)
            : Results.BadRequest("Falha ao salvar cliente");
    })
    .Produces<Cliente>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("PutClientes")
    .WithTags("Clientes");
    #endregion

    #region DELETE /clientes/1
    app.MapDelete("/clientes/{id}", [Authorize] [Authorize(Roles = "administrador")] async (DbContexto context, int id) => {
        var clienteDb = await context.Clientes.FindAsync(id);
        if (clienteDb == null) return Results.NotFound();

        context.Clientes.Remove(clienteDb);
        var result = await context.SaveChangesAsync();

        return result > 0
            ? Results.NoContent()
            : Results.BadRequest("Falha ao excluir cliente");
    })
    .Produces<Cliente>(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .WithName("DeleteClientes")
    .WithTags("Clientes");
    #endregion
}