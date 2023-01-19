﻿using Microsoft.EntityFrameworkCore;
using WebDavServer.EF;
using WebDavServer.EF.Entities;
using WebDavServer.Infrastructure.FileStorage.Enums;
using WebDavServer.Infrastructure.FileStorage.Exceptions;
using WebDavServer.Infrastructure.FileStorage.Models;
using WebDavServer.Infrastructure.FileStorage.Services.Abstract;

namespace WebDavServer.Infrastructure.FileStorage.Services
{
    public class VirtualStorageService : IVirtualStorageService
    {
        private readonly FileStorageDbContext _dbContext;

        public VirtualStorageService(FileStorageDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> FileExistsAsync(PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            return await _dbContext.Set<Item>()
                .Where(x => !x.IsDirectory)
                .Where(x => x.DirectoryId == directoryId)
                .Where(x => x.Title == pathInfo.ResourceName)
                .AnyAsync(cancellationToken);
        }

        public async Task<bool> DirectoryExistsAsync(PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            return await _dbContext.Set<Item>()
                .Where(x => x.IsDirectory)
                .Where(x => x.DirectoryId == directoryId)
                .Where(x => x.Title == pathInfo.ResourceName)
                .AnyAsync(cancellationToken);
        }

        public async Task CreateFileAsync(string fileName, PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            await _dbContext.Set<Item>().AddAsync(new Item
            {
                Title = pathInfo.ResourceName,
                IsDirectory = false,
                Name = fileName,
                DirectoryId = directoryId,
                Path = pathInfo.VirtualPath
            }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateDirectoryAsync(PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            await _dbContext.Set<Item>().AddAsync(new Item
            {
                DirectoryId = directoryId,
                Name = pathInfo.ResourceName,
                Title = pathInfo.ResourceName,
                IsDirectory = true
            }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<Item?> GetFileInfoAsync(PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            return await _dbContext.Set<Item>()
                .Where(x => !x.IsDirectory)
                .Where(x => x.Title == pathInfo.ResourceName)
                .Where(x => x.DirectoryId == directoryId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Item>> GetDirectoryInfoAsync(PathInfo pathInfo, bool withContent, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            var directory = await _dbContext.Set<Item>()
                .Where(x => x.IsDirectory)
                .Where(x => x.Title == pathInfo.ResourceName)
                .Where(x => x.DirectoryId == directoryId)
                .FirstAsync(cancellationToken);
            
            var result = new List<Item> { directory };

            if (withContent)
            {
                var contents = await GetDirectoryAsync(directory.Id, cancellationToken);
                result.AddRange(contents);
            }

            return result;
        }

        async Task<List<Item>> GetDirectoryAsync(long directoryId, CancellationToken cancellationToken = default)
        {
            var result = new List<Item>();

            return await _dbContext.Set<Item>()
                .AsNoTracking()
                .Where(x => x.DirectoryId == directoryId)
                .OrderBy(x => !x.IsDirectory)
                .ToListAsync(cancellationToken);
        }

        public async Task MoveFileAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default)
        {
            var directoryId = srcPath.Directory.Id;

            var item = await _dbContext.Set<Item>()
                .Where(x => !x.IsDirectory)
                .Where(x => x.Title == srcPath.ResourceName)
                .Where(x => x.DirectoryId == directoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                throw new FileStorageException(ErrorCodes.NotFound);
            }

            item.DirectoryId = dstPath.Directory.Id;
            
            if (!dstPath.IsDirectory)
            {
                item.Title = dstPath.ResourceName;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task MoveDirectoryAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default)
        {
            var directoryId = srcPath.Directory.Id;

            var item = await _dbContext.Set<Item>()
                .Where(x => x.IsDirectory)
                .Where(x => x.Title == srcPath.ResourceName)
                .Where(x => x.DirectoryId == directoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                throw new FileStorageException(ErrorCodes.NotFound);
            }

            item.DirectoryId = dstPath.Directory.Id;
            item.Title = dstPath.ResourceName;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task CopyFileAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task CopyDirectoryAsync(PathInfo srcPath, PathInfo dstPath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<string> DeleteFileAsync(PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            var item = await _dbContext.Set<Item>()
                .Where(x => !x.IsDirectory)
                .Where(x => x.Title == pathInfo.ResourceName)
                .Where(x => x.DirectoryId == directoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                throw new FileStorageException(ErrorCodes.NotFound);
            }

            _dbContext.Remove(item);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return item.Name;
        }

        public async Task<List<string>> DeleteDirectoryAsync(PathInfo pathInfo, CancellationToken cancellationToken = default)
        {
            var directoryId = pathInfo.Directory.Id;

            var item = await _dbContext.Set<Item>()
                .Where(x => x.IsDirectory)
                .Where(x => x.Title == pathInfo.ResourceName)
                .Where(x => x.DirectoryId == directoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is null)
            {
                throw new FileStorageException(ErrorCodes.NotFound);
            }

            var files = await RecursiveDirectoryDeleteAsync(item.Id, cancellationToken);
            
            _dbContext.Remove(item);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return files;
        }

        async Task<List<string>> RecursiveDirectoryDeleteAsync(long directoryId,
            CancellationToken cancellationToken = default)
        {
            var files = new List<string>();

            var items = await _dbContext.Set<Item>()
                .Where(x => x.DirectoryId == directoryId)
                .ToListAsync(cancellationToken);

            foreach (var directory in items.Where(x => x.IsDirectory))
            {
                var newFiles = await RecursiveDirectoryDeleteAsync(directory.Id, cancellationToken);
                files.AddRange(newFiles);

                _dbContext.Remove(directory);
            }

            var dirFiles = items.Where(x => !x.IsDirectory).ToList();

            files.AddRange(dirFiles.Select(x => x.Name));
            _dbContext.RemoveRange(dirFiles);
            
            return files;
        }
    }
}
