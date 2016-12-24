using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace IntegrationBase
{
    public class SQLConnector : IDisposable
    {
        // Constructors
        Connection _conn = null;
        public SQLConnector(string Server, string Database, string Username, string Password)
        {
            _conn = new Connection();
            _conn.SetConnectionString(Server, Database, Username, Password, 100);
        }
        public SQLConnector(string ConnectionString)
        {
            _conn = new Connection();
            _conn.SetConnectionString(ConnectionString);
        }
        public void Connect()
        {
            _conn.Connect();
        }
        /// <summary>
        /// Disposer, incase we're running an inbuilt connection
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_conn != null)
                    _conn.Dispose();
            }
            catch (Exception)
            {
            }
            _conn = null;
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue  
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Property to increase the default SQL Statement timeout period of 30 seconds.
        /// </summary>
        public int TimeOut { get; set; }
        /// <summary>
        /// Sets the default connection that's used if no connection is specified.
        /// </summary>
        public string DefaultConnection { get; set; }
        /// <summary>
        /// Loads a default connection for use in this connector.
        /// </summary>
        /// <param name="Conn"></param>
        public Connection LoadDefault()
        {
            if (_conn == null)
                _conn = new Connection();
            return _conn;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Executes an sql statement.. no returns
        public void ExecuteSQL(string strSQL)
        {
            Connection Conn = LoadDefault();
            SqlCommand cmdCommand = null;

            try
            {
                cmdCommand = Conn.Connect().CreateCommand();
                cmdCommand.CommandText = strSQL;
                cmdCommand.CommandTimeout = TimeOut;
                cmdCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
            }

            cmdCommand = null;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Executes an sql statement.. no returns
        public DataTable ExecuteSQLR(string strSQL)
        {
            DataTable Response;
            Connection Conn = LoadDefault();

            using (SqlCommand Command = Conn.Connect().CreateCommand())
            {
                Command.CommandText = strSQL;
                Command.CommandTimeout = TimeOut;
                SqlDataReader Reader = Command.ExecuteReader();
                Response = new DataTable();
                Response.Load(Reader);
                Reader = null;
            }
            return Response;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Same as above but converts the object to serialiable
        public object GetObjectSetFromSp(string StoredProcName, ref int ReturnValue, params object[] Args)
        {
            DataTableCollection dt = GetDataSetFromSP(StoredProcName, ref ReturnValue, Args);
            return getRt(ref dt);
        }
        public object getRt(ref DataTableCollection dt)
        {
            List<List<object>> rt = null;
            List<object> rt2 = null;
            try
            {
                rt = ConvertToObject(ref dt);
                if (((rt != null)))
                {
                    if (rt.Count == 1)
                    {
                        foreach (List<object> item in rt)
                        {
                            rt2 = item;
                            break;
                        }
                    }
                }

            }
            catch (Exception)
            {
            }
            dt = null;
            if (rt2 != null)
                return rt2;
            return rt;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Run an SQL on the database.
        public DataTableCollection GetDataSet(string strSQL)
        {
            Connection Conn = LoadDefault();
            SqlCommand cmdCommand = null;
            SqlDataAdapter daAdapter = new SqlDataAdapter();
            DataSet dsData = new DataSet();

            try
            {
                cmdCommand = Conn.Connect().CreateCommand();
                cmdCommand.CommandText = strSQL;
                cmdCommand.CommandTimeout = TimeOut;
                daAdapter.SelectCommand = cmdCommand;
                daAdapter.Fill(dsData);
            }
            catch (Exception)
            {
            }


            daAdapter = null;
            cmdCommand = null;
            return dsData.Tables;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Run a stored procedure from the database
        public void ExecuteSP(string StoredProcName, ref int ReturnValue, params object[] Args)
        {
            Connection Conn = LoadDefault();
            System.Data.SqlClient.SqlCommand dbCommand = null;
            System.Data.SqlClient.SqlParameter parameter = null;
            long lngCounter = 0;
            try
            {
                dbCommand = new System.Data.SqlClient.SqlCommand(StoredProcName, Conn.Connect());
                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.CommandTimeout = TimeOut;
                LoadParams(ref dbCommand);
                lngCounter = 0;
                if ((dbCommand.Parameters.Count > 0))
                {
                    foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                    {
                        parameter = parameter_loopVariable;
                        if ((!(parameter.Direction == ParameterDirection.ReturnValue)))
                        {
                            if ((Args.Length > lngCounter))
                            {
                                parameter.Value = Args[lngCounter] == null ? DBNull.Value : Args[lngCounter];
                            }
                            lngCounter = lngCounter + 1;
                        }
                    }
                }
                // Shove indataSet
                dbCommand.ExecuteNonQuery();
                // Retrieve the Result?
                int intRetCounter = 0;
                foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                {
                    parameter = parameter_loopVariable;
                    if ((parameter.Direction == ParameterDirection.ReturnValue))
                    {
                        ReturnValue = (int)parameter.Value;
                        intRetCounter -= 1;
                    }
                    intRetCounter += 1;
                }
                // Exit

            }
            catch (Exception)
            {
            }
            // Clean up
            dbCommand = null;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Run a stored procedure from the database
        public DataTableCollection GetDataSetFromSP(string StoredProcName, ref int ReturnValue, params object[] Args)
        {
            Connection Conn = LoadDefault();
            DataTableCollection functionReturnValue = null;
            System.Data.SqlClient.SqlDataAdapter dbData = null;
            System.Data.SqlClient.SqlCommand dbCommand = null;
            System.Data.SqlClient.SqlParameter parameter = null;
            long lngCounter = 0;
            DataSet ds = null;
            functionReturnValue = null;
            try
            {
                dbCommand = new System.Data.SqlClient.SqlCommand(StoredProcName, Conn.Connect());
                dbCommand.CommandType = CommandType.StoredProcedure;
                dbCommand.CommandTimeout = TimeOut;
                LoadParams(ref dbCommand);
                lngCounter = 0;
                if ((dbCommand.Parameters.Count > 0))
                {
                    foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                    {
                        parameter = parameter_loopVariable;
                        if ((!(parameter.Direction == ParameterDirection.ReturnValue)))
                        {
                            if ((Args.Length > lngCounter))
                            {
                                parameter.Value = Args[lngCounter] == null ? DBNull.Value : Args[lngCounter];
                            }
                            lngCounter = lngCounter + 1;
                        }
                    }
                }
                // Execute what we need
                dbData = new System.Data.SqlClient.SqlDataAdapter(dbCommand);
                ds = new DataSet();
                // Shove indataSet
                dbData.Fill(ds);
                functionReturnValue = ds.Tables;
                // Exit
            }
            catch (Exception)
            {
                functionReturnValue = null;
            }
            try
            {
                // Retrieve the Result?
                int intRetCounter = 0;
                foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                {
                    parameter = parameter_loopVariable;
                    if ((parameter.Direction == ParameterDirection.ReturnValue))
                    {
                        ReturnValue = (int)parameter.Value;
                        intRetCounter -= 1;
                    }
                    intRetCounter += 1;
                }
            }
            catch (Exception)
            {
            }

            // Clean up
            dbCommand = null;
            dbData = null;
            return functionReturnValue;
        }
        /// <summary>
        /// Gets a list of the stored procedure parameters, for returning information to clients on the
        /// schema of an SP.
        /// </summary>
        /// <param name="Procedure"></param>
        /// <returns></returns>
        public List<Dictionary<string, object>> GetSPParams(string Procedure)
        {
            SqlCommand dbCommand = null;
            List<Dictionary<string, object>> returnItem = new List<Dictionary<string, object>>();
            try
            {
                dbCommand = new SqlCommand(Procedure, _conn.Connect());
                dbCommand.CommandType = CommandType.StoredProcedure;
                LoadParams(ref dbCommand);
                SqlParameterCollection Params = dbCommand.Parameters;
                if (Params == null)
                    throw new Exception("Unable to find information on the provided stored procedure");
                // Convert the list into something that can be returned

                foreach (SqlParameter item in Params)
                {
                    if (item.ParameterName.ToLower() != "@return_value")
                    {
                        Dictionary<string, object> header = new Dictionary<string, object>();
                        header.Add("Name", item.ParameterName.ToString());
                        header.Add("Type", item.DbType.ToString());
                        header.Add("IsNullable", true);
                        header.Add("Size", item.Size);
                        returnItem.Add(header);
                    }
                }
                Params = null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (dbCommand != null)
                dbCommand.Dispose();
            dbCommand = null;
            return returnItem;
        }
        // //////////////////////////////////////////////////////////////////////////////////////////////
        // Caches and loads cached params for a stored procedure.... saves on the call backs
        Dictionary<string, List<SqlParameter>> static_LoadParams_dicParams;
        private void LoadParams(ref System.Data.SqlClient.SqlCommand dbCommand)
        {
            string strProcName = dbCommand.CommandText;
            List<SqlParameter> dbparameter = null;
            SqlParameter parameter = null;
            if (static_LoadParams_dicParams == null)
                static_LoadParams_dicParams = new Dictionary<string, List<SqlParameter>> { };
            // Find this itme in the list
            if (static_LoadParams_dicParams.ContainsKey(strProcName))
            {
                dbparameter = static_LoadParams_dicParams[strProcName];
                if (dbparameter != null)
                {
                    foreach (SqlParameter parameter_loopVariable in dbparameter)
                    {
                        parameter = parameter_loopVariable;
                        dbCommand.Parameters.Add(((ICloneable)parameter).Clone());
                    }
                }
            }
            else
            {
                System.Data.SqlClient.SqlCommandBuilder.DeriveParameters(dbCommand);
                // now record the params
                foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                {
                    parameter = parameter_loopVariable;
                    if ((dbparameter == null))
                    {
                        dbparameter = new List<SqlParameter> { };
                    }
                    dbparameter.Add((SqlParameter)((ICloneable)parameter).Clone());
                }
                static_LoadParams_dicParams.Add(strProcName, dbparameter);
            }
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Same as above but converts the object to serialiable
        public object GetObjectSet(string strSQL)
        {
            DataTableCollection dt = GetDataSet(strSQL);
            object rt = null;
            rt = getRt(ref dt);
            dt = null;
            return rt;
        }
        // ///////////////////////////////////////////////////////////////////////////////////////
        // Converts a datatable item to a serialisable object... we hope.
        public List<List<object>> ConvertToObject(ref DataTableCollection dts)
        {
            List<object> rt = null;
            List<List<object>> tabs = null;
            List<string> columns = null;

            // Exit on anomolies
            if ((dts == null))
            {
                return null;
            }

            if ((dts.Count == 0))
            {
                return null;
            }


            tabs = new List<List<object>> { };
            foreach (DataTable dt in dts)
            {
                // Creates an Object which contains the header details.
                columns = null;
                columns = new List<string> { };
                foreach (DataColumn column in dt.Columns)
                {
                    columns.Add(column.ColumnName);
                }

                rt = new List<object> { };
                foreach (DataRow row in dt.Rows)
                {
                    Dictionary<string, object> header = new Dictionary<string, object>();
                    int intColumnCount = columns.Count - 1;
                    for (int intCounter = 0; intCounter <= intColumnCount; intCounter++)
                    {
                        header.Add(columns[intCounter], row[intCounter]);
                    }
                    rt.Add(header);
                }
                tabs.Add(rt);
            }
            return tabs;
        }

    }
    public class Connection : IDisposable
    {
        public string strConnection = "";
        private SqlConnection conn = null;
        public string SetConnectionString(string ConnectionString)
        {
            strConnection = ConnectionString;
            return ConnectionString;
        }
        public string SetConnectionString(string Server, string Database, string Username, string Password, int MaxPoolSize)
        {
            if (Server == null || Database == null || Server.Trim() == "" || Database.Trim() == "")
            {
                throw new Exception("No valid server / database was specified");
            }
            string Connection = "Data Source=" + Server + ";Initial Catalog=" + Database + ";" + (Username == null || Username.Trim() == "" ? "" : " User ID=" + Username + ";") + (Password == null || Password.Trim() == "" ? "" : " Password=" + Password + ";") + " Max Pool Size=" + MaxPoolSize.ToString() + ";";
            strConnection = Connection;
            return Connection;
        }

        public SqlConnection Connect(string Server, string Database, string Username, string Password, int MaxPoolSize)
        {
            SetConnectionString(Server, Database, Username, Password, MaxPoolSize);
            return Connect();
        }
        public SqlConnection Connect()
        {
            if ((conn == null))
            {
                conn = new SqlConnection(strConnection);
                conn.Open();
            }
            return conn;
        }

        public void Dispose()
        {
            try
            {
                if (((conn != null)))
                {
                    if ((conn.State != System.Data.ConnectionState.Closed))
                    {
                        conn.Close();
                    }
                    conn.Dispose();
                }
            }
            catch (Exception)
            {
            }
            conn = null;
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue  
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
    }
}
