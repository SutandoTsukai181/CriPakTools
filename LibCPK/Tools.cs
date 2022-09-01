using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace LibCPK
{
    public static class Tools
    {
        public static bool CheckListRedundant(List<FileEntry> input)
        {

            bool result = false;
            List<string> tmp = new List<string>();
            for (int i = 0; i < input.Count; i++)
            {
                string name = ((input[i].DirName != null) ?
                                        input[i].DirName + "/" : "") + input[i].FileName;
                if (!tmp.Contains(name))
                {
                    tmp.Add(name);
                }
                else
                {
                    result = true;
                    return result;
                }
            }
            return result;
        }

        public static Dictionary<string, string> ReadBatchScript(string batch_script_name)
        {
            //---------------------
            // TXT内部
            // original_file_name(in cpk),patch_file_name(in folder)
            // /HD_font_a.ftx,patch/BOOT.cpk_unpacked/HD_font_a.ftx
            // OTHER/ICON0.PNG,patch/BOOT.cpk_unpacked/OTHER/ICON0.PNG

            Dictionary<string, string> flist = new Dictionary<string, string>();

            StreamReader sr = new StreamReader(batch_script_name, Encoding.Default);
            String line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.IndexOf(",") > -1)
                //只读取格式正确的行
                {
                    line = line.Replace("\n", "");
                    line = line.Replace("\r", "");
                    string[] currentValue = line.Split(',');
                    flist.Add(currentValue[0], currentValue[1]);
                }


            }
            sr.Close();

            return flist;
        }

        public static string ReadCString(BinaryReader br, int MaxLength = -1, long lOffset = -1, Encoding enc = null)
        {
            int Max;
            if (MaxLength == -1)
                Max = 255;
            else
                Max = MaxLength;

            long fTemp = br.BaseStream.Position;
            byte bTemp = 0;
            int i = 0;
            string result = "";

            if (lOffset > -1)
            {
                br.BaseStream.Seek(lOffset, SeekOrigin.Begin);
            }

            do
            {
                bTemp = br.ReadByte();
                if (bTemp == 0)
                    break;
                i += 1;
            } while (i < Max);

            if (MaxLength == -1)
                Max = i + 1;
            else
                Max = MaxLength;

            if (lOffset > -1)
            {
                br.BaseStream.Seek(lOffset, SeekOrigin.Begin);

                if (enc == null)
                    result = Encoding.UTF8.GetString(br.ReadBytes(i));
                else
                    result = enc.GetString(br.ReadBytes(i));

                br.BaseStream.Seek(fTemp, SeekOrigin.Begin);
            }
            else
            {
                br.BaseStream.Seek(fTemp, SeekOrigin.Begin);
                if (enc == null)
                    result = Encoding.ASCII.GetString(br.ReadBytes(i));
                else
                    result = enc.GetString(br.ReadBytes(i));

                br.BaseStream.Seek(fTemp + Max, SeekOrigin.Begin);
            }

            return result;
        }

        public static void DeleteFileIfExists(string sPath)
        {
            if (File.Exists(sPath))
                File.Delete(sPath);
        }

        public static string GetPath(string input)
        {
            return Path.GetDirectoryName(input) + "\\" + Path.GetFileNameWithoutExtension(input);
        }

        public static byte[] GetData(BinaryReader br, long offset, int size)
        {
            byte[] result = null;
            long backup = br.BaseStream.Position;
            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            result = br.ReadBytes(size);
            br.BaseStream.Seek(backup, SeekOrigin.Begin);
            return result;
        }

        public static string GetSafePath(string filename)
        {
            string dest = filename.Replace(@"\", "/");
            string fName = Path.GetFileName(dest);
            char[] invalids = Path.GetInvalidFileNameChars();
            string fixedName = fName;
            foreach (var t in invalids)
            {
                fixedName = fixedName.Replace(t, '_');
            }
            dest = dest.Replace(fName, fixedName);
            return dest;
        }

        public static byte[] CryptJoJoASBR(byte[] data, uint size)
        {
            var pos = 0;

            uint v1 = size * 0x5f64 + 0x5dec219f;
            v1 = v1 / 32 ^ v1 * 0x1da597;
            uint v2 = v1 / 32 + 0x85c9c2 ^ v1 * 0x1da597;
            uint v3 = v2 / 32 + 0x10b9384 ^ v2 * 0x1da597;
            uint v4 = v3 / 32 + 0x1915d46 ^ v3 * 0x1da597;

            do
            {
                v1 = v1 * 2048 ^ v1;
                uint v5 = v4 ^ ((v4 / 2048 ^ v1) / 256) ^ v1;
                var v6Arr = new byte[] { (byte)v5, (byte)(v5 >> 8), (byte)(v5 >> 16), (byte)(v5 >> 24) };

                uint v7 = Math.Min(4, size);
                for (var i = 0; i < v7; i++)
                {
                    data[pos + i] = (byte)(data[pos + i] ^ v6Arr[i]);
                }

                size -= v7;
                pos += 4;

                v1 = v2;
                v2 = v3;
                v3 = v4;
                v4 = v5;
            } while (size > 0);

            return data;
        }

        public static HashSet<string> UnencryptedCPKs = new HashSet<string> { "adx2.cpk", "movie.cpk", "sound.cpk" };
    }
}