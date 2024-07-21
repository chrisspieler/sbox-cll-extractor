using System.Text.Json;

namespace SboxCllExtractor;

public class CllFile
{
    public CllFile() { }

    public string PackageIdent { get; set; } = string.Empty;
    public string CompilerSettings { get; set; } = string.Empty;
    public string ProjectReferences { get; set; } = string.Empty;
    public List<TextBlock> TextBlocks { get; set; } = new();
    public List<TextFile> TextFiles { get; set; } = new();

    private const long MAGIC_OFFSET = 0x4;
    // 0x47, 0x4D, 0x43, 0x41: "GMCA"
    private const int MAGIC_VALUE = 1094929735;
    private const long PACKAGE_NAME_OFFSET = 0x10;

    public static bool IsCllFile( in byte[] bytes )
    {
        using MemoryStream reader = new MemoryStream(bytes, false)
            ?? throw new InvalidOperationException("Unable to create MemoryStream from input bytes.");

        if ( reader.Length < MAGIC_OFFSET + sizeof(int) )
            return false;

        reader.Seek(MAGIC_OFFSET, SeekOrigin.Begin);
        // Read the magic value
        if (!reader.TryReadInt32(out int magic))
            return false;

        return magic == MAGIC_VALUE;
    }

    public static CllFile Parse( in byte[] bytes, Action<string>? log = null )
    {
        using MemoryStream reader = new MemoryStream( bytes, false ) 
            ?? throw new InvalidOperationException("Unable to create MemoryStream from input bytes.");

        if ( !IsCllFile( bytes ) )
            throw new FormatException("Invalid magic, will not attempt to parse CLL.");

        log?.Invoke( $"Parsing CLL file" );

        // 0x08 seems to always be 1005
        // 0x0C is unknown

        reader.Seek(PACKAGE_NAME_OFFSET, SeekOrigin.Begin);
        if ( !reader.TryReadLengthPrependedAsciiString(out string packageIdent) )
            throw new FormatException("Unable to read package name.");
        log?.Invoke($"Package ident: {packageIdent}");

        if ( !reader.TryReadLengthPrependedAsciiString( out string compilerSettings ) )
            throw new FormatException("Unable to read compiler settings.");
        log?.Invoke($"Compiler settings: {compilerSettings}");

        if ( !reader.TryReadLengthPrependedAsciiString( out string projectReferences ) )
            throw new FormatException("Unable to read project references.");
        log?.Invoke($"Project references: {projectReferences}");

        if ( !reader.TryReadLengthPrependedAsciiString( out string csTextString ) )
            throw new FormatException("Unable to read C# text block.");
        csTextString = WrapTextFileArray(csTextString);
        log?.Invoke($"Read C# text block, length: {csTextString.Length}");
        
        var csTextBlock = JsonSerializer.Deserialize<TextBlock>( csTextString )
            ?? throw new FormatException($"Unable to deserialize C# text block: {csTextString}");

        log?.Invoke($"Read {csTextBlock.TextFiles.Count} C# text file(s).");

        if ( !reader.TryReadLengthPrependedAsciiString( out string razorTextString ) )
            throw new FormatException("Unable to read Razor text block.");
        razorTextString = WrapTextFileArray( razorTextString );
        log?.Invoke($"Read Razor text block, length: {razorTextString.Length}");

        var razorTextBlock = JsonSerializer.Deserialize<TextBlock>( razorTextString )
            ?? throw new FormatException($"Unable to deserialize Razor text block: {razorTextString}");

        log?.Invoke($"Read {razorTextBlock.TextFiles.Count} Razor text file(s).");

        var textBlocks = new List<TextBlock>() { csTextBlock, razorTextBlock };

        // There's another text block at the end, but it's just mappings between file paths.

        return new CllFile()
        {
            PackageIdent = packageIdent,
            CompilerSettings = compilerSettings,
            ProjectReferences = projectReferences,
            TextBlocks = textBlocks
        };

        static string WrapTextFileArray(string stringTextBlock) => $"{{ \"TextFiles\": {stringTextBlock}}}";
    }
}
