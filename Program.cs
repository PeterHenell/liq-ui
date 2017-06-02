using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;



namespace LiquibaseChangesetParser
{
    class Program
    {
        static string basePath = @"C:\src\git\ods\Application\APP\db\";

        static void Main(string[] args)
        {
            var f = @"update.xml";

            var log = GetChangeLogFromFile(Path.Combine(basePath, f));
            PrintChangeLog(log);
        }

        private static void PrintChangeLog(DatabaseChangeLog log, int ident = 1)
        {
            if (log == null)
                return;

            foreach (var item in log.Include)
            {
                foreach (var changeset in item.ChangeSets)
                {
                    Console.WriteLine(changeset);
                }

                Console.WriteLine(" ".PadLeft(ident) + item.File);
                PrintChangeLog(item.ChildChangeLog, ident + 5);
            }
        }

        private static DatabaseChangeLog GetChangeLogFromFile(string fullPath)
        {
            var log = DeserializeFromFile(fullPath);
            log.FullPath = fullPath;

            foreach (var item in log.Include)
            {
                item.FullPath = Path.Combine(basePath, item.File);

                if (item.File.EndsWith("master.xml"))
                {
                    item.ChildChangeLog = GetChangeLogFromFile(item.FullPath);
                }
                if (item.File.EndsWith(".sql"))
                {
                    var changeLogDir = Path.GetDirectoryName(log.FullPath);
                    var sqlFilePath = Path.Combine(changeLogDir, item.File);
                    item.ChangeSets = GetChangeSetsFromFile(sqlFilePath);
                }
            }
            return log;
        }

        private static List<ChangeSet> GetChangeSetsFromFile(string f)
        {
            var changeSets = new List<ChangeSet>();
            foreach (var line in File.ReadAllLines(f))
            {
                ChangeSet c = null;
                if (TryParseChangeSet(line, out c))
                    changeSets.Add(c);
            }
            return changeSets;
        }

        private static bool TryParseChangeSet(string line, out ChangeSet changeset)
        {
            changeset = null;

            if (line.ToLowerInvariant().StartsWith("--changeset"))
            {
                var pattern = @"--changeSet (.*?):(.*?) (.*)";
                var matches = Regex.Matches(line, pattern, RegexOptions.IgnoreCase);

                var c = new ChangeSet
                {
                    Id = matches[0].Groups[2].Value,
                    Author = matches[0].Groups[1].Value,
                    Options = matches[0].Groups[3].Value,
                };
                
                changeset = c;
                return true;
            }
            return false;
        }

        private static DatabaseChangeLog DeserializeFromFile(string f)
        {
            using (TextReader reader = new StreamReader(f))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DatabaseChangeLog));
                var log = (DatabaseChangeLog)serializer.Deserialize(reader);
                return log;
            }
        }
    }

    [XmlRoot(ElementName = "include", Namespace = "http://www.liquibase.org/xml/ns/dbchangelog")]
    public class Include
    {
        [XmlAttribute(AttributeName = "file")]
        public string File { get; set; }

        [XmlIgnore]
        public DatabaseChangeLog ChildChangeLog { get; set; }

        [XmlIgnore]
        public String FullPath { get; set; }

        [XmlIgnore]
        public List<ChangeSet> ChangeSets { get; set; }

        public Include()
        {
            ChangeSets = new List<ChangeSet>();
        }
    }

    [XmlRoot(ElementName = "databaseChangeLog", Namespace = "http://www.liquibase.org/xml/ns/dbchangelog")]
    public class DatabaseChangeLog
    {
        [XmlIgnore]
        public String FullPath { get; set; }

        [XmlElement(ElementName = "include", Namespace = "http://www.liquibase.org/xml/ns/dbchangelog")]
        public List<Include> Include { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Xsi { get; set; }
        [XmlAttribute(AttributeName = "ext", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string Ext { get; set; }
        [XmlAttribute(AttributeName = "schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string SchemaLocation { get; set; }
    }

    public class ChangeSet
    {
        public string Id { get; set; }
        public string Author { get; set; }
        public string Options { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return string.Format("Id: [{0}], Author: [{1}], Options: [{2}], Content: [{3}]", Id, Author, Options, Content);
        }
    }
}
