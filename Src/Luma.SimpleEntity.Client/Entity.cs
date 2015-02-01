namespace Luma.Client
{
    public abstract class Entity
    {
        protected void RaiseDataMemberChanging(string propertyName)
        {
            
        }

        protected void RaiseDataMemberChanged(string propertyName)
        {

        }

        protected void ValidateProperty(string propertyName, object newValue)
        {
            
        }
    }
}
