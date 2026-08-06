namespace Core.Builders
{
    public abstract class BuilderWithValues<TBuilder, TEntity> : Builder<TEntity>
        where TBuilder : BuilderWithValues<TBuilder, TEntity>
    {
        public TBuilder WithValues(Action<TEntity> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action(Item);
            return (TBuilder)this;
        }
    }
}
