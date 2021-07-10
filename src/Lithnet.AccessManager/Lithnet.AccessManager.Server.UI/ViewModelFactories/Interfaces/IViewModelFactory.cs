namespace Lithnet.AccessManager.Server.UI
{
    public interface IViewModelFactory<TViewModel, TModel1, TModel2>
    {
        TViewModel CreateViewModel(TModel1 model1, TModel2 model2);
    }

    public interface IViewModelFactory<TViewModel, TModel>
    {
        TViewModel CreateViewModel(TModel model);
    }

    public interface IViewModelFactory<TViewModel>
    {
        TViewModel CreateViewModel();
    }
}