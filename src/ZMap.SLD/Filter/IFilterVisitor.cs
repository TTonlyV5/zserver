namespace ZMap.SLD.Filter;

public interface IFilterVisitor
{
    object Visit(PropertyIsEqualTo filter, object data);
}