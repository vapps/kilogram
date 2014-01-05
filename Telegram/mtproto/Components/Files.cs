﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Core.Logging;
using Telegram.MTProto.Exceptions;

namespace Telegram.MTProto.Components {
    public class Files {
        private static readonly Logger logger = LoggerFactory.getLogger(typeof(Files));

        private TelegramSession session;

        public Files(TelegramSession session) {
            this.session = session;
        }

        public async Task<string> GetFile(FileLocation fileLocation) {
            logger.debug("Getting file {0}", fileLocation);
            if(fileLocation.Constructor == Constructor.fileLocation) {
                FileLocationConstructor location = (FileLocationConstructor) fileLocation;
                string filePath = FileLocationToCachePath(location);
                using(IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication()) {
                    if(!storage.DirectoryExists("cache")) {
                        storage.CreateDirectory("cache");
                    }

                    if(storage.FileExists(filePath)) {
                        logger.debug("Getting file {0} from cache", filePath);
                        return filePath;
                    }

                    TLApi api = await session.GetFileSession(location.dc_id);
                    logger.debug("Got file session for dc {0}", location.dc_id);

                    Upload_fileConstructor file = (Upload_fileConstructor) await api.upload_getFile(TL.inputFileLocation(location.volume_id, location.local_id, location.secret), 0, int.MaxValue);

                    logger.debug("File constructor found");

                    using (Stream fileStream = new IsolatedStorageFileStream(filePath, FileMode.OpenOrCreate,
                                                                          FileAccess.Write, storage)) {
                        await fileStream.WriteAsync(file.bytes, 0, file.bytes.Length);
                    }

                    logger.debug("File saved successfully");
                    return filePath;
                }
            } else {
                throw new MTProtoFileUnavailableException();
            }
        }

        private string FileLocationToCachePath(FileLocationConstructor fileLocation) {
            return String.Format("cache/{0}.{1}.{2}.jpg", fileLocation.volume_id, fileLocation.local_id, fileLocation.secret);
        }

        public async Task<string> GetAvatar(FileLocation location) {
            return await GetFile(location);
        }
    }
}
