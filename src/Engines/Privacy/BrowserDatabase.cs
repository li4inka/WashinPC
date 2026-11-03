using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using WashinPC.Engines.Operations;

namespace WashinPC.Engines.Privacy
{
    internal static class BrowserDatabase
    {
        private static SQLiteConnection Open(string path, bool readOnly)
        {
            var connection = new SQLiteConnection(new SQLiteConnectionStringBuilder { DataSource = path, ReadOnly = readOnly, FailIfMissing = true, Pooling = false, DefaultTimeout = 1 }.ToString());
            connection.Open(); return connection;
        }
        private static long Execute(SQLiteConnection connection, string sql, bool scalar = false)
        {
            using (var command = connection.CreateCommand()) { command.CommandText = sql; return scalar ? Convert.ToInt64(command.ExecuteScalar()) : command.ExecuteNonQuery(); }
        }
        private static HashSet<string> Columns(SQLiteConnection connection, string table)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(\"" + table + "\")";
                using (var reader = command.ExecuteReader()) while (reader.Read()) result.Add(reader.GetString(1));
            }
            return result;
        }
        private static string CountTable(FileOperationKind kind)
        {
            switch (kind)
            {
                case FileOperationKind.FirefoxHistory: return "moz_historyvisits";
                case FileOperationKind.FirefoxCookies: return "moz_cookies";
                case FileOperationKind.ChromiumHistory: return "visits";
                case FileOperationKind.ChromiumCookies: return "cookies";
                default: throw new InvalidOperationException("privacy.unsupported_schema");
            }
        }
        private static void Validate(SQLiteConnection connection, FileOperationKind kind)
        {
            var table = CountTable(kind);
            var required = kind == FileOperationKind.FirefoxHistory ? new[] { "id", "place_id", "visit_date" }
                : kind == FileOperationKind.ChromiumHistory ? new[] { "id", "url", "visit_time" }
                : kind == FileOperationKind.FirefoxCookies ? new[] { "id", "host", "name", "value" }
                : new[] { "host_key", "name", "value", "encrypted_value" };
            if (required.Any(x => !Columns(connection, table).Contains(x))) throw new InvalidOperationException("privacy.unsupported_schema");
            if (kind == FileOperationKind.FirefoxHistory &&
                (new[] { "id", "visit_count", "last_visit_date", "typed", "frecency", "hidden" }.Any(x => !Columns(connection, "moz_places").Contains(x)) ||
                 !Columns(connection, "moz_bookmarks").Contains("fk"))) throw new InvalidOperationException("privacy.unsupported_schema");
            if (kind == FileOperationKind.ChromiumHistory && !Columns(connection, "urls").Contains("id")) throw new InvalidOperationException("privacy.unsupported_schema");
        }
        internal static long Count(string path, FileOperationKind kind)
        {
            using (var connection = Open(path, true)) { Validate(connection, kind); return Execute(connection, "SELECT COUNT(*) FROM " + CountTable(kind), true); }
        }
        internal static long Clean(FileCandidate candidate, CancellationToken token, Action afterDelete = null)
        {
            token.ThrowIfCancellationRequested();
            using (var lease = new DirectoryLease(Path.GetDirectoryName(candidate.Path)))
            using (var identityLease = SafeFile.Open(candidate.Path, candidate.Root, false, true))
            {
                using (var file = SafeFile.Open(candidate.Path, candidate.Root, false))
                    if (file.Fingerprint != candidate.Fingerprint) throw new InvalidOperationException("op.changed");
                if (!PrivacyEngine.CandidateAvailable(candidate)) throw new InvalidOperationException("op.browser_busy");
                using (var connection = Open(candidate.Path, false))
                {
                    // Obtain a write reservation before validating. SQLite rolls back on failure/cancellation.
                    Execute(connection, "BEGIN EXCLUSIVE");
                    try
                    {
                        using (var file = SafeFile.Open(candidate.Path, candidate.Root, false, true))
                            if (file.Fingerprint != candidate.Fingerprint) throw new InvalidOperationException("op.changed");
                        if (PrivacyEngine.BrowserRunning(candidate.Browser)) throw new InvalidOperationException("op.browser_busy");
                        Validate(connection, candidate.Kind);
                        var count = Execute(connection, "SELECT COUNT(*) FROM " + CountTable(candidate.Kind), true);
                        token.ThrowIfCancellationRequested();
                        Execute(connection, "DELETE FROM " + CountTable(candidate.Kind));
                        afterDelete?.Invoke();
                        if (candidate.Kind == FileOperationKind.FirefoxHistory)
                        {
                            Execute(connection, "UPDATE moz_places SET visit_count=0, last_visit_date=NULL, typed=0");
                            Execute(connection, "UPDATE moz_places SET frecency=0, hidden=1 WHERE id NOT IN (SELECT fk FROM moz_bookmarks WHERE fk IS NOT NULL)");
                            if (Columns(connection, "moz_inputhistory").Contains("place_id")) Execute(connection, "DELETE FROM moz_inputhistory");
                            var unreferenced = "id NOT IN (SELECT fk FROM moz_bookmarks WHERE fk IS NOT NULL)";
                            if (Columns(connection, "moz_keywords").Contains("place_id")) unreferenced += " AND id NOT IN (SELECT place_id FROM moz_keywords WHERE place_id IS NOT NULL)";
                            if (Columns(connection, "moz_places").Contains("foreign_count")) unreferenced += " AND foreign_count=0";
                            foreach (var table in new[] { "moz_annos", "moz_places_metadata" })
                                if (Columns(connection, table).Contains("place_id")) Execute(connection, "DELETE FROM " + table + " WHERE place_id IN (SELECT id FROM moz_places WHERE " + unreferenced + ")");
                            Execute(connection, "DELETE FROM moz_places WHERE " + unreferenced);
                        }
                        else if (candidate.Kind == FileOperationKind.ChromiumHistory)
                        {
                            foreach (var table in new[] { "visit_source", "keyword_search_terms", "segments", "segment_usage", "context_annotations", "content_annotations" })
                                if (Columns(connection, table).Count > 0) Execute(connection, "DELETE FROM " + table);
                            Execute(connection, "DELETE FROM urls");
                        }
                        token.ThrowIfCancellationRequested();
                        if (Execute(connection, "SELECT COUNT(*) FROM " + CountTable(candidate.Kind), true) != 0) throw new IOException("Database verification failed.");
                        Execute(connection, "COMMIT"); return count;
                    }
                    catch { try { Execute(connection, "ROLLBACK"); } catch { } throw; }
                }
            }
        }
    }
}
