using System;

namespace Tlabs {
  ///<summary>A (void) unit type is a type that allows only one value (and thus can hold no information).</summary>
  ///<remarks>A <see cref="VoidUnit"/> can be used as a 'void type' with generic type parameters.</remarks>
  public readonly struct VoidUnit : IEquatable<VoidUnit> {
    ///<summary>Default value of a <see cref="VoidUnit"/>.</summary>
    public static readonly VoidUnit Void; //= default;
    ///<inheritdoc/>
    public bool Equals(VoidUnit other) => true;
    ///<inheritdoc/>
    public override int GetHashCode() => 271828183;
    ///<inheritdoc/>
    public override string ToString() => $"<{nameof(VoidUnit)}>";
    ///<inheritdoc/>
    public override bool Equals(object obj) => obj is VoidUnit unit && Equals(unit);
    ///<summary>Equality operator.</summary>
    public static bool operator ==(VoidUnit left, VoidUnit right) => left.Equals(right);
    ///<summary>Inequality operator.</summary>
    public static bool operator !=(VoidUnit left, VoidUnit right) =>!(left==right);
  }
}