using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DataModels;

namespace Common
{
    public class BioScreenHelper
    {
        static IEnumerable<string> ReadAsLines(string filename)
        {
            
            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }
        }

        static IEnumerable<string> ReadAsLines(Stream fileStream)
        {
            using (var reader = new StreamReader(fileStream))
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }
        }

        private class Well
        {
            public int Index { get; set; }
            public string Name { get; set; }

            public Well(int index, string name)
            {
                Index = index;
                Name = name;
            }

            public Well()
            {
                
            }
        }

        public static ExperimentalRun ImportBioscreenFile(string fileFullPath)
        {
            var reader = ReadAsLines(fileFullPath);
            var experimenRun = new ExperimentalRun(Path.GetFileName(fileFullPath));
           
            if (reader == null)
                throw new Exception("No Data");

            var fileType = GetFileType(reader, fileFullPath);
            experimenRun.FileType = fileType;

            if (fileType == BioscreenFileType.Other)
                throw new Exception("Invalid file extension");

            experimenRun.Name = GetRunName(reader, fileType);
            experimenRun.CreationDate = File.GetCreationTime(fileFullPath);

            var headers = GetHeaders(reader, fileType);
            var records = GetRecords(reader, fileType);

            var wells = GetWellQueue(headers, fileType);

            int counter = 0;
            foreach (var record in records)
            {
                counter += 1;
                if (string.IsNullOrEmpty(record.Trim()))
                    continue;

                var rowWellMeasurement = GetRowWellMeasurements(record, fileType);

                int time = GetTimeInSeconds(rowWellMeasurement[0], fileType);

                var rowToSkip = GetRowsToSkip(fileType);

                foreach (string item in rowWellMeasurement.Skip(rowToSkip))
                {
                    var well = wells.Dequeue();

                    if (item == string.Empty)
                    {
                        wells.Enqueue(well);
                        continue;
                    }

                    float od = GetOD(item);

                    if (od == 0)
                    {
                        wells.Enqueue(well);
                        continue;
                    }

                    if (experimenRun.Run.ElementAtOrDefault(well.Index - 1) != null)
                    {
                        var measurement = new GrowthMeasurement { OD = od, Time = time };
                        experimenRun.Run[well.Index - 1].GrowthMeasurements.GetMeasurements(DataType.Raw).Add(measurement);
                    }
                    else
                    {
                        var measurements = new List<GrowthMeasurement>();
                        var measurement = new GrowthMeasurement { OD = od, Time = time };
                        measurements.Add(measurement);
                        experimenRun.Run.Add(new Culture(well.Index, well.Name, measurements));
                    }
                    wells.Enqueue(well);
                }
            }

            return experimenRun;
        }

        public static ExperimentalRun ImportBioscreenFile(IEnumerable<String> reader, string fileName)
        {
            //var reader = ReadAsLines(fileStream);
            var experimenRun = new ExperimentalRun(Path.GetFileName(fileName));

            if (reader == null)
                throw new Exception("No Data");

            var fileType = GetFileType(reader, fileName);
            experimenRun.FileType = fileType;

            if (fileType == BioscreenFileType.Other)
                throw new Exception("Invalid file extension");

            experimenRun.Name = GetRunName(reader, fileType);
            experimenRun.CreationDate = File.GetCreationTime(fileName);

            var headers = GetHeaders(reader, fileType);
            var records = GetRecords(reader, fileType);

            var wells = GetWellQueue(headers, fileType);

            var counter = 0;
            foreach (var record in records)
            {
                counter += 1;
                if (string.IsNullOrEmpty(record.Trim()))
                    continue;

                var rowWellMeasurement = GetRowWellMeasurements(record, fileType);

                int time = GetTimeInSeconds(rowWellMeasurement[0], fileType);

                var rowToSkip = GetRowsToSkip(fileType);

                foreach (string item in rowWellMeasurement.Skip(rowToSkip))
                {
                    var well = wells.Dequeue();

                    if (item == string.Empty)
                    {
                        wells.Enqueue(well);
                        continue;
                    }

                    float od = GetOD(item);

                    if (od == 0)
                    {
                        wells.Enqueue(well);
                        continue;
                    }

                    if (experimenRun.Run.ElementAtOrDefault(well.Index - 1) != null)
                    {
                        var measurement = new GrowthMeasurement { OD = od, Time = time };
                        experimenRun.Run[well.Index - 1].GrowthMeasurements.GetMeasurements(DataType.Raw).Add(measurement);
                    }
                    else
                    {
                        var measurements = new List<GrowthMeasurement>();
                        var measurement = new GrowthMeasurement { OD = od, Time = time };
                        measurements.Add(measurement);
                        experimenRun.Run.Add(new Culture(well.Index, well.Name, measurements));
                    }
                    wells.Enqueue(well);
                }
            }

            return experimenRun;
        }


        private static int GetRowsToSkip(BioscreenFileType fileType)
        {
            //Note: Row == columns in the souorce file.  Basically this skips the time and other columns and points to the OD columns.
            int skip = 0;
            switch (fileType)
            {
                case BioscreenFileType.Legacy:
                case BioscreenFileType.Generic:
                    skip = 1;
                    break;
                case BioscreenFileType.CSV:
                case BioscreenFileType.CSV2:
                    skip = 2;
                    break;
                case BioscreenFileType.Other:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fileType");
            }
            return skip;
        }

        private static string[] GetRowWellMeasurements(string record, BioscreenFileType fileType)
        {
            switch (fileType)
            {
                case BioscreenFileType.Legacy:
                case BioscreenFileType.Generic:
                    return record.Split('\t');
                case BioscreenFileType.CSV:
                case BioscreenFileType.CSV2:
                    var arr = record.SplitXP(",", "\"", true).ToList();
                    var retArr = arr.Select(s => s.Replace("\"", "").Replace(",", ".")).ToList();
                    return retArr.ToArray();
                case BioscreenFileType.Other:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fileType");
            }
            return new string[] {};
        }

        private static BioscreenFileType GetFileType(IEnumerable<string> reader, string fileFullPath)
        {
            var extension = Path.GetExtension(fileFullPath);
            switch (extension.ToLower())
            {
                case ".xl~":
                    return BioscreenFileType.Legacy;
                case ".csv":
                    if (reader.First().Contains("Label"))
                        return BioscreenFileType.CSV2;
                    return BioscreenFileType.CSV;
                case ".csv2":
                    return BioscreenFileType.CSV2;
                case ".txt":
                    return BioscreenFileType.Generic;
                default:
                    return BioscreenFileType.Other;
            }
        }

        public static BioscreenFileType GetBioscreenFileType(string fileName)
        {
            if (fileName == null) return BioscreenFileType.Other;

            var extension = Path.GetExtension(fileName);
            switch (extension.ToLower())
            {
                case ".xl~":
                    return BioscreenFileType.Legacy;
                case ".csv":
                    return BioscreenFileType.CSV;
                case ".csv2":
                    return BioscreenFileType.CSV2;
                case ".txt":
                    return BioscreenFileType.Generic;
                default:
                    return BioscreenFileType.Other;
            }
        }

        private static int GetTimeInSeconds(string item, BioscreenFileType fileType)
        {
            int time;
            var succeded = false;
            switch (fileType)
            {
                case BioscreenFileType.Legacy:
                    //time is given in 10ths of second
                    succeded = int.TryParse(item, NumberStyles.Number, CultureInfo.InvariantCulture, out time);
                    time = time / 10;
                    break;
                case BioscreenFileType.CSV:
                    time = GetCVSSecondsFromTimeSpam(item, ref succeded);
                    break;
                case BioscreenFileType.CSV2:
                    time = GetCvs2SecondsFromTimeSpam(item, ref succeded);
                    break;
                case BioscreenFileType.Generic:
                    succeded = int.TryParse(item, NumberStyles.Number, CultureInfo.InvariantCulture, out time);
                    time = succeded ? 
                        time/10 :
                        GetCVSSecondsFromTimeSpam(item, ref succeded);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fileType");
            }
            if (!succeded)
                throw new Exception("could not convert Time, file must be corrupted! \r\n element: " + item);

            return time;
        }

        private static int GetCvs2SecondsFromTimeSpam(string item, ref bool succeded)
        {
            //time is given in HH:MM:00
            var timeSpanXP = new TimeSpan(
                int.Parse(item.Split(':')[0]), // hours
                int.Parse(item.Split(':')[1]), // minutes
                int.Parse(item.Split(':')[2])); // secs
            succeded = true;
            return (int) timeSpanXP.TotalSeconds;
        }

        private static int GetCVSSecondsFromTimeSpam(string stringTime, ref bool succeded)
        {
            //time is given in DD HH:MM:00
            TimeSpan timeSpan;
            stringTime = stringTime.Trim().Replace(' ', '.');
            succeded = TimeSpan.TryParse(stringTime, out timeSpan);
            if (succeded)
                return (int) timeSpan.TotalSeconds;
            return 0;
        }

        private static float GetOD(string item)
        {
            float od;
            bool succeded = float.TryParse(item, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out od);
            if (!succeded)
            {
                throw new Exception("could not convert OD, file must be corrupted! \r\n element: " + item);
            }
            return od;
        }

        private static Queue<Well> GetWellQueue(string[] headers, BioscreenFileType fileType)
        {
            var wells = new Queue<Well>();
            int skip = 0;
            //int wellCorrect = 0;

            switch (fileType)
            {
                case BioscreenFileType.Legacy:
                case BioscreenFileType.Generic:
                    skip = 1;
                    break;
                case BioscreenFileType.CSV:
                    skip = 2;
                    //wellCorrect = 100;
                    break;
                case BioscreenFileType.CSV2:
                    skip = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fileType");
            }

            var count = 1;
            foreach (string header in headers.Skip(skip))
            {
                var index = count;
                var name = header.Trim();
                var well = new Well(index, name);
                wells.Enqueue(well);
                count ++;
            }
            return wells;
        }

        private static IEnumerable<string> GetRecords(IEnumerable<string> reader, BioscreenFileType fileType)
        {
            try
            {

                switch (fileType)
                {
                    case BioscreenFileType.Legacy:
                        return reader.Skip(7);
                    case BioscreenFileType.CSV:
                    case BioscreenFileType.Generic:
                        return reader.Skip(1);
                    case BioscreenFileType.CSV2:
                        return reader.Skip(3);
                    default:
                        throw new ArgumentOutOfRangeException("fileType");
                }
            }
            catch (Exception)
            {
                switch (fileType)
                {
                    case BioscreenFileType.Legacy:
                        throw new Exception("Could not read the rest of the file from line 8");
                    case BioscreenFileType.CSV:
                    case BioscreenFileType.Generic:
                        throw new Exception("Could not read the file headers at line 1");
                    case BioscreenFileType.CSV2:
                        throw new Exception("Could not read the file headers at line 3");
                    default:
                        throw new ArgumentOutOfRangeException("fileType");
                }
            }
        }

        private static string[] GetHeaders(IEnumerable<string> reader, BioscreenFileType fileType)
        {
            try
            {
                switch (fileType)
                {
                    case BioscreenFileType.Legacy:
                        return reader.Skip(6).First().Split('\t');
                    case BioscreenFileType.CSV:
                        return reader.First().SplitXP(",", "\"", true);
                    case BioscreenFileType.CSV2:
                        return reader.Skip(2).First().SplitXP(",", "\"", true);
                    case BioscreenFileType.Generic:
                        return reader.First().Split('\t');
                    default:
                        throw new ArgumentOutOfRangeException("fileType");
                }
            }
            catch (Exception)
            {
                switch (fileType)
                {
                    case BioscreenFileType.Legacy:
                        throw new Exception("Could not read the file headers at line 7");
                    case BioscreenFileType.CSV:
                    case BioscreenFileType.Generic:
                        throw new Exception("Could not read the file headers at line 1");
                    case BioscreenFileType.CSV2:
                        throw new Exception("Could not read the file headers at line 3");
                    default:
                        throw new ArgumentOutOfRangeException("fileType");
                }
            }
        }

        private static string GetRunName(IEnumerable<string> reader, BioscreenFileType fileType)
        {
            switch (fileType)
            {
                case BioscreenFileType.Legacy:
                    try
                    {
                        var runNameLine = reader.Skip(1).First().Trim();
                        try
                        {
                            var runName = string.Empty;
                            var nameDelimitor = runNameLine.IndexOf(':');
                            if (nameDelimitor < runNameLine.Length - 1)
                                runName = runNameLine.Substring(nameDelimitor + 1, runNameLine.Length - nameDelimitor - 1);
                            return runName.Trim();
                        }   
                        catch (Exception ex)
                        {
                            throw new Exception("Could not parse the Experiment description");
                        }
                    }
                    catch (Exception)
                    {
                        throw new Exception("Could not read line 2 of the file");
                    }
                case BioscreenFileType.CSV:
                case BioscreenFileType.CSV2:
                case BioscreenFileType.Generic:
                    return "-";
                default:
                    throw new ArgumentOutOfRangeException("fileType");
            }
        }

        
    }
}
