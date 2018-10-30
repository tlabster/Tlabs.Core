using System;
using System.Collections.Generic;

namespace Tlabs.Dynamic {
  /// <summary>
  /// DynamicProperty
  /// </summary>
  public class DynamicProperty {
    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicProperty"/> class.
    /// </summary>
    /// <param name="name">The name from the property.</param>
    /// <param name="type">The type from the property.</param>
    /// <param name="attributes">The list of attributes to annotate the property (optional)</param>
    public DynamicProperty(string name, Type type, IList<DynamicAttribute> attributes= null) {
      Name = name;
      Type = type;
      Attributes = attributes;
    }

    /// <summary>
    /// Gets the name from the property.
    /// </summary>
    /// <value>
    /// The name from the property.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the type from the property.
    /// </summary>
    /// <value>
    /// The type from the property.
    /// </value>
    public Type Type { get; }

    /// <summary>
    /// Gets the list of attributes
    /// </summary>
    /// <value>
    /// The attributes from the property.
    /// </value>
    public IList<DynamicAttribute> Attributes { get; }
  }
}