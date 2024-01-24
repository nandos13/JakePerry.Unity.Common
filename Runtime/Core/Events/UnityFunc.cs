namespace JakePerry.Unity.Events
{
    public delegate TResult UnityFunc<out TResult>();

    public delegate TResult UnityFunc<in T0, out TResult>(T0 arg0);

    public delegate TResult UnityFunc<in T0, in T1, out TResult>(T0 arg0, T1 arg1);

    public delegate TResult UnityFunc<in T0, in T1, in T2, out TResult>(T0 arg0, T1 arg1, T2 arg2);

    public delegate TResult UnityFunc<in T0, in T1, in T2, in T3, out TResult>(T0 arg0, T1 arg1, T2 arg2, T3 arg3);
}
