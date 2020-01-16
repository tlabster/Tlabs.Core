

namespace Tlabs.Misc {

  ///<summary>Supports cloning, which creates a new instance of a class with the same value</summary>
  public interface ICloneable<T> {
    ///<summary>Create a clone of this instance.</summary>
    T Clone();
  }

}