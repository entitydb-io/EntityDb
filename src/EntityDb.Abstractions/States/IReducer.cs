namespace EntityDb.Abstractions.States;

/// <summary>
///     Represents a type that can reduce one state into another state.
/// </summary>
/// <typeparam name="TState">The state to be reduced.</typeparam>
public interface IReducer<TState>
{
    /// <summary>
    ///     Returns a new <typeparamref name="TState" /> that incorporates this object into input
    ///     <typeparamref name="TState" />.
    /// </summary>
    /// <param name="state">The state to be reduced.</param>
    /// <returns>The new state</returns>
    TState Reduce(TState state);
}
