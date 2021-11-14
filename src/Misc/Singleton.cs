namespace Tlabs.Misc {
  ///<summary>Generic singleton class</summary>
  ///<remarks>Wraps any <typeparamref name="T"/> (with default ctor) into a singleton instance.</remarks>
  public sealed class Singleton<T> where T : new() {
    Singleton() { } //private ctor

    ///<summary>Singleton instance</summary>
    public static T Instance {
      get { return Lazy.instance; }
    }

    class Lazy {
      static Lazy() { } //Explicit static ctor for *NOT* to marking type with beforefieldinit
      internal static readonly T instance= new T();
    }
  }
}