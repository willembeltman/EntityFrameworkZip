namespace EntityFrameworkZip;

public interface ILazy<T>
{
    T Value { get; }
}
