using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IAsyncViewModelFactory<TViewModel, TModel>
    {
        Task<TViewModel> CreateViewModelAsync(TModel model);
    }


    public interface IAsyncViewModelFactory<TViewModel, TModel1, TModel2>
    {
        Task<TViewModel> CreateViewModelAsync(TModel1 model1, TModel2 model2);
    }
}