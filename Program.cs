using System.IO.Compression;
using System.Reflection;

namespace SboxCllExtractor;

class Program
{
    private enum ReturnCodes
    {
        Success = 0,
        FileNotFound = 1,
        FileAccessDenied = 2,
        InvalidFileFormat = 3
    }

    static int Main(string[] args)
    {
        string? filePath = null;
        if ( args.Length > 0)
            filePath = args[0];

        var readResult = ReadCllFile( out CllFile? cllFile, filePath );


        if (readResult != ReturnCodes.Success || cllFile is null )
            return (int)readResult;

        string? outputDir = null;

        if ( args.Length > 1)
            outputDir = args[1];

        var outputResult = OutputCllFile(cllFile, outputDir);
        return (int)outputResult;
    }

    private static ReturnCodes ReadCllFile(out CllFile? cllFile, string? filePath )
    {
        cllFile = null;

        if ( filePath is null)
        {
            Console.Write("Enter a CLL file path:");
            filePath = Console.ReadLine();
        }

        if (filePath is null)
        {
            Log($"No file path was specified.");
            return ReturnCodes.FileNotFound;
        }

        if (!File.Exists(filePath))
        {
            Log($"Specified file does not exist: {filePath}");
            return ReturnCodes.FileNotFound;
        }

        Console.WriteLine($"Reading file: {filePath}");

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(filePath);
        }
        catch
        {
            Log($"Unable to read file: {filePath}");
            return ReturnCodes.FileAccessDenied;
        }

        try
        {
            if ( !CllFile.IsCllFile( in bytes ))
            {
                var inputStream = new MemoryStream( bytes );
                var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress);
                var outputStream = new MemoryStream();
                gZipStream.CopyTo( outputStream );
                bytes = outputStream.ToArray();
            }
            cllFile = CllFile.Parse(in bytes, Log);
        }
        catch
        {
            Log($"Aborting due to invalid file format.");
            return ReturnCodes.InvalidFileFormat;
        }
        return ReturnCodes.Success;
    }

    private const string OUTPUT_FOLDER_NAME = "output";

    private static ReturnCodes OutputCllFile( CllFile cllFile, string? outputDir )
    {
        if ( outputDir is null)
        {
            // Get executing assembly path
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var assemblyDir = Path.GetDirectoryName(assemblyPath);
            if ( assemblyDir is null )
            {
                Log("Unable to get assembly directory.");
                return ReturnCodes.FileNotFound;
            }
            outputDir = Path.Combine( assemblyDir, OUTPUT_FOLDER_NAME);
        }
        outputDir = Path.Combine(outputDir, cllFile.PackageIdent);
        Directory.CreateDirectory(outputDir);
        foreach( var textBlock in cllFile.TextBlocks)
        {
            OutputTextBlock(textBlock, outputDir);
        }
        return (int)ReturnCodes.Success;

        static void OutputTextBlock( TextBlock textBlock, string directory)
        {
            foreach( var textFile in textBlock.TextFiles )
            {
                // Null localPath shouldn't happen. Don't do anything with it.
                if (textFile.LocalPath is null)
                    continue;

                var filePath = Path.Combine(directory, textFile.LocalPath );
                var fileDir = Path.GetDirectoryName(filePath);
                if (fileDir is null)
                    continue;
                Directory.CreateDirectory( fileDir );
                File.WriteAllText(filePath, textFile.Text);
            }
        }
    }

    private static void Log( string message )
    {
        Console.WriteLine( message );
    }
}
