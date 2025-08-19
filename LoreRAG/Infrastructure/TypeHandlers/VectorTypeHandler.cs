using Dapper;
using Pgvector;
using System.Data;

namespace LoreRAG.Infrastructure.TypeHandlers;

public class VectorTypeHandler : SqlMapper.TypeHandler<Vector>
{
    public override Vector Parse(object value)
    {
        if (value is float[] floatArray)
        {
            return new Vector(floatArray);
        }
        
        if (value is string stringValue)
        {
            var values = stringValue.Trim('[', ']')
                .Split(',')
                .Select(float.Parse)
                .ToArray();
            return new Vector(values);
        }
        
        throw new NotSupportedException($"Cannot parse Vector from {value?.GetType()}");
    }

    public override void SetValue(IDbDataParameter parameter, Vector? value)
    {
        parameter.Value = value;
        parameter.DbType = DbType.Object;
    }
}