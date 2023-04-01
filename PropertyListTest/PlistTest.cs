using System.Collections;
using System.Diagnostics.Metrics;
using System.Text;
using NUnit.Framework;
using PropertyList;

namespace PropertyListTest;

[TestFixture]
public class PlistTest
{
    private const string Plist1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
	<key>Label</key>
	<string>com.apple.installer.osmessagetracing</string>
	<key>LaunchOnlyOnce</key>
	<true/>
	<key>ProgramArguments</key>
	<array>
		<string>/System/Library/PrivateFrameworks/OSInstaller.framework/Resources/OSMessageTracer</string>
	</array>
	<key>UserName</key>
	<string>root</string>
	<key>GroupName</key>
	<string>wheel</string>
	<key>WatchPaths</key>
	<array>
		<string>/var/db/.AppleDiagnosticsSetupDone</string>
	</array>
</dict>
</plist>
";

    [Test]
    public void ReadSimpleTest()
    {
        var reader = new PlistReader();
        var plist = reader.Read(new StringReader(Plist1));
        Assert.That(plist["Label"], Is.EqualTo("com.apple.installer.osmessagetracing"));
        var args = (IList)plist["ProgramArguments"];
        Assert.That(args.Count, Is.EqualTo(1));
        Assert.That(args[0], Is.EqualTo("/System/Library/PrivateFrameworks/OSInstaller.framework/Resources/OSMessageTracer"));
    }

    [Test]
    public void WriteSimpleTest()
    {
        var plist = new Dictionary<string, object>
        {
            {"Label", "here"},
            {"LaunchOnlyOnce", true},
        };
        using var writer = new MemoryStream();
        var plistWriter = new PlistWriter();
        plistWriter.Write(plist, writer);
        var s = Encoding.ASCII.GetString(writer.ToArray());
    }
}
