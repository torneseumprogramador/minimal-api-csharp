using Microsoft.EntityFrameworkCore;
using minimal_api.Models;
using minimal_api.Repositorio;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string? conexao = Environment.GetEnvironmentVariable("DATABASE_URL_MINIMAL_API");

builder.Services.AddDbContext<DbContexto>(options =>
{
    options.UseMySql(conexao, ServerVersion.AutoDetect(conexao));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

#region Home
app.MapGet("/", () => new {Mensagem = "Bem vindo a API"});
#endregion

#region ClientesContexto

#region GET /clientes
app.MapGet("/clientes", async (DbContexto context) =>
    await context.Clientes.ToListAsync()
)
.WithName("GetClientes")
.WithTags("Clientes");
#endregion

#region POST /clientes
app.MapPost("/clientes", async (DbContexto context, Cliente cliente) => {
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
app.MapGet("/clientes/{id}", async (int id, DbContexto context) =>
    await context.Clientes.FindAsync(id)
)
.WithName("GetClientePorId")
.WithTags("Clientes");
#endregion

#region PUT /clientes/1
app.MapPut("/clientes/{id}", async (DbContexto context, int id, Cliente cliente) => {
    var clienteDb = await context.Clientes.FindAsync(id);
    if (clienteDb == null) return Results.NotFound();

    clienteDb.Nome = cliente.Nome;
    clienteDb.Telefone = cliente.Telefone;
    clienteDb.CPF = cliente.CPF;

    context.Clientes.Update(clienteDb);
    var result = await context.SaveChangesAsync();

    return result > 0
        ? Results.Created($"/clientes/{cliente.Id}", cliente)
        : Results.BadRequest("Falha ao salvar cliente");
})
.Produces<Cliente>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status404NotFound)
.WithName("PutClientes")
.WithTags("Clientes");
#endregion

#region DELETE /clientes/1
app.MapDelete("/clientes/{id}", async (DbContexto context, int id) => {
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

#endregion

app.Run();
