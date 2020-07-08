using System;
using System.Linq.Expressions;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IFileSelectionViewModelFactory
    {
        FileSelectionViewModel CreateViewModel(object model, Expression<Func<string>> exp, string basePath);
    }
}