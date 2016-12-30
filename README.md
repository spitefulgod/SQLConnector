# SQLConnector
Allows connection to SQL Database without the use of Entity.

##Parameters
During the call to the stored procedure, parameters are enumerated and stored, you can pass the parameters as standard objects and and attempt to convert these to datatype of the procedure will be performed, no knowledge of the parameter datatyping is needed in these calls.

Stored procedure parameters are cached so that only a single DB run is fired to execute the command after the initial call to get the parameters

##Returns
The class can return a datatablecollection or an object (depending on the call), both types support multiple table.

The object class is constructed in such a way that it can be returned, as is, from a webservice and will be converted to XML or JSON without additional changes.

The stored procedure RETURN result is passed back as a ref int, this can be used for error checking (See below)

##Usage
Connect to a database stored as a connection string, return the table as an object for passing back from a webservice as JSON

	using(SQLConnector Connection = new SQLConnector("Some Connection String, Probably pulled from web.config")
	{
		int Return = 0;
		// Fire the stored Procedure SetCurrentWorkspace with the given parameters, a string, an object and a decimal, the return value is placed into Result
		object Result = Connection.GetObjectSetFromSp("SetCurrentWorkspace", ref Return, "Parameter 1", Param2, 3.45);
		return Result;
	}

Connect to a database via connection details, return a DataTableCollection object.

	using(SQLConnector Connection = new SQLConnector(ServerName, Database, Username, Password, 100) // Last parameter is the pool size
	{
		int Return = 0;
		DataTableCollection Result = Connection.GetDataSetFromSP("SetCurrentWorkspace", ref Return, Param1, Param2);
		return Result;
	}

Other function, execute a simple SQL, returning a Datatable collection

	DataTableCollection Result = ExecuteSQLR("SELECT * FROM MyTable");


Execute a stored procedure with no results

	ExecuteSP("SetCurrentWorkspace", ref Return, Param1);

