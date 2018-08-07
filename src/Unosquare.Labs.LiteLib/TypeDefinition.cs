namespace Unosquare.Labs.LiteLib
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    internal class TypeDefinition
    {
        private const BindingFlags PublicInstanceFlags = BindingFlags.Instance | BindingFlags.Public;

        private static readonly ConcurrentDictionary<Type, DefinitionCacheItem> DefinitionCache =
            new ConcurrentDictionary<Type, DefinitionCacheItem>();

        private readonly StringBuilder _createBuilder = new StringBuilder();
        private readonly List<string> _propertyNames = new List<string>();

        public TypeDefinition(Type type)
        {
            if (DefinitionCache.ContainsKey(type))
            {
                Definition = DefinitionCache[type];
                return;
            }

            Definition = new DefinitionCacheItem();
            var indexBuilder = new List<string>();

            var properties = type.GetProperties(PublicInstanceFlags);

            // Start off with the table name
            var tableName = type.Name;
            var tableAttribute = type.GetTypeInfo().GetCustomAttribute<TableAttribute>();
            if (tableAttribute != null)
                tableName = tableAttribute.Name;

            _createBuilder.AppendLine($"CREATE TABLE IF NOT EXISTS [{tableName}] (");
            _createBuilder.AppendLine($"    [{nameof(ILiteModel.RowId)}] INTEGER PRIMARY KEY AUTOINCREMENT,");

            foreach (var property in properties)
            {
                if (property.Name == nameof(ILiteModel.RowId) || property.CanWrite == false)
                    continue;

                // Skip if not mapped
                var notMappedAttribute = property.GetCustomAttribute<NotMappedAttribute>();
                if (notMappedAttribute != null)
                    continue;

                // Add to indexes if indexed attribute is ON
                var indexedAttribute = property.GetCustomAttribute<LiteIndexAttribute>();
                if (indexedAttribute != null)
                {
                    indexBuilder.Add(
                        $"CREATE INDEX IF NOT EXISTS [IX_{tableName}_{property.Name}] ON [{tableName}] ([{property.Name}]);");
                }

                // Add to unique indexes if indexed attribute is ON
                var uniqueIndexAttribute = property.GetCustomAttribute<LiteUniqueAttribute>();

                if (uniqueIndexAttribute != null)
                {
                    indexBuilder.Add(
                        $"CREATE UNIQUE INDEX IF NOT EXISTS [IX_{tableName}_{property.Name}] ON [{tableName}] ([{property.Name}]);");
                }

                GeneratePropertyInfo(property, _propertyNames);
            }

            if (_propertyNames.Any() == false)
                throw new Exception("Invalid DbSet, you need at least one property to bind");

            // trim out the extra comma
            _createBuilder.Remove(_createBuilder.Length - Environment.NewLine.Length - 1, Environment.NewLine.Length + 1);

            _createBuilder.AppendLine();
            _createBuilder.AppendLine(");");

            foreach (var indexDdl in indexBuilder)
            {
                _createBuilder.AppendLine(indexDdl);
            }

            var escapedColumnNames = string.Join(", ", _propertyNames.Select(p => $"[{p}]").ToArray());
            var parameterColumnNames = string.Join(", ", _propertyNames.Select(p => $"@{p}").ToArray());
            var keyValueColumnNames = string.Join(", ", _propertyNames.Select(p => $"[{p}] = @{p}").ToArray());

            Definition.TableName = tableName;
            Definition.TableDefinition = _createBuilder.ToString();
            Definition.PropertyNames = _propertyNames.ToArray();

            Definition.SelectDefinition = $"SELECT [{nameof(ILiteModel.RowId)}], {escapedColumnNames} FROM [{tableName}]";
            Definition.InsertDefinition =
                $"INSERT INTO [{tableName}] ({escapedColumnNames}) VALUES ({parameterColumnNames}); SELECT last_insert_rowid();";
            Definition.UpdateDefinition =
                $"UPDATE [{tableName}] SET {keyValueColumnNames}  WHERE [{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}";
            Definition.DeleteDefinition =
                $"DELETE FROM [{tableName}] WHERE [{nameof(ILiteModel.RowId)}] = @{nameof(ILiteModel.RowId)}";
            Definition.DeleteDefinitionWhere = $"DELETE FROM [{tableName}]";
            Definition.AnyDefinition = $"SELECT EXISTS(SELECT 1 FROM '{tableName}')";

            DefinitionCache[type] = Definition;
        }

        public DefinitionCacheItem Definition { get; }

        private void GeneratePropertyInfo(PropertyInfo property, List<string> propertyNames)
        {
            var isValidProperty = false;
            var propertyType = property.PropertyType;
            var isNullable = propertyType.GetTypeInfo().IsGenericType &&
                             propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable) propertyType = Nullable.GetUnderlyingType(propertyType);
            var nullStatement = isNullable ? "NULL" : "NOT NULL";

            if (propertyType == typeof(string))
            {
                isValidProperty = true;

                var stringLength = 4096;
                {
                    var stringLengthAttribute = property.GetCustomAttribute<StringLengthAttribute>();

                    if (stringLengthAttribute != null)
                    {
                        stringLength = stringLengthAttribute.MaximumLength;
                    }
                }

                isNullable = property.GetCustomAttribute<RequiredAttribute>() == null;

                nullStatement = isNullable ? "NULL" : "NOT NULL";
                if (stringLength != 4096)
                {
                    var checkLength = $"length({property.Name})<={stringLength}";
                    _createBuilder.AppendLine(
                        $"    [{property.Name}] NVARCHAR({stringLength}) {nullStatement} CHECK({checkLength}),");
                }
                else
                {
                    _createBuilder.AppendLine($"    [{property.Name}] NVARCHAR({stringLength}) {nullStatement},");
                }
            }
            else if (propertyType.GetTypeInfo().IsValueType)
            {
                isValidProperty = true;
                _createBuilder.AppendLine(
                    $"    [{property.Name}] {propertyType.GetTypeMapping()} {nullStatement},");
            }
            else if (propertyType == typeof(byte[]))
            {
                isValidProperty = true;
                _createBuilder.AppendLine($"    [{property.Name}] BLOB {nullStatement},");
            }

            if (isValidProperty)
            {
                propertyNames.Add(property.Name);
            }
        }
    }
}
