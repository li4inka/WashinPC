using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Engines.Operations;
using WashinPC.Engines.Privacy;

namespace WashinPC.Tests.Privacy
{
    internal static class BrowserFixtures
    {
        internal static void Sql(string path, string sql)
        {
            using (var connection = new SQLiteConnection("Data Source=" + path + ";Pooling=False"))
            { connection.Open(); using (var command = connection.CreateCommand()) { command.CommandText = sql; command.ExecuteNonQuery(); } }
        }
        internal static long Count(string path, string table)
        {
            using (var connection = new SQLiteConnection("Data Source=" + path + ";Pooling=False"))
            { connection.Open(); using (var command = connection.CreateCommand()) { command.CommandText = "SELECT COUNT(*) FROM " + table; return (long)command.ExecuteScalar(); } }
        }
        internal static void ChromiumHistory(string path) => Sql(path,
            "CREATE TABLE visits(id INTEGER PRIMARY KEY,url INTEGER,visit_time INTEGER); INSERT INTO visits VALUES(1,1,100);" +
            "CREATE TABLE urls(id INTEGER PRIMARY KEY,url TEXT); INSERT INTO urls VALUES(1,'https://example.test');" +
            "CREATE TABLE downloads(id INTEGER PRIMARY KEY,target_path TEXT); INSERT INTO downloads VALUES(1,'preserve.zip');");
        internal static void ChromiumCookies(string path) => Sql(path,
            "CREATE TABLE cookies(host_key TEXT,name TEXT,value TEXT,encrypted_value BLOB); INSERT INTO cookies VALUES('example.test','a','b',X'00');");
        internal static void FirefoxHistory(string path) => Sql(path,
            "CREATE TABLE moz_places(id INTEGER PRIMARY KEY,url TEXT,visit_count INTEGER,last_visit_date INTEGER,typed INTEGER,frecency INTEGER,hidden INTEGER);" +
            "INSERT INTO moz_places VALUES(1,'https://bookmark.test',1,100,1,100,0); INSERT INTO moz_places VALUES(2,'https://history.test',1,100,1,100,0);" +
            "CREATE TABLE moz_historyvisits(id INTEGER PRIMARY KEY,place_id INTEGER,visit_date INTEGER); INSERT INTO moz_historyvisits VALUES(1,1,100); INSERT INTO moz_historyvisits VALUES(2,2,100);" +
            "CREATE TABLE moz_bookmarks(id INTEGER PRIMARY KEY,fk INTEGER,title TEXT); INSERT INTO moz_bookmarks VALUES(1,1,'preserve bookmark');" +
            "CREATE TABLE moz_inputhistory(place_id INTEGER,input TEXT); INSERT INTO moz_inputhistory VALUES(2,'history');");
        internal static void FirefoxCookies(string path) => Sql(path,
            "CREATE TABLE moz_cookies(id INTEGER PRIMARY KEY,host TEXT,name TEXT,value TEXT); INSERT INTO moz_cookies VALUES(1,'example.test','a','b');");
    }

