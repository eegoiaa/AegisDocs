using System.Threading.Tasks;

namespace AegisDocs.UI.Interfaces;

public interface IFilePickerService
{
    Task<string?> PickFileAsync();
}
