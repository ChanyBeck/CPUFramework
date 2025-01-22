using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUFramework
{
    public class bizObject
    {
        string _tablename = "";
        string _getsproc = "";
        string _updatesproc = "";
        string _deletesproc = "";
        string _primarykeyname = "";
        string _primarykeyparamname = "";
        DataTable _datatable = new();
        public bizObject(string tablename)
        {
            _tablename = tablename;
            _getsproc = tablename + "Get";
            _updatesproc = tablename + "Update";
            _deletesproc = tablename + "Delete";
            _primarykeyname = tablename + "Id";
            _primarykeyparamname = "@" + _primarykeyname;
        }
        public DataTable Load(int primarykeyvalue)
        {
            DataTable dt = new();
            SqlCommand cmd = SQLUtility.GetSQLCommand(_getsproc);
            cmd.Parameters[_primarykeyparamname].Value = primarykeyvalue;
            dt = SQLUtility.GetDataTable(cmd);
            _datatable = dt;
            return dt;
        }
        public void Delete(DataTable dt)
        {
            int id = (int)dt.Rows[0][_primarykeyname];

            SqlCommand cmd = SQLUtility.GetSQLCommand(_deletesproc);

            SQLUtility.SetParameterValue(cmd, _primarykeyparamname, id);

            SQLUtility.ExecuteSQL(cmd);
        }
        public void Save(DataTable dt)
        {
            DataRow dr = dt.Rows[0];

            SQLUtility.SaveDataRow(dr, _updatesproc);
        }
    }
}
