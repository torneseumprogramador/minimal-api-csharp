```shell

dotnet tool install --global dotnet-ef

export DATABASE_URL_MINIMAL_API='server=localhost;port=3306;database=minimal;uid=root;password=root;persistsecurityinfo=True'

dotnet ef database update

```