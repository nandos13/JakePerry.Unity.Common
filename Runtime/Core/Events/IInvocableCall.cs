namespace JakePerry.Unity.Events
{
    internal interface IInvocableCall
    {
        bool AllowInvoke { get; }

        object Invoke(object[] args);
    }
}
