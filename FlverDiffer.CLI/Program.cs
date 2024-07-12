// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Security.Cryptography;
using FlverDiffer.CLI;
using SoulsFormats;

if (args.Contains("--help") || args.Contains("-h"))
{
    System.Console.WriteLine("This is a small CLI utility to compare the final result of different FLVER FBX import tools, it crawls through all properties and generates a table of differences");
    System.Console.WriteLine("Usage:\tFlverDiffer.exe [File 1] [File 2]");
    return;
}

if (args.Length < 2)
{
    System.Console.WriteLine("Missing Arguments! Use --help or -h for more details");
    return;
}

string file1Path = args[0].Trim();
string file2Path = args[1].Trim();

if (!File.Exists(file1Path))
{
    System.Console.WriteLine("File 1 does not exist!");
    return;
}

if (!File.Exists(file2Path))
{
    System.Console.WriteLine("File 2 does not exist!");
    return;
}

var file1 = File.ReadAllBytes(file1Path);
var file2 = File.ReadAllBytes(file2Path);

byte[] checksum1 = SHA256.HashData(file1);
byte[] checksum2 = SHA256.HashData(file2);

var equal = CheckSumsAreEqual(checksum1, checksum2);

if (equal)
{
    System.Console.WriteLine("Files are Identical.");
    return;
}

var files = new List<(FLVER2?, FLVER2?, string name)>();

if (FileIsFlver(file1Path))
{
    var flver1 = FLVER2.Read(file1);
    var flver2 = FLVER2.Read(file2);

    files.Add((flver1, flver2, Path.GetFileNameWithoutExtension(file1Path)));
}
else if (FileIsDCX(file1Path))
{
    var bnd1 = BND4.Read(file1);
    var bnd2 = BND4.Read(file2);

    for(int i = 0; i < bnd1.Files.Count; i++)
    {
        var nestedFile1 = bnd1.Files[i];
        var nestedFile2 = bnd2.Files[i];

        if (nestedFile1.Name == nestedFile2.Name && FileIsFlver(nestedFile1.Name))
        {
            var flver1 = FLVER2.Read(nestedFile1.Bytes);
            var flver2 = FLVER2.Read(nestedFile2.Bytes);

            files.Add((flver1, flver2, Path.GetFileNameWithoutExtension(nestedFile1.Name)));
        }
    }

}

var differences = files.SelectMany((item) => PropertyCrawler.CrawlAndCompare(item.Item1, item.Item2, item.Item3)).ToList();

System.Console.WriteLine($"Found {differences.Count} Differences.");

var path =  Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Cannot find Origin"), "template.html");

HTMLOutput htmlOutput = new(File.ReadAllText(path));
var content = htmlOutput.Output(differences);

File.WriteAllText("result.html", content);

static bool FileIsFlver(string file) => file.EndsWith(".flver") || file.EndsWith(".flv") | file.EndsWith(".flv.bak");

static bool FileIsDCX(string file)
{
    return file.EndsWith(".dcx") || file.EndsWith(".dcx.bak");
}

static bool CheckSumsAreEqual(byte[] a, byte[] b)
{
    if (a.Length != a.Length)
    {
        return false;
    }

    for (int i = 0; i < a.Length; i++)
    {
        if (a[i] != b[i])
        {
            return false;
        }
    }

    return true;
}
