namespace DraftPuck.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class InternalApiAuthAttribute : TypeFilterAttribute
{
    public InternalApiAuthAttribute() : base(typeof(InternalApiAuthFilter)) { }
}
