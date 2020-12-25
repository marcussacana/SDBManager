using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;

namespace SDBManager
{
    public class SQL : IDisposable
    {
        string[] Tables;
        Dictionary<long, (long Scene, long Index)> IndexMap;
        SqlConnectionStringBuilder ConnectionBuilder;
        SQLiteConnection SQLite;

        int TextCount;
        int NameCount;
        int NextCount;
        public SQL(string ScenePath)
        {
            ConnectionBuilder = new SqlConnectionStringBuilder()
            {
                DataSource = ScenePath,
            };
        }

        public string[] Import()
        {
            IndexMap = new Dictionary<long, (long Scene, long Index)>();
            SQLite = new SQLiteConnection(ConnectionBuilder.ConnectionString);
            SQLite.Open();

            Tables = GetTables().Cast<string>().ToArray();

            List<string> Strings = new List<string>();

            TextCount = 0;
            NameCount = 0;
            NextCount = 0;

            if (Tables.Contains("next"))
            {
                using (SQLiteCommand Command = new SQLiteCommand("SELECT text, scene, idx FROM next WHERE text IS NOT NULL ORDER BY scene, idx", SQLite))
                using (SQLiteDataReader Reader = Command.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        long Scene = (long)Reader["scene"];
                        long Index = (long)Reader["idx"];
                        IndexMap.Add(Strings.LongCount(), (Scene, Index));

                        object text = Reader["text"];
                        Strings.Add(text is DBNull ? "SQLNULL" : (string)text);
                        NextCount++;
                    }
                }
            }

            if (Tables.Contains("name"))
            {
                using (SQLiteCommand Command = new SQLiteCommand("SELECT id, name FROM name ORDER BY id", SQLite))
                using (SQLiteDataReader Reader = Command.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        long Scene = 0;
                        long Index = (long)Reader["id"];
                        IndexMap.Add(Strings.LongCount(), (Scene, Index));

                        object name = Reader["name"];
                        Strings.Add(name is DBNull ? "SQLNULL" : (string)name);
                        NameCount++;
                    }
                }
            }

            if (Tables.Contains("text"))
            {
                long BaseIndex = Strings.LongCount();
                using (SQLiteCommand Command = new SQLiteCommand("SELECT disp, text, scene, idx FROM text ORDER BY scene, idx", SQLite))
                using (SQLiteDataReader Reader = Command.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        long Scene = (long)Reader["scene"];
                        long Index = (long)Reader["idx"];
                        IndexMap.Add(BaseIndex + TextCount++, (Scene, Index));

                        object disp = Reader["disp"];
                        object text = Reader["text"];
                        Strings.Add(disp is DBNull ? "SQLNULL" : (string)disp);
                        Strings.Add(text is DBNull ? "SQLNULL" : (string)text);
                    }
                }
            }

            return Strings.ToArray();
        }

        public void Export(string[] Content)
        {
            long Base = 0;
            if (Tables.Contains("next"))
            {
                using (SQLiteCommand Command = new SQLiteCommand("UPDATE next SET text = @text WHERE idx = @idx AND scene = @scene;", SQLite))
                {
                    SQLiteTransaction Transaction = SQLite.BeginTransaction();
                    Command.Transaction = Transaction;
                    long Changes = 0;
                    for (long i = 0; i < NextCount; i++)
                    {
                        var Info = IndexMap[i + Base];
                        string text = Content[i + Base];

                        Command.Parameters.AddWithValue("text", text == "SQLNULL" ? null : text);
                        Command.Parameters.AddWithValue("scene", Info.Scene);
                        Command.Parameters.AddWithValue("idx", Info.Index);
                        Changes += Command.ExecuteNonQuery();
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception)
                    {
                        Transaction.Rollback();
                        throw;
                    }
                }
                Base += NextCount;
            }

            if (Tables.Contains("name"))
            {
                using (SQLiteCommand Command = new SQLiteCommand("UPDATE name SET name = @name WHERE id = @id;", SQLite))
                {
                    SQLiteTransaction Transaction = SQLite.BeginTransaction();
                    Command.Transaction = Transaction;
                    long Changes = 0;
                    for (long i = 0; i < NameCount; i++)
                    {
                        var Info = IndexMap[i + Base];
                        string name = Content[i + Base];

                        Command.Parameters.AddWithValue("name", name == "SQLNULL" ? null : name);
                        Command.Parameters.AddWithValue("id", Info.Index);
                        Changes += Command.ExecuteNonQuery();
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception)
                    {
                        Transaction.Rollback();
                        throw;
                    }
                }
                Base += NameCount;
            }

            if (Tables.Contains("text"))
            {
                using (SQLiteCommand Command = new SQLiteCommand("UPDATE text SET disp = @disp, text = @text WHERE idx = @idx AND scene = @scene;", SQLite))
                {
                    SQLiteTransaction Transaction = SQLite.BeginTransaction();
                    Command.Transaction = Transaction;
                    long Changes = 0;
                    for (long i = 0; i < TextCount; i++)
                    {
                        var Info = IndexMap[Base + i];
                        long BaseIndex = Base + (i * 2);
                        string disp = Content[BaseIndex++];
                        string text = Content[BaseIndex];

                        Command.Parameters.AddWithValue("disp", disp == "SQLNULL" ? null : disp);
                        Command.Parameters.AddWithValue("text", text == "SQLNULL" ? null : text);
                        Command.Parameters.AddWithValue("scene", Info.Scene);
                        Command.Parameters.AddWithValue("idx", Info.Index);
                        Changes += Command.ExecuteNonQuery();
                    }

                    try
                    {
                        Transaction.Commit();
                    }
                    catch (Exception)
                    {
                        Transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        //Stolen from: https://stackoverflow.com/a/20257164/4860216
        ArrayList GetTables()
        {
            ArrayList list = new ArrayList();

            // executes query that select names of all tables in master table of the database
            String query = "SELECT name FROM sqlite_master " +
                    "WHERE type = 'table'" +
                    "ORDER BY 1";
            try
            {

                DataTable table = GetDataTable(query);

                // Return all table names in the ArrayList

                foreach (DataRow row in table.Rows)
                {
                    list.Add(row.ItemArray[0].ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }

        DataTable GetDataTable(string sql)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, SQLite))
                {
                    using (SQLiteDataReader rdr = cmd.ExecuteReader())
                    {
                        dt.Load(rdr);
                        return dt;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public void Dispose()
        {
            SQLite?.Dispose();
        }
    }
}