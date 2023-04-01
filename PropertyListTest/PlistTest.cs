using NUnit.Framework;
using PropertyList;

namespace PropertyListTest;

[TestFixture]
public class PlistTest
{
    [Test]
    public void ReadSimpleTest()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
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

        var reader = new PlistReader();
        var plist = reader.Read(new StringReader(xml));
    }
}
