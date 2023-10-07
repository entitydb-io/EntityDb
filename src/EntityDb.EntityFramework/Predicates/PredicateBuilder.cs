using System.Linq.Expressions;

namespace EntityDb.EntityFramework.Predicates;

/// <summary>
///     Based on http://www.albahari.com/nutshell/predicatebuilder.aspx
/// </summary>
internal static class PredicateExpressionBuilder
{
    private static Expression<Func<T, bool>> False<T>()
    {
        return _ => false;
    }

    private static Expression<Func<T, bool>> Or<T>
    (
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right
    )
    {
        return Expression.Lambda<Func<T, bool>>
        (
            Expression.OrElse
            (
                left.Body,
                Expression.Invoke(right, left.Parameters)
            ),
            left.Parameters
        );
    }

    public static Expression<Func<T, bool>> Or<I, T>
    (
        IEnumerable<I> inputs,
        Func<I, Expression<Func<T, bool>>> mapper
    )
    {
        return inputs.Aggregate(False<T>(), (predicate, input) => Or(predicate, mapper.Invoke(input)));
    }
}