    [TestClass]
    public class BrowserPrivacyTests
    {
        private string _root;
        [TestInitialize] public void Init() { _root = Path.Combine(Path.GetTempPath(), "WashinPC.Tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_root); }
        [TestCleanup] public void Cleanup() { Directory.Delete(_root, true); }

        [DataTestMethod]
        [DataRow("chrome")][DataRow("edge")][DataRow("brave")][DataRow("yandex")][DataRow("opera")][DataRow("opera_gx")]
        public void ChromiumProfilesSeparateCategoriesAndPreserveOtherData(string browser)
        {
            var profile = browser.StartsWith("opera") ? _root : Path.Combine(_root, "Default"); Directory.CreateDirectory(profile);
            File.WriteAllText(Path.Combine(profile, "Preferences"), "{}");
            File.WriteAllText(Path.Combine(profile, "Bookmarks"), "preserve"); File.WriteAllText(Path.Combine(profile, "Login Data"), "preserve");
            File.WriteAllText(Path.Combine(profile, "Web Data"), "preserve"); Directory.CreateDirectory(Path.Combine(profile, "Sessions"));
            File.WriteAllText(Path.Combine(profile, "Sessions", "Session_1"), "preserve");
            var history = Path.Combine(profile, "History"); BrowserFixtures.ChromiumHistory(history);
            var cookies = Path.Combine(profile, "Cookies"); BrowserFixtures.ChromiumCookies(cookies);
            var cacheRoot = browser.StartsWith("opera") ? Path.Combine(_root, "LocalCache") : null;
            var cache = Path.Combine(cacheRoot ?? profile, "Cache"); Directory.CreateDirectory(cache); File.WriteAllText(Path.Combine(cache, "entry"), "cache");
            var plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => false, browserId: browser, cacheRoot: cacheRoot);
            Assert.AreEqual(3, plan.Files.Count); Assert.AreEqual(3, plan.Files.Select(x => x.Group).Distinct().Count());
            var result = FileOperations.Delete(plan, new[] { "privacy." + browser + ".history" }, CancellationToken.None);
            Assert.AreEqual(1, result.Applied); Assert.AreEqual(1L, result.CleanedRecords); Assert.AreEqual(0L, result.DeletedBytes); Assert.AreEqual(0, result.DeletedFiles);
            Assert.AreEqual(0L, BrowserFixtures.Count(history, "visits")); Assert.AreEqual(1L, BrowserFixtures.Count(history, "downloads"));
            Assert.AreEqual(1L, BrowserFixtures.Count(cookies, "cookies")); Assert.IsTrue(File.Exists(Path.Combine(cache, "entry")));
            result = FileOperations.Delete(plan, new[] { "privacy." + browser + ".cookies", "privacy." + browser + ".cache" }, CancellationToken.None);
            Assert.AreEqual(2, result.Applied); Assert.AreEqual(1L, result.CleanedRecords); Assert.AreEqual(5L, result.DeletedBytes); Assert.AreEqual(1, result.DeletedFiles);
            foreach (var file in new[] { "Bookmarks", "Login Data", "Web Data", @"Sessions\Session_1" }) Assert.AreEqual("preserve", File.ReadAllText(Path.Combine(profile, file)));
        }

        [TestMethod] public void FirefoxUsesIniProfilesAndPreservesBookmarksAndCredentials()
        {
            var roaming = Path.Combine(_root, "roaming"); var local = Path.Combine(_root, "local");
            var profile = Path.Combine(roaming, "CustomProfile"); Directory.CreateDirectory(profile);
            File.WriteAllText(Path.Combine(roaming, "profiles.ini"), "[Profile0]\r\nIsRelative=1\r\nPath=CustomProfile\r\n");
            File.WriteAllText(Path.Combine(profile, "prefs.js"), "// fixture"); File.WriteAllText(Path.Combine(profile, "logins.json"), "preserve");
            File.WriteAllText(Path.Combine(profile, "formhistory.sqlite"), "preserve"); File.WriteAllText(Path.Combine(profile, "sessionstore.jsonlz4"), "preserve");
            var history = Path.Combine(profile, "places.sqlite"); BrowserFixtures.FirefoxHistory(history);
            BrowserFixtures.FirefoxCookies(Path.Combine(profile, "cookies.sqlite"));
            var cache = Path.Combine(local, "CustomProfile", "cache2"); Directory.CreateDirectory(cache); File.WriteAllText(Path.Combine(cache, "entry"), "cache");
            var plan = new FileScanPlan(); PrivacyEngine.ScanFirefox(plan, roaming, local, CancellationToken.None, running: p => false);
            Assert.AreEqual(3, plan.Files.Count);
            foreach (var file in plan.Files) file.Browser = "WashinPC-fixture";
            var result = FileOperations.Delete(plan, new[] { "privacy.firefox.history" }, CancellationToken.None);
            Assert.AreEqual(1, result.Applied); Assert.AreEqual(2L, result.CleanedRecords); Assert.AreEqual(0L, result.DeletedBytes);
            Assert.AreEqual(0L, BrowserFixtures.Count(history, "moz_historyvisits")); Assert.AreEqual(1L, BrowserFixtures.Count(history, "moz_bookmarks"));
            Assert.AreEqual(1L, BrowserFixtures.Count(history, "moz_places"));
            foreach (var file in new[] { "logins.json", "formhistory.sqlite", "sessionstore.jsonlz4" }) Assert.AreEqual("preserve", File.ReadAllText(Path.Combine(profile, file)));
        }

        [TestMethod] public void UnknownSchemasBusyBrowsersAndChangedFilesArePreserved()
        {
            var profile = Path.Combine(_root, "Default"); Directory.CreateDirectory(profile); File.WriteAllText(Path.Combine(profile, "Preferences"), "{}");
            var history = Path.Combine(profile, "History"); BrowserFixtures.Sql(history, "CREATE TABLE unsupported(id INTEGER)");
            var plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => false);
            Assert.AreEqual(0, plan.Files.Count); Assert.AreEqual("privacy.unsupported_schema", plan.Notices.Single().Reason);
            File.Delete(history); BrowserFixtures.ChromiumHistory(history);
            plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => true);
            Assert.AreEqual(0, plan.Files.Count); Assert.AreEqual("op.browser_busy", plan.Notices.Single().Reason);
            plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => false);
            BrowserFixtures.Sql(history, "INSERT INTO visits VALUES(2,1,200)");
            var result = FileOperations.Delete(plan, new[] { "privacy.WashinPC-fixture.history" }, CancellationToken.None);
            Assert.AreEqual(0, result.Applied); Assert.AreEqual("op.changed", result.Items.Single().Reason); Assert.AreEqual(2L, BrowserFixtures.Count(history, "visits"));
        }

        [TestMethod] public void MultipleProfilesAndCancellationAreSupported()
        {
            foreach (var name in new[] { "Default", "Profile 1" })
            {
                var profile = Path.Combine(_root, name); Directory.CreateDirectory(profile); File.WriteAllText(Path.Combine(profile, "Preferences"), "{}");
                BrowserFixtures.ChromiumHistory(Path.Combine(profile, "History"));
            }
            var plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => false, browserId: "chrome");
            Assert.AreEqual(2, plan.Files.Count);
            var result = FileOperations.Delete(plan, new[] { "privacy.chrome.history" }, new CancellationToken(true));
            Assert.IsTrue(result.Cancelled); Assert.AreEqual(0, result.Applied); Assert.AreEqual(2, plan.Files.Count);
        }

        [TestMethod] public void DatabaseFailureAndCancellationRollBackChanges()
        {
            var profile = Path.Combine(_root, "Default"); Directory.CreateDirectory(profile); File.WriteAllText(Path.Combine(profile, "Preferences"), "{}");
            var history = Path.Combine(profile, "History"); BrowserFixtures.ChromiumHistory(history);
            var plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => false, browserId: "chrome");
            var candidate = plan.Files.Single();
            using (var cancellation = new CancellationTokenSource())
                Assert.ThrowsException<OperationCanceledException>(() => BrowserDatabase.Clean(candidate, cancellation.Token, cancellation.Cancel));
            Assert.AreEqual(1L, BrowserFixtures.Count(history, "visits")); Assert.AreEqual(1L, BrowserFixtures.Count(history, "urls"));
            BrowserFixtures.Sql(history, "CREATE TRIGGER fixture_block BEFORE DELETE ON visits BEGIN SELECT RAISE(ABORT, 'fixture'); END;");
            plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, _root, "WashinPC-fixture", CancellationToken.None, running: p => false, browserId: "chrome");
            var result = FileOperations.Delete(plan, new[] { "privacy.chrome.history" }, CancellationToken.None);
            Assert.AreEqual(0, result.Applied); Assert.AreEqual(1, result.Failed); Assert.AreEqual(1, plan.Files.Count);
            Assert.AreEqual(1L, BrowserFixtures.Count(history, "visits")); Assert.AreEqual(1L, BrowserFixtures.Count(history, "urls"));
        }
    }
}
