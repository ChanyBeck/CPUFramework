using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace CPUFramework
{
    public class bizObject : INotifyPropertyChanged
    {
        string _tablename = "";
        string _getsproc = "";
        string _updatesproc = "";
        string _deletesproc = "";
        string _primarykeyname = "";
        string _primarykeyparamname = "";
        DataTable _datatable = new();
        List<PropertyInfo> _properties = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bizObject() 
        {
            Type t = this.GetType();
            _tablename = t.Name;
            if (_tablename.ToLower().StartsWith("biz"))
            {
                _tablename = _tablename.Substring(3);
            }
            _getsproc = _tablename + "Get";
            _updatesproc = _tablename + "Update";
            _deletesproc = _tablename + "Delete";
            _primarykeyname = _tablename + "Id";
            _primarykeyparamname = "@" + _primarykeyname;
            _properties = t.GetProperties().ToList<PropertyInfo>();
        }
        public DataTable Load(int primarykeyvalue)
        {
            DataTable dt = new();
            SqlCommand cmd = SQLUtility.GetSQLCommand(_getsproc);
            cmd.Parameters[_primarykeyparamname].Value = primarykeyvalue;
            dt = SQLUtility.GetDataTable(cmd);
            if(dt.Rows.Count > 0)
            {
                LoadProps(dt.Rows[0]);
            }
            _datatable = dt;
            return dt;
        }
        private void LoadProps(DataRow dr)
        {
            foreach(DataColumn c in dr.Table.Columns)
            {
                var prop = GetProp(c.ColumnName, false, true);
                {
                    SetProp(c.ColumnName, dr[c.ColumnName]);
                }
            }
        }
        public void Delete(int id)
        {
            SqlCommand cmd = SQLUtility.GetSQLCommand(_deletesproc);
            SQLUtility.SetParameterValue(cmd, _primarykeyparamname, id);
            SQLUtility.ExecuteSQL(cmd);
        }
        public void Delete()
        {
            PropertyInfo? prop = GetProp(_primarykeyname, true, false);
            if (prop != null)
            {
                object? id = prop.GetValue(this);
                SqlCommand cmd = SQLUtility.GetSQLCommand(_deletesproc);
                if (id != null)
                {
                    this.Delete((int)id);
                }
            }
        }
        public void Delete(DataTable dt)
        {
            int id = (int)dt.Rows[0][_primarykeyname];
            this.Delete(id);
        }
        public void Save()
        {
            SqlCommand cmd = SQLUtility.GetSQLCommand(_updatesproc);
            foreach(SqlParameter param in cmd.Parameters)
            {
                var prop = GetProp(param.ParameterName, true, false);
                if(prop!= null)
                {
                    object? val = prop.GetValue(this);
                    if(val == null)
                    {
                        val = DBNull.Value;
                    }
                    param.Value = val;
                }
                SQLUtility.ExecuteSQL(cmd);
            }
            foreach(SqlParameter param in cmd.Parameters)
            {
                if(param.Direction == ParameterDirection.InputOutput)
                {
                    SetProp(param.ParameterName, param.Value);
                }
            }
        }
        public void Save(DataTable dt)
        {
            DataRow dr = dt.Rows[0];

            SQLUtility.SaveDataRow(dr, _updatesproc);
        }
        private PropertyInfo? GetProp(string propname, bool forread, bool forwrite)
        {
            propname = propname.ToLower();
            if (propname.StartsWith("@"))
            {
                propname = propname.Substring(1);
            }
            PropertyInfo? prop = _properties.FirstOrDefault(p => 
            p.Name.ToLower() == propname 
            && (forread == false || p.CanRead == true)
            && (forread == false || p.CanWrite == true)
            );
            return prop;
        }
        private void SetProp(string propname, object? value)
        {
            var prop = GetProp(propname, false, true);
            if(prop != null)
            {
                if(value == DBNull.Value)
                {
                    value = null;
                }
                prop.SetValue(this, value);
            }
        }
        protected void InvokePropertyChanged([CallerMemberName] string propertyname = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }
    }
}
