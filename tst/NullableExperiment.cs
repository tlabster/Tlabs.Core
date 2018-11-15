using System;
using Xunit;

namespace Tlabs.Core.Tests {
  public class NullableExperiment {

    class NullCondClass {
      public int inum= 123;
    }

    [Fact]
    public void NullCondExperiment() {
      NullCondClass nco= null;
      int inum= nco?.inum ?? 0;
      Assert.Equal(0, inum);
    }

    [Fact]
    public void BasicExperiment() {
      object o= 123;
      var ni= o as int?;
      Assert.NotNull(ni);
      Assert.Equal((int)o, ni.Value);

      var i= (object)"x" as int? ?? 0;
      Assert.Equal(0, i);
    }

    [Fact] void ConvertTest() {
      double dbl= 2.7182;
      var decType= typeof(decimal?);

      decimal? targetDec= (decimal?)Convert.ChangeType(dbl, Nullable.GetUnderlyingType(decType) ?? decType);
    }
  }
}
