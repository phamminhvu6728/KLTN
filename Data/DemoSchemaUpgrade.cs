using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
namespace TuTuc.Data;
// Startup compatibility for existing local demo databases created before Migrations.
// Only trusted CLR/EF model names/defaults are used. No request input enters this DDL.
#pragma warning disable EF1002
public static class DemoSchemaUpgrade
{
    static string Identifier(string value)
    {
        if(!Regex.IsMatch(value,@"^[A-Za-z_][A-Za-z0-9_]*$"))throw new InvalidOperationException("Unexpected EF identifier.");
        return "["+value+"]";
    }
    static string DefaultSql(IProperty p,object? instance)
    {
        var value=p.PropertyInfo?.GetValue(instance);
        if(value is DateTime)return "SYSUTCDATETIME()";
        if(value is bool b)return b?"1":"0";
        if(value is string s)return "N'"+s.Replace("'","''")+"'";
        if(value is Guid g)return "'"+g+"'";
        if(value is decimal or int or long or double or float)return Convert.ToString(value,CultureInfo.InvariantCulture)!;
        if(p.ClrType==typeof(string))return "N''";
        if(p.ClrType==typeof(bool))return "0";
        return "0";
    }
    public static void Apply(AppDbContext db)
    {
        db.Database.EnsureCreated();
        using var tx=db.Database.BeginTransaction();
        foreach(var entity in db.Model.GetEntityTypes())
        {
            var name=entity.GetTableName();if(name==null)continue;
            var table=Identifier(name);var store=StoreObjectIdentifier.Table(name,entity.GetSchema());
            object? instance=null;try{instance=Activator.CreateInstance(entity.ClrType);}catch(MissingMethodException){}
            foreach(var property in entity.GetProperties())
            {
                var columnName=property.GetColumnName(store);if(columnName==null||property.IsPrimaryKey())continue;
                var column=Identifier(columnName);var type=property.GetColumnType()??property.GetRelationalTypeMapping().StoreType;
                var definition=type.Contains("rowversion",StringComparison.OrdinalIgnoreCase)?"rowversion NOT NULL":type+(property.IsNullable?" NULL":" NOT NULL DEFAULT "+DefaultSql(property,instance));
                db.Database.ExecuteSqlRaw($"IF OBJECT_ID(N'{table}',N'U') IS NOT NULL AND COL_LENGTH(N'{name}',N'{columnName}') IS NULL ALTER TABLE {table} ADD {column} {definition};");
                if(Regex.IsMatch(type,@"^nvarchar\(\d+\)$",RegexOptions.IgnoreCase))
                    db.Database.ExecuteSqlRaw($"IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'{table}') AND name=N'{columnName}' AND max_length=-1) BEGIN DECLARE @ddl nvarchar(max); SELECT @ddl=N'ALTER TABLE {table} DROP CONSTRAINT '+QUOTENAME(d.name) FROM sys.default_constraints d JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id WHERE c.object_id=OBJECT_ID(N'{table}') AND c.name=N'{columnName}'; IF @ddl IS NOT NULL EXEC sp_executesql @ddl; ALTER TABLE {table} ALTER COLUMN {column} {type} {(property.IsNullable?"NULL":"NOT NULL")}; END;");
            }
        }
        var script=db.Database.GenerateCreateScript();
        var batches=Regex.Split(script,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase);
        foreach(var batch in batches)
        {
            var match=Regex.Match(batch,@"CREATE TABLE \[([^\]]+)\]");if(!match.Success)continue;
            var table=Identifier(match.Groups[1].Value);
            db.Database.ExecuteSqlRaw($"IF OBJECT_ID(N'{table}',N'U') IS NULL BEGIN\n{batch}\nEND");
        }
        // These two index predicates changed with SUC V04; recreate atomically at startup.
        db.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS [IX_Connections_CompanionId_SenderId] ON [Connections]; DROP INDEX IF EXISTS [IX_BookingChanges_BookingId] ON [BookingChanges];");
        foreach(var batch in batches)
        {
            var index=Regex.Match(batch,@"CREATE (?:UNIQUE )?INDEX \[([^\]]+)\] ON \[([^\]]+)\]");if(!index.Success)continue;
            var indexName=Identifier(index.Groups[1].Value);var table=Identifier(index.Groups[2].Value);
            db.Database.ExecuteSqlRaw($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'{index.Groups[1].Value}' AND object_id=OBJECT_ID(N'{table}')) BEGIN\n{batch}\nEND");
        }
        // Also add model relationships to retained pre-migration tables without dropping rows.
        foreach(var entity in db.Model.GetEntityTypes())
        foreach(var fk in entity.GetForeignKeys())
        {
            var tableName=entity.GetTableName();var parentName=fk.PrincipalEntityType.GetTableName();if(tableName==null||parentName==null)continue;
            var name=fk.GetConstraintName();if(name==null)continue;
            var store=StoreObjectIdentifier.Table(tableName,entity.GetSchema());var parentStore=StoreObjectIdentifier.Table(parentName,fk.PrincipalEntityType.GetSchema());
            var columns=string.Join(",",fk.Properties.Select(p=>Identifier(p.GetColumnName(store)!)));var parentColumns=string.Join(",",fk.PrincipalKey.Properties.Select(p=>Identifier(p.GetColumnName(parentStore)!)));
            var onDelete=fk.DeleteBehavior==DeleteBehavior.Cascade?" CASCADE":" NO ACTION";
            db.Database.ExecuteSqlRaw($"IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'{name}') ALTER TABLE {Identifier(tableName)} WITH CHECK ADD CONSTRAINT {Identifier(name)} FOREIGN KEY ({columns}) REFERENCES {Identifier(parentName)} ({parentColumns}) ON DELETE{onDelete};");
        }
        db.Database.ExecuteSqlRaw("UPDATE Accounts SET VerificationStatus=CASE WHEN ProviderVerified=1 THEN N'Đã xác minh' ELSE N'Chờ xác minh' END WHERE Role=N'Provider' AND VerificationStatus=N''; UPDATE Accounts SET ProviderStatus=N'Đang hoạt động' WHERE Role=N'Provider' AND VerificationStatus=N'Đã xác minh' AND ProviderStatus IS NULL;");
        db.Database.ExecuteSqlRaw("UPDATE Trips SET Status=N'Đã chấp nhận' WHERE Status=N'Đã xác nhận'; UPDATE Connections SET Status=N'Đã chấp nhận' WHERE Status=N'Đã chấp nhận kết nối'; UPDATE Bookings SET Status=N'Chờ phản hồi' WHERE Status=N'Chờ xác nhận'; UPDATE Bookings SET Status=N'Không tiếp tục' WHERE Status=N'Đã rút'; UPDATE Bookings SET Status=N'Đã hủy' WHERE Status=N'Đã hủy theo thỏa thuận'; UPDATE CompanionAgreements SET Status=N'Đã ngừng' WHERE Status=N'Đã kết thúc'; UPDATE Incidents SET Status=CASE WHEN Resolution<>N'' THEN N'Đã giải quyết' WHEN [Plan]<>N'' THEN N'Đã chọn phương án' ELSE N'Chưa giải quyết' END WHERE Status IN(N'Đang xử lý',N'Đã ghi nhận kết quả');");
        tx.Commit();
    }
}


