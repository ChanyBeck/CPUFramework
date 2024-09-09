using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace CPUFramework
{
    public class SQLUtility
    {
        public static string ConnectionString = "";

        public static SqlCommand GetSQLCommand(string sprocname)
        {
            SqlCommand cmd;
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                cmd = new SqlCommand(sprocname, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);
            }
            return cmd;
        }

        public static DataTable GetDataTable(SqlCommand cmd) {
            return DoExecuteSQL(cmd, true);
        }

        public static void SaveDataTable(DataTable dt, string sprocname)
        {
            var rows = dt.Select("", "", DataViewRowState.Added | DataViewRowState.ModifiedCurrent);
            foreach(DataRow r in rows)
            {
                SaveDataRow(r, sprocname, false);
            }
            dt.AcceptChanges();
        } 

        public static void SaveDataRow(DataRow row, string sprocname, bool acceptchanges = true)
        {
            SqlCommand cmd = GetSQLCommand(sprocname);
            foreach(DataColumn col in row.Table.Columns)
            {
                string paramname = $"@{col.ColumnName}";
                if (cmd.Parameters.Contains(paramname) == true)
                {
                    cmd.Parameters[paramname].Value = row[col.ColumnName];
                }
            }
            DoExecuteSQL(cmd, false);
            foreach(SqlParameter p in cmd.Parameters)
            {
                if(p.Direction == ParameterDirection.InputOutput)
                {
                    string colname = p.ParameterName.Substring(1);
                    if (row.Table.Columns.Contains(colname))
                    {
                        row[colname] = p.Value;
                    }
                }
            }
            if (acceptchanges == true)
            {
                row.Table.AcceptChanges();
            }
        }
        private static DataTable DoExecuteSQL(SqlCommand cmd, bool loadtable)
        {
            DataTable dt = new();
            using (SqlConnection conn = new SqlConnection(SQLUtility.ConnectionString))
            {
                conn.Open();
                cmd.Connection = conn;
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    CheckReturnValue(cmd);
                    if (loadtable == true)
                    {
                        dt.Load(dr);
                    }
                    foreach (SqlParameter p in cmd.Parameters)
                    {
                        if (p.Direction == ParameterDirection.InputOutput)
                        {
                            string colname = p.ParameterName.Substring(1);
                            if (dt.Columns.Contains(colname))
                            {
                                dt.Rows[0][colname] = p.Value;
                            }
                        }
                    }
                }
                catch(SqlException ex)
                {
                    string msg = ParseConstraintMessage(ex.Message);
                    throw new Exception(msg);
                }
            }
            AllowNulls(dt);
            return dt;
        }

        private static void CheckReturnValue(SqlCommand cmd)
        {
            int returnvalue = 0;
            string msg = "";
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction == ParameterDirection.ReturnValue)
                    {
                        if (p.Value != null)
                        {
                            returnvalue = (int)p.Value;
                        }
                    }
                    else if (p.ParameterName.ToLower() == "@message")
                        if (p.Value != null)
                        {
                            msg = p.Value.ToString();
                        }
                }
                if (returnvalue == 1)
                {
                    if (msg == "")
                    {
                        msg = $"{cmd.CommandText} was not completed";
                    }
                    throw new Exception(msg);
                }
            }
        }
        public static DataTable ExecuteSQL(string sqlstatement)
        {
            Debug.Print(sqlstatement); 
            return DoExecuteSQL(new SqlCommand(sqlstatement), true);
        }
        public static void ExecuteSQL(SqlCommand cmd)
        {
            DoExecuteSQL(cmd, false);
        }

        private static void AllowNulls(DataTable dt)
        {
            foreach (DataColumn dc in dt.Columns)
            {
                dc.AllowDBNull = true;
                dc.AutoIncrement = false;
            }
        }

        public static int GetValueFromFirstRowAsInt(DataTable dt, string columnname)
        {
            int value = 0;
            if(dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                if (r[columnname] != null && r[columnname] is int)
                {
                    value = (int)r[columnname];
                }
            }
            return value; 
        }
        public static string GetValueFromFirstRowAsString(DataTable dt, string columnname)
        {
            string value = "";
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                if (r[columnname] != null && r[columnname] is string)
                {
                    value = (string)r[columnname];
                }
            }
            return value;
        }
        public static bool TableHasChanges(DataTable dt)
        {
            bool b = false;
            if(dt.GetChanges() != null)
            {
                b = true;
            }
            return b; 
        }
        public static void DebugPringDataTable(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " = " + r[c.ColumnName].ToString());
                }
            }
        }
        public static void SetParameterValue(SqlCommand cmd, string paramnane, object value)
        {
            cmd.Parameters[paramnane].Value = value;
        }
        public static string ParseConstraintMessage(string msg)
        {
            //Cannot insert the value NULL into column 'UsersId', table 'HeartyHearthDB.dbo.Recipe'; column does not allow nulls.INSERT fails.

            string origmsg = msg;
            string prefix = "ck_";
            string msgend = "";
            string notnullprefix = "Cannot insert the value NULL into column '";
            if (msg.Contains(prefix) == false)
            {
                if (msg.Contains("u_"))
                {
                    prefix = "u_";
                    msgend = " must be unique";
                }
                else if (msg.Contains("f_"))
                {
                    prefix = "f_";
                }
                else if (msg.Contains(notnullprefix))
                {
                    prefix = notnullprefix;
                    msgend = " cannot be blank";
                }
            }
            if (msg.Contains(prefix))
            {
                msg = msg.Replace("\"", "'");
                int pos = msg.IndexOf(prefix) + prefix.Length;
                msg = msg.Substring(pos);
                pos = msg.IndexOf("\'");
                if (pos == -1)
                {
                    msg = origmsg;
                }
                else
                {
                    msg = msg.Substring(0, pos);
                    msg = msg.Replace("_", " ");
                    msg = msg + msgend;

                    if(prefix == "f_")
                    {
                        var words = msg.Split(" ");
                        if (words.Length > 0)
                        {
                            msg = $"Cannot delete {words[0]} because it has a related {words[1]} record";
                        }
                    }
                }
            }
            return msg;
        }
        public static int GetFirstColumnFirstRowValue(string sql)
        {
            int n = 0;

            DataTable dt = ExecuteSQL(sql);
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    int.TryParse(dt.Rows[0][0].ToString(), out n);
                }
            }
            return n;
        }
    }
}
