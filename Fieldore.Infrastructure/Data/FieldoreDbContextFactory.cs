using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fieldore.Infrastructure.Data;

public sealed class FieldoreDbContextFactory : IDesignTimeDbContextFactory<FieldoreDbContext>
{
    public FieldoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FieldoreDbContext>();
        var connectionString ="Data Source=sql.bsite.net\\MSSQL2016;User ID=samuelpaty_fieldore;Password=Sandeep@456890;Pooling=False;Connect Timeout=30;Encrypt=False;Trust Server Certificate=True;Authentication=SqlPassword;";

        optionsBuilder.UseSqlServer(connectionString);

        return new FieldoreDbContext(optionsBuilder.Options);
    }
}
