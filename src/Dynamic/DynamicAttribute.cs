using System;
using System.Collections.Generic;

namespace Tlabs.Dynamic {
  /// <summary>
  /// DynamicAttribute
  /// </summary>
  public readonly struct DynamicAttribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicAttribute"/> class.
    /// </summary>
    /// <param name="type">The type from the attribute.</param>
    /// <param name="parameters">The constructor parameters of the attribute.</param>
    public DynamicAttribute(Type type, params object[] parameters) {
      Type= type;
      Parameters= parameters;
    }
    /// <summary>
    /// Gets the type from the property.
    /// </summary>
    /// <value>
    /// The type from the property.
    /// </value>
    public Type Type { get; }

    /// <summary>
    /// Gets the type from the property.
    /// </summary>
    /// <value>
    /// The type from the property.
    /// </value>
    public object[] Parameters { get; }
  }
}