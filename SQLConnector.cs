﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Common
{
    public class SQLConnector : IDisposable
    {
        /// <summary>
        /// Internal SQLConnection object
        /// </summary>
        private SqlConnection Conn { get; set; }
        /// <summary>
        /// Change the max pool size, default to 100
        /// </summary>
        public int MaxPoolSize { get; set; } = 100;
        /// <summary>
        /// Property to increase the default SQL Statement execution timeout period of 30 seconds.
        /// </summary>
        public int TimeOut { get; set; } = 30000;

        /// <summary>
        /// Cleans the curren connection if it exists.
        /// </summary>
        private void CleanConnection()
        {
            if (Conn != null)
            {
                if (Conn.State != System.Data.ConnectionState.Closed)
                    Conn.Close();
                Conn.Dispose();
            }
            Conn = null;
        }

        /// <summary>
        /// Connects to a databse using the given connection string
        /// </summary>
        /// <param name="ConnectionString">The Connection string for the database</param>
        private void ConnectToDB(string ConnectionString)
        {
            CleanConnection();
            try
            {
                Conn = new SqlConnection(ConnectionString);
                Conn.Open();
            }
            catch (Exception ex)
            {
                // Failed to onnect, ohh well, send the error to the calling app.
                throw ex;
            }
        }
        /// <summary>
        /// Create a connection to the database using standard SQL authenication.
        /// </summary>
        /// <param name="Server">Server name</param>
        /// <param name="Database">Database</param>
        /// <param name="Username">Username</param>
        /// <param name="Password">Password</param>
        public SQLConnector(string Server, string Database, string Username, string Password)
        {
            if (Server == null || Database == null || Server.Trim() == "" || Database.Trim() == "")
            {
                throw new Exception("No valid server / database was specified");
            }
            string Connection = "Data Source=" + Server + ";Initial Catalog=" + Database + ";" + (Username == null || Username.Trim() == "" ? "" : " User ID=" + Username + ";") + (Password == null || Password.Trim() == "" ? "" : " Password=" + Password + ";") + " Max Pool Size=" + MaxPoolSize.ToString() + ";";
            ConnectToDB(Connection);
        }
        /// <summary>
        /// Create a connection to the database using a connection string.
        /// </summary>
        /// <param name="ConnectionString">Connection String</param>
        public SQLConnector(string ConnectionString)
        {
            ConnectToDB(ConnectionString);
        }
        /// <summary>
        /// Disposer, so we can use withing a using statement.
        /// </summary>
        public void Dispose()
        {
            CleanConnection();
            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue  
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Executes an SQL statement with no results
        /// </summary>
        /// <param name="strSQL">SQL Statement</param>
        public void ExecuteNRSQL(string strSQL)
        {
            SqlCommand cmdCommand = null;

            cmdCommand = Conn.CreateCommand();
            cmdCommand.CommandText = strSQL;
            cmdCommand.CommandTimeout = TimeOut;
            cmdCommand.ExecuteNonQuery();

            cmdCommand = null;
        }
        /// <summary>
        /// Executes an SQL statement, returns a datatable object
        /// </summary>
        /// <param name="strSQL">SQL Statement</param>
        /// <returns>DataTable results from the SQL Statement</returns>
        public DataTable ExecuteSQL(string strSQL)
        {
            DataTable Response;

            using (SqlCommand Command = Conn.CreateCommand())
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
        /// <summary>
        /// Returns an easily serialised object dataset from a stored procedure.
        /// </summary>
        /// <param name="StoredProcName">Name of the stored procedure</param>
        /// <param name="ReturnValue">Result from the stored procedure</param>
        /// <param name="Args">List of arguments</param>
        /// <returns>Returns an object collection</returns>
        public object GetObjectSetFromSp(string StoredProcName, ref int ReturnValue, params object[] Args)
        {
            DataTableCollection dt = GetDataSetFromSP(StoredProcName, ref ReturnValue, Args);
            return GetRt(ref dt);
        }
        /// <summary>
        /// Returns an easily serialised object dataset from a stored procedure.
        /// </summary>
        /// <param name="StoredProcName">Name of the stored procedure</param>
        /// <param name="Args">List of arguments</param>
        /// <returns>Returns an object collection</returns>
        public object GetObjectSetFromSp(string StoredProcName, params object[] Args)
        {
            int ReturnValue = 0;
            return GetObjectSetFromSp(StoredProcName, ref ReturnValue, Args);
        }
        /// <summary>
        /// Creates a collection of objects from a datatable collection.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private object GetRt(ref DataTableCollection dt)
        {
            List<List<object>> rt = null;
            List<object> rt2 = null;
            try
            {
                rt = ConvertToObject(ref dt);
                if (((rt != null)))
                    if (rt.Count == 1)
                        foreach (List<object> item in rt)
                        {
                            rt2 = item;
                            break;
                        }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            dt = null;
            if (rt2 != null)
                return rt2;
            return rt;
        }
        /// <summary>
        /// Execute a stored procedure on the connected server, no results returned
        /// </summary>
        /// <param name="StoredProcName">Name of stored procedure</param>
        /// <param name="ReturnValue">Result from the stored procedure</param>
        /// <param name="Args">List of arguments</param>
        public void ExecuteSP(string StoredProcName, ref int ReturnValue, params object[] Args)
        {
            SqlCommand dbCommand = null;
            SqlParameter parameter = null;
            long lngCounter = 0;
            try
            {
                dbCommand = new SqlCommand(StoredProcName, Conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = TimeOut
                };
                LoadParams(ref dbCommand);
                lngCounter = 0;
                // Find cached parameters
                if ((dbCommand.Parameters.Count > 0))
                    foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                    {
                        parameter = parameter_loopVariable;
                        if (parameter.Direction != ParameterDirection.ReturnValue)
                        {
                            if (Args.Length > lngCounter)
                                parameter.Value = Args[lngCounter] ?? DBNull.Value;
                            lngCounter = lngCounter + 1;
                        }
                    }
                // Shove indataSet
                dbCommand.ExecuteNonQuery();
                // Retrieve the Result?
                foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                {
                    parameter = parameter_loopVariable;
                    if ((parameter.Direction == ParameterDirection.ReturnValue))
                    {
                        ReturnValue = (int)parameter.Value;
                        break;
                    }
                }
                // Exit

            }
            catch (Exception ex)
            {
                throw ex;
            }
            // Clean up
            dbCommand = null;
        }
        /// <summary>
        /// Execute a stored procedure on the connected server, no results returned
        /// </summary>
        /// <param name="StoredProcName">Name of stored procedure</param>
        /// <param name="Args">List of arguments</param>
        public void ExecuteSP(string StoredProcName, params object[] Args)
        {
            int ReturnValue = 0;
            ExecuteSP(StoredProcName, ref ReturnValue, Args);
        }
        /// <summary>
        /// Executes a stored procedure from the current connection, returning a DataTableCollecion
        /// </summary>
        /// <param name="StoredProcName">Name of stored procedure</param>
        /// <param name="ReturnValue">Result from the stored procedure</param>
        /// <param name="Args">List of arguments</param>
        /// <returns>DataTableCollection of the stored procedure results</returns>
        public DataTableCollection GetDataSetFromSP(string StoredProcName, ref int ReturnValue, params object[] Args)
        {
            DataTableCollection functionReturnValue = null;
            System.Data.SqlClient.SqlDataAdapter dbData = null;
            System.Data.SqlClient.SqlCommand dbCommand = null;
            System.Data.SqlClient.SqlParameter parameter = null;
            long lngCounter = 0;
            DataSet ds = null;
            functionReturnValue = null;
            try
            {
                dbCommand = new SqlCommand(StoredProcName, Conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = TimeOut
                };
                LoadParams(ref dbCommand);
                lngCounter = 0;
                // Search for previously cached parameters
                if ((dbCommand.Parameters.Count > 0))
                {
                    foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                    {
                        parameter = parameter_loopVariable;
                        if (parameter.Direction != ParameterDirection.ReturnValue)
                        {
                            if ((Args.Length > lngCounter))
                                parameter.Value = Args[lngCounter] ?? DBNull.Value;
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
                foreach (SqlParameter parameter_loopVariable in dbCommand.Parameters)
                {
                    parameter = parameter_loopVariable;
                    if ((parameter.Direction == ParameterDirection.ReturnValue))
                    {
                        ReturnValue = (int)parameter.Value;
                        break;
                    }
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
        /// Executes a stored procedure from the current connection, returning a DataTableCollecion
        /// </summary>
        /// <param name="StoredProcName">Name of stored procedure</param>
        /// <param name="Args">List of arguments</param>
        /// <returns>DataTableCollection of the stored procedure results</returns>
        public DataTableCollection GetDataSetFromSP(string StoredProcName, params object[] Args)
        {
            int ReturnValue = 0;
            return GetDataSetFromSP(StoredProcName, ref ReturnValue, Args);
        }
        /// <summary>
        /// Gets a list of the stored procedure parameters, for returning information to clients on the
        /// schema of an SP, placed into a cache for later use.
        /// </summary>
        /// <param name="Procedure">Nam of the procedure</param>
        /// <returns></returns>
        public List<Dictionary<string, object>> GetSPParams(string Procedure)
        {
            SqlCommand dbCommand = null;
            List<Dictionary<string, object>> returnItem = new List<Dictionary<string, object>>();
            try
            {
                dbCommand = new SqlCommand(Procedure, Conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                LoadParams(ref dbCommand);
                SqlParameterCollection Params = dbCommand.Parameters;
                if (Params == null)
                    throw new Exception("Unable to find information on the provided stored procedure");
                // Convert the list into something that can be returned

                foreach (SqlParameter item in Params)
                {
                    if (item.ParameterName.ToLower() != "@return_value")
                    {
                        Dictionary<string, object> header = new Dictionary<string, object>
                        {
                            { "Name", item.ParameterName.ToString() },
                            { "Type", item.DbType.ToString() },
                            { "IsNullable", true },
                            { "Size", item.Size }
                        };
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
        private static Dictionary<string, List<SqlParameter>> static_LoadParams_dicParams;
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
        // Converts a datatable item to a serialisable object... we hope.
        private List<List<object>> ConvertToObject(ref DataTableCollection dts)
        {
            List<object> rt = null;
            List<List<object>> tabs = null;
            List<string> columns = null;

            // Exit on anomolies
            if (dts == null || dts.Count == 0)
                return null;

            tabs = new List<List<object>> { };
            foreach (DataTable dt in dts)
            {
                // Creates an Object which contains the header details.
                columns = null;
                columns = new List<string> { };
                foreach (DataColumn column in dt.Columns)
                    columns.Add(column.ColumnName);

                rt = new List<object> { };
                foreach (DataRow row in dt.Rows)
                {
                    Dictionary<string, object> header = new Dictionary<string, object>();
                    int intColumnCount = columns.Count - 1;
                    for (int intCounter = 0; intCounter <= intColumnCount; intCounter++)
                        header.Add(columns[intCounter], row[intCounter]);
                    rt.Add(header);
                }
                tabs.Add(rt);
            }
            return tabs;
        }

    }
}

