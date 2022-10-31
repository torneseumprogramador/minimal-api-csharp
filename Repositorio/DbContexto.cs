using Microsoft.EntityFrameworkCore;
using minimal_api.Models;

namespace minimal_api.Repositorio;

public class DbContexto : DbContext
{
    public DbContexto(DbContextOptions<DbContexto> options) : base(options) { }

    public DbSet<Cliente> Clientes { get; set; } = null!;
}
