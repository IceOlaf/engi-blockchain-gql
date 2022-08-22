using System.Collections;

namespace System.ComponentModel.DataAnnotations;

public class NotNullOrEmptyCollectionAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is ICollection collection)
        {
            return collection.Count != 0;
        }

        return value is IEnumerable enumerable
               && enumerable.GetEnumerator().MoveNext();
    }
}