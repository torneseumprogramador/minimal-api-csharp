using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims; // deixa informações disponíveis nos controllers
using System.IdentityModel.Tokens;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using Microsoft.IdentityModel.Tokens;
using minimal_api.Models;

namespace minimal_api.Servicos;
public static class TokenServico
{
public static string Gerar(Cliente cliente)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    
    JToken jAppSettings = JToken.Parse(File.ReadAllText(Path.Combine(Environment.CurrentDirectory,"appsettings.json")));
    var key = Encoding.ASCII.GetBytes(jAppSettings["Secret"].ToString());
    var tokenDescriptor = new SecurityTokenDescriptor()
    {
        Subject = new ClaimsIdentity(new Claim[]{
            new Claim(ClaimTypes.Name, cliente.Email),
            new Claim(ClaimTypes.Role, cliente.Permissao),
        }),
        Expires = DateTime.UtcNow.AddHours(3),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
}