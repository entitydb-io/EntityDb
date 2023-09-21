namespace EntityDb.Abstractions.Reducers;

/// <ignore />
[Obsolete("Please use the IReducer in the States namespace. This one will be removed in a future version.")]
public interface IReducer<TState> : States.IReducer<TState>
{
}
