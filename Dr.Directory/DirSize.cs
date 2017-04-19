using System;
using System.IO;
using System.Threading.Tasks;

namespace Dr.Directory
{
    class DirSize
    {
        public static long SizeDir(String dir)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            return(new DirSize().getDirSize(di));
        }
        private long getDirSize(DirectoryInfo di)
        {
            long size = 0;
            foreach (DirectoryInfo dir in di.GetDirectories())
                size += getDirSize(dir);
            //Parallel.ForEach(di.GetDirectories(), dir => size += getDirSize(dir));
            foreach (FileInfo f in di.GetFiles())
                size += f.Length;
            //Parallel.ForEach(di.GetFiles(), f => size += f.Length);
            return size;
        }
    }
}
