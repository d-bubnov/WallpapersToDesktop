using System;
using System.IO;

namespace WallpapersMoveToDesktop
{
    public class EnvVariables
    {
        private static string UserProfilePath => Environment.GetEnvironmentVariable(Constants.EnvUserProfile);

        private static string UserPicturesPath => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        public static string DestPath => Path.GetFullPath($"{UserPicturesPath}{Constants.DestRelativePath}");

        public static string SrcPath => Path.GetFullPath($"{UserProfilePath}{Constants.SrcRelativePath}");
    }
}
