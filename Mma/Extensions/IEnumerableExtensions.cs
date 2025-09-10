using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace Mma.Extensions;

public static class IEnumerableExtensions
{
    public static DataSet ToDataSet<T>(this IList<T> list)
    {
        Type elementType = typeof(T);
        DataSet ds = new();
        DataTable t = new();
        ds.Tables.Add(t);

        //add a column to table for each public property on T
        foreach (var propInfo in elementType.GetProperties())
        {
            Type ColType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;

            t.Columns.Add(propInfo.Name, ColType);
        }

        //go through each property on T and add each value to the table
        foreach (T item in list)
        {
            DataRow row = t.NewRow();

            foreach (var propInfo in elementType.GetProperties())
            {
                row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value;
            }

            t.Rows.Add(row);
        }

        return ds;
    }
    public static DataTable ToDataTable<T>(this IEnumerable<T> items)
    {
        DataTable dataTable = new DataTable(typeof(T).Name);
        //Get all the properties
        PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo prop in Props)
        {
            //Setting column names as Property names
            dataTable.Columns.Add(prop.Name);
        }
        foreach (T item in items)
        {
            var values = new object?[Props.Length];
            for (int i = 0; i < Props.Length; i++)
            {
                //inserting property values to datatable rows
                values[i] = Props[i].GetValue(item, null);
            }
            dataTable.Rows.Add(values);
        }
        //put a breakpoint here and check datatable
        return dataTable;
    }

    public static bool HasAny<T>(this IEnumerable<T> enumerable)
    {
        return !enumerable.IsNullOrEmpty();
    }

    public static IEnumerable<TSource> DistinctBy<TSource, TKey>
 (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        HashSet<TKey> seenKeys = new();
        foreach (TSource element in source)
        {
            if (seenKeys.Add(keySelector(element)))
            {
                yield return element;
            }
        }
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
        {
            return true;
        }
        /* If this is a list, use the Count property for efficiency. 
         * The Count property is O(1) while IEnumerable.Count() is O(N). */
        var collection = enumerable as ICollection<T>;
        if (collection != null)
        {
            return collection.Count < 1;
        }
        return !enumerable.Any();
    }

    public static string? ToString<T>(this IEnumerable<T> source, string separator)
    {
        if (source == null || string.IsNullOrEmpty(separator))
            return null;

        string?[] array = [.. source.Where(n => n != null).Select(n => n?.ToString())];

        return string.Join(separator, array);
    }
    public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunksize)
    {
        while (source.Any())
        {
            yield return source.Take(chunksize);
            source = source.Skip(chunksize);
        }
    }

    public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
    {
        return [.. source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())];
    }

    public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
    {
        for (int i = 0; i < locations.Count; i += nSize)
        {
            yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
        }
    }


    public sealed class Tuple<T1, T2>
    {
        public Tuple(T1 value1, T2 value2) { Value1 = value1; Value2 = value2; }
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }
    public static List<T?> MapTo<T>(this DataTable table)
        where T : class, new()
    {
        List<Tuple<DataColumn, PropertyInfo>> map = new();

        foreach (PropertyInfo pi in typeof(T).GetProperties())
        {
            var col = (ColumnAttribute)
                Attribute.GetCustomAttribute(pi, typeof(ColumnAttribute))!;
            if (col == null || col.Name!.IsNullOrEmpty())
            {
                if (table.Columns.Contains(pi.Name))
                {
                    map.Add(new Tuple<DataColumn, PropertyInfo>(
                    table.Columns[pi.Name]!, pi));
                }
            }
            else
            {
                if (table.Columns.Contains(col.Name!))
                {
                    map.Add(new Tuple<DataColumn, PropertyInfo>(
                        table.Columns[col.Name!]!, pi));
                }
            }
        }

        List<T?> list = [];
        foreach (DataRow row in table.Rows)
        {
            if (row == null)
            {
                list.Add(null);
                continue;
            }
            T item = new T();
            foreach (Tuple<DataColumn, PropertyInfo> pair in map)
            {
                object? value = row[pair.Value1];
                if (value is DBNull) value = null;
                pair.Value2.SetValue(item, value, null);
            }
            list.Add(item);
        }
        return list;
    }


}