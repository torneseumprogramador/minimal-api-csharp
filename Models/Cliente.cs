using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace minimal_api.Models;

[Table("clientes")]
public class Cliente 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get;set; }
    
    public string Nome { get;set; } = null!;
    public string? Telefone { get;set; }
    public string? CPF { get;set; }
    public string Email { get;set; } = null!;
    public string Senha { get;set; } = null!;
    
    [NotMapped]
    public string Permissao 
    {
        get
        {
            return "administrador";
        }
    }
}