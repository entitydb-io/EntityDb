namespace EntityDb.Abstractions.States;

/// <summary>
///     Represents a type that can mutate one state into another state.
/// </summary>
/// <typeparam name="TState">The state to be mutated.</typeparam>
public interface IMutator<in TState>
{
    /// <summary>
    ///     Incorporates this object into the input <typeparamref name="TState" />.
    /// </summary>
    /// <param name="state">The state to be mutated</param>
    void Mutate(TState state);
}
