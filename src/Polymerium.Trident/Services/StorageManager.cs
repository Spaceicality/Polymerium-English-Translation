﻿using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services.Storages;

namespace Polymerium.Trident.Services
{
    public class StorageManager(TridentContext context)
    {
        public Storage Open(string key)
        {
            string path = PathOf(key);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return new Storage(key, path);
        }

        public bool Destroy(string key)
        {
            string path = PathOf(key);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }

            return false;
        }

        public string RequestKey(string key)
        {
            string output = FileNameHelper.Sanitize(key);
            while (Directory.Exists(PathOf(output)))
            {
                key += '_';
            }

            return output;
        }

        private string PathOf(string key)
        {
            return Path.Combine(context.StorageDir, key);
        }
    }
}