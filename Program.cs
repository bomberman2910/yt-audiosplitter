using System.Diagnostics;
using System.Globalization;

internal class Program
{
    private const string TimestampTitleArtist = "sta";
    private const string TimestampArtistTitle = "sat";
    private const string TitleArtistTimestamp = "tas";
    private const string ArtistTitleTimestamp = "ats";

    private static void Main(string[] args)
    {
        ValidateArguments(args);

        var fullPathToTimestampFile = Path.GetFullPath(args[0]);
        var workingDirectory = Path.GetDirectoryName(fullPathToTimestampFile);
        var timestampFilename = Path.GetFileName(fullPathToTimestampFile);
        var mp3Filename = $"{timestampFilename}.mp3";

        Environment.CurrentDirectory = workingDirectory;

        var fileContent = File.ReadAllLines(timestampFilename);
        var fileOrder = GetFileOrderFromArguments(args);

        var parsedLines = new List<SongData>();
        var currentTrackNumber = 1;
        foreach (var line in fileContent)
        {
            ExtractMetadataAndTimestamp(fileOrder, line, out var title, out var timestamp, out var artist);
            if (timestamp.Count(x => x == ':') == 1)
                parsedLines.Add(new SongData(currentTrackNumber++, DateTime.ParseExact(timestamp, "mm:ss", CultureInfo.InvariantCulture), artist, title));
            else if (timestamp.Count(x => x == ':') == 2)
                parsedLines.Add(new SongData(currentTrackNumber++, DateTime.ParseExact(timestamp, "H:mm:ss", CultureInfo.InvariantCulture), artist, title));
            else
            {
                Console.WriteLine($"[ERR] Invalid timestamp in file: {timestamp}");
                Environment.Exit(1);
            }
        }

        for (var i = 0; i < parsedLines.Count - 1; i++)
            parsedLines[i].Length = parsedLines[i + 1].Timestamp - parsedLines[i].Timestamp;
        var totalLength = GetTotalLength(mp3Filename);
        parsedLines[^1].Length = totalLength - parsedLines[^1].Timestamp;

        var albumName = GetTitle(mp3Filename).Replace("\"", string.Empty);

        var totalTracks = parsedLines.Count;

        foreach(var parsedLine in parsedLines)
        {
            Console.WriteLine($"[INF] Processing Track {parsedLine.TrackNumber}: {parsedLine.Title} by {parsedLine.Artist}");
            var ffmpeg = new Process();
            ffmpeg.StartInfo.FileName = "ffmpeg";
            ffmpeg.StartInfo.Arguments = $"-ss {parsedLine.Timestamp:HH:mm:ss}.0 -t {parsedLine.Length:hh\\:mm\\:ss}.0 -i \"{mp3Filename}\" -metadata title=\"{parsedLine.Title}\" -metadata artist=\"{parsedLine.Artist}\" -metadata album=\"{albumName}\" -metadata album_artist=\"Various Artists\" -metadata track=\"{parsedLine.TrackNumber}/{totalTracks}\" -acodec copy -y -loglevel error \"{parsedLine.TrackNumber:D2}. {parsedLine.Artist} - {parsedLine.Title}.mp3\"";
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();
            if(ffmpeg.ExitCode != 0)
                Console.WriteLine($"[ERR] Error while processing track {parsedLine.TrackNumber}: {ffmpeg.StandardError.ReadToEnd()}");
        };
    }

    private static string GetFileOrderFromArguments(string[] args)
    {
        var fileOrder = args[1];
        if (!(fileOrder.Equals(TimestampTitleArtist) || fileOrder.Equals(TimestampArtistTitle) || fileOrder.Equals(TitleArtistTimestamp) || fileOrder.Equals(ArtistTitleTimestamp)))
        {
            Console.WriteLine("[ERR] Unrecognized timestamp file order");
            Environment.Exit(1);
        }

        return fileOrder;
    }

    private static void ExtractMetadataAndTimestamp(string fileOrder, string line, out string title, out string timestamp, out string artist)
    {
        title = string.Empty;
        timestamp = string.Empty;
        artist = string.Empty;
        string rest;
        switch (fileOrder)
        {
            case TimestampTitleArtist:
                artist = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[1];
                rest = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
                timestamp = rest.Split(' ', 2)[0];
                title = rest.Split(' ', 2)[1];
                break;
            case TimestampArtistTitle:
                title = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[1];
                rest = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
                timestamp = rest.Split(' ', 2)[0];
                artist = rest.Split(' ', 2)[1];
                break;
            case TitleArtistTimestamp:
                title = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
                rest = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[1];
                timestamp = rest.Split(' ')[^1];
                artist = string.Join(' ', rest.Split(' ')[..^1]);
                break;
            case ArtistTitleTimestamp:
                artist = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
                rest = line.Split(" - ", 2, StringSplitOptions.TrimEntries)[1];
                timestamp = rest.Split(' ')[^1];
                title = string.Join(' ', rest.Split(' ')[..^1]);
                break;
        }

        artist = artist.Replace("\"", "\\\"");
        title = title.Replace("\"", "\\\"");
    }

    private static void ValidateArguments(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("[ERR] More or less than two arguments given");
            Console.WriteLine("[INF] Usage: timestampconverter <path to timestamp file> <timestamp file order>");
            Environment.Exit(1);
        }

        if (!File.Exists(args[0]))
        {
            Console.WriteLine("[ERR] Timestamp file not found");
            Console.WriteLine("[INF] timestampconverter <path to timestamp file> <timestamp file order>");
            Environment.Exit(1);
        }

        if (!File.Exists($"{args[0]}.mp3"))
        {
            Console.WriteLine("[ERR] MP3 file not found or it has a different name");
            Console.WriteLine("[INF] timestampconverter <path to timestamp file> <timestamp file order>");
            Environment.Exit(1);
        }
    }

    private static DateTime GetTotalLength(string filename)
    {
        var ffprobe = new Process();
        ffprobe.StartInfo.FileName = "ffprobe";
        ffprobe.StartInfo.Arguments = $"-i \"{filename}\" -show_entries format=duration -v quiet -of csv=\"p=0\" -sexagesimal";
        ffprobe.StartInfo.RedirectStandardOutput = true;
        ffprobe.Start();

        var ffprobeReader = ffprobe.StandardOutput;
        var ffprobeOutput = ffprobeReader.ReadToEnd();

        ffprobe.WaitForExit();

        if(!DateTime.TryParseExact(ffprobeOutput[..ffprobeOutput.IndexOf('.')].Trim(), "h:mm:ss", CultureInfo.InvariantCulture , DateTimeStyles.None, out var totalLength))
        {
            Console.WriteLine($"[ERR] Illegal output from ffprobe: {ffprobeOutput}");
            Environment.Exit(1);
        }

        return totalLength;
    }

    private static string GetTitle(string filename)
    {
        var ffprobe = new Process();
        ffprobe.StartInfo.FileName = "ffprobe";
        ffprobe.StartInfo.Arguments = $"-i \"{filename}\" -show_entries format_tags=title -v quiet -of csv=\"p=0\"";
        ffprobe.StartInfo.RedirectStandardOutput = true;
        ffprobe.Start();

        var ffprobeReader = ffprobe.StandardOutput;
        var ffprobeOutput = ffprobeReader.ReadToEnd();

        ffprobe.WaitForExit();

        return ffprobeOutput.Trim();
    }
}