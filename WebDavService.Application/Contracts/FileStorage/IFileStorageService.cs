using WebDavService.Application.Contracts.FileStorage.Models;

namespace WebDavService.Application.Contracts.FileStorage
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Lock file method
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path to item</param>
        /// <param name="timeoutMin">Lock timeout per minute</param>
        /// <returns>Lock-Token</returns>
        string LockItemAsync(string drive, string path, int timeoutMin);
        /// <summary>
        /// UnLock file method
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path to item</param>
        void UnlockItem(string drive, string path);
        /// <summary>
        /// Get all items
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <returns></returns>
        Task<List<DeleteItem>> GetItemsAsync(string drive, string path);
        /// <summary>
        /// Get files\directories properties
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <param name="withDirectoryContent">Include files in current directory</param>
        /// <returns>List infoes</returns>
        List<ItemInfo> GetProperties(string drive, string path, bool withDirectoryContent);
        /// <summary>
        /// Get file data
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <returns>Data bytes</returns>
        Task<byte[]> GetContentAsync(string drive, string path);
        /// <summary>
        /// Create directory
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        void CreateDirectory(string drive, string path);
        /// <summary>
        /// Create file
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <param name="data">File data</param>
        /// <returns></returns>
        Task CreateFileAsync(string drive, string path, byte[] data);
        /// <summary>
        /// Delete file\directory
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        void Delete(string drive, string path);
        /// <summary>
        /// Delete from recycler
        /// </summary>
        /// <param name="drive">Drive</param>
        /// <param name="path">Path</param>
        /// <returns></returns>
        Task DeleteRecyclerAsync(string drive, string path);
        /// <summary>
        /// Move file\directory
        /// </summary>
        /// <param name="r">Move request</param>
        void Move(MoveRequest r);
        /// <summary>
        /// Copy file\directory
        /// </summary>
        /// <param name="r">Copy request</param>
        void Copy(CopyRequest r);
    }
}
