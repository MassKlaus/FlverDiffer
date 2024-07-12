using System.Collections;
using System.Reflection;

namespace FlverDiffer.CLI;

public record ValueDifference(string FieldPath, object? Value1, object? Value2);

public class PropertyCrawler
{
    public static List<ValueDifference> CrawlAndCompare(object? value1, object? value2, string flverPath = "")
    {
        var differences = new List<ValueDifference>();
        var cache = new List<object>();

        CompareObjects(value1, value2, flverPath, differences, cache);

        return differences;
    }

    private static void CompareObjects(object? obj1, object? obj2, string fieldPath, List<ValueDifference> differences, List<object> crawledObjects)
    {
        if (obj1 == null || obj2 == null)
        {
            if (obj1 != obj2)
            {
                differences.Add(new ValueDifference(fieldPath, obj1, obj2));
            }
            return;
        }

        if (obj1.GetType() != obj2.GetType())
        {
            differences.Add(new ValueDifference(fieldPath, obj1.GetType(), obj2.GetType()));
            return;
        }

        Type type = obj1.GetType();

        //compares primitive datatypes
        if (type.IsPrimitive || typeof(string).Equals(type) || typeof(DateTime).Equals(type))
        {
            if (!obj1.Equals(obj2))
            {
                differences.Add(new ValueDifference(fieldPath, obj1.ToString(), obj2.ToString()));
            }

            return;
        }
        else if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            var enumerable1 = (obj1 as IEnumerable);
            var enumerable2 = (obj2 as IEnumerable);

            var enumerator1 = (obj1 as IEnumerable)!.GetEnumerator();
            var enumerator2 = (obj2 as IEnumerable)!.GetEnumerator();

            int enum1Size = 0;
            int enum2Size = 0;

            foreach (var item in enumerable1!)
            {
                enum1Size++;
            }

            foreach (var item in enumerable2!)
            {
                enum2Size++;
            }

            if (enum1Size != enum2Size)
            {
                string currentPath = fieldPath + ".Length";
                differences.Add(new ValueDifference(currentPath, enumerable1, enumerable2));
            }

            var index = 0;
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                string currentPath = fieldPath + $"[{index}]";

                var enumValue1 = enumerator1.Current;
                var enumValue2 = enumerator2.Current;

                CompareObjects(enumValue1, enumValue2, currentPath, differences, crawledObjects);

                index++;
            }

            return;
        }
        else if (type.IsValueType)
        {
            if (!Equals(obj1, obj2))
            {
                differences.Add(new ValueDifference(fieldPath, obj1, obj2));
            }
            return;
        }
        if (crawledObjects.Contains(obj1))
        {
            // Avoid rechecking the same object
            return;
        }

        crawledObjects.Add(obj1);


        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // Compare properties
        foreach (PropertyInfo property in properties)
        {
            if (!property.CanRead)
            {
                continue;
            }


            string currentPath = $"{fieldPath}.{property.Name}";

            if ((property.PropertyType.IsClass || typeof(IEnumerable).IsAssignableFrom(property.PropertyType)) && property.PropertyType != typeof(string))
            {
                var value1 = property.GetValue(obj1);
                var value2 = property.GetValue(obj2);

                CompareObjects(value1, value2, currentPath, differences, crawledObjects);
            }
            else
            {
                property.GetIndexParameters();
                var value1 = property.GetValue(obj1);
                var value2 = property.GetValue(obj2);

                if (!Equals(value1, value2))
                {
                    differences.Add(new ValueDifference(currentPath, value1, value2));
                }
            }
        }

        // Compare fields
        foreach (FieldInfo field in fields)
        {
            var value1 = field.GetValue(obj1);
            var value2 = field.GetValue(obj2);
            string currentPath = $"{fieldPath}.{field.Name}";

            if ((field.FieldType.IsClass || typeof(IEnumerable).IsAssignableFrom(field.FieldType)) && field.FieldType != typeof(string))
            {
                CompareObjects(value1, value2, currentPath, differences, crawledObjects);
            }
            else
            {
                if (!Equals(value1, value2))
                {
                    differences.Add(new ValueDifference(currentPath, value1, value2));
                }
            }
        }
    }
}