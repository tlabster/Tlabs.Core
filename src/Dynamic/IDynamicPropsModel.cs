using System.Collections.Generic;

namespace Tlabs.Dynamic {
  /// <summary>
  /// Common interface for models containing document like dynamic properties
  /// </summary>
  public interface IDynamicPropsModel {
    /// <summary>
    /// Dictionary containing the dynamic properties to be validated
    /// </summary>
    Dictionary<string, object> Properties { get; }
  }
}