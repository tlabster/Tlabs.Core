using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tlabs.SrvBase")]
#if DEBUG
[assembly: InternalsVisibleTo("Tlabs.Core.Tests")]
[assembly: InternalsVisibleTo("Tlabs.SrvBase.Tests")]
#endif

