
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tlabs.IX {

  ///<summary>Interface of a source of import data.</summary>
  ///<remarks>Implementations of this import interface should deliver data as an enumerator of <typeparamref name="T"/>.</remarks>
  public interface IImportDataSource<T> : IEnumerable<T> {
    ///<summary>Configure the import interface with <paramref name="configProperties"/>.</summary>
    void Configure(IReadOnlyDictionary<string, object> configProperties);
  }

  ///<summary>Interface of an asynchronous import data source.</summary>
  public interface IImportDataSourceAsync<T> : IImportDataSource<T>, IEnumerable<Task<T>> {
  }

  ///<summary>Interface of a drain of data to be exported.</summary>
  public interface IExportDataDrain<T> {
    ///<summary>Configure the export drain interface with <paramref name="configProperties"/>.</summary>
    void Configure(IReadOnlyDictionary<string, object> configProperties);

    ///<summary>Drain all the data provided by the <paramref name="dataEnumeration"/> to the export.</summary>
    ///<returns>Number of <typeparamref name="T"/>(s) drained to the export</returns>
    int Drain(IEnumerable<T> dataEnumeration);
  }

  ///<summary>Interface of an asynchronous drain of data to be exported.</summary>
  public interface IExportDataDrainAsync<T> : IExportDataDrain<T> {
    ///<summary>Drain <paramref name="data"/> async. to the export.</summary>
    Task DrainAsync(T data);

    ///<summary>Drain all the data provided by the <paramref name="dataEnumeration"/> async. to the export.</summary>
    ///<returns>Number of <typeparamref name="T"/>(s) drained to the export</returns>
    Task<int> DrainAllAsync(IEnumerable<T> dataEnumeration);
  }

}