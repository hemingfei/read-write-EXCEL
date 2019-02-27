using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace consoleProto2Excel
{
    class Program
    {
        static void Main(string[] args)
        {
            //string dir = Environment.CurrentDirectory;
            //Console.WriteLine(dir);
            Console.WriteLine(args[0]);
            Console.WriteLine(args[1]);
            string excelFolder = args[0]; // dir + "\\..\\.." + "\\table";
            string protoFolder = args[1]; // dir + "\\..\\.." + "\\protobuf";
            Console.WriteLine(excelFolder);
            Console.WriteLine(protoFolder);
            CheckExcelFromProto(protoFolder, excelFolder);

            Console.ReadLine();
        }
        private static void CheckExcelFromProto(string protoDir, string excelDir)
        {
            if (!Directory.Exists(protoDir))
            {
                Console.WriteLine("Error of Proto Folder Address");
                return;
            }
            if (!Directory.Exists(excelDir))
            {
                Console.WriteLine("Error of Excel Folder Address");
                return;
            }
            foreach (string filePath in Directory.GetFiles(protoDir))
            {
                if (Path.GetExtension(filePath) != ".proto")
                {
                    continue;
                }
                List<string> protoNames = new List<string>();
                List<string> protoComment = new List<string>();
                // 获得 proto 内容
                GetProtoDetails(filePath, ref protoNames, ref protoComment);
                string[] protoNamesArray = protoNames.ToArray();
                string[] protoCommentArray = protoComment.ToArray();
                // 获得当前文件名
                string fileShortName;
                fileShortName = filePath.Replace(protoDir, string.Empty);
                fileShortName = fileShortName.Replace("\\", string.Empty);
                fileShortName = fileShortName.Replace(".proto", string.Empty);
                // 获得旧的 excel 内容
                List<string> excelNames = new List<string>();
                List<string> excelParam = new List<string>();
                GetExcelDetails(fileShortName, excelDir, ref excelNames, ref excelParam);
                string[] excelNamesArray = excelNames.ToArray();
                string[] excelParamArray = excelParam.ToArray();
                // 改写 excel
                SetExcelNewDetails(fileShortName, excelDir, protoNamesArray, protoCommentArray, excelNamesArray, excelParamArray);
            }
            Console.WriteLine("Finish");
        }

        private static void GetProtoDetails(string filePath, ref List<string> protoNames, ref List<string> protoComment)
        {
            Console.WriteLine($"GetProtoDetails {filePath}");
            bool enumStart = false;

            using (FileStream txt = new FileStream(filePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(txt))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    line = line.Trim();
                    if (line == "enum Param")
                    {
                        enumStart = true;
                    }
                    if (enumStart == false)
                    {
                        continue;
                    }
                    if (line.Contains("_C") || line.Contains("_S") || line.Contains("_Brd") || line.Contains("_CS") || line.Contains("_SC"))
                    {
                        string[] split = line.Split(new string[] { "=", ";", "//" }, 3, StringSplitOptions.RemoveEmptyEntries);
                        protoNames.Add(split[0].Trim().Trim('\t'));
                        protoComment.Add(split[split.Length - 1].Trim().Trim('\t').Replace("//", string.Empty));
                    }
                    if (line.Contains("}"))
                    {
                        break;
                    }
                }
                sr.Close();
            }
        }

        private static void GetExcelDetails(string fileShortName, string excelDir, ref List<string> excelNames, ref List<string> excelParam)
        {
            Console.WriteLine($"GetExcelDetails {fileShortName}");
            string filePath = excelDir + "/" + fileShortName + ".xlsx";
            XSSFWorkbook xssfWorkbook;
            if (!File.Exists(filePath))
            {
                Console.WriteLine("no such file! path: " + filePath);
                return;
            }
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                xssfWorkbook = new XSSFWorkbook(file);
            }
            ISheet zerosheet = xssfWorkbook.GetSheetAt(0);
            int allcellCount = zerosheet.GetRow(3).LastCellNum;
            for (int i = 3; i < allcellCount; i++)
            {
                string protoName = GetCellString(zerosheet, 2, i);
                excelNames.Add(protoName.Trim().Trim('\t'));
                string paramString = GetCellString(zerosheet, 5, i);
                excelParam.Add(paramString.Trim().Trim('\t'));
            }
        }

        private static void SetExcelNewDetails(string fileShortName, string excelDir, string[] protoNamesArray, string[] protoCommentArray, string[] excelNamesArray, string[] excelParamArray)
        {
            Console.WriteLine($"SetExcelNewDetails {fileShortName}");
            string filePath = excelDir + "/" + fileShortName + ".xlsx";
            if (!File.Exists(filePath))
            {
                //Debug.LogError("no such file! path: " + filePath);
                return;
            }
            XSSFWorkbook oldXssfWorkbook = new XSSFWorkbook();
            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                oldXssfWorkbook = new XSSFWorkbook(file);
            }
            ISheet oldSheet = oldXssfWorkbook.GetSheetAt(0);

            XSSFWorkbook xssfWorkbook = new XSSFWorkbook();
            ISheet sheet = xssfWorkbook.CreateSheet(fileShortName);
            // 写数据
            int allcellCount = protoNamesArray.Length + 3;
            for (int i = 3; i < allcellCount; i++)
            {
                sheet.SetColumnWidth(i, 15 * 350);//设置列的宽度
                CreateCellString(sheet, 2, i, protoNamesArray[i - 3]);
                CreateCellString(sheet, 3, i, protoNamesArray[i - 3].Split('_')[1]);
                CreateCellString(sheet, 4, i, protoCommentArray[i - 3]);
                string param = "";
                for (int j = 0; j < excelNamesArray.Length; j++)
                {
                    if (excelNamesArray[j].Trim() == protoNamesArray[i - 3].Trim())
                    {
                        param = excelParamArray[j];
                    }
                }
                CreateCellString(sheet, 5, i, param);
                CreateCellNum(sheet, 6, i, i - 2);
            }
            // 复制以前说明
            for(int ii = 2; ii < 7; ii++)
            {
                ICell outsidecell = sheet.GetRow(ii).CreateCell(2);
                outsidecell.CellComment = oldXssfWorkbook.GetSheetAt(0).GetRow(ii).GetCell(2).CellComment;
                //outsidecell.CellStyle = oldXssfWorkbook.GetSheetAt(0).GetRow(ii).GetCell(2).CellStyle;
                outsidecell.SetCellValue(oldXssfWorkbook.GetSheetAt(0).GetRow(ii).GetCell(2).StringCellValue);
            }
            var ms = new NpoiMemoryStream();
            ms.AllowClose = false;
            xssfWorkbook.Write(ms);
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);
            FileStream dumpFile = new FileStream(excelDir + "/" + fileShortName + ".xlsx", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            ms.WriteTo(dumpFile);//将流写入文件
            ms.AllowClose = true;
        }

        private static ICell GetCell(ISheet sheet, int i, int j)
        {
            return sheet.GetRow(i)?.GetCell(j);
        }

        private static string GetCellString(ISheet sheet, int i, int j)
        {
            return sheet.GetRow(i)?.GetCell(j)?.ToString() ?? "";
        }

        private static void SetCellString(ISheet sheet, int i, int j, string value = "")
        {
            sheet.GetRow(i)?.CreateCell(j)?.SetCellValue(value);
        }

        private static void CreateCellString(ISheet sheet, int i, int j, string value = "")
        {
            IRow row = sheet.GetRow(i);
            if(row == null)
            {
                row = sheet.CreateRow(i);
            }
            ICell cell = row.CreateCell(j);
            cell.SetCellType(CellType.String);
            cell.SetCellValue(value);
        }

        private static void CreateCellNum(ISheet sheet, int i, int j, double value)
        {
            IRow row = sheet.GetRow(i);
            if (row == null)
            {
                row = sheet.CreateRow(i);
            }
            ICell cell = row.CreateCell(j);
            cell.SetCellType(CellType.Numeric);
            cell.SetCellValue(value);
        }
    }
    public class NpoiMemoryStream : MemoryStream
    {
        public NpoiMemoryStream()
        {
            AllowClose = true;
        }

        public bool AllowClose { get; set; }

        public override void Close()
        {
            if (AllowClose)
                base.Close();
        }
    }
}
