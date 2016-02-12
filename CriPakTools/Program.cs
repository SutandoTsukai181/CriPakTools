﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CriPakTools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("err no args\n");
                Console.WriteLine("====================");
                Console.WriteLine("CriPakTool Usage:");
                Console.WriteLine(" -l - Displays all contained chunks.");
                Console.WriteLine(" -x - Extracts all files.");
                Console.WriteLine(" -r REPLACE_ME REPLACE_WITH - Replaces REPLACE_ME with REPLACE_WITH.");
                Console.WriteLine(" -o OUT_FILE - Set output file.");
                Console.WriteLine(" -d OUT_DIR - Set output directory.");
                Console.WriteLine(" -i IN_FILE - Set input file.");
                Console.WriteLine(" -b BATCH_REPLACE_LIST_TXT - Batch Replace file recorded in filelist.txt .");
                Console.WriteLine(" -h HELP");
                return;
            }

            bool doExtract = false;
            bool doReplace = false;
            bool doDisplay = false;
            bool doBatchReplace = false; //添加批量替换功能
            string outDir = ".";
            string inFile = "";
            string outFile = "";
            string replaceMe = "";
            string replaceWith = "";
            string batch_text_name = "";

            for (int i = 0; i < args.Length; i++)
            {
                string option =  args[i];
                if (option[0] == '-')
                {
                    switch (option[1])
                    {
                        case 'x': doExtract = true; break;
                        case 'r': doReplace = true; replaceMe = args[i + 1]; replaceWith = args[i + 2]; break;
                        case 'l': doDisplay = true; break;
                        case 'd': outDir = args[i + 1]; break;
                        case 'i': inFile = args[i + 1]; break;
                        case 'o': outFile = args[i + 1]; break;
                        case 'b': doBatchReplace = true; batch_text_name = args[i + 1]; break;
                        case 'h': 
                            Console.WriteLine("CriPakTool Usage:");
                            Console.WriteLine(" -l - Displays all contained chunks.");
                            Console.WriteLine(" -x - Extracts all files.");
                            Console.WriteLine(" -r REPLACE_ME REPLACE_WITH - Replaces REPLACE_ME with REPLACE_WITH.");
                            Console.WriteLine(" -o OUT_FILE - Set output file.");
                            Console.WriteLine(" -d OUT_DIR - Set output directory.");
                            Console.WriteLine(" -i IN_FILE - Set input file.");
                            Console.WriteLine(" -b BATCH_REPLACE_LIST_TXT - Batch Replace file recorded in filelist.txt .");
                            Console.WriteLine(" -h HELP");
                            Console.WriteLine("    [Extract files from cpk]");
                            Console.WriteLine("    CriPakTool.exe -x -i xxx.cpk -d xxx.cpk_unpacked");
                            Console.WriteLine("    [Display files from cpk]");
                            Console.WriteLine("    CriPakTool.exe -l -i xxx.cpk");
                            Console.WriteLine("    [Batch Replace files into cpk]");
                            Console.WriteLine("    CriPakTool.exe -b filelist.txt -i xxx.cpk -o xxx_patched.cpk");
                            Console.WriteLine("    //e.g. FILELIST.TXT");
                            Console.WriteLine("    original_file_name(in cpk),patch_file_name(in folder)");
                            Console.WriteLine("    /HD_font_a.ftx,patch/BOOT.cpk_unpacked/HD_font_a.ftx");
                            Console.WriteLine("    OTHER/ICON0.PNG,patch/BOOT.cpk_unpacked/OTHER/ICON0.PNG");
                            Console.WriteLine("...");
                            return;
                        default:
                            Console.WriteLine("CriPakTool Usage:");
                            Console.WriteLine(" -l - Displays all contained chunks.");
                            Console.WriteLine(" -x - Extracts all files.");
                            Console.WriteLine(" -r REPLACE_ME REPLACE_WITH - Replaces REPLACE_ME with REPLACE_WITH.");
                            Console.WriteLine(" -o OUT_FILE - Set output file.");
                            Console.WriteLine(" -d OUT_DIR - Set output directory.");
                            Console.WriteLine(" -i IN_FILE - Set input file.");
                            Console.WriteLine(" -b BATCH_REPLACE_LIST_TXT - Batch Replace file recorded in filelist.txt .");
                            break;

                    }
                }
             }
            if (inFile == "")
            {
                Console.WriteLine("ERROR :You must give -i argv");
                return;
            }

            if (!File.Exists(inFile))
            {
                Console.WriteLine("ERROR :INPUT FILE NOT EXISTS");
                return;
            }

            if (!(doExtract || doReplace || doDisplay || doBatchReplace))
            { //Lazy sanity checking for now
                Console.WriteLine("no? \n");
                return;
            }

            string cpk_name = inFile;

            CPK cpk = new CPK(new Tools());
            cpk.ReadCPK(cpk_name);

            BinaryReader oldFile = new BinaryReader(File.OpenRead(cpk_name));

            if (doDisplay)
            {
                List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();
                for (int i = 0; i < entries.Count; i++)
                {
                    Console.WriteLine(((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName);
                }
            }
            else if (doExtract)
            {
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                List<FileEntry> entries = null;

                entries = cpk.FileTable.Where(x => x.FileType == "FILE").ToList();

                if (entries.Count == 0)
                {
                    Console.WriteLine("err while extracting.");
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    if (!String.IsNullOrEmpty((string)entries[i].DirName))
                    {
                        Directory.CreateDirectory(outDir + "/" + entries[i].DirName.ToString());
                    }

                    oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

                    string isComp = Encoding.ASCII.GetString(oldFile.ReadBytes(8));
                    oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

                    byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                    Console.WriteLine("FileName :{0}\n    FileOffset:{1:x8}    ExtractSize:{2:x8}   FileSize:{3:x8}", 
                                                                            entries[i].FileName.ToString(),
                                                                            (long)entries[i].FileOffset, 
                                                                            entries[i].ExtractSize, 
                                                                            entries[i].FileSize);
                    if (isComp == "CRILAYLA")
                    {
                        Console.WriteLine("Got CRILAYLA !");
                        int size = Int32.Parse((entries[i].ExtractSize ?? entries[i].FileSize).ToString());

                        if (size != 0)
                            chunk = cpk.DecompressCRILAYLA(chunk, size);
                    }

                    File.WriteAllBytes(outDir + "/" + ((entries[i].DirName != null) ? entries[i].DirName + "/" : "") + entries[i].FileName.ToString(), chunk);

                }
            }
            else if (doBatchReplace)
            {
                //批量处理功能 ，读取filelist.txt内文件，将相应文件批量导入到cpk

                FileInfo fi = new FileInfo(cpk_name);

                string outputName = outFile;

                BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));

                List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

                Tools tool = new Tools();
                Dictionary<string, string> batch_file_list = tool.ReadBatchScript(batch_text_name);
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].FileType != "CONTENT")
                    {

                        if (entries[i].FileType == "FILE")
                        {
                            // I'm too lazy to figure out how to update the ContextOffset position so this works :)
                            if ((ulong)newCPK.BaseStream.Position < cpk.ContentOffset)
                            {
                                ulong padLength = cpk.ContentOffset - (ulong)newCPK.BaseStream.Position;
                                for (ulong z = 0; z < padLength; z++)
                                {
                                    newCPK.Write((byte)0);
                                }
                            }
                        }


                        if (!batch_file_list.Keys.Contains(entries[i].FileName.ToString()))
                            //如果不在表中，复制原始数据
                        {
                            oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);

                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            cpk.UpdateFileEntry(entries[i]);

                            byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                            newCPK.Write(chunk);
                        }
                        else
                        {
                            string replace_with = batch_file_list[entries[i].FileName.ToString()];
                            //Got patch file name
                            byte[] newbie = File.ReadAllBytes(replace_with);
                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            entries[i].FileSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            cpk.UpdateFileEntry(entries[i]);
                            newCPK.Write(newbie);
                        }

                        if ((newCPK.BaseStream.Position % 0x800) > 0)
                        {
                            long cur_pos = newCPK.BaseStream.Position;
                            for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }
                    else
                    {
                        // Content is special.... just update the position
                        cpk.UpdateFileEntry(entries[i]);
                    }
                }

                cpk.WriteCPK(newCPK);
                cpk.WriteITOC(newCPK);
                cpk.WriteTOC(newCPK);
                cpk.WriteETOC(newCPK);
                cpk.WriteGTOC(newCPK);

                newCPK.Close();
                oldFile.Close();


                


            }
            else
            {

                string ins_name = replaceMe;
                string replace_with = replaceWith;

                FileInfo fi = new FileInfo(cpk_name);

                string outputName = outFile;

                BinaryWriter newCPK = new BinaryWriter(File.OpenWrite(outputName));

                List<FileEntry> entries = cpk.FileTable.OrderBy(x => x.FileOffset).ToList();

                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].FileType != "CONTENT")
                    {

                        if (entries[i].FileType == "FILE")
                        {
                            // I'm too lazy to figure out how to update the ContextOffset position so this works :)
                            if ((ulong)newCPK.BaseStream.Position < cpk.ContentOffset)
                            {
                                ulong padLength = cpk.ContentOffset - (ulong)newCPK.BaseStream.Position;
                                for (ulong z = 0; z < padLength; z++)
                                {
                                    newCPK.Write((byte)0);
                                }
                            }
                        }
                        

                        if (entries[i].FileName.ToString() != ins_name)
                        {
                            oldFile.BaseStream.Seek((long)entries[i].FileOffset, SeekOrigin.Begin);
                            Console.WriteLine("{0},{1}", entries[i].FileName, entries[i].FileType);
                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            cpk.UpdateFileEntry(entries[i]);

                            byte[] chunk = oldFile.ReadBytes(Int32.Parse(entries[i].FileSize.ToString()));
                            newCPK.Write(chunk);
                        }
                        else
                        {
                            byte[] newbie = File.ReadAllBytes(replace_with);
                            entries[i].FileOffset = (ulong)newCPK.BaseStream.Position;
                            entries[i].FileSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            entries[i].ExtractSize = Convert.ChangeType(newbie.Length, entries[i].FileSizeType);
                            cpk.UpdateFileEntry(entries[i]);
                            newCPK.Write(newbie);
                        }

                        if ((newCPK.BaseStream.Position % 0x800) > 0)
                        {
                            long cur_pos = newCPK.BaseStream.Position;
                            for (int j = 0; j < (0x800 - (cur_pos % 0x800)); j++)
                            {
                                newCPK.Write((byte)0);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0},{1}", entries[i].FileName, entries[i].FileType);
                        // Content is special.... just update the position
                        cpk.UpdateFileEntry(entries[i]);
                    }
                }

                cpk.WriteCPK(newCPK);
                cpk.WriteITOC(newCPK);
                cpk.WriteTOC(newCPK);
                cpk.WriteETOC(newCPK);
                cpk.WriteGTOC(newCPK);

                newCPK.Close();
                oldFile.Close();

            }
        }
    }
}
