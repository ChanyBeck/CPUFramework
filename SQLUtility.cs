using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace CPUFramework
{
    public class SQLUtility
    {
        public static string ConnectionString = "";
        public static DataTable ExecuteSQL(string sqlstatement)
        {
            DataTable dt = new();
            SqlConnection conn = new();
            conn.ConnectionString = ConnectionString;
            conn.Open();

            SqlCommand cmd = new();
            cmd.Connection = conn;
            cmd.CommandText = sqlstatement;
            SqlDataReader dr = cmd.ExecuteReader();
            dt.Load(dr);

            AllowNulls(dt);

            return dt;
        }

        private static void AllowNulls(DataTable dt)
        {
            foreach (DataColumn dc in dt.Columns)
            {
                dc.AllowDBNull = true;
            }
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
