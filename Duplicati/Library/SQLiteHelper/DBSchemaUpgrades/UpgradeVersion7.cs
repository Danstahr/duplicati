using System.Data;
using Duplicati.Library.SQLiteHelper.DBUpdates;

namespace Duplicati.Library.SQLiteHelper
{
    class UpgradeVersion7 : IDbSchemaUpgrade
    {
        private static readonly string selectBlocksetEntryCommandText = @"
            SELECT 
                ""BlocksetEntry"".""BlocksetId"",
                ""BlocksetEntry"".""BlockId"",
                ""BlocksetEntry"".""Index"",
                ""Block"".""Size""
            FROM ""BlocksetEntry""
            LEFT JOIN ""Block""
                ON ""BlocksetEntry"".""BlockID"" = ""Block"".""ID""
            ORDER BY ""BlocksetEntry"".""BlocksetId"", ""BlocksetEntry"".""Index""";

        private static readonly string insertBlocksetEntryCommandText = @"
            INSERT INTO BlocksetEntry_Temp VALUES(?, ?, ?, ?)";

        private static readonly string removeOldBlocksetEntryTableCommandText = @"
            DROP TABLE BlocksetEntry";

        private static readonly string renameTmpBlocksetEntryTableCommandText = @"
            ALTER TABLE BlocksetEntry_Temp RENAME TO BlocksetEntry";

        public void BeforeSql(IDbConnection connection)
        {
        }

        public void AfterSql(System.Data.IDbConnection connection)
        {
            using (var txn = connection.BeginTransaction())
            using (var selectCommand = connection.CreateCommand())
            using (var insertCommand = connection.CreateCommand())
            using (var dropCommand = connection.CreateCommand())
            using (var renameCommand = connection.CreateCommand())
            {
                selectCommand.CommandText = selectBlocksetEntryCommandText;
                selectCommand.Transaction = txn;

                insertCommand.CommandText = insertBlocksetEntryCommandText;
                AddParameters(insertCommand, 4);

                dropCommand.CommandText = removeOldBlocksetEntryTableCommandText;
                dropCommand.Transaction = txn;

                renameCommand.CommandText = renameTmpBlocksetEntryTableCommandText;
                renameCommand.Transaction = txn;

                using (var reader = selectCommand.ExecuteReader())
                {
                    bool first = true;
                    long lastBlocksetId = -1;
                    long offset = 0;

                    while (reader.Read())
                    {
                        long blocksetId = reader.GetInt64(0);
                        long blockId = reader.GetInt64(1);
                        long index = reader.GetInt64(2);
                        long blockSize = reader.GetInt64(3);

                        if (first)
                        {
                            lastBlocksetId = blocksetId;
                            first = false;
                        }

                        if (lastBlocksetId != blocksetId)
                        {
                            lastBlocksetId = blocksetId;
                            offset = 0;
                        }

                        insertCommand.Transaction = txn;

                        SetParameterValue(insertCommand, 0, blocksetId);
                        SetParameterValue(insertCommand, 1, blockId);
                        SetParameterValue(insertCommand, 2, index);
                        SetParameterValue(insertCommand, 3, offset);

                        insertCommand.ExecuteNonQuery();

                        offset += blockSize;
                    }
                }

                dropCommand.ExecuteNonQuery();
                renameCommand.ExecuteNonQuery();

                txn.Commit();
            }
        }

        private static void SetParameterValue<T>(IDbCommand self, int index, T value)
        {
            ((System.Data.IDataParameter)self.Parameters[index]).Value = value;
        }

        private static void AddParameters(IDbCommand self, int count)
        {
            for (var i = 0; i < count; i++)
                self.Parameters.Add(self.CreateParameter());
        }
    }
}
